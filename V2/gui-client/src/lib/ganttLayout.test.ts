import { describe, expect, it } from "vitest";
import { buildScheduleGraph } from "./schedule";
import { BAR_HEIGHT, PIXELS_PER_DAY, ROW_HEIGHT, buildGanttLayout, computeGridLines } from "./ganttLayout";
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

  describe("subtreeBottomY (the Project 'extent' guide lines)", () => {
    it("is null for a Task bar, and for a childless Project", () => {
      const root = makeProject({ project_id: 1, name: "Root" });
      const childless = makeProject({ project_id: 2, parent_project_id: 1, name: "Childless" });
      const task = makeTask({ task_id: 1, project_id: 1, description: "T", effort_in_days: 1 });

      const graph = buildScheduleGraph([task], [root, childless], [], new Map([[1, 1]]));
      const layout = buildGanttLayout(graph, [], 1);

      const taskBar = layout.bars.find((b) => b.kind === "task")!;
      const childlessBar = layout.bars.find((b) => b.id === 2)!;
      expect(taskBar.subtreeBottomY).toBeNull();
      expect(childlessBar.subtreeBottomY).toBeNull();
    });

    it("reaches the bottom of a Project's own last direct Task", () => {
      const root = makeProject({ project_id: 1, name: "Root" });
      const taskA = makeTask({ task_id: 1, project_id: 1, description: "A", effort_in_days: 1 });
      const taskB = makeTask({ task_id: 2, project_id: 1, description: "B", effort_in_days: 1, start_relative_days_to_project: 5 });

      const graph = buildScheduleGraph([taskA, taskB], [root], [], new Map([[1, 1], [2, 1]]));
      const layout = buildGanttLayout(graph, [], 1);

      const rootBar = layout.bars.find((b) => b.kind === "project")!;
      const lastBar = layout.bars[layout.bars.length - 1];
      expect(rootBar.subtreeBottomY).toBe(lastBar.y + BAR_HEIGHT);
    });

    it("reaches all the way to the last row of a nested sub-Project's own subtree, for every ancestor", () => {
      // Root -> TaskRoot, SubA -> TaskSubA, SubSubA -> TaskSubSubA
      const root = makeProject({ project_id: 1, name: "Root" });
      const subA = makeProject({ project_id: 2, parent_project_id: 1, name: "SubA" });
      const subSubA = makeProject({ project_id: 3, parent_project_id: 2, name: "SubSubA" });
      const taskRoot = makeTask({ task_id: 1, project_id: 1, description: "TaskRoot", effort_in_days: 1 });
      const taskSubA = makeTask({ task_id: 2, project_id: 2, description: "TaskSubA", effort_in_days: 1 });
      const taskSubSubA = makeTask({ task_id: 3, project_id: 3, description: "TaskSubSubA", effort_in_days: 1 });

      const graph = buildScheduleGraph(
        [taskRoot, taskSubA, taskSubSubA],
        [root, subA, subSubA],
        [],
        new Map([[1, 1], [2, 1], [3, 1]]),
      );
      const layout = buildGanttLayout(graph, [], 1);

      const rootBar = layout.bars.find((b) => b.id === 1 && b.kind === "project")!;
      const subABar = layout.bars.find((b) => b.id === 2)!;
      const subSubABar = layout.bars.find((b) => b.id === 3)!;
      const lastBar = layout.bars[layout.bars.length - 1];

      // All three (deepest to shallowest) reach the very last row —
      // TaskSubSubA is the last descendant of all of them.
      expect(subSubABar.subtreeBottomY).toBe(lastBar.y + BAR_HEIGHT);
      expect(subABar.subtreeBottomY).toBe(lastBar.y + BAR_HEIGHT);
      expect(rootBar.subtreeBottomY).toBe(lastBar.y + BAR_HEIGHT);
    });

    it("stops a Project's extent at its own last child, not a later sibling Project's subtree", () => {
      // Root -> SubB (-> TaskSubB), SubC (no children) — SubB's own
      // extent must stop at TaskSubB, not run on into SubC's row.
      const root = makeProject({ project_id: 1, name: "Root" });
      const subB = makeProject({ project_id: 2, parent_project_id: 1, name: "SubB" });
      const subC = makeProject({ project_id: 3, parent_project_id: 1, name: "SubC" });
      const taskSubB = makeTask({ task_id: 1, project_id: 2, description: "TaskSubB", effort_in_days: 1 });

      const graph = buildScheduleGraph([taskSubB], [root, subB, subC], [], new Map([[1, 1]]));
      const layout = buildGanttLayout(graph, [], 1);

      const subBBar = layout.bars.find((b) => b.id === 2)!;
      const taskSubBBar = layout.bars.find((b) => b.kind === "task")!;
      const subCBar = layout.bars.find((b) => b.id === 3)!;

      expect(subBBar.subtreeBottomY).toBe(taskSubBBar.y + BAR_HEIGHT);
      expect(subCBar.subtreeBottomY).toBeNull();
    });
  });

  describe("hoverLabel ('P/T - name : [ancestor chain]', adapted from V1.2's own Gantt hover caption)", () => {
    it("for a top-level Project (no parent), is 'P - name' with no ' : [...]' suffix at all", () => {
      const root = makeProject({ project_id: 1, name: "Marketing" });
      const graph = buildScheduleGraph([], [root], [], new Map());
      const layout = buildGanttLayout(graph, [], 1);

      expect(layout.bars.find((b) => b.id === 1)!.hoverLabel).toBe("P: Marketing");
    });

    it("for a nested Project, is 'P - name : [ancestor chain]', excluding itself from the chain", () => {
      const root = makeProject({ project_id: 1, name: "Marketing" });
      const child = makeProject({ project_id: 2, parent_project_id: 1, name: "Website" });
      const grandchild = makeProject({ project_id: 3, parent_project_id: 2, name: "Login Page" });

      const graph = buildScheduleGraph([], [root, child, grandchild], [], new Map());
      const layout = buildGanttLayout(graph, [], 1);

      expect(layout.bars.find((b) => b.id === 2)!.hoverLabel).toBe("P: Website : [Marketing]");
      expect(layout.bars.find((b) => b.id === 3)!.hoverLabel).toBe("P: Login Page : [Marketing=>Website]");
    });

    it("for a Task, is 'T - description : [chain]', including its own Project in the chain", () => {
      const root = makeProject({ project_id: 1, name: "Marketing" });
      const child = makeProject({ project_id: 2, parent_project_id: 1, name: "Website" });
      const task = makeTask({ task_id: 1, project_id: 2, description: "Fix bug", effort_in_days: 1 });

      const graph = buildScheduleGraph([task], [root, child], [], new Map([[1, 1]]));
      const layout = buildGanttLayout(graph, [], 1);

      expect(layout.bars.find((b) => b.kind === "task")!.hoverLabel).toBe("T: Fix bug : [Marketing=>Website]");
    });
  });
});

