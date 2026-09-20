import type { DependencyRecord } from "../api/types";
import {
  addCalendarDays,
  calendarDayNumber,
  calendarDaysBetween,
  computeTaskRowColour,
  computeUrgency,
  formatMonthMarker,
  getProjectSchedule,
  getTaskSchedule,
  type ScheduleGraph,
} from "./schedule";

/**
 * Positioning/hierarchy model for the Gantt/Plan View (D1.4-24) — ported
 * from V1.2's own from-scratch implementation (V1.2/Libs/PlanDisplay/) in
 * preference to a third-party library. `PIXELS_PER_DAY`/`BAR_HEIGHT` mirror
 * PlanDisplay/Constants.cs's `PixelsPerDay`/`TaskHeight` exactly; the tree
 * walk mirrors Apps/ProjectPal/ProjectPal/GanttDisplayHelper.cs's
 * `AddPlanDetail` (the Project-hierarchy overload, not the parallel
 * Component-hierarchy one ComponentWindow uses — see 4_GuiClient/Plan.md
 * D1.4-24 and Requirements/UserInterfaceWindows.md §3.7's correction).
 *
 * Deliberately no DOM/React here, same testable-utility split as
 * lib/schedule.ts — a renderer (features/plan/PlanPage.tsx) turns this
 * plain layout data into SVG.
 */

export const PIXELS_PER_DAY = 10;
export const BAR_HEIGHT = 10;
export const ROW_HEIGHT = 20;

// V1.2 colours Gantt bars by a People/Status/Priority toggle
// (PlanControl.xaml.cs's radioButtonPeople/Status/Priority_Checked) — not
// ported (D1.4-24 defers it). Project bars get one flat colour here; Task
// bars reuse the same Urgency-based colouring already built for All Tasks/
// Task Detail (computeTaskRowColour, Phase 5).
const PROJECT_BAR_COLOUR = "#607d8b";

export interface GanttBar {
  kind: "task" | "project";
  id: number;
  label: string;
  depth: number;
  row: number;
  startDate: Date | null;
  endDate: Date | null;
  x: number;
  width: number;
  y: number;
  color: string;
  // Project bars only: the bottom (y + BAR_HEIGHT) of the last row in
  // this Project's own subtree (its last descendant Task, or its last
  // descendant sub-Project's own last descendant, whichever sorts last)
  // — how far down the two faint "extent" guide lines at this bar's own
  // left/right edges should reach. `null` for a Task bar, or a Project
  // with no visible children at all (nothing to draw).
  subtreeBottomY: number | null;
  // The hover label shown while the mouse is over this bar — "P: " or
  // "T: ", then this bar's own name (a Project's own name, or a Task's
  // own description), then " : [" + a hierarchy-chain text + "]" (a
  // Project's own chain excludes itself — buildAncestorChain — since its
  // name is already shown before the " : "; omitted entirely for a
  // top-level Project, which has no ancestors at all; a Task's chain
  // includes its own Project — buildProjectChain — since a Task's name
  // is its description, not a Project name, so there's nothing to avoid
  // repeating there). Adapted from, but not identical to, V1.2's own
  // Gantt hover caption (GanttDisplayHelper.cs), which puts the chain
  // first and has no "P:"/"T:" prefix.
  hoverLabel: string;
  // The sibling group this bar belongs to (D1.4-27, manual row reorder)
  // — "root" for a top-level Project, or `project:<id>` for a direct
  // child (Task or sub-Project) of Project `<id>`. Drag-reordering is
  // scoped to bars sharing the same parentKey: it's how the UI knows
  // which other bars are this bar's own draggable siblings, and where the
  // parent's own bounds are (never letting a drag imply a hierarchy/
  // parent change, which stays entirely off-limits — only relative order
  // among existing siblings is ever user-editable).
  parentKey: string;
}

