import { describe, expect, it } from "vitest";
import { buildScheduleGraph, isProjectActive } from "./schedule";
import type { ProjectRecord, TaskRecord } from "../api/types";

// ProjectDetailPlan.md §2.2 / 4_GuiClient/Plan.md D1.4-41 — porting V1.2's
// `Project.IsActive` exactly: Priority not Cancelled/Closed, and at least
// one direct open Task or one active sub-Project (recursively).

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
    start_date: "2026-01-05",
    due_date: null,
    ...overrides,
  };
}

describe("isProjectActive", () => {
  it("is false for a Cancelled-priority Project regardless of its Tasks", () => {
    const project = makeProject({ project_id: 1, priority: "Cancelled" });
    const task = makeTask({ task_id: 1, project_id: 1, status: "NotStarted" });
    const graph = buildScheduleGraph([task], [project], [], new Map());
    expect(isProjectActive(graph, 1)).toBe(false);
  });

  it("is false for a Closed-priority Project regardless of its Tasks", () => {
    const project = makeProject({ project_id: 1, priority: "Closed" });
    const task = makeTask({ task_id: 1, project_id: 1, status: "NotStarted" });
    const graph = buildScheduleGraph([task], [project], [], new Map());
    expect(isProjectActive(graph, 1)).toBe(false);
  });

  it("is false when every direct Task is Closed/Cancelled and there is no sub-Project", () => {
    const project = makeProject({ project_id: 1, priority: "Med" });
    const tasks = [
      makeTask({ task_id: 1, project_id: 1, status: "Closed" }),
      makeTask({ task_id: 2, project_id: 1, status: "Cancelled" }),
    ];
    const graph = buildScheduleGraph(tasks, [project], [], new Map());
    expect(isProjectActive(graph, 1)).toBe(false);
  });

  it("is true when it has at least one direct Task that isn't Closed/Cancelled", () => {
    const project = makeProject({ project_id: 1, priority: "Med" });
    const tasks = [
      makeTask({ task_id: 1, project_id: 1, status: "Closed" }),
      makeTask({ task_id: 2, project_id: 1, status: "InProgress" }),
    ];
    const graph = buildScheduleGraph(tasks, [project], [], new Map());
    expect(isProjectActive(graph, 1)).toBe(true);
  });

  it("D1.4-116 — is true when a Project's only non-Closed/Cancelled Task is Paused", () => {
    const project = makeProject({ project_id: 1, priority: "Med" });
    const tasks = [
      makeTask({ task_id: 1, project_id: 1, status: "Closed" }),
      makeTask({ task_id: 2, project_id: 1, status: "Paused" }),
    ];
    const graph = buildScheduleGraph(tasks, [project], [], new Map());
    expect(isProjectActive(graph, 1)).toBe(true);
  });

  it("is true when made active only by a deeply-nested active grandchild sub-Project", () => {
    const root = makeProject({ project_id: 1, priority: "Med" });
    const child = makeProject({ project_id: 2, parent_project_id: 1, priority: "Med" });
    const grandchild = makeProject({ project_id: 3, parent_project_id: 2, priority: "Med" });
    // Only the grandchild has an open Task — root and child have none of
    // their own, so only the recursive sub-Project check can find this.
    const task = makeTask({ task_id: 1, project_id: 3, status: "NotStarted" });
    const graph = buildScheduleGraph([task], [root, child, grandchild], [], new Map());
    expect(isProjectActive(graph, 1)).toBe(true);
    expect(isProjectActive(graph, 2)).toBe(true);
    expect(isProjectActive(graph, 3)).toBe(true);
  });

  it("is false when no direct Task and no sub-Project is active", () => {
    const root = makeProject({ project_id: 1, priority: "Med" });
    const child = makeProject({ project_id: 2, parent_project_id: 1, priority: "Med" });
    const closedTask = makeTask({ task_id: 1, project_id: 2, status: "Closed" });
    const graph = buildScheduleGraph([closedTask], [root, child], [], new Map());
    expect(isProjectActive(graph, 1)).toBe(false);
    expect(isProjectActive(graph, 2)).toBe(false);
  });

  it("does not hang on a cyclic parent_project_id chain, and treats it as inactive", () => {
    const a = makeProject({ project_id: 1, parent_project_id: 2, priority: "Med" });
    const b = makeProject({ project_id: 2, parent_project_id: 1, priority: "Med" });
    const graph = buildScheduleGraph([], [a, b], [], new Map());
    expect(isProjectActive(graph, 1)).toBe(false);
    expect(isProjectActive(graph, 2)).toBe(false);
  });

  it("returns false for an unknown Project id", () => {
    const graph = buildScheduleGraph([], [], [], new Map());
    expect(isProjectActive(graph, 999)).toBe(false);
  });
});
