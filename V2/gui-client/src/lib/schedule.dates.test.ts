import { describe, expect, it } from "vitest";
import { addBusinessDays, buildScheduleGraph, getProjectSchedule, getTaskSchedule } from "./schedule";
import type { DependencyRecord, ProjectRecord, TaskRecord } from "../api/types";

// Fixtures the previous one-level-only approximation this module used to
// make (5_UrgencyCalculation/Plan.md D1.5-2/§4.7) could never have passed
// — a multi-hop Dependency chain, a Project aggregating over several
// children, an inherited (not directly-owned) dependency constraint, and
// a malformed cyclic graph that must not hang.

function makeTask(overrides: Partial<TaskRecord> & { task_id: number; project_id: number }): TaskRecord {
  return {
    component_id: null,
    priority: "Med",
    description: "",
    detailed_description: null,
    external_reference_url: null,
    requestor_person_id: null,
    owner_person_id: null,
    date_added: "2026-01-01T00:00:00Z",
    effort_in_days: 1,
    effort_type: "Duration",
    percentage_allocation: 1,
    task_type: null,
    status: "NotStarted",
    status_date: null,
    tentative_resource_assignment: false,
    start_relative_days_to_project: 0,
    ...overrides,
  };
}

function makeProject(overrides: Partial<ProjectRecord> & { project_id: number }): ProjectRecord {
  return {
    parent_project_id: null,
    team_id: 1,
    name: "",
    priority: "Med",
    detailed_description: null,
    owner_person_id: null,
    start_date: "2026-01-05", // a Monday
    due_date: null,
    ...overrides,
  };
}

function dep(preTaskId: number | null, postTaskId: number | null): DependencyRecord {
  return {
    dependency_id: preTaskId ?? 0 * 1000 + (postTaskId ?? 0),
    pre_task_id: preTaskId,
    pre_project_id: null,
    post_task_id: postTaskId,
    post_project_id: null,
  };
}

describe("getTaskSchedule — recursive Dependency chain", () => {
  it("propagates a predecessor's own resolved EndDate through multiple hops, not just one", () => {
    const project = makeProject({ project_id: 1 });
    const taskA = makeTask({ task_id: 1, project_id: 1, effort_in_days: 3 });
    const taskB = makeTask({ task_id: 2, project_id: 1, effort_in_days: 2 });
    const taskC = makeTask({ task_id: 3, project_id: 1, effort_in_days: 1 });
    const dependencies = [dep(1, 2), dep(2, 3)];
    const resourceCounts = new Map([
      [1, 1],
      [2, 1],
      [3, 1],
    ]);

    const graph = buildScheduleGraph([taskA, taskB, taskC], [project], dependencies, resourceCounts);

    const scheduleA = getTaskSchedule(graph, 1);
    const scheduleB = getTaskSchedule(graph, 2);
    const scheduleC = getTaskSchedule(graph, 3);

    // A: unconstrained, starts at the Project's own start_date.
    const expectedStartA = new Date(project.start_date!);
    const expectedEndA = addBusinessDays(expectedStartA, 3 - 1);
    expect(scheduleA.startDate?.getTime()).toBe(expectedStartA.getTime());
    expect(scheduleA.endDate?.getTime()).toBe(expectedEndA.getTime());

    // B: constrained by A's own end date + 1 business day (later than B's
    // own unconstrained earliest start, which is also the Project start).
    const expectedStartB = addBusinessDays(expectedEndA, 1);
    const expectedEndB = addBusinessDays(expectedStartB, 2 - 1);
    expect(scheduleB.startDate?.getTime()).toBe(expectedStartB.getTime());
    expect(scheduleB.endDate?.getTime()).toBe(expectedEndB.getTime());

    // C: constrained by B's own end date — which itself only resolved
    // correctly because B's own end date correctly resolved from A first.
    // The old one-level approximation had no way to get this right, since
    // it never resolved a predecessor's own predecessor.
    const expectedStartC = addBusinessDays(expectedEndB, 1);
    const expectedEndC = addBusinessDays(expectedStartC, 1 - 1);
    expect(scheduleC.startDate?.getTime()).toBe(expectedStartC.getTime());
    expect(scheduleC.endDate?.getTime()).toBe(expectedEndC.getTime());
  });

  it("a Task with no Dependency of its own inherits its parent Project's own constraint", () => {
    const parentProject = makeProject({ project_id: 1, start_date: "2026-01-05" });
    const childProject = makeProject({ project_id: 2, parent_project_id: 1, start_date: "2026-01-05" });
    // A predecessor Task feeding a dependency onto the *parent* Project.
    const predecessorTask = makeTask({ task_id: 1, project_id: 1, effort_in_days: 4 });
    // A Task inside the child Project with no Dependency of its own.
    const dependentTask = makeTask({ task_id: 2, project_id: 2, effort_in_days: 1 });
    const dependencies: DependencyRecord[] = [
      { dependency_id: 1, pre_task_id: 1, pre_project_id: null, post_task_id: null, post_project_id: 2 },
    ];
    const resourceCounts = new Map([
      [1, 1],
      [2, 1],
    ]);

    const graph = buildScheduleGraph(
      [predecessorTask, dependentTask],
      [parentProject, childProject],
      dependencies,
      resourceCounts,
    );

    const predecessorSchedule = getTaskSchedule(graph, 1);
    const dependentSchedule = getTaskSchedule(graph, 2);

    // The child Project's own StartDate is constrained by the predecessor
    // Task's EndDate (a dependency directly on the *Project*).
    const expectedChildProjectStart = addBusinessDays(predecessorSchedule.endDate!, 1);
    const childProjectSchedule = getProjectSchedule(graph, 2);
    expect(childProjectSchedule.startDate?.getTime()).toBe(expectedChildProjectStart.getTime());

    // The dependent Task has no Dependency of its own, but its own
    // Project does — so it inherits that same constraint rather than
    // starting from the Project's raw start_date.
    expect(dependentSchedule.startDate?.getTime()).toBe(expectedChildProjectStart.getTime());
  });

  it("does not hang on a cyclic parent_project_id chain, and resolves an otherwise-normal Task in it", () => {
    const cyclicA = makeProject({ project_id: 1, parent_project_id: 2 });
    const cyclicB = makeProject({ project_id: 2, parent_project_id: 1 });
    const task = makeTask({ task_id: 1, project_id: 1, effort_in_days: 1 });
    const graph = buildScheduleGraph([task], [cyclicA, cyclicB], [], new Map([[1, 1]]));

    const schedule = getTaskSchedule(graph, 1);
    expect(schedule.startDate).not.toBeNull();
    expect(schedule.endDate).not.toBeNull();
  });
});

