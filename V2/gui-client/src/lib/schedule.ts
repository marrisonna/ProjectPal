import type { DependencyRecord, ProjectRecord, TaskRecord } from "../api/types";

/**
 * Reproduces V1.2's Task/Project scheduling engine
 * (V1.2/Libs/DBProjectPal/DBProjectPal/Task.cs and Project.cs —
 * EarliestStartDate, StartDate, Duration, EndDate, LatestPreDepenentEndDate
 * on both) exactly, including the full recursive Task/Project dependency
 * graph (5_UrgencyCalculation/Plan.md D1.5-2/§4.7 — upgraded from an
 * earlier one-level-only approximation this module used to make, once
 * Urgency's own need for accurate dates made that approximation worth
 * fixing rather than merely inheriting). Dates were never stored on Task
 * or Project in either version
 * (Claude/Level1_Implementation/8_ValidationAndVerification/Plan.md §4.1);
 * V1.2's UI computed them for display every time, so V2's GUI does too
 * (4_GuiClient/Plan.md D1.4-14).
 */

export function addBusinessDays(start: Date, days: number): Date {
  const result = new Date(start);
  let remaining = Math.trunc(days);
  const step = remaining < 0 ? -1 : 1;
  remaining = Math.abs(remaining);
  while (remaining > 0) {
    result.setDate(result.getDate() + step);
    const dayOfWeek = result.getDay();
    if (dayOfWeek !== 0 && dayOfWeek !== 6) remaining--;
  }
  return result;
}

/** Inverse of addBusinessDays: the signed count of business days between two
 * dates, ignoring time-of-day. Used so a user can edit Requested Start Date
 * directly (V1.2's actual UX — its date picker back-computes the stored
 * offset the same way) rather than a raw day-count field (D1.4-19). */
export function businessDaysBetween(start: Date, end: Date): number {
  const a = new Date(start.getFullYear(), start.getMonth(), start.getDate());
  const b = new Date(end.getFullYear(), end.getMonth(), end.getDate());
  if (a.getTime() === b.getTime()) return 0;
  const step = b > a ? 1 : -1;
  const cursor = new Date(a);
  let count = 0;
  while (cursor.getTime() !== b.getTime()) {
    cursor.setDate(cursor.getDate() + step);
    const dayOfWeek = cursor.getDay();
    if (dayOfWeek !== 0 && dayOfWeek !== 6) count += step;
  }
  return count;
}

/** V1.2's Task.Duration getter: its ManDays mode (V2: PersonDays, D1.4-16)
 * splits Effort across assigned resources and %Allocation; Duration is
 * already a calendar-day count. */
export function computeDuration(
  task: Pick<TaskRecord, "effort_in_days" | "effort_type" | "percentage_allocation">,
  assignedResourceCount: number,
): number | null {
  if (task.effort_in_days == null) return null;
  if (task.effort_type === "Duration") return task.effort_in_days;

  // PersonDays (also the fallback when effort_type isn't set, matching
  // V1.2's GetComboValues_static default of treating unset as ManDays/
  // PersonDays-shaped).
  let resourceCount = assignedResourceCount;
  let allocation = task.percentage_allocation ?? 1;
  if (resourceCount === 0) {
    resourceCount = 1;
    allocation = 1;
  }
  if (allocation <= 0) return null;
  return task.effort_in_days / resourceCount / allocation;
}

/**
 * V1.2's `MainWindow.ShowReport`'s own per-resource effort split — a
 * genuinely different question from `computeDuration` above, despite the
 * similar inputs: that one answers "how many calendar days will this Task
 * span" (Duration ignores resource count/allocation entirely — the stored
 * value already *is* the calendar span), this one answers "how many
 * person-days of actual work does one assigned resource contribute"
 * (Duration is converted *into* person-days here by multiplying by
 * allocation, the opposite direction). Used by DashboardPage.tsx's own
 * per-Resource workload report (D1.4-104); `resourceCount` there is the
 * count of *distinct effective identities* on the Task (a real resource,
 * or the single shared "Other"/"Unassigned" bucket standing in for
 * one-or-more stale/missing ones) — always >= 1, so this never needs
 * `computeDuration`'s own zero-resource fallback.
 */
