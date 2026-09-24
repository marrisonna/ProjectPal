import { useMemo } from "react";
import Box from "@mui/material/Box";
import { useGridApiRef } from "@mui/x-data-grid";
import {
  useAllDependencies,
  useAllTaskResources,
  usePeople,
  usePersonRoles,
  useProjects,
  useTasks,
  useTeams,
} from "../../api/hooks";
import type { ProjectRecord, TaskRecord } from "../../api/types";
import { useAuth } from "../../auth/AuthContext";
import { DenseDataGrid, useDenseGridColumns } from "../../components/DenseDataGrid";
import { buildScheduleGraph, computeUrgency, getTaskSchedule, urgencyRowPaletteSx } from "../../lib/schedule";
import {
  buildResourceWorkloadRows,
  pinTotalLast,
  pinTotalLastNumeric,
  resourceWorkloadAvgUrgency,
  type ResourceWorkloadRow,
} from "../../lib/resourceWorkload";
import { useDocumentTitle } from "../../lib/useDocumentTitle";
import { openTasksForResource, useSingletonWindowIdentity } from "../../lib/windowNav";

// The same shared urgency-bucket palette a Task row uses
// (`lib/schedule.ts`'s `urgencyRowPaletteSx`/`urgencyRowClassName`) — not
// `urgencyRowClassName` itself, since its bucketing is gated on a Task's
// own Priority (grey for Cancelled/Closed/unset), a concept that doesn't
// apply to a Resource's *aggregate* row; the bucket math itself (an
// Urgency value clamped into the same 100-200 range) is identical.
function dashboardUrgencyRowClassName(avgUrgency: number): string {
  const m = Math.max(0, Math.min(100, Math.trunc(avgUrgency) - 100));
  return `urgency-row-${m}`;
}

