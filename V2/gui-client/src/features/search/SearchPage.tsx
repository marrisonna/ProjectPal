import { useMemo, useState } from "react";
import Box from "@mui/material/Box";
import { useGridApiRef, type GridColDef } from "@mui/x-data-grid";
import {
  useAllAttachments,
  useAllDependencies,
  useAllRemarks,
  useAllTaskResources,
  useComponents,
  usePeople,
  usePersonRoles,
  useProjects,
  useSearch,
  useTasks,
  type SearchResultRecord,
} from "../../api/hooks";
import type { ComponentRecord, TaskRecord } from "../../api/types";
import { useAuth } from "../../auth/AuthContext";
import { DenseButton } from "../../components/DenseField";
import { DenseDataGrid, clickableCellSx, useDenseGridColumns } from "../../components/DenseDataGrid";
import { openItemWindow, useSingletonWindowIdentity } from "../../lib/windowNav";
import { useDocumentTitle } from "../../lib/useDocumentTitle";
import { personDisplayName } from "../../lib/people";
import {
  buildScheduleGraph,
  computeUrgency,
  formatDdMmmYy,
  getTaskSchedule,
  isProjectActive,
  urgencyRowClassName,
  urgencyRowPaletteSx,
} from "../../lib/schedule";
import { isTaskVisible } from "../projects/Projects";
import { DENSE_FONT_SIZE } from "../../theme/theme";

type SearchType = SearchResultRecord["type"];
const SEARCH_TYPES: SearchType[] = ["Task", "Project", "Component", "Remark", "Attachment"];

// A record with an owner discriminated the same way Remark/Attachment both
// are (SearchPlan.md §2.3) — structural, not imported from api/types, since
// both RemarkRecord and AttachmentRecord already satisfy this shape as-is.
interface OwnedRecord {
  task_id: number | null;
  project_id: number | null;
  component_id: number | null;
}

interface SearchDisplayRow {
  rowKey: string;
  type: SearchType;
  id: number;
  label: string;
  date: string;
  person: string;
  // Only set for a Task row — drives its urgency tint (D1.4-76); every
  // other type renders untinted (no Urgency concept for those types).
  taskPriority?: string | null;
  taskUrgency?: number;
}

