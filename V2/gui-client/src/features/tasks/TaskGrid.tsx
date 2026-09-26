import { useEffect, useMemo, useRef, useState } from "react";
import {
  GridActionsCellItem,
  useGridApiRef,
  type GridCellParams,
  type GridColDef,
  type GridRowParams,
} from "@mui/x-data-grid";
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
  type TeamRecord,
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
  formatDdMmmYy,
  getTaskSchedule,
  urgencyRowClassName,
  urgencyRowPaletteSx,
  type ScheduleGraph,
} from "../../lib/schedule";
import { READONLY_BG } from "../../components/DenseField";
import { formatApiError } from "../../lib/apiErrors";
import {
  CELL_FONT_WEIGHT,
  DENSE_ROW_HEIGHT,
  DenseDataGrid,
  DenseSingleSelectEditCell,
  HEADER_FONT_WEIGHT,
  measureTextWidth,
  useDenseGridColumns,
} from "../../components/DenseDataGrid";
import type { ColumnFilterState } from "../../components/GridColumnFilter";

// Description: capped at 2.5x the width of its own header text, rather
// than growing arbitrarily wide with long content. Ref URL: capped at
// roughly 12 characters of content width — "0" is the same reference
// glyph CSS's own `ch` unit is defined against for "how wide is N
// characters" in a proportional font. Detailed Description: capped at 2x
// its own header text width, the same shape as Description just with a
// different multiplier.
//
// Applied only to the *default* (computed) width, via `withFilter`'s own
// `widthCap` argument below — never as a hard `colDef.maxWidth`, which
// would also permanently block a user manually dragging either column
// wider afterwards (the actual bug reported when this was first tried as
// `maxWidth`). A column the user has already manually resized skips this
// entirely (`useDenseGridColumns`'s own `withFilter`).
//
// A function, not a module-level constant computed once at import time —
// that was tried first and is wrong for the same reason a truncation bug
// once was: `@fontsource/inter`'s actual font files (main.tsx) load over
// the network, asynchronously, and a module evaluates long before that can
// possibly have finished, so a constant computed then would be measuring
// against whatever fallback font the browser substitutes for Inter,
// forever — not a one-off first-render flash, an outright wrong constant.
function defaultMaxWidths(): Record<string, number> {
  return {
    description: measureTextWidth("Description", HEADER_FONT_WEIGHT) * 2.5,
    external_reference_url: measureTextWidth("0", CELL_FONT_WEIGHT) * 12,
    detailed_description: measureTextWidth("Detailed Description", HEADER_FONT_WEIGHT) * 2,
  };
}

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
  | "team_id"
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
  "team_id",
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

// The narrower, reordered column set for the Project GUI component's own
// embedded `TaskGrid` (`ProjectsGUIComponent.md` §2.1/§4.5, `Project.tsx`/
// `Projects.tsx`, not `AllTaskPage`) — no ID column (redundant once a Task
// is already reached by browsing into its own Project), and the columns
// a user most wants at a glance (Urgency/T/Description/Status/Resources/
// End Date/Effort/Effort Type/%Allocation/Task Type) moved to the front;
// every other column keeps DEFAULT_TASK_GRID_COLUMNS's own relative order
// after that, minus `project_id` (redundant here) and
// `detailed_description`. A fixed, same-for-every-user set/order for now —
// a user-configurable picker is deferred to Level 2
// (`Claude/Level2_Implementation/Scope.md`).
export const EMBEDDED_TASK_GRID_COLUMNS: TaskGridColumnKey[] = [
  "urgency",
  "tentative_resource_assignment",
  "description",
  "status",
  "resources",
  "end_date",
  "effort_in_days",
  "effort_type",
  "percentage_allocation",
  "task_type",
  "component_id",
  "priority",
  "start_date",
  "attachments",
  "remarks",
  "owner_person_id",
  "requestor_person_id",
  "date_added",
  "status_date",
  "external_reference_url",
];