// A parent's children (Tasks and sub-Projects together, in this parent's
// current display order) keyed by `parentKey` — the caller's own record
// of any manual reordering (D1.4-27). Purely a display-order override: it
// never adds, removes, or reparents anything, and any child key not
// present in a given list keeps its default relative position, appended
// after every explicitly-ordered child (so a newly added Task/Project a
// user has never reordered still shows up sensibly rather than
// disappearing from view).
export type GanttCustomOrder = Record<string, string[]>;

function applyCustomOrder<T>(items: T[], keyOf: (item: T) => string, order: string[] | undefined): T[] {
  if (!order || order.length === 0) return items;
  const rank = new Map(order.map((key, index) => [key, index]));
  return items
    .map((item, index) => ({ item, rank: rank.get(keyOf(item)) ?? order.length + index }))
    .sort((a, b) => a.rank - b.rank)
    .map((entry) => entry.item);
}

/**
 * Number of weekdays (Mon–Fri) in the half-open range [from, to) — the
 * "Weekends" checkbox's own day-to-pixel mapping when unchecked (D1.4-35):
 * each weekday still contributes one PIXELS_PER_DAY-wide unit, but a
 * Saturday/Sunday contributes none at all, so between any two Mondays
 * there are only 5 units of width, not 7.
 *
 * This one formula also produces the exact start/end clamping the
 * feature asks for, with no separate clamping step: since a Saturday and
 * the Sunday right after it contain no weekdays at all, and neither does
 * the gap between them and the *following* Monday, `to` being any of
 * Sat/Sun/next-Monday all yield the *same* result — i.e. a bar
 * "starting" on a Saturday or Sunday already lands at exactly the same
 * offset a Monday start would (drawn as if it starts at the beginning of
 * Monday), and a bar "ending" on a Saturday or Sunday already lands at
 * exactly the offset the *preceding* Friday's end would (drawn as if it
 * ends at the end of Friday) — both are just this same function applied
 * to `from`/`to` as given, unmodified.
 */
export function compressedDayOffset(from: Date, to: Date): number {
  // Compares whole calendar days (via calendarDayNumber, the same
  // normalisation calendarDaysBetween already relies on), not raw
  // timestamps — a real, once-live bug: a computed schedule date can
  // carry a non-midnight time-of-day component (seen in production: a
  // date-only string like "2023-09-12" parses as UTC midnight, which is
  // 01:00 local during British Summer Time; repeated addBusinessDays
  // calls preserve whatever time-of-day a date starts with, so that
  // stray hour rides along all the way to the final result). A raw
  // `cursor.getTime() < to.getTime()` comparison would then count `to`'s
  // own day as one extra iteration whenever `to` sits even a minute past
  // local midnight, drawing every bar exactly one compressed day late —
  // reproduced against real seed data (D1.4-38) and invisible in
  // hand-built tests, which only ever used exact-midnight dates.
  const toDayNumber = calendarDayNumber(to);
  let count = 0;
  let cursor = from;
  while (calendarDayNumber(cursor) < toDayNumber) {
    const dow = cursor.getDay();
    if (dow !== 0 && dow !== 6) count++;
    cursor = addCalendarDays(cursor, 1);
  }
  return count;
}

/**
 * Inverse of `compressedDayOffset` — the date reached after `units`
 * compressed (weekday-only) day-units have elapsed from `from`. Needed to
 * convert a pixel position back into a real date when weekends are
 * excluded (a cursor's own hovered date; keeping a chosen date anchored
 * at a fixed screen position — e.g. 25% from the left — across the
 * "Weekends" checkbox being toggled, D1.4-36).
 *
 * `compressedDayOffset`'s own forward mapping is many-to-one at a weekend
 * boundary (a Saturday, the Sunday after it, and the following Monday
 * all produce the same offset, by design — see that function's own
 * comment), so this inverse necessarily picks one canonical answer for
 * such an offset: the next real (weekday) date reached, i.e. the
 * following Monday. That's the right choice here — the compressed
 * timeline has no pixel space of its own for a weekend at all, so the
 * offset numerically "landing on" one doesn't correspond to a visible
 * position; the Monday immediately after is the nearest date that
 * actually occupies real, visible width there.
 */