describe("getProjectSchedule — EndDate aggregation", () => {
  it("is the max EndDate over every child Task and child Project", () => {
    const project = makeProject({ project_id: 1 });
    const subProject = makeProject({ project_id: 2, parent_project_id: 1 });
    const shortTask = makeTask({ task_id: 1, project_id: 1, effort_in_days: 1 });
    const longTask = makeTask({ task_id: 2, project_id: 2, effort_in_days: 10 });
    const graph = buildScheduleGraph(
      [shortTask, longTask],
      [project, subProject],
      [],
      new Map([
        [1, 1],
        [2, 1],
      ]),
    );

    const projectSchedule = getProjectSchedule(graph, 1);
    const longTaskSchedule = getTaskSchedule(graph, 2);
    expect(projectSchedule.endDate?.getTime()).toBe(longTaskSchedule.endDate?.getTime());
  });

  it("excludes Cancelled/Closed child Tasks and Cancelled/Closed-priority sub-Projects from the max", () => {
    const project = makeProject({ project_id: 1 });
    const excludedSubProject = makeProject({ project_id: 2, parent_project_id: 1, priority: "Cancelled" });
    const includedTask = makeTask({ task_id: 1, project_id: 1, effort_in_days: 1, status: "NotStarted" });
    const excludedTask = makeTask({ task_id: 2, project_id: 1, effort_in_days: 20, status: "Closed" });
    // A Task inside the excluded sub-Project with a huge effort — if this
    // leaked into the aggregation, it would dominate the max and the test
    // below would fail, proving the exclusion actually took effect.
    const taskInExcludedSubProject = makeTask({ task_id: 3, project_id: 2, effort_in_days: 100 });

    const graph = buildScheduleGraph(
      [includedTask, excludedTask, taskInExcludedSubProject],
      [project, excludedSubProject],
      [],
      new Map([
        [1, 1],
        [2, 1],
        [3, 1],
      ]),
    );

    const projectSchedule = getProjectSchedule(graph, 1);
    const includedTaskSchedule = getTaskSchedule(graph, 1);
    expect(projectSchedule.endDate?.getTime()).toBe(includedTaskSchedule.endDate?.getTime());
  });

  it("D1.4-116 — a Paused child Task still counts toward the max (not excluded like Closed/Cancelled)", () => {
    const project = makeProject({ project_id: 1 });
    const shortTask = makeTask({ task_id: 1, project_id: 1, effort_in_days: 1, status: "NotStarted" });
    const pausedTask = makeTask({ task_id: 2, project_id: 1, effort_in_days: 20, status: "Paused" });
    const graph = buildScheduleGraph(
      [shortTask, pausedTask],
      [project],
      [],
      new Map([
        [1, 1],
        [2, 1],
      ]),
    );

    const projectSchedule = getProjectSchedule(graph, 1);
    const pausedTaskSchedule = getTaskSchedule(graph, 2);
    expect(projectSchedule.endDate?.getTime()).toBe(pausedTaskSchedule.endDate?.getTime());
  });
});