// The Component GUI component's own embedded `TaskGrid` column set
// (`ComponentDetailPlan.md` §4.3, `D1.4-68`) — the inverse swap from
// `EMBEDDED_TASK_GRID_COLUMNS` above: a Component's own Tasks can belong to
// *any* Project (Task→Component is independent of Task→Project), so
// `project_id` is useful here rather than redundant, while `component_id`
// is now the one that's redundant inside that Component's own section.
// Same front-loaded ordering otherwise, `project_id` sitting where
// `component_id` used to in the tail.
export const COMPONENT_EMBEDDED_TASK_GRID_COLUMNS: TaskGridColumnKey[] = [
  "urgency",
  "tentative_resource_assignment",
  "description",
  "status",
  "resources",
  "end_date",
  "effort_in_days",
  "effort_type",
  "percentage_allocation",
  "task_type",
  "project_id",
  "priority",
  "start_date",
  "attachments",
  "remarks",
  "owner_person_id",
  "requestor_person_id",
  "date_added",
  "status_date",
  "external_reference_url",
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

export interface TaskGridProps {
  tasks: TaskRecord[];
  // Reference data this grid's own column value-getters/editors need,
  // handed down rather than fetched internally (TaskGrid is a controlled, data-in
  // component, TaskGridPlan.md §4.1 — it fetches nothing itself).
  projects: ProjectRecord[];
  components: ComponentRecord[];
  people: PersonRecord[];
  personRoles: PersonRoleRecord[];
  // Optional, defaulting to `[]` — unlike the reference data above, only
  // the "team_id" column (AllTaskPage.tsx's own DEFAULT_TASK_GRID_COLUMNS)
  // actually needs it; Project/Component's own embedded TaskGrid call
  // sites never request that column (redundant for the Project-embedded
  // one — every row shares the same Team already — and out of scope for
  // the Component-embedded one), so making this required would mean
  // plumbing it through ProjectDetailPage/ComponentDetailPage and their
  // own Project.tsx/Projects.tsx/Component.tsx/Components.tsx for no
  // actual benefit there.
  teams?: TeamRecord[];
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
  // Seeds DenseDataGrid's own `initiallyHiddenFields` — read once, at first
  // mount, same as `initialFilterState` above (AllTaskPage.tsx uses this to
  // start the "team_id" column hidden when every visible row shares one
  // Team, so it isn't gated on `defaultFilterReady`-style plumbing here).
  initiallyHiddenColumns?: TaskGridColumnKey[];
  defaultSort?: { field: string; sort: "asc" | "desc" };
  onRowDoubleClick?: (task: TaskRecord) => void;
  // D1.4-109 — AllTaskPage.tsx's own "View Gantt" button needs to know
  // exactly which Tasks currently pass this grid's own filters (its filter
  // state is otherwise fully internal, via useDenseGridColumns below) —
  // called only when the actual set of passing Task ids changes, not on
  // every render (`filteredTasks` is a fresh array reference each time
  // regardless), so a consumer that stores this in its own state doesn't
  // get caught in a render loop. Optional and unused by every other
  // TaskGrid call site (Project/Component Detail's own embedded grids).
  onFilteredTasksChange?: (tasks: TaskRecord[]) => void;
  // D1.4-118 — passed straight through to `DenseDataGrid`'s own prop of the
  // same name; see its doc comment. `false` (the default) for every embedded
  // call site (Project/Component's own nested grids, D1.4-59) — only
  // `AllTaskPage.tsx` opts in, since it alone gives this grid a real, bounded
  // parent height to fill.
  fillHeight?: boolean;
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
  teams = [],
  scheduleGraph,
  resourceIdsByTask,
  attachmentsCountByTask,
  remarksCountByTask,
  columns: columnKeys = DEFAULT_TASK_GRID_COLUMNS,
  showFilters = true,
  initialFilterState,
  initiallyHiddenColumns,
  defaultSort = { field: "urgency", sort: "desc" as const },
  onFilteredTasksChange,
  onRowDoubleClick,
  fillHeight = false,
}: TaskGridProps) {
  const { person } = useAuth();
  const updateTaskField = useUpdateTaskField();
  const deleteTask = useDeleteTask();
  const [snackbarError, setSnackbarError] = useState<string | null>(null);
  const apiRef = useGridApiRef();

  const {
    withFilter,
    getFilteredRows,
    fitColumnsToContent,
    filterVisible,
    setFilterVisible,
    resetFilters,
    onColumnResize,
  } = useDenseGridColumns<TaskRecord>({
    rows: tasks,
    getRowId: (row) => row.task_id,
    initialFilterState,
    showFilters,
    apiRef,
  });

  // D1.4-117 — Resources/Component/Project default to a width fit for the
  // widest value across every Task this grid *could* show (every Team-scoped
  // Task, `getCachedColumnContentWidth`'s own comment) rather than whatever
  // actually passes the *initial* filter — usually much narrower once a
  // Resources filter is already pre-applied (e.g. a non-Team-Lead's own
  // default-to-self filter, D-Win-15), reported as these three columns
  // opening far wider than their own visible content, only fixed by
  // double-clicking each label by hand. Runs once, on mount only (`[]`) —
  // matches "when the window is first opened," not a continuous re-fit that
  // would otherwise jump these columns around every time a filter or the
  // row set itself later changes.
  //
  // Deferred one frame (`requestAnimationFrame`), not called synchronously
  // from this effect — confirmed via logging that calling it immediately on
  // mount *does* correctly update the `columns` prop `<DataGrid>` receives
  // (the right, narrower `width` was there on the very next render), yet the
  // column stayed visually at its old, wider size regardless: this early,
  // before the grid's own internal column-sizing effects (and `apiRef`
  // itself, which `fitColumnsToContent` also now calls `setColumnWidth`
  // through directly) have settled, MUI DataGrid doesn't reliably pick up a
  // `columns`-prop width change the way it does once the grid's been live
  // for a while (a user's own later double-click-to-fit, going through that
  // exact same prop, works fine). One frame is enough for that settling to
  // finish; cancelled on unmount so a fast unmount can't fire it after the
  // fact.
  useEffect(() => {
    const frame = requestAnimationFrame(() => {
      fitColumnsToContent(["resources", "component_id", "project_id"]);
    });
    return () => cancelAnimationFrame(frame);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const projectsById = useMemo(() => byId(projects, "project_id"), [projects]);
  const componentsById = useMemo(() => byId(components, "component_id"), [components]);
  const teamsById = useMemo(() => byId(teams, "team_id"), [teams]);

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

  // The urgency-bucket palette itself is shared with Search's own results
  // grid (`lib/schedule.ts`'s `urgencyRowPaletteSx`, `SearchPlan.md`
  // D1.4-76) — only the two TaskGrid-specific classes (governed-cell grey,
  // the delete cell's own always-white fill) stay local here.
  const urgencyRowSx = useMemo(
    () => ({
      "& .task-grid-readonly-cell": { bgcolor: READONLY_BG },
      // No fill at all (not even the row's own urgency tint showing
      // through) — a plain trash icon sitting in its own cell, matching
      // the unfilled rename/delete/add-task icons on Project.tsx's rows.
      // "transparent" is the wrong value here even though it sounds
      // right: a transparent cell lets the row's own colour underneath
      // show straight through it; an opaque colour is what actually
      // *blocks* that tint from showing in this one cell.
      "& .task-grid-delete-cell": { bgcolor: "#fff" },
      ...urgencyRowPaletteSx(),
    }),
    [],
  );

  function rowClassName(row: TaskRecord): string {
    return urgencyRowClassName(row.priority, taskUrgency(row));
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
  const maxWidths = defaultMaxWidths();

  const allColumnDefs: Partial<Record<TaskGridColumnKey, GridColDef<TaskRecord>>> = {
    task_id: withFilter(
      { field: "task_id", headerName: "ID", align: "center" },
      (row) => [String(row.task_id)],
      "number",
    ),
    urgency: withFilter(
      {
        field: "urgency",
        headerName: "Urgency",
        align: "center",
        valueGetter: (_value, row) => taskUrgency(row),
      },
      (row) => [String(taskUrgency(row))],
      "number",
    ),
    resources: withFilter(
      {
        field: "resources",
        headerName: "Resources",
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
          type: "singleSelect",
          valueOptions: ({ row }) =>
            row ? [...editableTaskStatusValues(person, projectTeamId(row), row.owner_person_id)] : [],
          renderEditCell: DenseSingleSelectEditCell,
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
      { field: "description", headerName: "Description" },
      (row) => [row.description ?? ""],
      "string",
      maxWidths.description,
    ),
    component_id: withFilter(
      {
        field: "component_id",
        headerName: "Component",
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
        valueGetter: (_value, row) => projectsById.get(row.project_id)?.name ?? row.project_id,
      },
      (row) => [projectsById.get(row.project_id)?.name ?? String(row.project_id)],
      "string",
    ),
    // A Task has no team_id of its own — resolved transitively via its own
    // Project's team_id, the same indirection projectTeamId() already uses
    // for permission checks elsewhere in this file.
    team_id: withFilter(
      {
        field: "team_id",
        headerName: "Team",
        valueGetter: (_value, row) => teamsById.get(projectTeamId(row) ?? -1)?.name ?? "",
      },
      (row) => [teamsById.get(projectTeamId(row) ?? -1)?.name ?? ""],
      "string",
    ),
    priority: governed(
      withFilter(
        {
          field: "priority",
          headerName: "Priority",
          type: "singleSelect",
          valueOptions: [...PRIORITY_LEVELS],
          renderEditCell: DenseSingleSelectEditCell,
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
        align: "center",
        valueGetter: (_value, row) => formatDdMmmYy(getTaskSchedule(scheduleGraph, row.task_id).endDate),
      },
      (row) => [formatDdMmmYy(getTaskSchedule(scheduleGraph, row.task_id).endDate)],
      "date",
    ),
    start_date: withFilter(
      {
        field: "start_date",
        headerName: "Planned Start",
        align: "center",
        valueGetter: (_value, row) => formatDdMmmYy(getTaskSchedule(scheduleGraph, row.task_id).startDate),
      },
      (row) => [formatDdMmmYy(getTaskSchedule(scheduleGraph, row.task_id).startDate)],
      "date",
    ),
    attachments: withFilter(
      {
        field: "attachments",
        headerName: "Attachments",
        align: "center",
        valueGetter: (_value, row) => attachmentsCountByTask.get(row.task_id) ?? 0,
      },
      (row) => [String(attachmentsCountByTask.get(row.task_id) ?? 0)],
      "number",
    ),
    remarks: withFilter(
      {
        field: "remarks",
        headerName: "Remarks",
        align: "center",
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
          renderEditCell: DenseSingleSelectEditCell,
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
          type: "singleSelect",
          valueOptions: ({ row }) => [
            { value: null, label: "(none)" },
            ...activePeople.map((p) => ({
              value: p.person_id,
              label: personDisplayName(p.person_id, row ? projectTeamId(row) : undefined, people, personRoles),
            })),
          ],
          renderEditCell: DenseSingleSelectEditCell,
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
        align: "center",
        valueGetter: (_value, row) => formatDdMmmYy(new Date(row.date_added)),
      },
      (row) => [formatDdMmmYy(new Date(row.date_added))],
      "date",
    ),
    effort_in_days: governed(
      withFilter(
        { field: "effort_in_days", headerName: "Effort", align: "center", type: "number" },
        (row) => [row.effort_in_days != null ? String(row.effort_in_days) : ""],
        "number",
      ),
      "effort_in_days",
    ),
    // Deliberately never governed (TaskGridPlan.md §4.4) — changing this can
    // have large knock-on effects on what Effort itself means, so it stays a
    // Task-Detail-only edit.
    effort_type: withFilter(
      { field: "effort_type", headerName: "Effort Type" },
      (row) => [row.effort_type ?? ""],
      "string",
    ),
    percentage_allocation: governed(
      withFilter(
        {
          field: "percentage_allocation",
          headerName: "% Allocation",
          align: "center",
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
        {
          field: "task_type",
          headerName: "Task Type",
          type: "singleSelect",
          valueOptions: [...TASK_TYPES],
          renderEditCell: DenseSingleSelectEditCell,
        },
        (row) => [row.task_type ?? ""],
        "string",
      ),
      "task_type",
    ),
    status_date: withFilter(
      {
        field: "status_date",
        headerName: "Status Date",
        align: "center",
        valueGetter: (_value, row) => (row.status_date ? formatDdMmmYy(new Date(row.status_date)) : ""),
      },
      (row) => [row.status_date ? formatDdMmmYy(new Date(row.status_date)) : ""],
      "date",
    ),
    external_reference_url: withFilter(
      { field: "external_reference_url", headerName: "Ref URL" },
      (row) => [row.external_reference_url ?? ""],
      "string",
      maxWidths.external_reference_url,
    ),
    detailed_description: governed(
      withFilter(
        { field: "detailed_description", headerName: "Detailed Description" },
        (row) => [row.detailed_description ?? ""],
        "string",
        maxWidths.detailed_description,
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
  // §4.10/D1.4-53), matching how Project.tsx's own trash icon is
  // gated purely on permission, never a prop.
  const deleteColumn: GridColDef<TaskRecord> = {
    field: "__delete",
    type: "actions",
    headerName: "",
    // The "actions" column type inherits GRID_STRING_COL_DEF's own
    // minWidth: 50 (@mui/x-data-grid's gridActionsColDef.js) — an
    // unmatched `width` alone is silently clamped back up to 50, which is
    // what was actually making this cell wider than it looked like it
    // should be; minWidth/maxWidth both have to be pinned to the same
    // value as width for the clamp to land on it exactly.
    width: DENSE_ROW_HEIGHT,
    minWidth: DENSE_ROW_HEIGHT,
    maxWidth: DENSE_ROW_HEIGHT,
    cellClassName: "task-grid-delete-cell",
    sortable: false,
    filterable: false,
    hideSortIcons: true,
    // D1.4-99/D1.4-100 — always rendered now (previously an empty array
    // when !canDeleteRow, matching this app's older convention); a row you
    // can't delete instead shows a lighter-grey, genuinely disabled icon —
    // clicking it is a real no-op (`onClick` never fires), not just
    // discouraged — the same "always present, disabled when not
    // applicable" treatment TeamsManagementPage.tsx's own Delete icon uses
    // (D1.4-98), for a consistent look across every grid's own actions
    // column rather than some rows having the icon and some not. Same two
    // colours as TeamsManagementPage.tsx/ManagePeoplePage.tsx's own Delete
    // icons too (D1.4-100) — this cell is the whole point of its own
    // narrow column, not a secondary affordance beside other content,
    // superseding the older, lighter rgba(0,0,0,0.28) this column used
    // when it could only ever be "shown" or "not shown," never "shown but
    // disabled."
    getActions: (params: GridRowParams<TaskRecord>) => {
      const deletable = canDeleteRow(params.row);
      return [
        <GridActionsCellItem
          key="delete"
          icon={<DeleteIcon fontSize="inherit" sx={{ color: deletable ? "rgba(0,0,0,0.87)" : "rgba(0,0,0,0.18)" }} />}
          label="Delete"
          disabled={!deletable}
          onClick={deletable ? () => handleDeleteTask(params.row) : undefined}
        />,
      ];
    },
  };

  const columns: GridColDef<TaskRecord>[] = [
    deleteColumn,
    ...effectiveColumnKeys.map((key) => allColumnDefs[key]).filter((c): c is GridColDef<TaskRecord> => !!c),
  ];

  // Only safe to call now — every `withFilter` call above (building
  // `allColumnDefs`) has already registered its own column's filter, and
  // `getFilteredRows` reads all of them.
  const filteredTasks = getFilteredRows();

  // D1.4-109 — `filteredTasks` is a fresh array every render regardless of
  // whether the actual set of passing Tasks changed; comparing against the
  // last id-set actually *sent* (not just re-running on every render) is
  // what keeps a consumer that stores this in its own state from looping.
  const filteredTaskIdsKey = filteredTasks.map((t) => t.task_id).join(",");
  const lastSentFilteredKeyRef = useRef<string | null>(null);
  useEffect(() => {
    if (!onFilteredTasksChange) return;
    if (lastSentFilteredKeyRef.current === filteredTaskIdsKey) return;
    lastSentFilteredKeyRef.current = filteredTaskIdsKey;
    onFilteredTasksChange(filteredTasks);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filteredTaskIdsKey, onFilteredTasksChange]);

  function isEditableCell(row: TaskRecord, field: string): boolean {
    return GOVERNED_FIELDS.has(field as EditableTaskField) && canEditCell(row, field as EditableTaskField);
  }

  function openTask(row: TaskRecord) {
    if (onRowDoubleClick) onRowDoubleClick(row);
    else openItemWindow("tasks", row.task_id);
  }

  return (
    <>
      <DenseDataGrid<TaskRecord>
        apiRef={apiRef}
        rows={filteredTasks}
        columns={columns}
        getRowId={(row) => row.task_id}
        onRowDoubleClick={openTask}
        onColumnResize={onColumnResize}
        getRowClassName={rowClassName}
        sx={urgencyRowSx}
        defaultSort={[defaultSort]}
        initiallyHiddenFields={initiallyHiddenColumns}
        fillHeight={fillHeight}
        filtering={{
          filterVisible,
          onToggleFilterVisible: () => setFilterVisible((prev) => !prev),
          onResetFilters: resetFilters,
        }}
        processRowUpdate={processRowUpdate}
        onProcessRowUpdateError={(err) => setSnackbarError(formatApiError(err, "please try again."))}
        isCellEditable={(params: GridCellParams<TaskRecord>) => {
          if (!GOVERNED_FIELDS.has(params.field as EditableTaskField)) return false;
          return canEditCell(params.row, params.field as EditableTaskField);
        }}
        // Single click, not the DataGrid default of double click, starts
        // editing a governed cell the current user can edit — this alone
        // is what makes a single click sufficient; double-click keeps its
        // own, unconditional job of opening Task Detail, exactly as it
        // does for every other, non-editable cell/column. The two are
        // independent gestures reaching independent handlers, not one
        // suppressing the other: double-clicking an editable cell starts
        // an edit (from the first click) *and* opens Task Detail (from
        // the double-click itself) — matching how every other column's
        // double-click has always worked, rather than special-casing
        // editable columns to swallow it.
        onCellClick={(params) => {
          if (!isEditableCell(params.row, params.field)) return;
          if (apiRef.current.getCellMode(params.id, params.field) === "edit") return;
          apiRef.current.startCellEditMode({ id: params.id, field: params.field });
        }}
      />
      <Snackbar open={!!snackbarError} autoHideDuration={6000} onClose={() => setSnackbarError(null)}>
        <Alert severity="error" onClose={() => setSnackbarError(null)}>
          {snackbarError}
        </Alert>
      </Snackbar>
    </>
  );
}
