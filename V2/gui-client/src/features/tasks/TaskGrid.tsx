import { useMemo, useState } from "react";
import {
  DataGrid,
  GridActionsCellItem,
  type GridCellParams,
  type GridColDef,
  type GridRowParams,
} from "@mui/x-data-grid";
import Box from "@mui/material/Box";
import Snackbar from "@mui/material/Snackbar";
import Alert from "@mui/material/Alert";
import DeleteIcon from "@mui/icons-material/Delete";
import { useDeleteTask, useUpdateTaskField } from "../../api/hooks";
import {
  PRIORITY_LEVELS,
  TASK_TYPES,
  type ComponentRecord,
  type PersonRecord,
  type PersonRoleRecord,
  type ProjectRecord,
  type TaskRecord,
} from "../../api/types";
import { openItemWindow } from "../../lib/windowNav";
import { personDisplayName } from "../../lib/people";
import {
  canEditOwnedRecord,
  canEditTaskField,
  editableTaskStatusValues,
  isTeamLeadOfAnyTeam,
  type EditableTaskField,
} from "../../lib/permissions";
import { useAuth } from "../../auth/AuthContext";
import {
  computeUrgency,
  computeUrgencyColour,
  formatDdMmmYy,
  getTaskSchedule,
  type ScheduleGraph,
} from "../../lib/schedule";
import { READONLY_BG } from "../../components/DenseField";
import { DENSE_FONT_SIZE } from "../../theme/theme";
import { formatApiError } from "../../lib/apiErrors";
import {
  columnFilterPasses,
  EMPTY_COLUMN_FILTER,
  FilterableHeader,
  sortFilterOptions,
  type ColumnFilterState,
  type FilterSortType,
} from "../../components/GridColumnFilter";

// Same dense, WinForms-like row/header sizing AllTaskOrigPage.tsx already
// uses — see that file's own top-of-file comment (D-Win-11/14) for why
// these exact pixel values, and why `density="compact"` is never used.
const DENSE_ROW_HEIGHT = 22;
const HEADER_HEIGHT = 48;

// The full column catalog (TaskGridPlan.md §4.2) — identical set, order,
// and value-getters to today's All Tasks. A window's own `columns` prop
// only ever picks a *subset* of this, in this order; it can never add a
// column that isn't here, and never decides editability (§2.3/§4.3).
export type TaskGridColumnKey =
  | "task_id"
  | "urgency"
  | "resources"
  | "status"
  | "tentative_resource_assignment"
  | "description"
  | "component_id"
  | "project_id"
  | "priority"
  | "end_date"
  | "start_date"
  | "attachments"
  | "remarks"
  | "owner_person_id"
  | "requestor_person_id"
  | "date_added"
  | "effort_in_days"
  | "effort_type"
  | "percentage_allocation"
  | "task_type"
  | "status_date"
  | "external_reference_url"
  | "detailed_description";

export const DEFAULT_TASK_GRID_COLUMNS: TaskGridColumnKey[] = [
  "task_id",
  "urgency",
  "resources",
  "status",
  "tentative_resource_assignment",
  "description",
  "component_id",
  "project_id",
  "priority",
  "end_date",
  "start_date",
  "attachments",
  "remarks",
  "owner_person_id",
  "requestor_person_id",
  "date_added",
  "effort_in_days",
  "effort_type",
  "percentage_allocation",
  "task_type",
  "status_date",
  "external_reference_url",
  "detailed_description",
];

// The 9 columns ever editable in a cell (TaskGridPlan.md §4.4, up from the
// original 8 once Detailed Description joined per D1.4-52) — Effort Type is
// deliberately excluded on purpose (large knock-on effects on what Effort
// itself means), and every other catalog column (computed fields, counts,
// system-set dates) is never editable from the grid without a further
// explicit decision.
const GOVERNED_FIELDS = new Set<EditableTaskField>([
  "status",
  "tentative_resource_assignment",
  "priority",
  "owner_person_id",
  "effort_in_days",
  "percentage_allocation",
  "task_type",
  "requestor_person_id",
  "detailed_description",
]);

interface FilterConfig {
  getValues: (row: TaskRecord) => string[];
  sortType: FilterSortType;
}

export interface TaskGridProps {
  tasks: TaskRecord[];
  // Reference data this grid's own column value-getters/editors need — the
  // same queries AllTaskOrigPage.tsx already fetches today, just handed
  // down instead of fetched internally (TaskGrid is a controlled, data-in
  // component, TaskGridPlan.md §4.1 — it fetches nothing itself).
  projects: ProjectRecord[];
  components: ComponentRecord[];
  people: PersonRecord[];
  personRoles: PersonRoleRecord[];
  scheduleGraph: ScheduleGraph;
  resourceIdsByTask: Map<number, number[]>;
  attachmentsCountByTask: Map<number, number>;
  remarksCountByTask: Map<number, number>;

