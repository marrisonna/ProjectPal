import { useEffect, useMemo, useRef, useState } from "react";
import { DataGrid, type GridColDef } from "@mui/x-data-grid";
import Box from "@mui/material/Box";
import {
  useAllAttachments,
  useAllDependencies,
  useAllRemarks,
  useAllTaskResources,
  useComponents,
  usePeople,
  usePersonRoles,
  useProjects,
  useTasks,
} from "../../api/hooks";
import { TASK_STATUSES, type ProjectRecord, type TaskRecord } from "../../api/types";
import { openItemWindow, useSingletonWindowIdentity } from "../../lib/windowNav";
import { useDocumentTitle } from "../../lib/useDocumentTitle";
import { personDisplayName } from "../../lib/people";
import { isTeamLeadOfAnyTeam } from "../../lib/permissions";
import { useAuth } from "../../auth/AuthContext";
import { computeDuration, computeEndDate, computeStartDate, formatDdMmmYy } from "../../lib/schedule";
import { DENSE_FONT_SIZE } from "../../theme/theme";
import {
  columnFilterPasses,
  EMPTY_COLUMN_FILTER,
  FilterableHeader,
  sortFilterOptions,
  type ColumnFilterState,
  type FilterSortType,
} from "../../components/GridColumnFilter";

// Row height matches DenseField.tsx's edit-box height (22px) — the same
// "dense, WinForms-like" density Task Detail already uses, driven off the
// same shared DENSE_FONT_SIZE for the text itself, rather than the grid
// quietly being a different, more spacious density from every other screen
// in the app (D-Win-11). Header height fits the two-row title+filter
// layout every column now has (D-Win-14).
//
// Deliberately no `density="compact"` prop (D-Win-14 follow-up): DataGrid
// multiplies whatever rowHeight/columnHeaderHeight is *given* by a further
// density factor (0.7 for "compact" — `useGridDimensions.js`'s
// `Math.floor(props.columnHeaderHeight * density)`), so with it set, the
// 22px asked for here was silently rendering at 15px the whole time
// (44 at 30px) — never actually verified against a real measurement until
// the header-filter row's own layout problems prompted checking. These two
// constants are already the exact literal pixel values wanted, so no
// density multiplier should touch them at all.
const DENSE_ROW_HEIGHT = 22;
const HEADER_HEIGHT = 48;

function byId<T extends Record<K, number>, K extends string>(
  records: T[] | undefined,
  key: K,
): Map<number, T> {
  const map = new Map<number, T>();
  for (const r of records ?? []) map.set(r[key], r);
  return map;
}

interface FilterConfig {
  getValues: (row: TaskRecord) => string[];
  sortType: FilterSortType;
}

