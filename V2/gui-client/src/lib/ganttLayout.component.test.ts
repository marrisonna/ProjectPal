import { describe, expect, it } from "vitest";
import { buildScheduleGraph } from "./schedule";
import { buildComponentGanttLayout } from "./ganttLayout";
import type { ComponentRecord, DependencyRecord, ProjectRecord, TaskRecord } from "../api/types";

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

function makeComponent(overrides: Partial<ComponentRecord> & { component_id: number }): ComponentRecord {
  return {
    parent_component_id: null,
    team_id: 1,
    name: "",
    owner_person_id: null,
    ...overrides,
  };
}

function dep(overrides: Partial<DependencyRecord> & { dependency_id: number }): DependencyRecord {
  return {
    pre_task_id: null,
    pre_project_id: null,
    post_task_id: null,
    post_project_id: null,
    ...overrides,
  };
}

// D1.4-109 — the Component-hierarchy counterpart to buildGanttLayout,
// walking Component -> SubComponents (+ each Component's own directly
// -tagged Tasks) rather than Project -> SubProjects, per
// Requirements/UserInterfaceWindows.md §3.7/§3.8's own correction that the
// two trees never share rendering data.
describe("buildComponentGanttLayout", () => {
  it("lists a Component's own Tasks before recursing into its sub-Components, in tree order", () => {
    const project = makeProject({ project_id: 1, name: "Project" });
    const root = makeComponent({ component_id: 1, name: "Root" });
    const child = makeComponent({ component_id: 2, parent_component_id: 1, name: "Child" });
    const rootTask = makeTask({ task_id: 1, project_id: 1, component_id: 1, description: "Root task", effort_in_days: 2 });
    const childTask = makeTask({ task_id: 2, project_id: 1, component_id: 2, description: "Child task", effort_in_days: 2 });

    const graph = buildScheduleGraph(
      [rootTask, childTask],
      [project],
      [],
      new Map([
        [1, 1],
        [2, 1],
      ]),
    );

    const layout = buildComponentGanttLayout(graph, [root, child], [], 1);

    expect(layout.bars.map((b) => `${b.kind}:${b.id}`)).toEqual(["component:1", "task:1", "component:2", "task:2"]);
  });

  it("gives a Component's own bar a span covering its Tasks and sub-Components' Tasks", () => {
    const project = makeProject({ project_id: 1, name: "Project" });
    const root = makeComponent({ component_id: 1, name: "Root" });
    const child = makeComponent({ component_id: 2, parent_component_id: 1, name: "Child" });
    // rootTask starts at the Project's own start_date (2026-01-05); give
    // childTask a later start via a longer preceding effort on rootTask's
    // own dependency-free timeline isn't needed here — placing childTask on
    // a different Project's own later-starting timeline is simpler.
    const project2 = makeProject({ project_id: 2, name: "Project 2", start_date: "2026-01-12" });
    const rootTask = makeTask({ task_id: 1, project_id: 1, component_id: 1, description: "Root task", effort_in_days: 2 });
    const childTask = makeTask({ task_id: 2, project_id: 2, component_id: 2, description: "Child task", effort_in_days: 2 });

    const graph = buildScheduleGraph(
      [rootTask, childTask],
      [project, project2],
      [],
      new Map([
        [1, 1],
        [2, 1],
      ]),
    );

    const layout = buildComponentGanttLayout(graph, [root, child], [], 1);
    const rootBar = layout.bars.find((b) => b.kind === "component" && b.id === 1)!;
    const childBar = layout.bars.find((b) => b.kind === "component" && b.id === 2)!;

    // Root's own span covers both its own Task and the sub-Component's
    // later Task — its endDate reaches at least as far as the child's.
    expect(rootBar.endDate!.getTime()).toBeGreaterThanOrEqual(childBar.endDate!.getTime());
    expect(rootBar.startDate).not.toBeNull();
  });

  it("never lets a dependency's own pre_project_id/post_project_id match a same-numbered Component", () => {
    // Deliberately gives the Component the SAME numeric id (1) as a real
    // Project a Dependency actually references — the exact collision risk
    // reusing bar kind "project" for Component containers would have
    // created (barByKey keyed "project:1" either way).
    const project = makeProject({ project_id: 1, name: "Project" });
    const otherProject = makeProject({ project_id: 99, name: "Other" });
    const component = makeComponent({ component_id: 1, name: "Same-numbered Component" });
    const task = makeTask({ task_id: 1, project_id: 1, component_id: 1, description: "Task" });
    const otherTask = makeTask({ task_id: 2, project_id: 99, description: "Other task" });

    const graph = buildScheduleGraph(
      [task, otherTask],
      [project, otherProject],
      [],
      new Map([
        [1, 1],
        [2, 1],
      ]),
    );

    // A Dependency naming Project #1 (this test's own Component id) as its
    // predecessor — never a legitimate case in the real domain model (a
    // Dependency never references a Component), but exactly the shape that
    // would wrongly resolve if Component bars were stored as kind "project".
    const dependencies = [dep({ dependency_id: 1, pre_project_id: 1, post_task_id: 2 })];

    const layout = buildComponentGanttLayout(graph, [component], dependencies, 1);

    expect(layout.arrows).toHaveLength(0);
  });
});