export function dateAtCompressedOffset(from: Date, units: number): Date {
  let count = 0;
  let cursor = from;
  while (count < units) {
    cursor = addCalendarDays(cursor, 1);
    const dow = cursor.getDay();
    if (dow !== 0 && dow !== 6) count++;
  }
  return cursor;
}

export interface GanttArrow {
  x1: number;
  y1: number;
  x2: number;
  y2: number;
}

export interface GanttLayout {
  bars: GanttBar[];
  arrows: GanttArrow[];
  rowCount: number;
  minDate: Date | null;
  todayX: number;
}

interface RawRow {
  kind: "task" | "project";
  id: number;
  label: string;
  depth: number;
  startDate: Date | null;
  endDate: Date | null;
  color: string;
  hoverLabel: string;
  parentKey: string;
}

// V1.2's AddPlanDetail excludes a Task with Status Closed/Cancelled or no
// resolvable dates (GanttDisplayHelper.cs), and a Project with Priority
// Cancelled/Closed or !IsActive — V2's ProjectRecord has no separate
// is_active flag, so Priority is the closest portable equivalent.
function isVisibleTaskStatus(status: string): boolean {
  return status !== "Closed" && status !== "Cancelled";
}
function isVisibleProjectPriority(priority: string | null): boolean {
  return priority !== "Cancelled" && priority !== "Closed";
}

// The chain-of-ancestor-names part of the Gantt bar hover label below —
// V1.2's own "special syntax" for showing a Project's place in the
// hierarchy (DBProjectPal/Project.cs's FullName/FullParentName), built
// here directly rather than by calling FullName itself: this chain
// includes the Project *itself*, "=>"-joined bare (no spaces, no
// brackets) from the top-level ancestor down — e.g. "Marketing=>Website"
// for the "Website" Project. A cycle guard on the parent_project_id walk
// is not in the V1.2 original (D1.5-3's reasoning for adding one here
// anyway, generalised the same way elsewhere in this codebase).
function buildProjectChain(
  graph: ScheduleGraph,
  projectId: number,
  visited: Set<number> = new Set(),
): string {
  const project = graph.projectsById.get(projectId);
  if (!project) return "";
  if (project.parent_project_id == null || visited.has(project.parent_project_id)) {
    return project.name;
  }
  visited.add(projectId);
  return `${buildProjectChain(graph, project.parent_project_id, visited)}=>${project.name}`;
}

// V1.2's own `FullParentName`: the same chain, but *excluding* this
// Project itself — just its ancestors, empty for a top-level Project
// with no parent at all. Used for a Project's own hover label (its
// bracketed hierarchy shouldn't repeat the name already shown before the
// " : "); a Task's hover label uses `buildProjectChain` unchanged, since
// its own name is a Task description, not a Project name, so there's
// nothing to avoid repeating there.
function buildAncestorChain(graph: ScheduleGraph, projectId: number): string {
  const project = graph.projectsById.get(projectId);
  if (!project || project.parent_project_id == null) return "";
  return buildProjectChain(graph, project.parent_project_id);
}