export function computeResourceEffortDays(
  task: Pick<TaskRecord, "effort_in_days" | "effort_type" | "percentage_allocation">,
  resourceCount: number,
): number {
  if (resourceCount === 0) return 0;
  let effort = (task.effort_in_days ?? 0) / resourceCount;
  if (task.effort_type === "Duration") effort *= task.percentage_allocation ?? 1;
  return effort;
}

/** V1.2's Task.EarliestStartDate: purely the relative offset, unconstrained
 * by any Dependency — this is what "Requested Start Date" displays. */
export function computeEarliestStartDate(
  task: Pick<TaskRecord, "start_relative_days_to_project">,
  project: Pick<ProjectRecord, "start_date"> | undefined,
): Date | null {
  if (task.start_relative_days_to_project == null || !project?.start_date) return null;
  return addBusinessDays(new Date(project.start_date), task.start_relative_days_to_project);
}

/** V1.2's Task.EndDate/Project.EndDate: StartDate + Duration business days
 * (Duration is already a calendar-day count computed elsewhere). */
export function computeEndDate(startDate: Date | null, duration: number | null): Date | null {
  if (!startDate || duration == null) return null;
  return addBusinessDays(startDate, Math.ceil(duration) - 1);
}

export interface ComputedSchedule {
  startDate: Date | null;
  endDate: Date | null;
}

const NO_SCHEDULE: ComputedSchedule = { startDate: null, endDate: null };

function laterOf(a: Date | null, b: Date | null): Date | null {
  if (a && b) return a > b ? a : b;
  return a ?? b;
}

type NodeKind = "task" | "project";

/**
 * Opaque, lazily-resolved handle on one Task/Project dependency graph — see
 * `getTaskSchedule`/`getProjectSchedule`. Every node's schedule is resolved
 * at most once regardless of how many other nodes reference it (D1.5-4's
 * ancestor-band memoization, generalised to this bigger graph), and a
 * visited-node guard (`resolving`) turns a malformed cyclic graph into an
 * unresolved (`null`/`null`) result for the node that would otherwise loop
 * forever, rather than actually hanging — V1.2's own equivalent has no such
 * guard (D1.5-3's reasoning, generalised the same way).
 */
export interface ScheduleGraph {
  tasksById: Map<number, TaskRecord>;
  projectsById: Map<number, ProjectRecord>;
  childTasksByProject: Map<number, TaskRecord[]>;
  childProjectsByParent: Map<number, ProjectRecord[]>;
  preDependenciesByNode: Map<string, DependencyRecord[]>;
  resourceCountByTaskId: Map<number, number>;
  cache: Map<string, ComputedSchedule>;
  resolving: Set<string>;
}

/**
 * Builds the lookup structure `getTaskSchedule`/`getProjectSchedule` resolve
 * against — cheap (a handful of `O(n)` index passes over data every caller
 * already has loaded, per 5_UrgencyCalculation/Plan.md §3.2/§4.7), with
 * nothing actually resolved until asked for. `resourceCountByTaskId` needs
 * every Task's real assigned-Resource count, not just the one(s) a caller
 * is directly displaying — V1.2's own `Task.Duration` always uses a Task's
 * real Resources, recursive lookup or not (confirmed by reading
 * `Task.cs`'s `Duration` getter directly), so a predecessor reached only
 * through recursion needs its own real count too, not a placeholder.
 */