describe("computeGridLines", () => {
  it("returns no lines when there's no minDate (nothing to lay out at all)", () => {
    expect(computeGridLines(null, 500)).toEqual({ weekLineXs: [], monthLineXs: [], monthMarkers: [] });
  });

  it("places a week line at every Monday, and a month line at every 1st-of-month, within the chart's date range", () => {
    const minDate = new Date(2026, 0, 5); // a Monday
    expect(minDate.getDay()).toBe(1);

    const { weekLineXs, monthLineXs, monthMarkers } = computeGridLines(minDate, 30 * PIXELS_PER_DAY);

    // Mondays 5/12/19/26-Jan and 2-Feb (day offsets 0/7/14/21/28) all fall
    // within a 30-day range from minDate.
    expect(weekLineXs).toEqual([0, 7, 14, 21, 28].map((d) => d * PIXELS_PER_DAY));
    // 1-Feb-2026 is 27 days after 5-Jan-2026.
    expect(monthLineXs).toEqual([27 * PIXELS_PER_DAY]);
    expect(monthMarkers).toEqual([{ x: 27 * PIXELS_PER_DAY, label: "Feb" }]);
  });

  it("labels a January month marker with its year (Mmm-YY)", () => {
    const minDate = new Date(2025, 11, 1); // 1-Dec-2025
    const { monthMarkers } = computeGridLines(minDate, 45 * PIXELS_PER_DAY);

    // 1-Jan-2026 is 31 days after 1-Dec-2025.
    expect(monthMarkers).toEqual([
      { x: 0, label: "Dec" },
      { x: 31 * PIXELS_PER_DAY, label: "Jan-26" },
    ]);
  });

  it("stops at the given chart width, not beyond it", () => {
    const minDate = new Date(2026, 0, 5); // a Monday
    const { weekLineXs } = computeGridLines(minDate, 10 * PIXELS_PER_DAY);

    // Only the Mondays at day offset 0 and 7 fit within a 10-day range.
    expect(weekLineXs).toEqual([0, 7 * PIXELS_PER_DAY]);
  });
});