  // Window-level configuration (§2.3) — "what can be seen here," never
  // "what's editable" (that's decided centrally below, regardless of what
  // a window asks for).
  columns?: TaskGridColumnKey[];
  showFilters?: boolean;
  initialFilterState?: Record<string, ColumnFilterState>;
  defaultSort?: { field: string; sort: "asc" | "desc" };
  onRowDoubleClick?: (task: TaskRecord) => void;
}

function byId<T extends Record<K, number>, K extends string>(
  records: T[],
  key: K,
): Map<number, T> {
  const map = new Map<number, T>();
  for (const r of records) map.set(r[key], r);
  return map;
}

export function TaskGrid({
  tasks,
  projects,
  components,
  people,
  personRoles,
  scheduleGraph,
  resourceIdsByTask,
  attachmentsCountByTask,
  remarksCountByTask,
  columns: columnKeys = DEFAULT_TASK_GRID_COLUMNS,
  showFilters = true,
  initialFilterState,
  defaultSort = { field: "urgency", sort: "desc" as const },
  onRowDoubleClick,
}: TaskGridProps) {
  const { person } = useAuth();
  const updateTaskField = useUpdateTaskField();
  const deleteTask = useDeleteTask();
  const [snackbarError, setSnackbarError] = useState<string | null>(null);

  const [filterState, setFilterState] = useState<Record<string, ColumnFilterState>>(
    () => initialFilterState ?? {},
  );

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

  function projectTeamId(row: TaskRecord): number | undefined {
    return projectsById.get(row.project_id)?.team_id;
  }

  function isAssignedResource(row: TaskRecord): boolean {
    if (!person) return false;
    return (resourceIdsByTask.get(row.task_id) ?? []).includes(person.person_id);
  }

  function canEditCell(row: TaskRecord, field: EditableTaskField): boolean {
    return canEditTaskField(
      person,
      projectTeamId(row),
      row.owner_person_id,
      isAssignedResource(row),
      field,
    );
  }

  function canDeleteRow(row: TaskRecord): boolean {
    return canEditOwnedRecord(person, projectTeamId(row), row.owner_person_id);
  }

  function personName(personId: number | null, project: ProjectRecord | undefined): string {
    if (personId == null) return "—";
    return personDisplayName(personId, project?.team_id, people, personRoles);
  }

  // Owner/Resources candidates: Team-scoped to the row's own Project (same
  // restriction Task Detail's own Owner/Resources fields already use,
  // D1.4-22) — a Person needs is_resource on that specific Team to be
  // offered, not merely to exist somewhere.
  function teamResourceCandidates(teamId: number | undefined): PersonRecord[] {
    const resourcePersonIds = new Set(
      personRoles.filter((pr) => pr.team_id === teamId && pr.is_resource).map((pr) => pr.person_id),
    );
    return people
      .filter((p) => p.is_active && resourcePersonIds.has(p.person_id))
      .sort((a, b) =>
        personDisplayName(a.person_id, teamId, people, personRoles).localeCompare(
          personDisplayName(b.person_id, teamId, people, personRoles),
        ),
      );
  }

  // Requestor: org-wide active People, not Team-scoped (matches Task
  // Detail's own Requestor field, D1.4-15).
  const activePeople = useMemo(
    () => people.filter((p) => p.is_active).sort((a, b) => a.name.localeCompare(b.name)),
    [people],
  );

  async function handleDeleteTask(task: TaskRecord) {
    if (!window.confirm(`Delete Task "${task.description}"? This cannot be undone.`)) return;
    try {
      await deleteTask.mutateAsync(task.task_id);
    } catch (err) {
      setSnackbarError(formatApiError(err, "please try again."));
    }
  }

  // Immediate per-cell save (TaskGridPlan.md §4.9/D1.4-49) — diffs the one
  // changed field against the row DataGrid had before this edit and PATCHes
  // just that field. A rejection is left to DataGrid's own
  // onProcessRowUpdateError (below), which reverts the cell to its prior
  // value — nothing to do here but let the throw propagate.
  async function processRowUpdate(newRow: TaskRecord, oldRow: TaskRecord): Promise<TaskRecord> {
    const changedField = (Object.keys(newRow) as (keyof TaskRecord)[]).find(
      (key) => newRow[key] !== oldRow[key],
    );
    if (!changedField) return oldRow;
    return updateTaskField.mutateAsync({
      taskId: oldRow.task_id,
      body: { [changedField]: newRow[changedField] },
    });
  }

  const scheduledUrgency = useMemo(() => {
    const map = new Map<number, number>();
    for (const task of tasks) {
      const { startDate, endDate } = getTaskSchedule(scheduleGraph, task.task_id);
      map.set(task.task_id, computeUrgency(task, projectsById, startDate, endDate));
    }
    return map;
  }, [tasks, scheduleGraph, projectsById]);

  function taskUrgency(row: TaskRecord): number {
    return scheduledUrgency.get(row.task_id) ?? 100;
  }

  // Same "precompute a palette of CSS classes" approach as
  // AllTaskOrigPage.tsx (see its own comment) — DataGrid has no per-row
  // inline-style hook, only discrete classes via getRowClassName.
  const urgencyRowSx = useMemo(() => {
    const sx: Record<string, { bgcolor: string }> = {
      "& .urgency-row-grey": { bgcolor: "rgb(190, 190, 190)" },
      "& .task-grid-readonly-cell": { bgcolor: READONLY_BG },
    };
    for (let m = 0; m <= 100; m++) {
      sx[`& .urgency-row-${m}`] = { bgcolor: computeUrgencyColour(100 + m) };
    }
    return sx;
  }, []);

  function urgencyRowClassName(row: TaskRecord): string {
    if (row.priority == null || row.priority === "Cancelled" || row.priority === "Closed") {
      return "urgency-row-grey";
    }
    const m = Math.max(0, Math.min(100, Math.trunc(taskUrgency(row)) - 100));
    return `urgency-row-${m}`;
  }

  // Populated below as each column is built (withFilter), then used by
  // getOptionsForField/passesAllFilters — safe because both are only ever
  // *called* later, from event handlers, by which point this render's
  // columns array (and so this object) is already fully built.
  const filterConfigs: Record<string, FilterConfig> = {};

  function passesAllFilters(row: TaskRecord, excludeField?: string): boolean {
    if (!showFilters) return true;
    for (const field of Object.keys(filterConfigs)) {
      if (field === excludeField) continue;
      const config = filterConfigs[field];
      const state = filterState[field] ?? EMPTY_COLUMN_FILTER;
      if (!columnFilterPasses(config.getValues(row), state)) return false;
    }
    return true;
  }

  function getOptionsForField(field: string): string[] {
    const config = filterConfigs[field];
    if (!config) return [];
    const values = new Set<string>();
    for (const row of tasks) {
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
    if (!showFilters) return { ...col, hideSortIcons: true };
    filterConfigs[col.field] = { getValues, sortType };
    return {
      ...col,
      hideSortIcons: true,
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

  // Governed columns (§4.3/§4.5): editable at the DataGrid level, with the
  // actual per-row/per-user decision made by isCellEditable (grid-wide,
  // below) and the visual grey treatment by cellClassName here — both call
  // canEditCell, never a window-supplied override.
  function governed(col: GridColDef<TaskRecord>, field: EditableTaskField): GridColDef<TaskRecord> {
    return {
      ...col,
      editable: true,
      cellClassName: (params: GridCellParams<TaskRecord>) =>
        canEditCell(params.row, field) ? "" : "task-grid-readonly-cell",
    };
  }

  const isTeamLead = isTeamLeadOfAnyTeam(person);

  const allColumnDefs: Partial<Record<TaskGridColumnKey, GridColDef<TaskRecord>>> = {
    task_id: withFilter({ field: "task_id", headerName: "ID", width: 60 }, (row) => [String(row.task_id)], "number"),
    urgency: withFilter(
      {
        field: "urgency",
        headerName: "Urgency",
        width: 80,
        align: "right",
        headerAlign: "right",
        valueGetter: (_value, row) => taskUrgency(row),
      },
      (row) => [String(taskUrgency(row))],
      "number",
    ),
    resources: withFilter(
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
    status: governed(
      withFilter(
        {
          field: "status",
          headerName: "Status",
          width: 110,
          type: "singleSelect",
          valueOptions: ({ row }) =>
            row ? [...editableTaskStatusValues(person, projectTeamId(row), row.owner_person_id)] : [],
        },
        (row) => [row.status ?? ""],
        "string",
      ),
      "status",
    ),
    // Team-Lead-only visibility (D-Win-15) — decided here, by TaskGrid
    // itself, regardless of whether an embedding window's own `columns`
    // prop lists this key (§4.3's own explicit note); see the filtering of
    // `effectiveColumnKeys` below.
    tentative_resource_assignment: governed(
      withFilter(
        {
          field: "tentative_resource_assignment",
          headerName: "T",
          width: 36,
          align: "center",
          headerAlign: "center",
          type: "boolean",
        },
        (row) => [row.tentative_resource_assignment ? "✓" : ""],
        "string",
      ),
      "tentative_resource_assignment",
    ),
    description: withFilter(
      { field: "description", headerName: "Description", flex: 1, minWidth: 180 },
      (row) => [row.description ?? ""],
      "string",
    ),
    component_id: withFilter(
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
    project_id: withFilter(
      {
        field: "project_id",
        headerName: "Project",
        width: 140,
        valueGetter: (_value, row) => projectsById.get(row.project_id)?.name ?? row.project_id,
      },
      (row) => [projectsById.get(row.project_id)?.name ?? String(row.project_id)],
      "string",
    ),
    priority: governed(
      withFilter(
        {
          field: "priority",
          headerName: "Priority",
          width: 90,
          type: "singleSelect",
          valueOptions: [...PRIORITY_LEVELS],
        },
        (row) => [row.priority ?? ""],
        "string",
      ),
      "priority",
    ),
    end_date: withFilter(
      {
        field: "end_date",
        headerName: "End Date",
        width: 100,
        valueGetter: (_value, row) => formatDdMmmYy(getTaskSchedule(scheduleGraph, row.task_id).endDate),
      },
      (row) => [formatDdMmmYy(getTaskSchedule(scheduleGraph, row.task_id).endDate)],
      "date",
    ),
    start_date: withFilter(
      {
        field: "start_date",
        headerName: "Planned Start",
        width: 100,
        valueGetter: (_value, row) => formatDdMmmYy(getTaskSchedule(scheduleGraph, row.task_id).startDate),
      },
      (row) => [formatDdMmmYy(getTaskSchedule(scheduleGraph, row.task_id).startDate)],
      "date",
    ),
    attachments: withFilter(
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
    remarks: withFilter(
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
    owner_person_id: governed(
      withFilter(
        {
          field: "owner_person_id",
          headerName: "Owner",
          width: 120,
          type: "singleSelect",
          valueOptions: ({ row }) => {
            if (!row) return [];
            const candidates = teamResourceCandidates(projectTeamId(row));
            return [
              { value: null, label: "(none)" },
              ...candidates.map((p) => ({
                value: p.person_id,
                label: personDisplayName(p.person_id, projectTeamId(row), people, personRoles),
              })),
            ];
          },
        },
        (row) => [personName(row.owner_person_id, projectsById.get(row.project_id))],
        "string",
      ),
      "owner_person_id",
    ),
    requestor_person_id: governed(
      withFilter(
        {
          field: "requestor_person_id",
          headerName: "Requested By",
          width: 120,
          type: "singleSelect",
          valueOptions: ({ row }) => [
            { value: null, label: "(none)" },
            ...activePeople.map((p) => ({
              value: p.person_id,
              label: personDisplayName(p.person_id, row ? projectTeamId(row) : undefined, people, personRoles),
            })),
          ],
        },
        (row) => [personName(row.requestor_person_id, projectsById.get(row.project_id))],
        "string",
      ),
      "requestor_person_id",
    ),
    date_added: withFilter(
      {
        field: "date_added",
        headerName: "Date Added",
        width: 100,
        valueGetter: (_value, row) => formatDdMmmYy(new Date(row.date_added)),
      },
      (row) => [formatDdMmmYy(new Date(row.date_added))],
      "date",
    ),
    effort_in_days: governed(
      withFilter(
        { field: "effort_in_days", headerName: "Effort", width: 70, align: "right", headerAlign: "right", type: "number" },
        (row) => [row.effort_in_days != null ? String(row.effort_in_days) : ""],
        "number",
      ),
      "effort_in_days",
    ),
    // Deliberately never governed (TaskGridPlan.md §4.4) — changing this can
    // have large knock-on effects on what Effort itself means, so it stays a
    // Task-Detail-only edit.
    effort_type: withFilter(
      { field: "effort_type", headerName: "Effort Type", width: 100 },
      (row) => [row.effort_type ?? ""],
      "string",
    ),
    percentage_allocation: governed(
      withFilter(
        {
          field: "percentage_allocation",
          headerName: "% Allocation",
          width: 100,
          align: "right",
          headerAlign: "right",
          type: "number",
          // Stored/edited as a fraction (1 = 100%), displayed/typed as a
          // whole percentage — the same transform Task Detail's own field
          // already does.
          valueGetter: (_value, row) =>
            row.percentage_allocation != null ? Math.round(row.percentage_allocation * 100) : null,
          valueSetter: (value, row) => ({
            ...row,
            percentage_allocation: value == null || value === "" ? null : Number(value) / 100,
          }),
        },
        (row) => [
          row.percentage_allocation != null ? `${Math.round(row.percentage_allocation * 100)}%` : "",
        ],
        "number",
      ),
      "percentage_allocation",
    ),
    task_type: governed(
      withFilter(
        { field: "task_type", headerName: "Task Type", width: 130, type: "singleSelect", valueOptions: [...TASK_TYPES] },
        (row) => [row.task_type ?? ""],
        "string",
      ),
      "task_type",
    ),
    status_date: withFilter(
      {
        field: "status_date",
        headerName: "Status Date",
        width: 100,
        valueGetter: (_value, row) => (row.status_date ? formatDdMmmYy(new Date(row.status_date)) : ""),
      },
      (row) => [row.status_date ? formatDdMmmYy(new Date(row.status_date)) : ""],
      "date",
    ),
    external_reference_url: withFilter(
      { field: "external_reference_url", headerName: "Ref URL", width: 160 },
      (row) => [row.external_reference_url ?? ""],
      "string",
    ),
    detailed_description: governed(
      withFilter(
        { field: "detailed_description", headerName: "Detailed Description", width: 240 },
        (row) => [row.detailed_description ?? ""],
        "string",
      ),
      "detailed_description",
    ),
  };

  const effectiveColumnKeys = columnKeys.filter(
    (key) => key !== "tentative_resource_assignment" || isTeamLead,
  );

  // A leading, always-present row-actions column — never part of a
  // window's own `columns` catalog (§4.1's "deliberately absent" note):
  // whether the trash icon shows at all is a per-row permission decision
  // (canDeleteRow), not something any window configures (TaskGridPlan.md
  // §4.10/D1.4-53), matching how ProjectTaskTree.tsx's own trash icon is
  // gated purely on permission, never a prop.
  const deleteColumn: GridColDef<TaskRecord> = {
    field: "__delete",
    type: "actions",
    headerName: "",
    width: 36,
    sortable: false,
    filterable: false,
    hideSortIcons: true,
    getActions: (params: GridRowParams<TaskRecord>) =>
      canDeleteRow(params.row)
        ? [
            <GridActionsCellItem
              key="delete"
              icon={<DeleteIcon fontSize="inherit" />}
              label="Delete"
              onClick={() => handleDeleteTask(params.row)}
            />,
          ]
        : [],
  };

  const columns: GridColDef<TaskRecord>[] = [
    deleteColumn,
    ...effectiveColumnKeys.map((key) => allColumnDefs[key]).filter((c): c is GridColDef<TaskRecord> => !!c),
  ];

  const filteredTasks = tasks.filter((row) => passesAllFilters(row));

  return (
    <Box sx={{ height: 600 }}>
      <DataGrid<TaskRecord>
        rows={filteredTasks}
        getRowId={(row) => row.task_id}
        columns={columns}
        onRowDoubleClick={(params) =>
          onRowDoubleClick ? onRowDoubleClick(params.row) : openItemWindow("tasks", params.id)
        }
        rowHeight={DENSE_ROW_HEIGHT}
        columnHeaderHeight={HEADER_HEIGHT}
        disableColumnMenu
        disableColumnFilter
        showColumnVerticalBorder
        showCellVerticalBorder
        processRowUpdate={processRowUpdate}
        onProcessRowUpdateError={(err) =>
          setSnackbarError(formatApiError(err, "please try again."))
        }
        isCellEditable={(params: GridCellParams<TaskRecord>) => {
          if (!GOVERNED_FIELDS.has(params.field as EditableTaskField)) return false;
          return canEditCell(params.row, params.field as EditableTaskField);
        }}
        initialState={{
          pagination: { paginationModel: { pageSize: 100 } },
          sorting: { sortModel: [defaultSort] },
        }}
        sortingOrder={["asc", "desc"]}
        getRowClassName={(params) => urgencyRowClassName(params.row)}
        sx={{
          fontSize: DENSE_FONT_SIZE,
          "& .MuiDataGrid-columnHeader": { paddingLeft: 0, paddingRight: 0 },
          ...urgencyRowSx,
        }}
      />
      <Snackbar
        open={!!snackbarError}
        autoHideDuration={6000}
        onClose={() => setSnackbarError(null)}
      >
        <Alert severity="error" onClose={() => setSnackbarError(null)}>
          {snackbarError}
        </Alert>
      </Snackbar>
    </Box>
  );
}