export function buildScheduleGraph(
  tasks: TaskRecord[],
  projects: ProjectRecord[],
  dependencies: DependencyRecord[],
  resourceCountByTaskId: Map<number, number>,
): ScheduleGraph {
  const tasksById = new Map(tasks.map((t) => [t.task_id, t]));
  const projectsById = new Map(projects.map((p) => [p.project_id, p]));

  const childTasksByProject = new Map<number, TaskRecord[]>();
  for (const t of tasks) {
    const list = childTasksByProject.get(t.project_id);
    if (list) list.push(t);
    else childTasksByProject.set(t.project_id, [t]);
  }

  const childProjectsByParent = new Map<number, ProjectRecord[]>();
  for (const p of projects) {
    if (p.parent_project_id == null) continue;
    const list = childProjectsByParent.get(p.parent_project_id);
    if (list) list.push(p);
    else childProjectsByParent.set(p.parent_project_id, [p]);
  }

  const preDependenciesByNode = new Map<string, DependencyRecord[]>();
  for (const dep of dependencies) {
    const postKey =
      dep.post_task_id != null
        ? `task:${dep.post_task_id}`
        : dep.post_project_id != null
          ? `project:${dep.post_project_id}`
          : null;
    if (!postKey) continue;
    const list = preDependenciesByNode.get(postKey);
    if (list) list.push(dep);
    else preDependenciesByNode.set(postKey, [dep]);
  }

  return {
    tasksById,
    projectsById,
    childTasksByProject,
    childProjectsByParent,
    preDependenciesByNode,
    resourceCountByTaskId,
    cache: new Map(),
    resolving: new Set(),
  };
}

/** A node's own `ExpectedEndDate` for cross-referencing as a predecessor —
 * V1.2's `ITaskOrProject.ExpectedEndDate` on both Task and Project is just
 * `EndDate` itself, no special-casing by status/priority (that exclusion
 * only applies inside `resolveProject`'s own aggregation over its
 * children, not to how a node reports its end date to something depending
 * on it). */
function resolveNode(graph: ScheduleGraph, kind: NodeKind, id: number): ComputedSchedule {
  const key = `${kind}:${id}`;
  const cached = graph.cache.get(key);
  if (cached) return cached;
  if (graph.resolving.has(key)) return NO_SCHEDULE;

  graph.resolving.add(key);
  const result = kind === "task" ? resolveTask(graph, id) : resolveProject(graph, id);
  graph.resolving.delete(key);

  graph.cache.set(key, result);
  return result;
}

/** V1.2's Task/Project.LatestPreDepenentEndDate: the *max* of every direct
 * Dependency predecessor's own end date and the parent Project's own
 * LatestPreDepenentEndDate — not a fallback used only when this node has no
 * direct predecessor of its own, but always compared against it, taking
 * whichever is later. This is what makes a node with no dependency of its
 * own still inherit a constraint from its ancestry.
 *
 * `visitedParents` guards this function's own walk *up* the
 * `parent_project_id` chain specifically — a separate recursion axis from
 * `resolveNode`'s own `resolving` guard (which only protects against a
 * cyclic *Dependency* graph, not a cyclic containment chain). Caught by
 * `schedule.dates.test.ts`'s own cyclic-parent-chain case: without this,
 * a cyclic `parent_project_id` chain recurses here forever even though
 * `resolveNode` was never re-entered for the same node. */
function resolveLatestPreDependentEnd(
  graph: ScheduleGraph,
  kind: NodeKind,
  id: number,
  parentProjectId: number | null,
  visitedParents: Set<number> = new Set(),
): Date | null {
  let latest: Date | null = null;
  for (const dep of graph.preDependenciesByNode.get(`${kind}:${id}`) ?? []) {
    const preEnd =
      dep.pre_task_id != null
        ? resolveNode(graph, "task", dep.pre_task_id).endDate
        : dep.pre_project_id != null
          ? resolveNode(graph, "project", dep.pre_project_id).endDate
          : null;
    latest = laterOf(latest, preEnd);
  }
  if (parentProjectId != null && !visitedParents.has(parentProjectId)) {
    visitedParents.add(parentProjectId);
    const parent = graph.projectsById.get(parentProjectId);
    const parentLatest = resolveLatestPreDependentEnd(
      graph,
      "project",
      parentProjectId,
      parent?.parent_project_id ?? null,
      visitedParents,
    );
    latest = laterOf(latest, parentLatest);
  }
  return latest;
}