// SearchPlan.md — global text search across Task/Project/Component/Remark/
// Attachment (metadata only), reachable at /search as its own popped-out
// singleton window (D1.4-8), matching Plan/Projects/Components.
export function SearchPage() {
  useDocumentTitle("Search");
  useSingletonWindowIdentity("search-list");
  const { person } = useAuth();
  // Shared between `useDenseGridColumns` (below) and `DenseDataGrid`'s own
  // `apiRef` prop — the double-click sort-preservation feature (D1.4-79)
  // subscribes directly to this grid instance's own event bus, not to a
  // prop `DenseDataGrid` exposes, so both ends need the *same* ref.
  const apiRef = useGridApiRef();

  const { data: tasks } = useTasks();
  const { data: projects } = useProjects();
  const { data: components } = useComponents();
  const { data: people } = usePeople();
  const { data: personRoles } = usePersonRoles();
  const { data: allDependencies } = useAllDependencies();
  const { data: allTaskResources } = useAllTaskResources();
  const { data: allRemarks } = useAllRemarks();
  const { data: allAttachments } = useAllAttachments();

  const [inputText, setInputText] = useState("");
  // The query actually sent to /search — only ever updated by an explicit
  // trigger (Enter/"Find", D1.4-74), never by `inputText` changing on its
  // own.
  const [submittedQuery, setSubmittedQuery] = useState("");
  const [typeFilters, setTypeFilters] = useState<Record<SearchType, boolean>>(() =>
    Object.fromEntries(SEARCH_TYPES.map((t) => [t, true])) as Record<SearchType, boolean>,
  );
  const [includeClosed, setIncludeClosed] = useState(false);

  const { data: searchResults, isLoading: searchLoading, isError: searchFailed } = useSearch(submittedQuery);

  function runSearch() {
    setSubmittedQuery(inputText.trim());
  }

  // The same client-side Team-scoping floor already established for Tasks/
  // Projects/Components/the Gantt view (D-Win-17, ProjectDetailPage.tsx's
  // own `memberTeamIds`) — extended here to Remark/Attachment too
  // (SearchPlan.md §2.3), since neither carries a `team_id` of its own.
  const memberTeamIds = useMemo(
    () => new Set((person?.team_roles ?? []).map((tr) => tr.team_id)),
    [person],
  );

  const tasksById = useMemo(() => new Map((tasks ?? []).map((t) => [t.task_id, t])), [tasks]);
  const projectsById = useMemo(() => new Map((projects ?? []).map((p) => [p.project_id, p])), [projects]);
  const componentsById = useMemo(
    () => new Map((components ?? []).map((c) => [c.component_id, c])),
    [components],
  );
  const remarksById = useMemo(() => new Map((allRemarks ?? []).map((r) => [r.remark_id, r])), [allRemarks]);
  const attachmentsById = useMemo(
    () => new Map((allAttachments ?? []).map((a) => [a.attachment_id, a])),
    [allAttachments],
  );

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

  function teamIdForTask(task: TaskRecord): number | undefined {
    return projectsById.get(task.project_id)?.team_id;
  }

  function teamIdForOwner(row: OwnedRecord): number | undefined {
    if (row.task_id != null) {
      const task = tasksById.get(row.task_id);
      return task ? teamIdForTask(task) : undefined;
    }
    if (row.project_id != null) return projectsById.get(row.project_id)?.team_id;
    if (row.component_id != null) return componentsById.get(row.component_id)?.team_id;
    return undefined;
  }

  function isTaskOpen(task: TaskRecord): boolean {
    return isTaskVisible(task, "Open");
  }

  // Component has no Priority/activity concept of its own (D1.4-67) — "open"
  // mirrors V1.2's own `ActiveTaskCount == 0` rule: at least one of its own
  // (directly tagged) Tasks isn't Closed/Cancelled.
  function isComponentOpen(component: ComponentRecord): boolean {
    return (tasks ?? []).some((t) => t.component_id === component.component_id && isTaskOpen(t));
  }

  // Mirrors V1.2's own `Remark.IsClosed => m_task.IsClosed`, generalised to
  // V2's three possible owners (a Remark/Attachment can sit on a Task,
  // Project, or Component).
  function isOwnerOpen(row: OwnedRecord): boolean {
    if (row.task_id != null) {
      const task = tasksById.get(row.task_id);
      return task ? isTaskOpen(task) : true;
    }
    if (row.project_id != null) return isProjectActive(scheduleGraph, row.project_id);
    if (row.component_id != null) {
      const component = componentsById.get(row.component_id);
      return component ? isComponentOpen(component) : true;
    }
    return true;
  }

  const displayRows = useMemo<SearchDisplayRow[]>(() => {
    if (!searchResults) return [];
    const rows: SearchDisplayRow[] = [];
    for (const result of searchResults) {
      if (!typeFilters[result.type]) continue;

      if (result.type === "Task") {
        const task = tasksById.get(result.id);
        if (!task) continue;
        const teamId = teamIdForTask(task);
        if (teamId == null || !memberTeamIds.has(teamId)) continue;
        if (!includeClosed && !isTaskOpen(task)) continue;
        const { endDate } = getTaskSchedule(scheduleGraph, task.task_id);
        const date =
          task.status === "Closed" || task.status === "Cancelled"
            ? task.status_date
              ? formatDdMmmYy(new Date(task.status_date))
              : ""
            : formatDdMmmYy(endDate);
        const personNames = (resourceIdsByTask.get(task.task_id) ?? [])
          .map((pid) => personDisplayName(pid, teamId, people, personRoles))
          .join(", ");
        rows.push({
          rowKey: `Task-${task.task_id}`,
          type: "Task",
          id: task.task_id,
          label: task.description,
          date,
          person: personNames,
          taskPriority: task.priority,
          taskUrgency: scheduledUrgency.get(task.task_id) ?? 100,
        });
      } else if (result.type === "Project") {
        const project = projectsById.get(result.id);
        if (!project) continue;
        if (!memberTeamIds.has(project.team_id)) continue;
        if (!includeClosed && !isProjectActive(scheduleGraph, project.project_id)) continue;
        rows.push({
          rowKey: `Project-${project.project_id}`,
          type: "Project",
          id: project.project_id,
          label: project.name,
          date: project.start_date ? formatDdMmmYy(new Date(project.start_date)) : "",
          person: personDisplayName(project.owner_person_id, project.team_id, people, personRoles),
        });
      } else if (result.type === "Component") {
        const component = componentsById.get(result.id);
        if (!component) continue;
        if (!memberTeamIds.has(component.team_id)) continue;
        if (!includeClosed && !isComponentOpen(component)) continue;
        rows.push({
          rowKey: `Component-${component.component_id}`,
          type: "Component",
          id: component.component_id,
          label: component.name,
          date: "",
          person: personDisplayName(component.owner_person_id, component.team_id, people, personRoles),
        });
      } else if (result.type === "Remark") {
        const remark = remarksById.get(result.id);
        if (!remark) continue;
        const teamId = teamIdForOwner(remark);
        if (teamId == null || !memberTeamIds.has(teamId)) continue;
        if (!includeClosed && !isOwnerOpen(remark)) continue;
        rows.push({
          rowKey: `Remark-${remark.remark_id}`,
          type: "Remark",
          id: remark.remark_id,
          label: remark.remark_text,
          date: formatDdMmmYy(new Date(remark.created_time)),
          person: personDisplayName(remark.created_by_person_id, teamId, people, personRoles),
        });
      } else if (result.type === "Attachment") {
        const attachment = attachmentsById.get(result.id);
        if (!attachment) continue;
        const teamId = teamIdForOwner(attachment);
        if (teamId == null || !memberTeamIds.has(teamId)) continue;
        if (!includeClosed && !isOwnerOpen(attachment)) continue;
        rows.push({
          rowKey: `Attachment-${attachment.attachment_id}`,
          type: "Attachment",
          id: attachment.attachment_id,
          label: attachment.name,
          date: formatDdMmmYy(new Date(attachment.created_time)),
          person: personDisplayName(attachment.owner_person_id, teamId, people, personRoles),
        });
      }
    }
    return rows;
    // eslint-disable-next-line react-hooks/exhaustive-deps -- helper functions above all close over the same dependency values already listed
  }, [
    searchResults,
    typeFilters,
    includeClosed,
    tasksById,
    projectsById,
    componentsById,
    remarksById,
    attachmentsById,
    memberTeamIds,
    scheduleGraph,
    scheduledUrgency,
    resourceIdsByTask,
    people,
    personRoles,
  ]);

  // Real per-column filtering after all (D1.4-77) — the search box/type
  // checkboxes/Include Closed checkbox narrow the row set before it
  // reaches the grid, but the grid still gets the same filter-row/
  // Reset-All-Filters/Show-Hide-Filter treatment `TaskGrid` has, since
  // that's shared chrome (`DenseDataGrid.tsx`'s `useDenseGridColumns`),
  // not something worth a one-off "plain header" opt-out for.
  const {
    withFilter,
    getFilteredRows,
    filterVisible,
    setFilterVisible,
    resetFilters,
    onColumnResize,
  } = useDenseGridColumns<SearchDisplayRow>({
    rows: displayRows,
    getRowId: (row) => row.rowKey,
    apiRef,
    // Hidden by default (unlike TaskGrid) — the search box/type checkboxes/
    // Include Closed checkbox already narrow the row set before it reaches
    // the grid, so a search is more likely to land on a small, already-
    // relevant result set than to need a second filtering pass straight
    // away. Still reachable via the same right-click "Show Filter" menu
    // item (D1.4-56) whenever it is needed.
    showFilters: false,
  });

  function rowClassName(row: SearchDisplayRow): string {
    if (row.type !== "Task") return "";
    return urgencyRowClassName(row.taskPriority ?? null, row.taskUrgency ?? 100);
  }

  function openResult(row: SearchDisplayRow) {
    if (row.type === "Task") {
      openItemWindow("tasks", row.id);
    } else if (row.type === "Project") {
      openItemWindow("projects", row.id);
    } else if (row.type === "Component") {
      openItemWindow("components", row.id);
    } else if (row.type === "Remark") {
      const remark = remarksById.get(row.id);
      if (!remark) return;
      if (remark.task_id != null) openItemWindow("tasks", remark.task_id);
      else if (remark.project_id != null) openItemWindow("projects", remark.project_id);
      else if (remark.component_id != null) openItemWindow("components", remark.component_id);
    } else if (row.type === "Attachment") {
      const attachment = attachmentsById.get(row.id);
      if (!attachment) return;
      if (attachment.task_id != null) openItemWindow("tasks", attachment.task_id);
      else if (attachment.project_id != null) openItemWindow("projects", attachment.project_id);
      else if (attachment.component_id != null) openItemWindow("components", attachment.component_id);
    }
  }

  const columns: GridColDef<SearchDisplayRow>[] = [
    withFilter({ field: "type", headerName: "Type" }, (row) => [row.type], "string"),
    withFilter({ field: "date", headerName: "Date", align: "center" }, (row) => [row.date], "date"),
    withFilter({ field: "person", headerName: "Person" }, (row) => [row.person], "string"),
    // Description is last *and* the `flex: 1` column (D1.4-78) — the free-
    // text field is the natural one to grow/shrink and fill the grid's
    // remaining width as the window resizes; Type/Date/Person stay at
    // their own auto-sized widths. Without this, MUI DataGrid leaves the
    // leftover width as dead space after the last column (its own
    // "filler" element) — easy to mistake for an extra, blank column,
    // which is exactly what prompted this fix in the first place. It has
    // to be the *last* column specifically: a middle `flex` column (the
    // original attempt, with Description ahead of Date/Person) has a
    // divider on *both* sides, and MUI drops `flex` the moment either one
    // is dragged (`useGridColumnResize.js` always converts a resized
    // column to a fixed `width` internally) — reopening the phantom gap,
    // and disabling that divider's own drag entirely once `resizable:
    // false` was added to stop it. As the trailing column, Description
    // only has a divider on its *left* (shared with Person's own right
    // edge) — dragging that still works normally, resizing Person, and
    // Description just absorbs whatever that leaves.
    withFilter({ field: "label", headerName: "Description" }, (row) => [row.label], "string", undefined, 1),
  ];

  // Only safe to call now — every `withFilter` call above has already
  // registered its own column's filter, and `getFilteredRows` reads all
  // of them.
  const filteredRows = getFilteredRows();

  if (
    !tasks ||
    !projects ||
    !components ||
    !people ||
    !personRoles ||
    !allDependencies ||
    !allTaskResources ||
    !allRemarks ||
    !allAttachments
  ) {
    return null;
  }

  return (
    <Box sx={{ p: "6px", boxSizing: "border-box", height: "100%", display: "flex", flexDirection: "column" }}>
      <Box sx={{ flex: 1, minHeight: 0, display: "flex", flexDirection: "column" }}>
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
            flex: 1,
            minHeight: 0,
            overflow: "auto",
          }}
        >
          <Box sx={{ display: "flex", alignItems: "center", flexWrap: "wrap" }}>
            <Box sx={{ fontSize: 14, fontWeight: 600 }}>Search</Box>
            <Box sx={{ display: "flex", alignItems: "center", gap: "14px", flexWrap: "wrap", ml: "4em" }}>
              {SEARCH_TYPES.map((t) => (
                <Box
                  component="label"
                  key={t}
                  sx={{
                    display: "flex",
                    alignItems: "center",
                    gap: "3px",
                    fontSize: DENSE_FONT_SIZE,
                    cursor: "pointer",
                  }}
                >
                  <Box
                    component="input"
                    type="checkbox"
                    checked={typeFilters[t]}
                    onChange={() => setTypeFilters((prev) => ({ ...prev, [t]: !prev[t] }))}
                    sx={{ width: 13, height: 13, m: 0, cursor: "pointer" }}
                  />
                  {t}
                </Box>
              ))}
              <Box
                component="label"
                sx={{
                  display: "flex",
                  alignItems: "center",
                  gap: "3px",
                  fontSize: DENSE_FONT_SIZE,
                  cursor: "pointer",
                }}
              >
                <Box
                  component="input"
                  type="checkbox"
                  checked={includeClosed}
                  onChange={() => setIncludeClosed((prev) => !prev)}
                  sx={{ width: 13, height: 13, m: 0, cursor: "pointer" }}
                />
                Include Closed Items
              </Box>
            </Box>
          </Box>

          <Box sx={{ display: "flex", alignItems: "center", gap: "10px" }}>
            <Box
              component="input"
              value={inputText}
              onChange={(event: React.ChangeEvent<HTMLInputElement>) => setInputText(event.target.value)}
              onKeyDown={(event: React.KeyboardEvent<HTMLInputElement>) => {
                if (event.key === "Enter") runSearch();
              }}
              placeholder="Search…"
              sx={{
                fontSize: DENSE_FONT_SIZE,
                fontFamily: "inherit",
                border: "1px solid rgba(0,0,0,0.25)",
                borderRadius: "3px",
                px: "6px",
                py: "3px",
                flex: 1,
                minWidth: 0,
              }}
            />
            <DenseButton onClick={runSearch}>Find</DenseButton>
          </Box>

          {searchFailed && (
            <Box sx={{ fontSize: DENSE_FONT_SIZE, color: "error.main" }}>
              Something went wrong running that search — please try again.
            </Box>
          )}

          {submittedQuery.length > 0 && !searchLoading && (
            <DenseDataGrid<SearchDisplayRow>
              apiRef={apiRef}
              rows={filteredRows}
              columns={columns}
              getRowId={(row) => row.rowKey}
              // D1.4-123 (UserInteractionPlan.md) — single click, not
              // double: this grid has no inline editing to conflict with,
              // so it follows the app-wide "opening an item is a single
              // click" rule rather than TaskGrid's own one stated exception.
              onCellClick={(params) => openResult(params.row)}
              hint="Click: Open the result."
              getRowClassName={rowClassName}
              sx={[urgencyRowPaletteSx(), clickableCellSx()]}
              onColumnResize={onColumnResize}
              defaultSort={[
                { field: "type", sort: "asc" },
                { field: "label", sort: "asc" },
              ]}
              filtering={{
                filterVisible,
                onToggleFilterVisible: () => setFilterVisible((prev) => !prev),
                onResetFilters: resetFilters,
              }}
            />
          )}
        </Box>
      </Box>
    </Box>
  );
}
