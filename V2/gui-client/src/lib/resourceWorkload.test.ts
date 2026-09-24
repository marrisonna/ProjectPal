import { describe, expect, it } from "vitest";
import type { GridSortCellParams } from "@mui/x-data-grid";
import {
  buildResourceWorkloadRows,
  pinTotalLastNumeric,
  resourceWorkloadAvgUrgency,
  type ResourceWorkloadRow,
} from "./resourceWorkload";
import type { ProjectRecord, TaskRecord } from "../api/types";

function sortCellParams(id: string): GridSortCellParams<number> {
  return { id, field: "x", rowNode: {} as never, value: 0, api: {} as never };
}

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
    effort_in_days: 10,
    effort_type: "PersonDays",
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
    start_date: null,
    due_date: null,
    ...overrides,
  };
}

const isResource1And2 = (personId: number) => personId === 1 || personId === 2;
const nameOf = (personId: number) => `Person ${personId}`;

function rowByKey(rows: ResourceWorkloadRow[], key: string): ResourceWorkloadRow | undefined {
  return rows.find((r) => r.key === key);
}

describe("buildResourceWorkloadRows", () => {
  it("splits effort across resources and always includes a Total row first, even with no Tasks", () => {
    const rows = buildResourceWorkloadRows([], new Map(), new Map(), () => true, new Map(), nameOf);
    expect(rows).toHaveLength(1);
    expect(rows[0].kind).toBe("total");
    expect(rows[0].taskCount).toBe(0);
  });

  it("gives each real resource their own row, splitting PersonDays effort evenly", () => {
    const project = makeProject({ project_id: 1 });
    const task = makeTask({ task_id: 1, project_id: 1, effort_in_days: 10, status: "Ready" });
    const projectsById = new Map([[1, project]]);
    const resourceIdsByTask = new Map([[1, [1, 2]]]);
    const urgencyByTaskId = new Map([[1, 150]]);

    const rows = buildResourceWorkloadRows(
      [task],
      projectsById,
      resourceIdsByTask,
      isResource1And2,
      urgencyByTaskId,
      nameOf,
    );

    const person1 = rowByKey(rows, "person:1")!;
    const person2 = rowByKey(rows, "person:2")!;
    expect(person1.totalEffort).toBe(5);
    expect(person2.totalEffort).toBe(5);
    expect(person1.taskCount).toBe(1);
    expect(person1.readyCount).toBe(1);
    expect(person1.maxUrgency).toBe(150);

    const total = rowByKey(rows, "total")!;
    expect(total.taskCount).toBe(1);
    expect(total.totalEffort).toBe(10);
  });

  it("collapses every stale (non-current-resource) assignee on the same Task into one shared Other row", () => {
    const project = makeProject({ project_id: 1 });
    // 3 and 4 are both stale on this Team; 1 is current.
    const task = makeTask({ task_id: 1, project_id: 1, effort_in_days: 30 });
    const projectsById = new Map([[1, project]]);
    const resourceIdsByTask = new Map([[1, [1, 3, 4]]]);
    const urgencyByTaskId = new Map([[1, 100]]);

    const rows = buildResourceWorkloadRows(
      [task],
      projectsById,
      resourceIdsByTask,
      isResource1And2,
      urgencyByTaskId,
      nameOf,
    );

    // Two distinct identities on this Task: person:1 and the shared
    // "other" bucket (3 and 4 collapse into one, not two) — so effort
    // splits in half, not three ways.
    const person1 = rowByKey(rows, "person:1")!;
    const other = rowByKey(rows, "other")!;
    expect(person1.totalEffort).toBe(15);
    expect(other.totalEffort).toBe(15);
    expect(other.taskCount).toBe(1);
  });

  it("buckets a Task with no assigned resources at all into Unassigned", () => {
    const project = makeProject({ project_id: 1 });
    const task = makeTask({ task_id: 1, project_id: 1, effort_in_days: 8 });
    const projectsById = new Map([[1, project]]);
    const rows = buildResourceWorkloadRows(
      [task],
      projectsById,
      new Map(),
      () => true,
      new Map([[1, 100]]),
      nameOf,
    );

    const unassigned = rowByKey(rows, "unassigned")!;
    expect(unassigned.taskCount).toBe(1);
    expect(unassigned.totalEffort).toBe(8);
    expect(rowByKey(rows, "other")).toBeUndefined();
  });

  it("excludes an empty Other/Unassigned row entirely rather than showing a zero row", () => {
    const project = makeProject({ project_id: 1 });
    const task = makeTask({ task_id: 1, project_id: 1 });
    const rows = buildResourceWorkloadRows(
      [task],
      new Map([[1, project]]),
      new Map([[1, [1]]]),
      () => true,
      new Map(),
      nameOf,
    );
    expect(rowByKey(rows, "other")).toBeUndefined();
    expect(rowByKey(rows, "unassigned")).toBeUndefined();
  });

  it("converts Duration effort into person-days via %Allocation, matching V1.2's own split", () => {
    const project = makeProject({ project_id: 1 });
    const task = makeTask({
      task_id: 1,
      project_id: 1,
      effort_in_days: 20,
      effort_type: "Duration",
      percentage_allocation: 0.5,
    });
    const rows = buildResourceWorkloadRows(
      [task],
      new Map([[1, project]]),
      new Map([[1, [1]]]),
      () => true,
      new Map(),
      nameOf,
    );
    expect(rowByKey(rows, "person:1")!.totalEffort).toBe(10);
  });

  it("sorts Total last, then by descending average Urgency, ties broken alphabetically", () => {
    const project = makeProject({ project_id: 1 });
    const taskA = makeTask({ task_id: 1, project_id: 1 });
    const taskB = makeTask({ task_id: 2, project_id: 1 });
    const rows = buildResourceWorkloadRows(
      [taskA, taskB],
      new Map([[1, project]]),
      new Map([
        [1, [1]],
        [2, [2]],
      ]),
      () => true,
      new Map([
        [1, 120],
        [2, 180],
      ]),
      nameOf,
    );
    expect(rows.map((r) => r.key)).toEqual(["person:2", "person:1", "total"]);
  });
});