function resolveTask(graph: ScheduleGraph, taskId: number): ComputedSchedule {
  const task = graph.tasksById.get(taskId);
  if (!task) return NO_SCHEDULE;

  const project = graph.projectsById.get(task.project_id);
  const earliestStart = computeEarliestStartDate(task, project);
  const latestPreDependentEnd = resolveLatestPreDependentEnd(graph, "task", taskId, task.project_id);
  const constrainedStart = latestPreDependentEnd ? addBusinessDays(latestPreDependentEnd, 1) : null;
  const startDate = laterOf(constrainedStart, earliestStart);

  const resourceCount = graph.resourceCountByTaskId.get(taskId) ?? 0;
  const duration = computeDuration(task, resourceCount);
  const endDate = computeEndDate(startDate, duration);

  return { startDate, endDate };
}

function resolveProject(graph: ScheduleGraph, projectId: number): ComputedSchedule {
  const project = graph.projectsById.get(projectId);
  if (!project) return NO_SCHEDULE;

  // V1.2's Project.EndDate: the max EndDate over every child Task/Project —
  // but Cancelled/Closed children are excluded from the aggregation
  // entirely (Task.cs's own status; a sub-Project's own Priority, since a
  // Project has no separate status field — Project.cs's EndDate getter,
  // read directly, checks exactly these two conditions and no others).
  let endDate: Date | null = null;
  for (const childTask of graph.childTasksByProject.get(projectId) ?? []) {
    if (childTask.status === "Cancelled" || childTask.status === "Closed") continue;
    endDate = laterOf(endDate, resolveNode(graph, "task", childTask.task_id).endDate);
  }
  for (const childProject of graph.childProjectsByParent.get(projectId) ?? []) {
    if (childProject.priority === "Cancelled" || childProject.priority === "Closed") continue;
    endDate = laterOf(endDate, resolveNode(graph, "project", childProject.project_id).endDate);
  }

  // V1.2's Project.StartDate: the later of its own stored start_date and
  // (latest predecessor's end date + 1 business day) — always compared,
  // not a fallback only used when there's no own start_date (Project's
  // own base start is never null, unlike a Task's).
  const ownStart = project.start_date ? new Date(project.start_date) : null;
  const latestPreDependentEnd = resolveLatestPreDependentEnd(
    graph,
    "project",
    projectId,
    project.parent_project_id,
  );
  const constrainedStart = latestPreDependentEnd ? addBusinessDays(latestPreDependentEnd, 1) : null;
  const startDate = laterOf(constrainedStart, ownStart);

  return { startDate, endDate };
}

export function getTaskSchedule(graph: ScheduleGraph, taskId: number): ComputedSchedule {
  return resolveNode(graph, "task", taskId);
}

export function getProjectSchedule(graph: ScheduleGraph, projectId: number): ComputedSchedule {
  return resolveNode(graph, "project", projectId);
}

/**
 * V1.2's `Project.IsActive` (`V1.2/Libs/DBProjectPal/DBProjectPal/
 * Project.cs`) — a computed property, never a stored column (unlike
 * `Person.IsActive`, a real stored flag): a Project is active if its own
 * Priority isn't Cancelled/Closed, *and* it has at least one direct Task
 * that isn't Closed/Cancelled, *or* at least one sub-Project that is
 * itself active (recursively — an active grandchild makes every ancestor
 * above it active too). `4_GuiClient/Plan.md` `D1.4-41`'s own investigation
 * found this ported faithfully, replacing the Priority-only stand-in
 * `lib/ganttLayout.ts`'s `isVisibleProjectPriority` currently uses there
 * (that switch-over itself is deferred to Stage 6, per the same decision —
 * this function is written now only for `ProjectDetailPage.tsx`'s own use).
 *
 * Two deliberate deltas from the V1.2 source: no `Status.HasValue` check
 * (V2's `TaskRecord.status` is never null, unlike V1.2's nullable Status —
 * every Task is considered, matching `isVisibleTaskStatus`'s own identical
 * exclusion list), and a `visited` cycle guard on the sub-Project
 * recursion, which V1.2's own version doesn't have (the same guard already
 * applied elsewhere in this codebase when porting a parent-chain walk,
 * e.g. `ancestorPriorityChain` below — protects against a malformed
 * `parent_project_id` chain looping forever).
 */