function collectRows(
  graph: ScheduleGraph,
  projectId: number,
  depth: number,
  today: Date,
  rows: RawRow[],
  parentKey: string,
  customOrder: GanttCustomOrder | undefined,
  collapsedProjectIds: Set<number> | undefined,
): void {
  const project = graph.projectsById.get(projectId);
  if (!project) return;

  const projectSchedule = getProjectSchedule(graph, projectId);
  const ancestorChain = buildAncestorChain(graph, projectId);
  rows.push({
    kind: "project",
    id: projectId,
    label: project.name,
    depth,
    startDate: projectSchedule.startDate,
    endDate: projectSchedule.endDate,
    color: PROJECT_BAR_COLOUR,
    // No " : [...]" at all for a top-level Project — an empty ancestor
    // chain would otherwise show as a bare, empty "[]" suffix — matching
    // V1.2's own FullName getter, which likewise omits its "=> [...]"
    // suffix entirely when Parent is null.
    hoverLabel: ancestorChain ? `P: ${project.name} : [${ancestorChain}]` : `P: ${project.name}`,
    parentKey,
  });

  // Collapsed (the same client-only, PlanPage-owned state as
  // GanttCustomOrder above, never persisted server-side): this Project's
  // own row is still shown, but every descendant Task/sub-Project is left
  // out of `rows` entirely, rather than merely hidden by the renderer —
  // so a collapsed Project's own arrows, extent guide lines, and Boxed-mode
  // box all fall out of the same "no visible children" handling an empty
  // Project already gets, with nothing extra to special-case.
  if (collapsedProjectIds?.has(projectId)) return;

  // Default order — Tasks by start date, then sub-Projects alphabetically
  // — same as before D1.4-27. A user's own manual reorder (`customOrder`,
  // keyed by this Project's own childKey) is applied across *both*
  // together afterwards, since visually they're all just this Project's
  // child rows: a user is free to drag a Task above/below a sub-Project,
  // not just reorder within one or the other.
  const tasks = (graph.childTasksByProject.get(projectId) ?? [])
    .filter((t) => isVisibleTaskStatus(t.status))
    .map((t) => ({ task: t, schedule: getTaskSchedule(graph, t.task_id) }))
    .filter((t) => t.schedule.startDate && t.schedule.endDate)
    .sort((a, b) => a.schedule.startDate!.getTime() - b.schedule.startDate!.getTime());

  const subProjects = (graph.childProjectsByParent.get(projectId) ?? [])
    .filter((p) => isVisibleProjectPriority(p.priority))
    .sort((a, b) => a.name.localeCompare(b.name));

  type ChildEntry =
    | { kind: "task"; key: string; task: (typeof tasks)[number]["task"]; schedule: (typeof tasks)[number]["schedule"] }
    | { kind: "project"; key: string; project: (typeof subProjects)[number] };

  const children: ChildEntry[] = [
    ...tasks.map((t): ChildEntry => ({ kind: "task", key: `task:${t.task.task_id}`, task: t.task, schedule: t.schedule })),
    ...subProjects.map((p): ChildEntry => ({ kind: "project", key: `project:${p.project_id}`, project: p })),
  ];
  const childKey = `project:${projectId}`;
  const orderedChildren = applyCustomOrder(children, (c) => c.key, customOrder?.[childKey]);

  for (const child of orderedChildren) {
    if (child.kind === "task") {
      const { task, schedule } = child;
      const urgency = computeUrgency(task, graph.projectsById, schedule.startDate, schedule.endDate, today);
      rows.push({
        kind: "task",
        id: task.task_id,
        label: task.description,
        depth: depth + 1,
        startDate: schedule.startDate,
        endDate: schedule.endDate,
        color: computeTaskRowColour(task.priority, urgency),
        hoverLabel: `T: ${task.description} : [${buildProjectChain(graph, task.project_id)}]`,
        parentKey: childKey,
      });
    } else {
      collectRows(graph, child.project.project_id, depth + 1, today, rows, childKey, customOrder, collapsedProjectIds);
    }
  }
}

/**
 * `rootProjectId`: a single Project's subtree (`Plan Display` scoped to one
 * Project), or `null` for every top-level active Project (`Plan Display`'s
 * "Top Level Projects" mode, `UserInterfaceWindows.md` §3.7).
 */