describe("resourceWorkloadAvgUrgency", () => {
  it("is 0 for a row with no Tasks, not NaN", () => {
    expect(
      resourceWorkloadAvgUrgency({
        key: "x",
        kind: "person",
        label: "x",
        taskCount: 0,
        readyCount: 0,
        inProgressCount: 0,
        totalEffort: 0,
        totalUrgency: 0,
        maxUrgency: 0,
      }),
    ).toBe(0);
  });
});

// The exact bug this guards against: MUI's own default `sortComparator`
// handling silently negates the result for a descending sort, so a naive
// "Total always sorts last" rule built on the plain form would flip back
// to sorting Total *first* the moment the user reverses direction.
// `getSortComparator` sidesteps that by taking direction as an explicit
// argument instead — these tests exercise both directions to prove the
// pin survives the flip.
describe("pinTotalLastNumeric", () => {
  it("sorts Total after a lower-valued row when ascending", () => {
    const compare = pinTotalLastNumeric("asc");
    expect(compare(999, 1, sortCellParams("total"), sortCellParams("person:1"))).toBeGreaterThan(0);
  });

  it("still sorts Total after a lower-valued row when descending", () => {
    const compare = pinTotalLastNumeric("desc");
    expect(compare(999, 1, sortCellParams("total"), sortCellParams("person:1"))).toBeGreaterThan(0);
  });

  it("still sorts Total after a higher-valued row when descending", () => {
    const compare = pinTotalLastNumeric("desc");
    expect(compare(1, 999, sortCellParams("total"), sortCellParams("person:1"))).toBeGreaterThan(0);
  });

  it("sorts two ordinary rows ascending normally", () => {
    const compare = pinTotalLastNumeric("asc");
    expect(compare(1, 2, sortCellParams("person:1"), sortCellParams("person:2"))).toBeLessThan(0);
  });

  it("sorts two ordinary rows descending normally", () => {
    const compare = pinTotalLastNumeric("desc");
    expect(compare(1, 2, sortCellParams("person:1"), sortCellParams("person:2"))).toBeGreaterThan(0);
  });
});