export function isProjectActive(
  graph: ScheduleGraph,
  projectId: number,
  visited: Set<number> = new Set(),
): boolean {
  if (visited.has(projectId)) return false;
  visited.add(projectId);

  const project = graph.projectsById.get(projectId);
  if (!project) return false;
  if (project.priority === "Cancelled" || project.priority === "Closed") return false;

  for (const task of graph.childTasksByProject.get(projectId) ?? []) {
    if (task.status !== "Closed" && task.status !== "Cancelled") return true;
  }

  for (const childProject of graph.childProjectsByParent.get(projectId) ?? []) {
    if (isProjectActive(graph, childProject.project_id, visited)) return true;
  }

  return false;
}

/**
 * Urgency (`Requirements/KeyConcepts.md` §12.1, `V1.2/Apps/ProjectPal/
 * ProjectPal/Tasks/GUITask.cs`'s `Urgency` getter) — ported exactly,
 * constants included, per 5_UrgencyCalculation/Plan.md's D1.5-1 (verified
 * line-by-line against that source, §12.1's own updated preamble records
 * the verification and the two confirmed-dead pieces of that source
 * deliberately not reflected here).
 */

// Requirements/KeyConcepts.md §12.1's Pr(x): High=5, MedHigh=4, Med=3,
// MedLow=2, Low=1, Closed=0, Cancelled=-1 — the same 7-value vocabulary for
// both a Task's own priority and every ancestor Project's, matching
// V1.2's PriorityValue enum member names exactly (8_ValidationAndVerification/
// Plan.md D1.8-4 already confirmed V2's own Priority strings align to these
// names, not V1.2's differently-worded GUI dropdown labels).
const PRIORITY_WEIGHT: Record<string, number> = {
  High: 5,
  MedHigh: 4,
  Med: 3,
  MedLow: 2,
  Low: 1,
  Closed: 0,
  Cancelled: -1,
};

// A missing/unrecognised Priority is always treated as Med (3) — both for
// a Task's own Priority and every ancestor Project's, matching V1.2's
// `Priority.HasValue` check (Task) and `?? PriorityValue._3_Med` (Project).
// Exported for features/projects/Projects.tsx's own sibling-Project
// ordering (highest Priority first, then alphabetically) — the same
// Priority-to-number mapping Urgency already uses, not a second one.
export function priorityWeight(priority: string | null): number {
  if (priority == null) return 3;
  return PRIORITY_WEIGHT[priority] ?? 3;
}

// A UTC-normalised day number, so calendar-day differences (unlike
// addBusinessDays/businessDaysBetween above, which deliberately walk
// day-by-day) can be plain subtraction without a local-timezone DST
// transition silently shifting the count by a day (§12.1's own two
// day-count computations are plain `.Days` on a `DateTime` TimeSpan in
// V1.2 — calendar days, not business days, per D1.5's own §4.3 callout).
// Exported for lib/ganttLayout.ts's own day-to-pixel positioning (D1.4-24)
// — same DST-safe UTC day-numbering, reused rather than duplicated.
export function calendarDayNumber(date: Date): number {
  return Math.round(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()) / 86_400_000);
}

export function calendarDaysBetween(start: Date, end: Date): number {
  return calendarDayNumber(end) - calendarDayNumber(start);
}

