import { describe, expect, it } from "vitest";
import { computeResourceEffortDays } from "./schedule";

// V1.2's own MainWindow.ShowReport formula, ported verbatim
// (MainWindow.cs): PersonDays splits the stated effort evenly across
// resources; Duration instead converts a calendar span into person-days by
// multiplying by %Allocation.
describe("computeResourceEffortDays", () => {
  it("splits PersonDays effort evenly across resources", () => {
    expect(computeResourceEffortDays({ effort_in_days: 10, effort_type: "PersonDays", percentage_allocation: 1 }, 2)).toBe(5);
  });

  it("ignores %Allocation for PersonDays", () => {
    expect(
      computeResourceEffortDays({ effort_in_days: 10, effort_type: "PersonDays", percentage_allocation: 0.5 }, 2),
    ).toBe(5);
  });

  it("converts Duration effort into person-days via %Allocation, split across resources", () => {
    expect(
      computeResourceEffortDays({ effort_in_days: 10, effort_type: "Duration", percentage_allocation: 0.5 }, 2),
    ).toBe(2.5);
  });

  it("returns 0 for a zero resource count", () => {
    expect(computeResourceEffortDays({ effort_in_days: 10, effort_type: "PersonDays", percentage_allocation: 1 }, 0)).toBe(0);
  });

  it("treats a null effort_in_days as 0", () => {
    expect(computeResourceEffortDays({ effort_in_days: null, effort_type: "PersonDays", percentage_allocation: 1 }, 2)).toBe(0);
  });

  it("treats a null percentage_allocation as 1 (full allocation) for Duration", () => {
    expect(
      computeResourceEffortDays({ effort_in_days: 10, effort_type: "Duration", percentage_allocation: null }, 1),
    ).toBe(10);
  });
});
