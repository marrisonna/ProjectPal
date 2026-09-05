import { DataGrid, type GridColDef } from "@mui/x-data-grid";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import { useProjects, usePeople, useTasks } from "../../api/hooks";
import type { TaskRecord } from "../../api/types";
import { openItemWindow } from "../../lib/windowNav";
import { useDocumentTitle } from "../../lib/useDocumentTitle";

// Column set adapted from V1.2's TaskWindow grid (GUITaskColumns.cs) — a
// practical subset given V2 has no absolute Start/End dates yet (schedule is
// derived, Stage 3) and Urgency isn't computed until Stage 3 either
// (Plan.md D1.4-13's "strong hint, not a constraint" principle).
export function TaskListPage() {
  useDocumentTitle("All Tasks");
  const { data: tasks, isLoading } = useTasks();
  const { data: projects } = useProjects();
  const { data: people } = usePeople();

  const columns: GridColDef<TaskRecord>[] = [
    { field: "task_id", headerName: "ID", width: 70 },
    { field: "description", headerName: "Description", flex: 1, minWidth: 200 },
    {
      field: "project_id",
      headerName: "Project",
      width: 160,
      valueGetter: (_value, row) =>
        projects?.find((p) => p.project_id === row.project_id)?.name ?? row.project_id,
    },
    { field: "priority", headerName: "Priority", width: 110 },
    { field: "status", headerName: "Status", width: 130 },
    { field: "task_type", headerName: "Type", width: 140 },
    {
      field: "owner_person_id",
      headerName: "Owner",
      width: 150,
      valueGetter: (_value, row) =>
        people?.find((p) => p.person_id === row.owner_person_id)?.name ?? "—",
    },
  ];

  return (
    <Box>
      <Typography variant="h5" component="h1" gutterBottom>
        All Tasks
      </Typography>
      <Box sx={{ height: 600 }}>
        <DataGrid
          rows={tasks ?? []}
          getRowId={(row) => row.task_id}
          columns={columns}
          loading={isLoading}
          // A new/re-focused window, not in-place navigation (D1.4-8's
          // multi-window model — the same singleton-per-object window the
          // Task Detail header's own "open in new window" icon uses).
          onRowDoubleClick={(params) => openItemWindow("tasks", params.id)}
          density="compact"
          initialState={{ pagination: { paginationModel: { pageSize: 25 } } }}
        />
      </Box>
    </Box>
  );
}