// Exported for features/plan/PlanPage.tsx's date-under-cursor indicator.
export function addCalendarDays(date: Date, days: number): Date {
  const result = new Date(date);
  result.setDate(result.getDate() + days);
  return result;
}

/** §12.1 Step 1: the effective-priority band `[min, max]`, root-first over
 * a Task's own Project and every ancestor above it (V1.2's own
 * `parentProjectPriorityList`, built bottom-up then reversed) — a cycle
 * guard stops a malformed `parent_project_id` chain from looping forever
 * (D1.5-3's reasoning, applied here too), which V1.2's own equivalent walk
 * doesn't have. */
function ancestorPriorityChain(
  projectId: number | undefined,
  projectsById: Map<number, ProjectRecord>,
): number[] {
  const chain: number[] = [];
  const visited = new Set<number>();
  let current = projectId != null ? projectsById.get(projectId) : undefined;
  while (current && !visited.has(current.project_id)) {
    visited.add(current.project_id);
    chain.push(priorityWeight(current.priority));
    current = current.parent_project_id != null ? projectsById.get(current.parent_project_id) : undefined;
  }
  chain.reverse();
  return chain;
}

function priorityBand(ancestorPriorities: number[]): { min: number; max: number } {
  const n = ancestorPriorities.length;
  let max = n === 0 ? 0.5 : ancestorPriorities[0] + 0.5;
  let min = Math.max(0, max - 1);

  for (let i = 1; i < n; i++) {
    let thisPriorityMax = ancestorPriorities[i] + 0.5;
    const thisPriorityMin = Math.max(0, thisPriorityMax - 1) / 6;
    thisPriorityMax /= 6;

    const thisMean = thisPriorityMax + thisPriorityMin;
    const thisExaggerate = Math.pow(thisMean, 1.5);

    const newMax = min + thisExaggerate * thisPriorityMax * (max - min);
    const newMin = min + thisExaggerate * thisPriorityMin * (max - min);
    max = newMax;
    min = newMin;
  }

  return { min, max };
}

/**
 * Requirements/KeyConcepts.md §12.1's `U`, rounded to one decimal place —
 * `startDate`/`endDate` are passed in (from `getTaskSchedule`, §4.7) rather
 * than recomputed here, since every caller already has them from the same
 * call it uses for direct display; `today` defaults to the real current
 * date but is a parameter so it can be pinned in tests (worked examples,
 * §12.1's own three).
 */
export function computeUrgency(
  task: Pick<TaskRecord, "status" | "status_date" | "priority" | "project_id">,
  projectsById: Map<number, ProjectRecord>,
  startDate: Date | null,
  endDate: Date | null,
  today: Date = new Date(),
): number {
  const todayDateOnly = new Date(today.getFullYear(), today.getMonth(), today.getDate());
  let result: number;

  if (task.status === "Cancelled" || task.status === "Closed") {
    if (task.status_date) {
      const d = calendarDaysBetween(new Date(task.status_date), todayDateOnly);
      result = d > 10 ? Math.trunc(100 / d) / 10 : 1;
    } else {
      result = 1;
    }
  } else {
    const { min, max } = priorityBand(ancestorPriorityChain(task.project_id, projectsById));

    const taskFactor = priorityWeight(task.priority) / 3;
    const finalTaskPriority = (taskFactor * (min + max)) / 2;
    const taskPriorityMultiplier = (finalTaskPriority - 3) / 3 + 1;

    let taskDate: Date | null = null;
    if (startDate) {
      if (task.status === "NotStarted" || !endDate) {
        taskDate = startDate;
      } else if (task.status === "InProgress") {
        taskDate = addCalendarDays(startDate, Math.trunc(calendarDaysBetween(startDate, endDate) / 2));
      } else {
        taskDate = endDate;
      }
    }

    if (!taskDate) {
      result = 100 * taskPriorityMultiplier;
    } else {
      const daysUntilDue = calendarDaysBetween(todayDateOnly, taskDate);
      result =
        daysUntilDue <= 0
          ? 100 * taskPriorityMultiplier * (1 - daysUntilDue / 60)
          : 100 * taskPriorityMultiplier * Math.pow(0.5, daysUntilDue / 60);
    }
  }

  return Math.trunc(result * 10) / 10;
}