// D1.4-104 — the MainWindow-equivalent landing dashboard (D1.4-39), a
// per-Resource workload report ported from V1.2's actual
// MainWindow.cs/GUIMainReportItem.cs (not just UserInterfaceWindows.md's
// own higher-level summary) — see that file for the exact source this
// mirrors. An in-place page like AllTaskPage.tsx (nav bar stays visible),
// not a popped-out singleton window like Search/Admin Tools/Team
// Management — V1.2's MainWindow was the app's *only* window and doubled
// as the nav hub; V2's AppShell already covers that duty, so this page's
// only remaining job is the report itself.
export function DashboardPage() {
  useDocumentTitle("Dashboard");
  useSingletonWindowIdentity("dashboard-list");
  const apiRef = useGridApiRef();
  const { person } = useAuth();

  const { data: tasks } = useTasks();
  const { data: projects } = useProjects();
  const { data: people } = usePeople();
  const { data: personRoles } = usePersonRoles();
  const { data: teams } = useTeams();
  const { data: allDependencies } = useAllDependencies();
  const { data: allTaskResources } = useAllTaskResources();

  const projectsById = useMemo(() => {
    const map = new Map<number, ProjectRecord>();
    for (const p of projects ?? []) map.set(p.project_id, p);
    return map;
  }, [projects]);

  const peopleById = useMemo(() => new Map((people ?? []).map((p) => [p.person_id, p])), [people]);

  // Every Team in scope for this viewer — every Team for an admin, only
  // their own for anyone else (D-Win-17, the same floor already
  // established for All Tasks/Search/Plan; no separate "Teams I lead"
  // notion introduced just for this page).
  const myTeamIds = useMemo(() => {
    if (person?.is_organisation_admin) return new Set((teams ?? []).map((t) => t.team_id));
    return new Set((person?.team_roles ?? []).map((tr) => tr.team_id));
  }, [person, teams]);

  // Whether a Person currently counts as a resource on a given Team —
  // exactly the predicate Admin Tools' own "stale resource assignment"
  // check uses (rest-api/app/routes/admin.py's integrity_check).
  const resourceTeamPairs = useMemo(() => {
    const set = new Set<string>();
    for (const pr of personRoles ?? []) {
      if (pr.is_resource) set.add(`${pr.person_id}:${pr.team_id}`);
    }
    return set;
  }, [personRoles]);

  const resourceIdsByTask = useMemo(() => {
    const map = new Map<number, number[]>();
    for (const r of allTaskResources ?? []) {
      const list = map.get(r.task_id);
      if (list) list.push(r.person_id);
      else map.set(r.task_id, [r.person_id]);
    }
    return map;
  }, [allTaskResources]);

  const resourceCountByTaskId = useMemo(() => {
    const map = new Map<number, number>();
    for (const [taskId, ids] of resourceIdsByTask) map.set(taskId, ids.length);
    return map;
  }, [resourceIdsByTask]);

  // Built from the *full*, unscoped Task/Project/Dependency set, same as
  // AllTaskPage.tsx/SearchPage.tsx — schedule/Urgency resolution needs the
  // whole dependency graph regardless of which rows this viewer ends up
  // seeing; only the report's own *rows* are Team-scoped, below.
  const scheduleGraph = useMemo(
    () => buildScheduleGraph(tasks ?? [], projects ?? [], allDependencies ?? [], resourceCountByTaskId),
    [tasks, projects, allDependencies, resourceCountByTaskId],
  );
  const scheduledUrgency = useMemo(() => {
    const map = new Map<number, number>();
    for (const task of tasks ?? []) {
      const { startDate, endDate } = getTaskSchedule(scheduleGraph, task.task_id);
      map.set(task.task_id, computeUrgency(task, projectsById, startDate, endDate));
    }
    return map;
  }, [tasks, scheduleGraph, projectsById]);

  // V1.2's own `task.Status == Cancelled || Closed => continue` — a Closed/
  // Cancelled Task contributes to no Resource's workload at all.
  const openTeamScopedTasks = useMemo(() => {
    return (tasks ?? []).filter((t: TaskRecord) => {
      if (t.status === "Cancelled" || t.status === "Closed") return false;
      return myTeamIds.has(projectsById.get(t.project_id)?.team_id ?? -1);
    });
  }, [tasks, projectsById, myTeamIds]);

  const rows = useMemo<ResourceWorkloadRow[]>(
    () =>
      buildResourceWorkloadRows(
        openTeamScopedTasks,
        projectsById,
        resourceIdsByTask,
        (personId, teamId) => resourceTeamPairs.has(`${personId}:${teamId}`),
        scheduledUrgency,
        (personId) => peopleById.get(personId)?.name ?? `Person #${personId}`,
      ),
    [openTeamScopedTasks, projectsById, resourceIdsByTask, resourceTeamPairs, scheduledUrgency, peopleById],
  );

  // Routed through the same `withFilter`/`filtering` machinery every other
  // grid in the app uses (TaskGrid.tsx, Teams Management, ...) — a plain
  // `GridColDef[]` with no `filtering` prop, as this grid used to be built,
  // renders with MUI's own default header chrome instead of
  // `FilterableHeader`'s (different font weight, no shaded background) and
  // loses the right-click "Reset All Filters"/"Show Filter" menu items
  // entirely, which is exactly what made this grid and Admin Tools' own
  // integrity-check grids (below) visibly different from every other one.
  const { withFilter, getFilteredRows, filterVisible, setFilterVisible, resetFilters, onColumnResize } =
    useDenseGridColumns<ResourceWorkloadRow>({
      rows,
      getRowId: (row) => row.key,
      apiRef,
    });

  const columns = [
    withFilter(
      { field: "label", headerName: "Resource", getSortComparator: pinTotalLast((v1: string, v2: string) => v1.localeCompare(v2)) },
      (row) => [row.label],
      "string",
      undefined,
      1,
    ),
    withFilter(
      { field: "taskCount", headerName: "Open Tasks", align: "center", type: "number", getSortComparator: pinTotalLastNumeric },
      (row) => [String(row.taskCount)],
      "number",
    ),
    withFilter(
      { field: "readyCount", headerName: "Ready", align: "center", type: "number", getSortComparator: pinTotalLastNumeric },
      (row) => [String(row.readyCount)],
      "number",
    ),
    withFilter(
      {
        field: "inProgressCount",
        headerName: "In Progress",
        align: "center",
        type: "number",
        getSortComparator: pinTotalLastNumeric,
      },
      (row) => [String(row.inProgressCount)],
      "number",
    ),
    withFilter(
      {
        field: "totalEffort",
        headerName: "Effort (days)",
        align: "center",
        type: "number",
        valueGetter: (_value, row) => Math.round(row.totalEffort * 10) / 10,
        getSortComparator: pinTotalLastNumeric,
      },
      (row) => [String(Math.round(row.totalEffort * 10) / 10)],
      "number",
    ),
    withFilter(
      {
        field: "maxUrgency",
        headerName: "Max Urgency",
        align: "center",
        type: "number",
        valueGetter: (_value, row) => Math.round(row.maxUrgency * 10) / 10,
        getSortComparator: pinTotalLastNumeric,
      },
      (row) => [String(Math.round(row.maxUrgency * 10) / 10)],
      "number",
    ),
    withFilter(
      {
        field: "avgUrgency",
        headerName: "Avg Urgency",
        align: "center",
        type: "number",
        valueGetter: (_value, row) => resourceWorkloadAvgUrgency(row),
        getSortComparator: pinTotalLastNumeric,
      },
      (row) => [String(resourceWorkloadAvgUrgency(row))],
      "number",
    ),
  ];

  // V1.2's own DoubleClickRow, opening a *separate* window rather than
  // navigating this one away from the Dashboard (D1.4-105) — one per
  // Resource, so double-clicking several rows in turn leaves several All
  // Tasks windows open side by side rather than replacing the same one
  // each time. A Person row opens the Task list filtered to them; "Total"
  // is a no-op (no single coherent filter for a grand total); "Unassigned"
  // filters to genuinely-unassigned Tasks — a small, deliberate fix over a
  // V1.2 quirk where that click filtered on the literal string
  // "Unassigned" and always showed nothing, since nothing is really ever
  // assigned to a Person by that name. "Other" (D1.4-106) filters to every
  // Task with at least one stale-resource assignment — AllTaskPage.tsx's
  // own `scopedTasks` narrows the row set directly for this one, rather
  // than trying to express it through the Resources column's by-name
  // filter, which has no concept of "stale" at all.
  function openFilteredTasks(row: ResourceWorkloadRow): void {
    if (row.kind === "person" && row.personId != null) {
      openTasksForResource(String(row.personId));
    } else if (row.kind === "unassigned") {
      openTasksForResource("unassigned");
    } else if (row.kind === "other") {
      openTasksForResource("other");
    }
  }

  if (!tasks || !projects || !people || !personRoles || !teams || !allDependencies || !allTaskResources) {
    return null;
  }

  if (myTeamIds.size === 0) {
    return <Box sx={{ p: 2, fontSize: 13 }}>You aren't a member of any Team yet.</Box>;
  }

  return (
    <Box sx={{ p: "6px", boxSizing: "border-box", height: "100%", display: "flex", flexDirection: "column" }}>
      <Box
        sx={{
          bgcolor: "#fff",
          border: "1px solid rgba(0,0,0,0.08)",
          borderRadius: "8px",
          boxShadow: "0 1px 3px rgba(0,0,0,0.06)",
          p: "12px",
          display: "flex",
          flexDirection: "column",
          gap: "10px",
        }}
      >
        <Box sx={{ fontSize: 14, fontWeight: 600 }}>Dashboard</Box>
        {rows.length <= 1 ? (
          <Box sx={{ fontSize: 13, color: "rgba(0,0,0,0.6)" }}>No open Tasks in your Team(s).</Box>
        ) : (
          <DenseDataGrid<ResourceWorkloadRow>
            apiRef={apiRef}
            rows={getFilteredRows()}
            columns={columns}
            getRowId={(row) => row.key}
            onRowDoubleClick={openFilteredTasks}
            onColumnResize={onColumnResize}
            // Highest Max Urgency first — "Total" still pinned last
            // regardless, via this column's own `getSortComparator`
            // (`pinTotalLastNumeric`, D1.4-105), which this default sort
            // routes through exactly the same way an interactive
            // column-header click would.
            defaultSort={[{ field: "maxUrgency", sort: "desc" }]}
            filtering={{
              filterVisible,
              onToggleFilterVisible: () => setFilterVisible((prev) => !prev),
              onResetFilters: resetFilters,
            }}
            getRowClassName={(row) => dashboardUrgencyRowClassName(resourceWorkloadAvgUrgency(row))}
            sx={urgencyRowPaletteSx()}
          />
        )}
      </Box>
    </Box>
  );
}
