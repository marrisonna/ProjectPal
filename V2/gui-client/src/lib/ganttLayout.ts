import type { DependencyRecord } from "../api/types";
import {
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
  rows.push({
    kind: "project",
    id: projectId,
    label: project.name,
    depth,
    startDate: projectSchedule.startDate,
    endDate: projectSchedule.endDate,
    color: PROJECT_BAR_COLOUR,
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
    };
  });

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
