import { describe, expect, it } from "vitest";
import { buildScheduleGraph } from "./schedule";
import { buildFilteredGanttLayout } from "./ganttLayout";
import type { ProjectRecord, TaskRecord } from "../api/types";

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

// D1.4-109 — AllTaskPage.tsx's own "View Gantt" button: exactly the
// currently-filtered Tasks, with just enough Project ancestry to place
// them, pruning any Project (at any depth) with nothing allowed anywhere
// in its own subtree.
describe("buildFilteredGanttLayout", () => {
  it("includes only the allowed Task, and its own ancestor Project, out of an unrelated sibling", () => {
    const projectA = makeProject({ project_id: 1, name: "A" });
    const projectB = makeProject({ project_id: 2, name: "B" });
    const taskA = makeTask({ task_id: 1, project_id: 1, description: "Task A" });
    const taskB = makeTask({ task_id: 2, project_id: 2, description: "Task B" });

    const graph = buildScheduleGraph(
      [taskA, taskB],
      [projectA, projectB],
      [],
      new Map([
        [1, 1],
        [2, 1],
      ]),
    );

    const layout = buildFilteredGanttLayout(graph, [], new Set([1]));

    expect(layout.bars.map((b) => `${b.kind}:${b.id}`)).toEqual(["project:1", "task:1"]);
  });

  it("prunes a sub-Project entirely when none of its own Tasks are allowed, even if a sibling sub-Project has one", () => {
    const root = makeProject({ project_id: 1, name: "Root" });
    const keptChild = makeProject({ project_id: 2, parent_project_id: 1, name: "Kept" });
    const prunedChild = makeProject({ project_id: 3, parent_project_id: 1, name: "Pruned" });
    const keptTask = makeTask({ task_id: 1, project_id: 2, description: "Kept task" });
    const prunedTask = makeTask({ task_id: 2, project_id: 3, description: "Pruned task" });

    const graph = buildScheduleGraph(
      [keptTask, prunedTask],
      [root, keptChild, prunedChild],
      [],
      new Map([
        [1, 1],
        [2, 1],
      ]),
    );

    const layout = buildFilteredGanttLayout(graph, [], new Set([1]));

    expect(layout.bars.map((b) => `${b.kind}:${b.id}`)).toEqual(["project:1", "project:2", "task:1"]);
  });

  it("returns an empty layout when nothing is allowed", () => {
    const project = makeProject({ project_id: 1, name: "Root" });
    const task = makeTask({ task_id: 1, project_id: 1 });
    const graph = buildScheduleGraph([task], [project], [], new Map([[1, 1]]));

    const layout = buildFilteredGanttLayout(graph, [], new Set());

    expect(layout.bars).toHaveLength(0);
  });

  it("still shows an allowed Task even when its own Project's Priority is Cancelled", () => {
    const project = makeProject({ project_id: 1, name: "Root", priority: "Cancelled" });
    const task = makeTask({ task_id: 1, project_id: 1, description: "Task" });
    const graph = buildScheduleGraph([task], [project], [], new Map([[1, 1]]));

    const layout = buildFilteredGanttLayout(graph, [], new Set([1]));

    expect(layout.bars.map((b) => `${b.kind}:${b.id}`)).toEqual(["project:1", "task:1"]);
  });
});
