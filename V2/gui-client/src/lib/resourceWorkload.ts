import type { GridComparatorFn, GridSortCellParams, GridSortDirection } from "@mui/x-data-grid";
import type { ProjectRecord, TaskRecord } from "../api/types";
import { computeResourceEffortDays } from "./schedule";

/**
 * DashboardPage.tsx's own per-Resource workload report (D1.4-104) — ported
 * from V1.2's actual `MainWindow.cs`/`GUIMainReportItem.cs`/
 * `Task.TeamResources` (V1.2/Libs/DBProjectPal/DBProjectPal/Task.cs), not
 * just `UserInterfaceWindows.md`'s own higher-level summary. Extracted from
 * DashboardPage.tsx itself (unlike AllTaskPage.tsx/SearchPage.tsx's own,
 * simpler per-page derived maps) because the bucketing rules below have
 * several real, easy-to-get-wrong subtleties worth locking in with tests
 * rather than only ever exercised visually.
 */

export type ResourceWorkloadRowKind = "total" | "person" | "other" | "unassigned";

export interface ResourceWorkloadRow {
  key: string;
  kind: ResourceWorkloadRowKind;
  personId?: number;
  label: string;
  taskCount: number;
  readyCount: number;
  inProgressCount: number;
  totalEffort: number;
  totalUrgency: number;
  maxUrgency: number;
}

function newRow(key: string, kind: ResourceWorkloadRowKind, label: string, personId?: number): ResourceWorkloadRow {
  return {
    key,
    kind,
    personId,
    label,
    taskCount: 0,
    readyCount: 0,
    inProgressCount: 0,
    totalEffort: 0,
    totalUrgency: 0,
    maxUrgency: 0,
  };
}

function addTaskData(row: ResourceWorkloadRow, effort: number, urgency: number, status: string): void {
  row.taskCount++;
  row.totalEffort += effort;
  row.totalUrgency += urgency;
  row.maxUrgency = Math.max(row.maxUrgency, urgency);
  if (status === "Ready") row.readyCount++;
  if (status === "InProgress") row.inProgressCount++;
}

export function resourceWorkloadAvgUrgency(row: ResourceWorkloadRow): number {
  return row.taskCount === 0 ? 0 : Math.round((row.totalUrgency / row.taskCount) * 10) / 10;
}

/**
 * `tasks` must already be filtered to whatever this viewer should see
 * (Team-scoped, Closed/Cancelled excluded — DashboardPage.tsx's own
 * `openTeamScopedTasks`); this function has no scoping opinion of its own.
 *
 * `isCurrentResource(personId, teamId)` mirrors `admin.py`'s own "stale
 * resource assignment" predicate exactly (`rest-api/app/routes/admin.py`'s
 * `integrity_check`) — an assigned Person for whom this returns false
 * collapses into the shared "Other" identity below, the same one that
 * integrity check surfaces individually and by name.
 */
export function buildResourceWorkloadRows(
  tasks: TaskRecord[],
  projectsById: Map<number, ProjectRecord>,
  resourceIdsByTask: Map<number, number[]>,
  isCurrentResource: (personId: number, teamId: number | undefined) => boolean,
  urgencyByTaskId: Map<number, number>,
  personName: (personId: number) => string,
): ResourceWorkloadRow[] {
  const total = newRow("total", "total", "Total");
  const other = newRow("other", "other", "Other");
  const unassigned = newRow("unassigned", "unassigned", "Unassigned");
  const personRows = new Map<number, ResourceWorkloadRow>();

  for (const task of tasks) {
    const teamId = projectsById.get(task.project_id)?.team_id;
    const resourcePersonIds = resourceIdsByTask.get(task.task_id) ?? [];

    // V1.2's own Task.TeamResources getter: an assigned Person who is no
    // longer a resource on this Task's own Team collapses into one shared
    // "Other" identity rather than being named individually (multiple
    // stale assignees on the *same* Task still only count once); a Task
    // with no assignees at all falls back to a single "Unassigned"
    // identity. `identities.size` is therefore always >= 1, standing in
    // for V1.2's own `task.TeamResources.Count`.
    const identities = new Set<string>();
    for (const personId of resourcePersonIds) {
      identities.add(isCurrentResource(personId, teamId) ? `person:${personId}` : "other");
    }
    if (identities.size === 0) identities.add("unassigned");

    const resourceCount = identities.size;
    const effortPerIdentity = computeResourceEffortDays(task, resourceCount);
    const urgency = urgencyByTaskId.get(task.task_id) ?? 0;

    for (const identity of identities) {
      if (identity === "other") {
        addTaskData(other, effortPerIdentity, urgency, task.status);
      } else if (identity === "unassigned") {
        addTaskData(unassigned, effortPerIdentity, urgency, task.status);
      } else {
        const personId = Number(identity.slice("person:".length));
        let row = personRows.get(personId);
        if (!row) {
          row = newRow(identity, "person", personName(personId), personId);
          personRows.set(personId, row);
        }
        addTaskData(row, effortPerIdentity, urgency, task.status);
      }
    }

    // One entry per Task, not per identity — V1.2's own
    // `total.AddTaskData(effortPerResource * resourcesCount, ...)`.
    addTaskData(total, effortPerIdentity * resourceCount, urgency, task.status);
  }

  const result = [total, ...personRows.values()];
  if (other.taskCount > 0) result.push(other);
  if (unassigned.taskCount > 0) result.push(unassigned);

  // Total pinned last (deliberately the opposite end from V1.2's own
  // `ReportItemSort`, which pinned it first — this is this default,
  // *initial* order only; DashboardPage.tsx's own per-column
  // `getSortComparator` keeps it pinned last through any interactive
  // re-sort too, which a plain array order like this can't do on its own).
  // Everything else by descending average Urgency, ties broken
  // alphabetically for a stable order.
  return result.sort((a, b) => {
    if (a.kind === "total") return 1;
    if (b.kind === "total") return -1;
    const aAvg = resourceWorkloadAvgUrgency(a);
    const bAvg = resourceWorkloadAvgUrgency(b);
    if (aAvg !== bAvg) return bAvg - aAvg;
    return a.label.localeCompare(b.label);
  });
}

const TOTAL_ROW_KEY = "total";

/**
 * DashboardPage.tsx's own per-column `getSortComparator` (D1.4-105) — keeps
 * the "Total" row pinned to the bottom through an interactive
 * column-header sort too, not just `buildResourceWorkloadRows`'s own
 * initial order above (which a plain array order can't survive once the
 * user clicks a column header). `getSortComparator` (unlike a plain
 * `sortComparator`) takes the current sort direction as its own argument
 * and entirely replaces MUI's default behaviour for that column, rather
 * than having its result silently negated for a descending sort — that
 * negation is exactly what would undo a naive "Total always sorts last"
 * rule the moment the user reverses direction, so the direction has to be
 * handled explicitly here instead of relying on MUI's own default
 * negate-for-descending behaviour.
 */
export function pinTotalLast<V>(
  compareAscending: (v1: V, v2: V) => number,
): (sortDirection: GridSortDirection) => GridComparatorFn<V> {
  return (sortDirection) => (v1, v2, p1: GridSortCellParams<V>, p2: GridSortCellParams<V>) => {
    if (p1.id === TOTAL_ROW_KEY) return 1;
    if (p2.id === TOTAL_ROW_KEY) return -1;
    const base = compareAscending(v1, v2);
    return sortDirection === "desc" ? -base : base;
  };
}

const numericAscending = (v1: number, v2: number) => v1 - v2;
export const pinTotalLastNumeric = pinTotalLast(numericAscending);