export function buildGanttLayout(
  graph: ScheduleGraph,
  dependencies: DependencyRecord[],
  rootProjectId: number | null,
  today: Date = new Date(),
  customOrder?: GanttCustomOrder,
  excludeWeekends = false,
  collapsedProjectIds?: Set<number>,
): GanttLayout {
  // The one place this whole layout's day-to-pixel mapping is chosen —
  // every x/width/todayX below goes through this, so the "Weekends"
  // checkbox (D1.4-35) is a single switch here, not a parallel code path.
  const dayOffset = (from: Date, to: Date) =>
    excludeWeekends ? compressedDayOffset(from, to) : calendarDaysBetween(from, to);
  const rows: RawRow[] = [];

  if (rootProjectId != null) {
    collectRows(graph, rootProjectId, 0, today, rows, "root", customOrder, collapsedProjectIds);
  } else {
    const topLevelProjects = Array.from(graph.projectsById.values())
      .filter((p) => p.parent_project_id == null && isVisibleProjectPriority(p.priority))
      .sort((a, b) => a.name.localeCompare(b.name));
    const orderedTopLevel = applyCustomOrder(topLevelProjects, (p) => `project:${p.project_id}`, customOrder?.root);
    for (const project of orderedTopLevel) {
      collectRows(graph, project.project_id, 0, today, rows, "root", customOrder, collapsedProjectIds);
    }
  }

  let minDate: Date | null = null;
  for (const row of rows) {
    if (row.startDate && (!minDate || row.startDate < minDate)) minDate = row.startDate;
  }

  const bars: GanttBar[] = rows.map((row, index) => {
    const x = minDate && row.startDate ? dayOffset(minDate, row.startDate) * PIXELS_PER_DAY : 0;
    // Both ends measured from the same minDate reference, then
    // subtracted, rather than compressedDayOffset(startDate, endDate)
    // directly — `from` being itself a weekend in that direct form
    // doesn't clamp the same way `to` does (the clamping falls out of
    // `to` being compared against a *weekday* reference point), so
    // anchoring both ends to minDate (already a fixed, consistent
    // reference for the whole chart) sidesteps that asymmetry entirely.
    //
    // The right edge is one calendar day *past* endDate itself — endDate
    // is a whole day the bar occupies, not an instant it stops at, so
    // the bar has to reach the start of the day *after* it to actually
    // cover that day's own full width (D1.4-37: a bar spans from the
    // beginning of its start day to the end of its end day — a task that
    // starts and ends the same day is a full one-day-wide bar, not the
    // sliver `endX === x` would otherwise draw). Adding the day here,
    // before `dayOffset` runs, rather than adding 1 to the final offset
    // afterwards, is what keeps this correct under "Weekends" exclusion
    // too — e.g. an endDate of Friday needs to reach Saturday's own
    // (weekend-clamped) offset, not just "Friday's offset + 1 unit".
    const endX = minDate && row.endDate ? dayOffset(minDate, addCalendarDays(row.endDate, 1)) * PIXELS_PER_DAY : x;
    const width = row.startDate && row.endDate ? Math.max(1, endX - x) : 0;
    return {
      kind: row.kind,
      id: row.id,
      label: row.label,
      depth: row.depth,
      row: index,
      startDate: row.startDate,
      endDate: row.endDate,
      x,
      width,
      y: index * ROW_HEIGHT,
      color: row.color,
      subtreeBottomY: null,
      hoverLabel: row.hoverLabel,
      parentKey: row.parentKey,
    };
  });

  // `collectRows`'s own preorder walk (a Project's row, then its Tasks,
  // then each child Project's own whole subtree in turn) means every
  // Project's subtree is a *contiguous* run in `bars` starting right
  // after its own row — so "the last row in this Project's subtree" is
  // just "the row right before the next bar at this Project's own depth
  // or shallower." A stack of still-open Project bars, closed off as
  // soon as a bar at or above its depth is seen, finds every Project's
  // own subtree end in one pass (the same bracket-matching shape as
  // parsing nested parentheses).
  const openProjectIndexes: number[] = [];
  for (let i = 0; i < bars.length; i++) {
    while (openProjectIndexes.length > 0 && bars[openProjectIndexes[openProjectIndexes.length - 1]].depth >= bars[i].depth) {
      const projectIndex = openProjectIndexes.pop()!;
      const lastDescendantBar = bars[i - 1];
      if (lastDescendantBar !== bars[projectIndex]) {
        bars[projectIndex].subtreeBottomY = lastDescendantBar.y + BAR_HEIGHT;
      }
    }
    if (bars[i].kind === "project") openProjectIndexes.push(i);
  }
  while (openProjectIndexes.length > 0) {
    const projectIndex = openProjectIndexes.pop()!;
    const lastDescendantBar = bars[bars.length - 1];
    if (lastDescendantBar !== bars[projectIndex]) {
      bars[projectIndex].subtreeBottomY = lastDescendantBar.y + BAR_HEIGHT;
    }
  }

  const barByKey = new Map(bars.map((b) => [`${b.kind}:${b.id}`, b]));
  const arrows: GanttArrow[] = [];
  for (const dep of dependencies) {
    const fromBar = dep.pre_task_id != null
      ? barByKey.get(`task:${dep.pre_task_id}`)
      : dep.pre_project_id != null
        ? barByKey.get(`project:${dep.pre_project_id}`)
        : undefined;
    const toBar = dep.post_task_id != null
      ? barByKey.get(`task:${dep.post_task_id}`)
      : dep.post_project_id != null
        ? barByKey.get(`project:${dep.post_project_id}`)
        : undefined;
    if (!fromBar || !toBar || !fromBar.width || !toBar.width) continue;
    arrows.push({
      x1: fromBar.x + fromBar.width,
      y1: fromBar.y + BAR_HEIGHT / 2,
      x2: toBar.x,
      y2: toBar.y + BAR_HEIGHT / 2,
    });
  }

  const todayX = minDate ? dayOffset(minDate, today) * PIXELS_PER_DAY : 0;

  return { bars, arrows, rowCount: rows.length, minDate, todayX };
}