// Requirements/KeyConcepts.md §12.2 ("Current Urgency-to-Colour Algorithm"),
// V1.2's `Utils.Colours.UrgencyColour` — a 101-entry white-to-light-red
// blend, indexed by how far Urgency sits into the 100-200 range (clamped).
const URGENCY_COLOUR_WHITE = { r: 255, g: 255, b: 255 };
const URGENCY_COLOUR_MAX = { r: 255, g: 128, b: 128 };

function clampByte(value: number): number {
  return Math.max(0, Math.min(255, Math.trunc(value)));
}

export function computeUrgencyColour(urgency: number): string {
  const u = Math.trunc(urgency);
  const mixMax = Math.max(0, Math.min(100, u - 100)) / 100;
  const mixMin = 1 - mixMax;
  const r = clampByte(URGENCY_COLOUR_WHITE.r * mixMin + URGENCY_COLOUR_MAX.r * mixMax);
  const g = clampByte(URGENCY_COLOUR_WHITE.g * mixMin + URGENCY_COLOUR_MAX.g * mixMax);
  const b = clampByte(URGENCY_COLOUR_WHITE.b * mixMin + URGENCY_COLOUR_MAX.b * mixMax);
  return `rgb(${r}, ${g}, ${b})`;
}

// V1.2's own Read-Only grey (`Colours.ReadOnlyColour`) — not part of the
// urgency-to-colour blend itself, but what a Task's row shows *instead of*
// it under the gating rule below.
const READ_ONLY_GREY = "rgb(190, 190, 190)";

/**
 * V1.2's actual Task row colour (`GUITask.Colour`, `Tasks/GUITask.cs`) —
 * not the same thing as `computeUrgencyColour` alone. A Task whose own
 * Priority (not Status — the two are independent, §11) is unset,
 * Cancelled, or Closed always renders as flat grey, regardless of its
 * computed Urgency; only otherwise does the urgency colour actually show.
 * `Requirements/KeyConcepts.md` §12.2's own "Applying it to a Task's row
 * colour" documents this exactly — found missing from that section on a
 * first pass over it, added once `GUITask.Colour` was read directly.
 */
export function computeTaskRowColour(priority: string | null, urgency: number): string {
  if (priority == null || priority === "Cancelled" || priority === "Closed") return READ_ONLY_GREY;
  return computeUrgencyColour(urgency);
}

// MUI DataGrid's own default row-hover style is a flat, solid
// backgroundColor (measured live: rgb(245, 245, 245)) painted straight over
// whatever a row's own background already was — for an urgency-tinted row,
// that erases the tint entirely for as long as the cursor sits over the
// row, rather than just tinting it. Blending the two (50/50) instead means
// the hover state still reads as "the same row, now highlighted," not "a
// different, flat-grey row" — the hovered colour is derived from the row's
// own urgency colour, not a fixed value applied regardless of it.
const HOVER_GREY = { r: 245, g: 245, b: 245 };
const RGB_PATTERN = /rgb\((\d+),\s*(\d+),\s*(\d+)\)/;

function blendWithHoverGrey(rgbColor: string): string {
  const match = rgbColor.match(RGB_PATTERN);
  if (!match) return rgbColor;
  const r = Math.round((HOVER_GREY.r + Number(match[1])) / 2);
  const g = Math.round((HOVER_GREY.g + Number(match[2])) / 2);
  const b = Math.round((HOVER_GREY.b + Number(match[3])) / 2);
  return `rgb(${r}, ${g}, ${b})`;
}

