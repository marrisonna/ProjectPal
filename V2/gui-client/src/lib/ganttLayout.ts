import type { DependencyRecord } from "../api/types";
import {
  addCalendarDays,
  calendarDaysBetween,
  computeTaskRowColour,
  computeUrgency,
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
  });

  const tasks = (graph.childTasksByProject.get(projectId) ?? [])
    .filter((t) => isVisibleTaskStatus(t.status))
    .map((t) => ({ task: t, schedule: getTaskSchedule(graph, t.task_id) }))
    .filter((t) => t.schedule.startDate && t.schedule.endDate)
    .sort((a, b) => a.schedule.startDate!.getTime() - b.schedule.startDate!.getTime());

  for (const { task, schedule } of tasks) {
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
    });
  }

  const subProjects = (graph.childProjectsByParent.get(projectId) ?? [])
    .filter((p) => isVisibleProjectPriority(p.priority))
    .sort((a, b) => a.name.localeCompare(b.name));

  for (const sub of subProjects) {
    collectRows(graph, sub.project_id, depth + 1, today, rows);
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
): GanttLayout {
  const rows: RawRow[] = [];

  if (rootProjectId != null) {
    collectRows(graph, rootProjectId, 0, today, rows);
  } else {
    const topLevelProjects = Array.from(graph.projectsById.values())
      .filter((p) => p.parent_project_id == null && isVisibleProjectPriority(p.priority))
      .sort((a, b) => a.name.localeCompare(b.name));
    for (const project of topLevelProjects) {
      collectRows(graph, project.project_id, 0, today, rows);
    }
  }

  let minDate: Date | null = null;
  for (const row of rows) {
    if (row.startDate && (!minDate || row.startDate < minDate)) minDate = row.startDate;
  }

  const bars: GanttBar[] = rows.map((row, index) => {
    const x = minDate && row.startDate ? calendarDaysBetween(minDate, row.startDate) * PIXELS_PER_DAY : 0;
    const width =
      row.startDate && row.endDate
        ? Math.max(1, calendarDaysBetween(row.startDate, row.endDate)) * PIXELS_PER_DAY
        : 0;
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

  const todayX = minDate ? calendarDaysBetween(minDate, today) * PIXELS_PER_DAY : 0;

  return { bars, arrows, rowCount: rows.length, minDate, todayX };
}

export interface GridLines {
  // x positions (base units, pre-zoom-scale — the renderer applies the
  // same scaleX it applies to everything else) of each Monday and each
  // 1st-of-month within the chart's date range.
  weekLineXs: number[];
  monthLineXs: number[];
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
export function computeGridLines(minDate: Date | null, chartWidthBase: number): GridLines {
  if (!minDate) return { weekLineXs: [], monthLineXs: [] };
  const totalDays = Math.ceil(chartWidthBase / PIXELS_PER_DAY);
  const weekLineXs: number[] = [];
  const monthLineXs: number[] = [];
  for (let dayOffset = 0; dayOffset <= totalDays; dayOffset++) {
    const date = addCalendarDays(minDate, dayOffset);
    const x = dayOffset * PIXELS_PER_DAY;
    if (date.getDay() === 1) weekLineXs.push(x);
    if (date.getDate() === 1) monthLineXs.push(x);
  }
  return { weekLineXs, monthLineXs };
}