export interface MonthMarker {
  x: number;
  label: string;
}

export interface GridLines {
  // x positions (base units, pre-zoom-scale — the renderer applies the
  // same scaleX it applies to everything else) of each Monday and each
  // 1st-of-month within the chart's date range.
  weekLineXs: number[];
  monthLineXs: number[];
  // Same x positions as monthLineXs, paired with the footer label to show
  // for the month that's about to begin at that x (D1.4-26) — "Mmm", or
  // "Mmm-YY" for January (formatMonthMarker, schedule.ts).
  monthMarkers: MonthMarker[];
}

/**
 * `chartWidthBase`: the caller's own already-computed chart width (base
 * units) — kept as an input rather than folded into `GanttLayout` itself,
 * since that value already only exists in the renderer today (derived
 * from `layout.bars`/`layout.todayX` plus its own right-padding choice,
 * features/plan/PlanPage.tsx's `chartWidthBase`), not something
 * `buildGanttLayout` computes or needs for anything else.
 *
 * Monday/1st-of-month are checked using each date's *local* calendar
 * components (`getDay`/`getDate`), matching `addCalendarDays`'s own
 * local-time semantics (schedule.ts) — not UTC, which would disagree
 * with `addCalendarDays` by a day right around a DST transition.
 */
export function computeGridLines(minDate: Date | null, chartWidthBase: number, excludeWeekends = false): GridLines {
  if (!minDate) return { weekLineXs: [], monthLineXs: [], monthMarkers: [] };
  const totalUnits = Math.ceil(chartWidthBase / PIXELS_PER_DAY);
  const weekLineXs: number[] = [];
  const monthLineXs: number[] = [];
  const monthMarkers: MonthMarker[] = [];
  // Walks real calendar days (so Monday/1st-of-month are still found by
  // their true dates), but positions each one by its own *compressed*
  // offset when weekends are excluded — `unit` only advances on a
  // weekday then, so consecutive Mondays end up 5 units apart, not 7.
  // Loops on `unit`, not the calendar day count, since excluding
  // weekends means more calendar days are needed to fill the same
  // pixel width.
  let unit = 0;
  for (let calendarDayOffset = 0; unit <= totalUnits; calendarDayOffset++) {
    const date = addCalendarDays(minDate, calendarDayOffset);
    const dow = date.getDay();
    const isWeekend = dow === 0 || dow === 6;
    const x = unit * PIXELS_PER_DAY;
    if (dow === 1) weekLineXs.push(x);
    if (date.getDate() === 1) {
      monthLineXs.push(x);
      monthMarkers.push({ x, label: formatMonthMarker(date) });
    }
    if (!(excludeWeekends && isWeekend)) unit++;
  }
  return { weekLineXs, monthLineXs, monthMarkers };
}