// V1.2's own Task List column set and order, unchanged, from
// V1.2/Apps/ProjectPal/ProjectPal/Tasks/GUITaskColumns.cs's ColumnNames —
// the one column dropped is Private ("Prvt"), since the Private/Visibility
// flag itself was never carried into V2's domain model (DomainModel.md).
// Urgency isn't computed yet (Stage 3, D1.2-2) — shown as a fixed 100 for
// every row so the column, and its position in this order, can be
// previewed now rather than waiting for the real calculation.
export function TaskListPage() {
  useDocumentTitle("All Tasks");
  // Claims the "tasks-list" identity for whichever window this page is
  // rendered in — including the main app window, reached here by a plain
  // in-place nav click, not window.open — so openListWindow() elsewhere
  // can find and focus it instead of opening a duplicate (D-Win-4/10).
  useSingletonWindowIdentity("tasks-list");
  const { person } = useAuth();
  const { data: tasks, isLoading } = useTasks();
  const { data: projects } = useProjects();
  const { data: components } = useComponents();
  const { data: people } = usePeople();
  const { data: personRoles } = usePersonRoles();
  const { data: allDependencies } = useAllDependencies();
  const { data: allTaskResources } = useAllTaskResources();
  const { data: allRemarks } = useAllRemarks();
  const { data: allAttachments } = useAllAttachments();

  // A person can be TeamLeadUser on one Team and not another; this screen
  // spans every Team at once, so — like V1.2's own equivalent check
  // (`Permissions.IsSuperUser`, a single global flag, since V1.2 had no
  // per-Team roles at all) — "is a Team Lead" here means on *some* Team,
  // not any one Team in particular (D-Win-15).
  const isTeamLead = isTeamLeadOfAnyTeam(person);

  // Per-column filter state (D-Win-14) — text-contains and/or an
  // exact-match checklist selection, combined with AND both within a
  // column and across every column, mirroring V1.2's own
  // GridControl.ApplyFiltersToRows. Status defaults to every value except
  // Closed/Cancelled for every user (D-Win-16, matching V1.2's own
  // `TaskWindow.cs` constructor: `StatusFilterValues` lists the same five
  // non-terminal statuses, applied via `SetDefaultFilterValues`) — a plain
  // lazy initial value, not an effect, since `TASK_STATUSES` is a static
  // list rather than data that has to load first.
  const [filterState, setFilterState] = useState<Record<string, ColumnFilterState>>(() => ({
    status: {
      contains: "",
      exact: new Set(TASK_STATUSES.filter((s) => s !== "Cancelled" && s !== "Closed")),
    },
  }));

  // A non-Team-Lead's own Tasks are what they came here for; a Team Lead
  // presumably wants to see their whole Team's work by default. Applied
  // once — when both who's logged in and their own displayed name are
  // known — as if the Resources checklist's own OK button had been
  // pressed with only that one name checked (D-Win-15, mirroring V1.2's
  // MainWindow.cs buttonShowTasks_Click). Never re-applied after that, so
  // it doesn't fight a filter the user has since changed themselves.
  const appliedDefaultResourceFilter = useRef(false);
  useEffect(() => {
    if (appliedDefaultResourceFilter.current) return;
    if (!person || !people) return;
    appliedDefaultResourceFilter.current = true;
    if (isTeamLead) return;
    const ownName = personDisplayName(person.person_id, undefined, people, personRoles);
    setFilterState((prev) => ({
      ...prev,
      resources: { contains: "", exact: new Set([ownName]) },
    }));
  }, [person, people, personRoles, isTeamLead]);

  function setContains(field: string, contains: string) {
    setFilterState((prev) => ({
      ...prev,
      [field]: { contains, exact: (prev[field] ?? EMPTY_COLUMN_FILTER).exact },
    }));
  }

  function setExact(field: string, exact: Set<string> | null) {
    setFilterState((prev) => ({
      ...prev,
      [field]: { contains: (prev[field] ?? EMPTY_COLUMN_FILTER).contains, exact },
    }));
  }

  const projectsById = useMemo(() => byId(projects, "project_id"), [projects]);
  const componentsById = useMemo(() => byId(components, "component_id"), [components]);

  const resourceIdsByTask = useMemo(() => {
    const map = new Map<number, number[]>();
    for (const r of allTaskResources ?? []) {
      const list = map.get(r.task_id);
      if (list) list.push(r.person_id);
      else map.set(r.task_id, [r.person_id]);
    }
    return map;
  }, [allTaskResources]);

  const predecessorsByTask = useMemo(() => {
    const map = new Map<number, typeof allDependencies>();
    for (const dep of allDependencies ?? []) {
      if (dep.post_task_id == null) continue;
      const list = map.get(dep.post_task_id);
      if (list) list.push(dep);
      else map.set(dep.post_task_id, [dep]);
    }
    return map;
  }, [allDependencies]);

  const remarksCountByTask = useMemo(() => {
    const map = new Map<number, number>();
    for (const r of allRemarks ?? []) {
      if (r.task_id == null) continue;
      map.set(r.task_id, (map.get(r.task_id) ?? 0) + 1);
    }
    return map;
  }, [allRemarks]);

  const attachmentsCountByTask = useMemo(() => {
    const map = new Map<number, number>();
    for (const a of allAttachments ?? []) {
      if (a.task_id == null) continue;
      map.set(a.task_id, (map.get(a.task_id) ?? 0) + 1);
    }
    return map;
  }, [allAttachments]);

  // Planned Start/End Date computed once per Task here, not separately in
  // each column's own valueGetter — computeStartDate/computeEndDate is the
  // exact same pipeline TaskDetailPage.tsx uses for one Task at a time
  // (lib/schedule.ts), just run for every row; End Date needs Start
  // Date's own result, so this avoids computing Start Date twice.
  const scheduleByTask = useMemo(() => {
    const map = new Map<number, { startDate: Date | null; endDate: Date | null }>();
    for (const task of tasks ?? []) {
      const project = projectsById.get(task.project_id);
      const predecessors = predecessorsByTask.get(task.task_id) ?? [];
      const startDate = computeStartDate(task, project, predecessors, tasks ?? [], projects ?? []);
      const resourceCount = resourceIdsByTask.get(task.task_id)?.length ?? 0;
      const duration = computeDuration(task, resourceCount);
      const endDate = computeEndDate(startDate, duration);
      map.set(task.task_id, { startDate, endDate });
    }
    return map;
  }, [tasks, projects, projectsById, predecessorsByTask, resourceIdsByTask]);

  function personName(personId: number | null, project: ProjectRecord | undefined): string {
    if (personId == null) return "—";
    return personDisplayName(personId, project?.team_id, people, personRoles);
  }

  // Populated below as each column is built (withFilter), then used by
  // getOptionsForField/passesAllFilters — safe because both are only ever
  // *called* later, from event handlers, by which point this render's
  // columns array (and so this object) is already fully built.
  const filterConfigs: Record<string, FilterConfig> = {};

  function passesAllFilters(row: TaskRecord, excludeField?: string): boolean {
    for (const field of Object.keys(filterConfigs)) {
      if (field === excludeField) continue;
      const config = filterConfigs[field];
      const state = filterState[field] ?? EMPTY_COLUMN_FILTER;
      if (!columnFilterPasses(config.getValues(row), state)) return false;
    }
    return true;
  }

  // The checklist popup's own value list is scoped to what every *other*
  // column's active filter currently leaves visible — not the full,
  // unfiltered Task set V1.2's equivalent actually (if unintentionally)
  // uses (D-Win-14) — so picking a value in one column narrows what
  // another column's own list offers next.
  function getOptionsForField(field: string): string[] {
    const config = filterConfigs[field];
    if (!config) return [];
    const values = new Set<string>();
    for (const row of tasks ?? []) {
      if (!passesAllFilters(row, field)) continue;
      for (const v of config.getValues(row)) values.add(v);
    }
    return sortFilterOptions([...values], config.sortType);
  }

  function withFilter(
    col: GridColDef<TaskRecord>,
    getValues: (row: TaskRecord) => string[],
    sortType: FilterSortType,
  ): GridColDef<TaskRecord> {
    filterConfigs[col.field] = { getValues, sortType };
    return {
      ...col,
      // MUI's own sort-icon slot is `width:0;visibility:hidden` normally
      // and genuinely widens on hover/when sorted (confirmed by reading
      // `GridIconButtonContainer.js`), which pushed this column's own
      // filter text box and button sideways every time the cursor merely
      // passed near the header (D-Win-14 follow-up). Hiding it removes
      // the only thing that was ever moving — clicking the title still
      // sorts (that's a separate click handler, unaffected).
      hideSortIcons: true,
      // `params.colDef.computedWidth` (the live column width — the same
      // one that grows/shrinks as the user drags the column's own resize
      // handle), passed down as an explicit pixel width rather than left
      // as "100%": DataGrid's own `.MuiDataGrid-columnHeaderTitleContainerContent`
      // (the direct parent this renders into) has no width or flex-basis
      // of its own, so a percentage-width child creates a genuine circular
      // sizing dependency — parent sized by content, content sized by
      // percentage-of-parent — which the browser resolves once at first
      // layout and then never revisits, freezing the filter row at
      // whatever width it happened to get on the very first resize.
      // Sizing from the actual number instead breaks that circularity
      // outright.
      renderHeader: (params) => (
        <FilterableHeader
          label={col.headerName ?? col.field}
          state={filterState[col.field] ?? EMPTY_COLUMN_FILTER}
          onContainsChange={(text) => setContains(col.field, text)}
          onExactChange={(exact) => setExact(col.field, exact)}
          getOptions={() => getOptionsForField(col.field)}
          columnWidth={params.colDef.computedWidth}
        />
      ),
    };
  }

  // Order matches V1.2's actual runtime order for this exact screen, not
  // GUITaskColumns.ColumnNames' own declared order — `Tasks/TaskWindow.cs`
  // (the real All Tasks window; `GUITaskColumns.ColumnNames`' declared
  // order is only what's *left over* after that) calls `SetColumnOrder`
  // with its own 14-column prefix, and `GridControl.SetUpGrid` puts those
  // first in the given order, then appends whatever ColumnNames still has
  // left, in ColumnNames' own order, skipping anything already placed
  // (D-Win-12 correction, `Claude/Requirements/UserInterfaceWindows.md`).
  const columns: GridColDef<TaskRecord>[] = [
    withFilter(
      { field: "task_id", headerName: "ID", width: 60 },
      (row) => [String(row.task_id)],
      "number",
    ),
    withFilter(
      {
        field: "urgency",
        headerName: "Urgency",
        width: 80,
        align: "right",
        headerAlign: "right",
        valueGetter: () => 100,
      },
      () => ["100"],
      "number",
    ),
    withFilter(
      {
        field: "resources",
        headerName: "Resources",
        width: 180,
        valueGetter: (_value, row) => {
          const project = projectsById.get(row.project_id);
          const ids = resourceIdsByTask.get(row.task_id) ?? [];
          return ids.map((id) => personName(id, project)).join(", ");
        },
      },
      (row) => {
        const project = projectsById.get(row.project_id);
        const ids = resourceIdsByTask.get(row.task_id) ?? [];
        return ids.length > 0 ? ids.map((id) => personName(id, project)) : [""];
      },
      "string",
    ),
    withFilter(
      { field: "status", headerName: "Status", width: 110 },
      (row) => [row.status ?? ""],
      "string",
    ),
    // Team-Lead-only (D-Win-15), matching V1.2's own
    // `GUITaskColumns.ColumnNames`' identical `IsSuperUser || IsPowerUser`
    // gate on this exact column — an ordinary team member has no use for
    // seeing which of *other* people's assignments are still tentative.
    ...(isTeamLead
      ? [
          withFilter(
            {
              field: "tentative_resource_assignment",
              headerName: "T",
              width: 36,
              align: "center",
              headerAlign: "center",
              valueGetter: (_value, row) => (row.tentative_resource_assignment ? "✓" : ""),
            },
            (row) => [row.tentative_resource_assignment ? "✓" : ""],
            "string",
          ),
        ]
      : []),
    withFilter(
      { field: "description", headerName: "Description", flex: 1, minWidth: 180 },
      (row) => [row.description ?? ""],
      "string",
    ),
    withFilter(
      {
        field: "component_id",
        headerName: "Component",
        width: 140,
        valueGetter: (_value, row) =>
          row.component_id != null ? (componentsById.get(row.component_id)?.name ?? "") : "",
      },
      (row) => [row.component_id != null ? (componentsById.get(row.component_id)?.name ?? "") : ""],
      "string",
    ),
    withFilter(
      {
        field: "project_id",
        headerName: "Project",
        width: 140,
        valueGetter: (_value, row) => projectsById.get(row.project_id)?.name ?? row.project_id,
      },
      (row) => [projectsById.get(row.project_id)?.name ?? String(row.project_id)],
      "string",
    ),
    withFilter(
      { field: "priority", headerName: "Priority", width: 90 },
      (row) => [row.priority ?? ""],
      "string",
    ),
    withFilter(
      {
        field: "end_date",
        headerName: "End Date",
        width: 100,
        valueGetter: (_value, row) => formatDdMmmYy(scheduleByTask.get(row.task_id)?.endDate ?? null),
      },
      (row) => [formatDdMmmYy(scheduleByTask.get(row.task_id)?.endDate ?? null)],
      "date",
    ),
    withFilter(
      {
        field: "start_date",
        headerName: "Planned Start",
        width: 100,
        valueGetter: (_value, row) => formatDdMmmYy(scheduleByTask.get(row.task_id)?.startDate ?? null),
      },
      (row) => [formatDdMmmYy(scheduleByTask.get(row.task_id)?.startDate ?? null)],
      "date",
    ),
    withFilter(
      {
        field: "attachments",
        headerName: "Attachments",
        width: 90,
        align: "right",
        headerAlign: "right",
        valueGetter: (_value, row) => attachmentsCountByTask.get(row.task_id) ?? 0,
      },
      (row) => [String(attachmentsCountByTask.get(row.task_id) ?? 0)],
      "number",
    ),
    withFilter(
      {
        field: "remarks",
        headerName: "Remarks",
        width: 80,
        align: "right",
        headerAlign: "right",
        valueGetter: (_value, row) => remarksCountByTask.get(row.task_id) ?? 0,
      },
      (row) => [String(remarksCountByTask.get(row.task_id) ?? 0)],
      "number",
    ),
    withFilter(
      {
        field: "owner_person_id",
        headerName: "Owner",
        width: 120,
        valueGetter: (_value, row) => personName(row.owner_person_id, projectsById.get(row.project_id)),
      },
      (row) => [personName(row.owner_person_id, projectsById.get(row.project_id))],
      "string",
    ),
    withFilter(
      {
        field: "requestor_person_id",
        headerName: "Requested By",
        width: 120,
        valueGetter: (_value, row) => personName(row.requestor_person_id, projectsById.get(row.project_id)),
      },
      (row) => [personName(row.requestor_person_id, projectsById.get(row.project_id))],
      "string",
    ),
    withFilter(
      {
        field: "date_added",
        headerName: "Date Added",
        width: 100,
        valueGetter: (_value, row) => formatDdMmmYy(new Date(row.date_added)),
      },
      (row) => [formatDdMmmYy(new Date(row.date_added))],
      "date",
    ),
    withFilter(
      { field: "effort_in_days", headerName: "Effort", width: 70, align: "right", headerAlign: "right" },
      (row) => [row.effort_in_days != null ? String(row.effort_in_days) : ""],
      "number",
    ),
    withFilter(
      { field: "effort_type", headerName: "Effort Type", width: 100 },
      (row) => [row.effort_type ?? ""],
      "string",
    ),
    withFilter(
      {
        field: "percentage_allocation",
        headerName: "% Allocation",
        width: 100,
        align: "right",
        headerAlign: "right",
        valueGetter: (_value, row) =>
          row.percentage_allocation != null ? `${Math.round(row.percentage_allocation * 100)}%` : "",
      },
      (row) => [
        row.percentage_allocation != null ? `${Math.round(row.percentage_allocation * 100)}%` : "",
      ],
      "number",
    ),
    withFilter(
      { field: "task_type", headerName: "Task Type", width: 130 },
      (row) => [row.task_type ?? ""],
      "string",
    ),
    withFilter(
      {
        field: "status_date",
        headerName: "Status Date",
        width: 100,
        valueGetter: (_value, row) => (row.status_date ? formatDdMmmYy(new Date(row.status_date)) : ""),
      },
      (row) => [row.status_date ? formatDdMmmYy(new Date(row.status_date)) : ""],
      "date",
    ),
    withFilter(
      { field: "orig_task_number", headerName: "Orig Task ID", width: 100 },
      (row) => [row.orig_task_number ?? ""],
      "string",
    ),
    withFilter(
      { field: "external_reference_url", headerName: "Ref URL", width: 160 },
      (row) => [row.external_reference_url ?? ""],
      "string",
    ),
    withFilter(
      { field: "detailed_description", headerName: "Detailed Description", width: 240 },
      (row) => [row.detailed_description ?? ""],
      "string",
    ),
  ];

  const filteredTasks = (tasks ?? []).filter((row) => passesAllFilters(row));

  return (
    // No heading here — the window's own title bar ("All Tasks", set by
    // useDocumentTitle above) already says what this is; a second, large
    // on-page label repeated the same information for no reason. No outer
    // padding either — AppShell's <main> already supplies a small, dense
    // margin to the window edge (D-Win-11).
    <Box sx={{ height: 600 }}>
      <DataGrid
        rows={filteredTasks}
        getRowId={(row) => row.task_id}
        columns={columns}
        loading={isLoading}
        // A new/re-focused singleton-per-object window, not in-place
        // navigation (D1.4-8's multi-window model).
        onRowDoubleClick={(params) => openItemWindow("tasks", params.id)}
        // No density="compact" — see this file's own top-of-file comment;
        // it would silently scale rowHeight/columnHeaderHeight below by a
        // further factor rather than rendering the literal pixel values
        // asked for.
        rowHeight={DENSE_ROW_HEIGHT}
        columnHeaderHeight={HEADER_HEIGHT}
        // Neither of MUI's own native per-column affordances is used here
        // — filtering is this page's own custom control (D-Win-14), and
        // the column menu (hide/pin/etc.) was never wired to anything —
        // so both are switched off outright, rather than merely unused:
        // each one's own icon slot is `width:0` until hover/sort, then
        // genuinely widens (confirmed by reading the installed package's
        // own `GridIconButtonContainer.js`), which was shoving this
        // column's own filter text box and button sideways every time the
        // cursor passed near a header. `hideSortIcons: true` on every
        // column (`withFilter`, above) closes the third such icon the
        // same way — sorting itself still works from clicking the title.
        disableColumnMenu
        disableColumnFilter
        // Fine vertical lines at every column edge, matching V1.2's own
        // WinForms DataGridView (which always draws cell gridlines) —
        // MUI's own defaults only draw a border on the *header* row's
        // columns, leaving the data rows below looking like a single
        // undivided block of text per row.
        showColumnVerticalBorder
        showCellVerticalBorder
        // V1.2 shows every Task at once, scrolling rather than paging
        // (D-Win-13). The free DataGrid Community tier this app uses can't
        // actually do that: `pagination` is hard-locked to `true`
        // internally, AND `pageSize` is hard-capped at 100 — DataGrid
        // *throws* (doesn't just warn) if given a larger pageSize, which
        // crashed this screen blank when first tried here (no error
        // boundary catches it). True unlimited/no-pagination scrolling is
        // a paid DataGridPro/Premium feature, deferred to Level 2
        // (`Claude/Level2_Implementation/Scope.md`) — for Level 1 this
        // uses the largest page size the MIT license allows, with the
        // pagination footer left visible so a Task list over 100 rows is
        // still reachable.
        // Default sort: Urgency descending (D-Win-16) — inert today since
        // every row's Urgency is still the fixed 100 placeholder (Stage 3,
        // D1.2-2 hasn't landed yet), but the ordering takes effect on its
        // own the moment real per-Task values do, with nothing further to
        // change here.
        initialState={{
          pagination: { paginationModel: { pageSize: 100 } },
          sorting: { sortModel: [{ field: "urgency", sort: "desc" }] },
        }}
        // V1.2's own grid toggles a clicked column between ascending and
        // descending only (WinForms DataGridView's default Automatic sort
        // mode) — MUI's own default cycle adds a third "unsorted" click;
        // this removes that third state to match.
        sortingOrder={["asc", "desc"]}
        sx={{
          fontSize: DENSE_FONT_SIZE,
          // Zeroed out here, not clawed back with a negative margin on the
          // filter row itself (an earlier attempt at this — pushing that
          // row's box out past its own parent to reach the column's true
          // edge worked geometrically, but the parent's own overflow:
          // hidden clips a border sitting exactly on that boundary, which
          // is what actually happened: the filter box/button's outer
          // borders vanished, per a follow-up screenshot). Removing the
          // padding at its source means the filter row's plain 100% width
          // already reaches the true edge with nothing left to clip.
          // FilterableHeader's own label re-adds this same 10px as its
          // own padding, since the title text still wants it.
          "& .MuiDataGrid-columnHeader": { paddingLeft: 0, paddingRight: 0 },
        }}
      />
    </Box>
  );
}
