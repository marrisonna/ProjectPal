import { describe, expect, it } from "vitest";
import { computeUrgency, computeUrgencyColour, computeTaskRowColour } from "./schedule";
import type { ProjectRecord } from "../api/types";

// Requirements/KeyConcepts.md §12.1/§12.2's own worked examples, used
// verbatim as fixtures — each one recomputed independently to full
// precision while writing these tests, which is what actually caught
// §12.1's own Worked Example 2 stating a rounded `89.1` where the real,
// full-precision result is `89.0` (fixed in that document alongside this
// file landing, D1.5-1).

function project(id: number, priority: string | null, parentId: number | null = null): ProjectRecord {
  return {
    project_id: id,
    parent_project_id: parentId,
    team_id: 1,
    name: `Project ${id}`,
    priority,
    detailed_description: null,
    owner_person_id: null,
    start_date: null,
    due_date: null,
  };
}

describe("computeUrgency", () => {
  it("Worked example 1 — closed task, decaying urgency", () => {
    const today = new Date(2026, 0, 26);
    const statusDate = new Date(2026, 0, 1); // 25 days before today
    const task = {
      status: "Closed",
      status_date: statusDate.toISOString(),
      priority: null as string | null,
      project_id: 1,
    };
    expect(computeUrgency(task, new Map(), null, null, today)).toBe(0.4);
  });

  it("closed task within the 10-day grace window stays at 1", () => {
    const today = new Date(2026, 0, 6);
    const statusDate = new Date(2026, 0, 1); // 5 days before today
    const task = {
      status: "Cancelled",
      status_date: statusDate.toISOString(),
      priority: null as string | null,
      project_id: 1,
    };
    expect(computeUrgency(task, new Map(), null, null, today)).toBe(1);
  });

  it("closed task with no StatusDate at all stays at 1", () => {
    const task = { status: "Closed", status_date: null, priority: null as string | null, project_id: 1 };
    expect(computeUrgency(task, new Map(), null, null, new Date(2026, 0, 26))).toBe(1);
  });

  it("Worked example 2 — open task, not yet due, damped by time", () => {
    const today = new Date(2026, 0, 1);
    const startDate = today;
    const endDate = new Date(2026, 0, 21); // today + 20
    const projectsById = new Map([[1, project(1, "Med")]]);
    const task = { status: "InProgress", status_date: null, priority: "Med", project_id: 1 };
    expect(computeUrgency(task, projectsById, startDate, endDate, today)).toBe(89.0);
  });

  it("Worked example 3 — open task, overdue and high priority", () => {
    const today = new Date(2026, 0, 6);
    const startDate = new Date(2026, 0, 1); // today - 5
    const projectsById = new Map([[1, project(1, "Med")]]);
    const task = { status: "NotStarted", status_date: null, priority: "High", project_id: 1 };
    expect(computeUrgency(task, projectsById, startDate, null, today)).toBe(180.5);
  });

  it("a missing Task Priority defaults to Med, same as an explicit 'Med'", () => {
    const today = new Date(2026, 0, 1);
    const projectsById = new Map([[1, project(1, "Med")]]);
    const withMed = computeUrgency(
      { status: "InProgress", status_date: null, priority: "Med", project_id: 1 },
      projectsById,
      today,
      new Date(2026, 0, 21),
      today,
    );
    const withMissing = computeUrgency(
      { status: "InProgress", status_date: null, priority: null, project_id: 1 },
      projectsById,
      today,
      new Date(2026, 0, 21),
      today,
    );
    expect(withMissing).toBe(withMed);
  });

  it("a missing ancestor Project Priority defaults to Med too", () => {
    const today = new Date(2026, 0, 1);
    const withMed = computeUrgency(
      { status: "InProgress", status_date: null, priority: "Med", project_id: 1 },
      new Map([[1, project(1, "Med")]]),
      today,
      new Date(2026, 0, 21),
      today,
    );
    const withMissing = computeUrgency(
      { status: "InProgress", status_date: null, priority: "Med", project_id: 1 },
      new Map([[1, project(1, null)]]),
      today,
      new Date(2026, 0, 21),
      today,
    );
    expect(withMissing).toBe(withMed);
  });

  it("no Task date at all (never started, no offset) still produces a priority-only score", () => {
    const today = new Date(2026, 0, 1);
    const projectsById = new Map([[1, project(1, "Med")]]);
    const task = { status: "Ready", status_date: null, priority: "Med", project_id: 1 };
    // No startDate at all -> taskDate undefined -> U = 100 * multiplier; Med under Med is multiplier 1.
    expect(computeUrgency(task, projectsById, null, null, today)).toBe(100);
  });

  it("D1.4-116 — Paused falls into the same 'any other open status -> end date' branch as Ready/Support/Tentative", () => {
    const today = new Date(2026, 0, 1);
    const startDate = new Date(2025, 11, 20);
    const endDate = new Date(2026, 0, 15);
    const projectsById = new Map([[1, project(1, "Med")]]);
    const paused = { status: "Paused", status_date: null, priority: "Med", project_id: 1 };
    const ready = { status: "Ready", status_date: null, priority: "Med", project_id: 1 };
    expect(computeUrgency(paused, projectsById, startDate, endDate, today)).toBe(
      computeUrgency(ready, projectsById, startDate, endDate, today),
    );
  });

  it("walks multiple ancestor Projects root-first, and a cyclic parent chain doesn't hang", () => {
    const today = new Date(2026, 0, 1);
    const cyclicProjects = new Map([
      [1, project(1, "High", 2)],
      [2, project(2, "Low", 1)], // cycle: 1 -> 2 -> 1
    ]);
    const task = { status: "InProgress", status_date: null, priority: "Med", project_id: 1 };
    // Should terminate (not hang) and produce *some* finite number.
    const result = computeUrgency(task, cyclicProjects, today, new Date(2026, 0, 21), today);
    expect(Number.isFinite(result)).toBe(true);
  });
});

describe("computeUrgencyColour", () => {
  it("Worked example 1 — below the threshold", () => {
    expect(computeUrgencyColour(89.0)).toBe("rgb(255, 255, 255)");
  });

  it("Worked example 2 — partway through the range", () => {
    expect(computeUrgencyColour(180.5)).toBe("rgb(255, 153, 153)");
  });

  it("Worked example 3 — at and beyond saturation", () => {
    expect(computeUrgencyColour(250)).toBe("rgb(255, 128, 128)");
    expect(computeUrgencyColour(300)).toBe("rgb(255, 128, 128)");
  });
});

describe("computeTaskRowColour", () => {
  it("shows the fixed grey when the Task's own Priority is Cancelled, regardless of Urgency", () => {
    expect(computeTaskRowColour("Cancelled", 250)).toBe("rgb(190, 190, 190)");
  });

  it("shows the fixed grey when the Task's own Priority is Closed, regardless of Urgency", () => {
    expect(computeTaskRowColour("Closed", 250)).toBe("rgb(190, 190, 190)");
  });

  it("shows the fixed grey when the Task's own Priority is unset", () => {
    expect(computeTaskRowColour(null, 250)).toBe("rgb(190, 190, 190)");
  });

  it("shows the real urgency colour for any other Priority", () => {
    expect(computeTaskRowColour("High", 250)).toBe(computeUrgencyColour(250));
  });
});
