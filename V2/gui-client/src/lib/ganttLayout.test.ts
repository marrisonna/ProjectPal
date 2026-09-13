import { describe, expect, it } from "vitest";
import { buildScheduleGraph } from "./schedule";
import { BAR_HEIGHT, PIXELS_PER_DAY, ROW_HEIGHT, buildGanttLayout } from "./ganttLayout";
import type { DependencyRecord, ProjectRecord, TaskRecord } from "../api/types";

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

function dep(
  overrides: Partial<DependencyRecord> & { dependency_id: number },
): DependencyRecord {
  return {
    pre_task_id: null,
    pre_project_id: null,
    post_task_id: null,
    post_project_id: null,
    ...overrides,
  };
}

describe("buildGanttLayout", () => {
  it("lists a Project's own Tasks before recursing into its sub-Projects, in tree order", () => {
    const root = makeProject({ project_id: 1, name: "Root" });
    const child = makeProject({ project_id: 2, parent_project_id: 1, name: "Child" });
    const rootTask = makeTask({ task_id: 1, project_id: 1, description: "Root task", effort_in_days: 2 });
    const childTask = makeTask({ task_id: 2, project_id: 2, description: "Child task", effort_in_days: 2 });

    const graph = buildScheduleGraph(
      [rootTask, childTask],
      [root, child],
      [],
      new Map([[1, 1], [2, 1]]),
    );

    const layout = buildGanttLayout(graph, [], 1);

    expect(layout.bars.map((b) => `${b.kind}:${b.id}`)).toEqual([
      "project:1",
      "task:1",
      "project:2",
      "task:2",
    ]);
    expect(layout.bars.map((b) => b.depth)).toEqual([0, 1, 1, 2]);
  });

  it("excludes a Closed/Cancelled Task and a Cancelled/Closed-priority sub-Project", () => {
    const root = makeProject({ project_id: 1, name: "Root" });
    const cancelledSub = makeProject({ project_id: 2, parent_project_id: 1, name: "Dead sub", priority: "Cancelled" });
    const liveTask = makeTask({ task_id: 1, project_id: 1, description: "Live", effort_in_days: 1 });
    const closedTask = makeTask({ task_id: 2, project_id: 1, description: "Closed", status: "Closed" });

    const graph = buildScheduleGraph(
      [liveTask, closedTask],
      [root, cancelledSub],
      [],
      new Map([[1, 1], [2, 1]]),
    );

    const layout = buildGanttLayout(graph, [], 1);

    expect(layout.bars.map((b) => `${b.kind}:${b.id}`)).toEqual(["project:1", "task:1"]);
  });

  it("positions bars using PIXELS_PER_DAY from the earliest bar's own start date, and stacks rows by ROW_HEIGHT", () => {
    const project = makeProject({ project_id: 1, name: "Root", start_date: "2026-01-05" });
    const task = makeTask({ task_id: 1, project_id: 1, description: "T", effort_in_days: 4 });

    const graph = buildScheduleGraph([task], [project], [], new Map([[1, 1]]));
    const layout = buildGanttLayout(graph, [], 1);

    const projectBar = layout.bars.find((b) => b.kind === "project")!;
    const taskBar = layout.bars.find((b) => b.kind === "task")!;

    // The Project's own bar starts at its own start_date -> the earliest
    // date overall -> x = 0.
    expect(projectBar.x).toBe(0);
    expect(taskBar.y).toBe(ROW_HEIGHT * taskBar.row);
    expect(taskBar.width).toBeGreaterThanOrEqual(PIXELS_PER_DAY);
    expect(BAR_HEIGHT).toBeLessThanOrEqual(ROW_HEIGHT);
  });

  it("draws an arrow only between two bars both present in the current layout", () => {
    const project = makeProject({ project_id: 1, name: "Root" });
    const taskA = makeTask({ task_id: 1, project_id: 1, description: "A", effort_in_days: 1 });
    const taskB = makeTask({ task_id: 2, project_id: 1, description: "B", effort_in_days: 1 });
    // References a Task (99) that doesn't exist in this graph at all —
    // must be silently dropped, not throw or produce a dangling arrow.
    const dangling = dep({ dependency_id: 2, pre_task_id: 99, post_task_id: 2 });
    const real = dep({ dependency_id: 1, pre_task_id: 1, post_task_id: 2 });

    const graph = buildScheduleGraph([taskA, taskB], [project], [real, dangling], new Map([[1, 1], [2, 1]]));
    const layout = buildGanttLayout(graph, [real, dangling], 1);

    expect(layout.arrows).toHaveLength(1);
    const barA = layout.bars.find((b) => b.kind === "task" && b.id === 1)!;
    const barB = layout.bars.find((b) => b.kind === "task" && b.id === 2)!;
    expect(layout.arrows[0]).toEqual({
      x1: barA.x + barA.width,
      y1: barA.y + BAR_HEIGHT / 2,
      x2: barB.x,
      y2: barB.y + BAR_HEIGHT / 2,
    });
  });

  it("with no rootProjectId, includes every top-level active Project but not a Cancelled/Closed one", () => {
    const active = makeProject({ project_id: 1, name: "Active" });
    const cancelled = makeProject({ project_id: 2, name: "Cancelled", priority: "Cancelled" });

    const graph = buildScheduleGraph([], [active, cancelled], [], new Map());
    const layout = buildGanttLayout(graph, [], null);

    expect(layout.bars.map((b) => b.id)).toEqual([1]);
  });
});
