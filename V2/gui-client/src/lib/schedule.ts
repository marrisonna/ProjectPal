import type { DependencyRecord, ProjectRecord, TaskRecord } from "../api/types";

/**
 * Reproduces V1.2's Task scheduling engine
 * (V1.2/Libs/DBProjectPal/DBProjectPal/Task.cs — EarliestStartDate, StartDate,
 * Duration, EndDate) — dates were never stored on Task in either version
 * (Claude/Level1_Implementation/8_ValidationAndVerification/Plan.md §4.1);
 * V1.2's UI computed them for display, so V2's GUI needs to as well
 * (4_GuiClient/Plan.md D1.4-14).
 *
 * Bounded for Stage 2: accounts for one level of predecessor Dependencies
 * (a direct predecessor's own earliest start + duration, or a predecessor
 * Project's due_date directly), not the full recursive dependency graph a
 * correct Gantt render needs — that full-tree walk is Stage 3's job
 * (4_GuiClient/Plan.md §6.3), reusing these same primitives.
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

/** V1.2's Task.EarliestStartDate: purely the relative offset, unconstrained
 * by any Dependency — this is what "Requested Start Date" displays. */
export function computeEarliestStartDate(
  task: Pick<TaskRecord, "start_relative_days_to_project">,
  project: Pick<ProjectRecord, "start_date"> | undefined,
): Date | null {
  if (task.start_relative_days_to_project == null || !project?.start_date) return null;
  return addBusinessDays(new Date(project.start_date), task.start_relative_days_to_project);
}

/** One direct predecessor's own end date — approximated (not recursed
 * further), per this module's Stage 2 scope note above. */
function approximatePredecessorEndDate(
  dependency: DependencyRecord,
  tasks: TaskRecord[],
  projects: ProjectRecord[],
): Date | null {
  if (dependency.pre_project_id != null) {
    const project = projects.find((p) => p.project_id === dependency.pre_project_id);
    return project?.due_date ? new Date(project.due_date) : null;
  }
  if (dependency.pre_task_id != null) {
    const preTask = tasks.find((t) => t.task_id === dependency.pre_task_id);
    const preProject = projects.find((p) => p.project_id === preTask?.project_id);
    if (!preTask) return null;
    const earliestStart = computeEarliestStartDate(preTask, preProject);
    const duration = computeDuration(preTask, 1); // resource count unknown at this depth
    if (!earliestStart || duration == null) return null;
    return addBusinessDays(earliestStart, Math.ceil(duration) - 1);
  }
  return null;
}

/** V1.2's Task.StartDate: the later of EarliestStartDate and (latest direct
 * predecessor's end date + 1 business day). */
export function computeStartDate(
  task: TaskRecord,
  project: ProjectRecord | undefined,
  predecessorDependencies: DependencyRecord[],
  allTasks: TaskRecord[],
  allProjects: ProjectRecord[],
): Date | null {
  const earliestStart = computeEarliestStartDate(task, project);

  let latestPredecessorEnd: Date | null = null;
  for (const dep of predecessorDependencies) {
    const end = approximatePredecessorEndDate(dep, allTasks, allProjects);
    if (end && (!latestPredecessorEnd || end > latestPredecessorEnd)) latestPredecessorEnd = end;
  }
  const constrainedStart = latestPredecessorEnd
    ? addBusinessDays(latestPredecessorEnd, 1)
    : null;

  if (constrainedStart && earliestStart) {
    return constrainedStart > earliestStart ? constrainedStart : earliestStart;
  }
  return constrainedStart ?? earliestStart;
}

/** V1.2's Task.EndDate: StartDate + Duration business days. */
export function computeEndDate(startDate: Date | null, duration: number | null): Date | null {
  if (!startDate || duration == null) return null;
  return addBusinessDays(startDate, Math.ceil(duration) - 1);
}