// Shared by `TaskGrid.tsx` and Search's own results grid (`SearchPlan.md`
// D1.4-76) — DataGrid has no per-row inline-style hook, only discrete
// classes via `getRowClassName`, so a Task row's urgency tint (otherwise a
// continuous, per-row colour via `computeTaskRowColour` above) has to be
// expressed as a lookup into a precomputed palette of one CSS class per
// possible urgency bucket instead. `urgencyRowClassName` picks the class
// for one row; `urgencyRowPaletteSx` builds the `sx` map every one of
// those classes resolves against — a consumer merges it into its own
// DataGrid `sx` (e.g. `DenseDataGrid`'s own `sx` prop) alongside any
// classes of its own (TaskGrid's `task-grid-readonly-cell`/
// `task-grid-delete-cell`, kept local to TaskGrid.tsx since neither is an
// Urgency concept).
export function urgencyRowClassName(priority: string | null, urgency: number): string {
  if (priority == null || priority === "Cancelled" || priority === "Closed") {
    return "urgency-row-grey";
  }
  const m = Math.max(0, Math.min(100, Math.trunc(urgency) - 100));
  return `urgency-row-${m}`;
}

export function urgencyRowPaletteSx(): Record<string, { bgcolor: string }> {
  const sx: Record<string, { bgcolor: string }> = {};
  sx["& .urgency-row-grey"] = { bgcolor: READ_ONLY_GREY };
  // A higher-specificity selector than DataGrid's own plain
  // ".MuiDataGrid-row:hover" (this one carries an extra class, the
  // urgency class itself) — no !important needed to win.
  sx["& .urgency-row-grey:hover"] = { bgcolor: blendWithHoverGrey(READ_ONLY_GREY) };
  for (let m = 0; m <= 100; m++) {
    const base = computeUrgencyColour(100 + m);
    sx[`& .urgency-row-${m}`] = { bgcolor: base };
    sx[`& .urgency-row-${m}:hover`] = { bgcolor: blendWithHoverGrey(base) };
  }
  return sx;
}

const MONTHS = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

/**
 * Shared display format for every scheduling date shown anywhere in the
 * app (Task Detail's Requested Start/Planned Start/End Date, the All Tasks
 * grid's End Date/Planned Start) — one function so they can't drift apart
 * the way Requested Start and Planned Start once briefly did (mockup 1a
 * used a second, longer format for one of them with no stated reason).
 * Deliberately not the browser's locale-controlled native date-input
 * format.
 */
export function formatDdMmmYy(date: Date | null): string {
  if (!date) return "—";
  const yy = String(date.getFullYear()).slice(-2);
  return `${String(date.getDate()).padStart(2, "0")}-${MONTHS[date.getMonth()]}-${yy}`;
}

/**
 * The Gantt view's month-start footer marker (D1.4-26): "Mmm" normally,
 * "Mmm-YY" for January so a marker crossing a year boundary still shows
 * which year it's entering without needing every marker to carry one.
 */
export function formatMonthMarker(date: Date): string {
  const month = MONTHS[date.getMonth()];
  if (date.getMonth() !== 0) return month;
  return `${month}-${String(date.getFullYear()).slice(-2)}`;
}

/**
 * Inverse of formatDdMmmYy, for sorting a date column's already-formatted
 * display strings (the All Tasks grid's column filter popup, D-Win-14,
 * sorts by the same strings it filters/displays, not a separately-tracked
 * raw Date) — returns a millisecond timestamp, or null for "—"/unparsable.
 */
export function parseDdMmmYy(formatted: string): number | null {
  const match = /^(\d{2})-([A-Za-z]{3})-(\d{2})$/.exec(formatted);
  if (!match) return null;
  const [, dd, mon, yy] = match;
  const month = MONTHS.indexOf(mon);
  if (month === -1) return null;
  return new Date(2000 + Number(yy), month, Number(dd)).getTime();
}
