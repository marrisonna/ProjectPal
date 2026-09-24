import { useMemo, useState } from "react";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import CircularProgress from "@mui/material/CircularProgress";
import Snackbar from "@mui/material/Snackbar";
import Alert from "@mui/material/Alert";
import Tooltip from "@mui/material/Tooltip";
import type { GridColDef } from "@mui/x-data-grid";
import { useExportAllData, useIntegrityCheck, usePeople, useTasks, useTeams } from "../../api/hooks";
import { useAuth } from "../../auth/AuthContext";
import { DenseDataGrid } from "../../components/DenseDataGrid";
import { formatApiError } from "../../lib/apiErrors";
import { useDocumentTitle } from "../../lib/useDocumentTitle";
import { openItemWindow, useSingletonWindowIdentity } from "../../lib/windowNav";

interface LeaderlessTeamRow {
  team_id: number;
  name: string;
}

interface StaleResourceRow {
  rowKey: string;
  task_id: number;
  taskLabel: string;
  personLabel: string;
  teamLabel: string;
}

const cardSx = {
  bgcolor: "#fff",
  border: "1px solid rgba(0,0,0,0.08)",
  borderRadius: "8px",
  boxShadow: "0 1px 3px rgba(0,0,0,0.06)",
  p: "12px",
  display: "flex",
  flexDirection: "column",
  gap: "10px",
} as const;

// D1.4-103 — thin admin-only tooling over rest-api/app/routes/admin.py's two
// endpoints (UseCases.md's Administer the System), plus a disabled Import
// placeholder. Import is explicitly out of scope for Level 1 (Goals.md's
// Q-G-11 already anticipates a manual export/import migration tool, but
// scopes it to Level 2) — the placeholder just marks where it will live.
export function AdminPage() {
  useDocumentTitle("Admin Tools");
  useSingletonWindowIdentity("admin-list");
  const { person: caller } = useAuth();

  const {
    data: integrityResult,
    isLoading: integrityLoading,
    refetch: refetchIntegrity,
  } = useIntegrityCheck();
  const { data: teams } = useTeams();
  const { data: people } = usePeople();
  const { data: tasks } = useTasks();
  const exportAllData = useExportAllData();
  const [snackbarError, setSnackbarError] = useState<string | null>(null);

  const teamsById = useMemo(() => new Map((teams ?? []).map((t) => [t.team_id, t])), [teams]);
  const peopleById = useMemo(() => new Map((people ?? []).map((p) => [p.person_id, p])), [people]);
  const tasksById = useMemo(() => new Map((tasks ?? []).map((t) => [t.task_id, t])), [tasks]);

  const leaderlessTeamRows: LeaderlessTeamRow[] = integrityResult?.teams_without_a_team_lead_user ?? [];
  // Resolved to names/descriptions here — the raw ids the API returns
  // (`admin.py`'s own integrity_check) aren't useful on their own to an
  // admin deciding what to fix.
  const staleResourceRows: StaleResourceRow[] = (
    integrityResult?.task_resource_assignments_no_longer_valid ?? []
  ).map((r) => ({
    rowKey: `${r.task_id}-${r.person_id}`,
    task_id: r.task_id,
    taskLabel: tasksById.get(r.task_id)?.description ?? `Task #${r.task_id}`,
    personLabel: peopleById.get(r.person_id)?.name ?? `Person #${r.person_id}`,
    teamLabel: teamsById.get(r.team_id)?.name ?? `Team #${r.team_id}`,
  }));
  const hasIssues = leaderlessTeamRows.length > 0 || staleResourceRows.length > 0;

  const leaderlessColumns: GridColDef<LeaderlessTeamRow>[] = [{ field: "name", headerName: "Team", flex: 1 }];
  const staleResourceColumns: GridColDef<StaleResourceRow>[] = [
    { field: "taskLabel", headerName: "Task", flex: 2 },
    { field: "personLabel", headerName: "Person", flex: 1 },
    { field: "teamLabel", headerName: "Team", flex: 1 },
  ];

  async function handleExport() {
    try {
      const data = await exportAllData.mutateAsync();
      const blob = new Blob([JSON.stringify(data, null, 2)], { type: "application/json" });
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = `projectpal-export-${new Date().toISOString().slice(0, 10)}.json`;
      link.click();
      URL.revokeObjectURL(url);
    } catch (err) {
      setSnackbarError(formatApiError(err, "please try again."));
    }
  }

  if (!caller?.is_organisation_admin) {
    return <Box sx={{ p: 2, fontSize: 13 }}>You don't have access to this page.</Box>;
  }

  return (
    <Box
      sx={{
        p: "6px",
        boxSizing: "border-box",
        height: "100%",
        display: "flex",
        flexDirection: "column",
        gap: "10px",
        overflow: "auto",
      }}
    >
      <Box sx={cardSx}>
        <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
          <Box sx={{ fontSize: 14, fontWeight: 600 }}>Data Integrity Check</Box>
          <Button size="small" onClick={() => refetchIntegrity()} disabled={integrityLoading}>
            Refresh
          </Button>
        </Box>
        {integrityLoading ? (
          <CircularProgress size={20} sx={{ alignSelf: "flex-start" }} />
        ) : !hasIssues ? (
          <Box sx={{ fontSize: 13, color: "success.main" }}>No issues found.</Box>
        ) : (
          <>
            {leaderlessTeamRows.length > 0 && (
              <Box>
                <Box sx={{ fontSize: 12, fontWeight: 600, mb: "4px" }}>
                  Teams without a Team Lead ({leaderlessTeamRows.length})
                </Box>
                {/* Double-click opens that Team's own Team Management
                    window (the same singleton-per-Team mechanism the rest
                    of the app uses) so fixing it is one click away, not a
                    dead-end report. */}
                <DenseDataGrid<LeaderlessTeamRow>
                  rows={leaderlessTeamRows}
                  columns={leaderlessColumns}
                  getRowId={(row) => row.team_id}
                  onRowDoubleClick={(row) => openItemWindow("team-management", row.team_id)}
                />
              </Box>
            )}
            {staleResourceRows.length > 0 && (
              <Box>
                <Box sx={{ fontSize: 12, fontWeight: 600, mb: "4px" }}>
                  Stale resource assignments ({staleResourceRows.length})
                </Box>
                {/* Double-click opens the Task itself, where the
                    assignment actually gets fixed. */}
                <DenseDataGrid<StaleResourceRow>
                  rows={staleResourceRows}
                  columns={staleResourceColumns}
                  getRowId={(row) => row.rowKey}
                  onRowDoubleClick={(row) => openItemWindow("tasks", row.task_id)}
                />
              </Box>
            )}
          </>
        )}
      </Box>

      <Box sx={cardSx}>
        <Box sx={{ fontSize: 14, fontWeight: 600 }}>Data Export</Box>
        <Box sx={{ fontSize: 12, color: "rgba(0,0,0,0.6)" }}>
          Downloads every Team, Person, Project, Component, Task, and their Resource/Dependency/Remark
          records as one JSON file. Attachments aren't included — their file contents aren't exportable
          this way.
        </Box>
        <Button
          size="small"
          variant="contained"
          onClick={handleExport}
          disabled={exportAllData.isPending}
          sx={{ alignSelf: "flex-start" }}
        >
          {exportAllData.isPending ? "Exporting…" : "Download Export"}
        </Button>
      </Box>

      <Box sx={cardSx}>
        <Box sx={{ fontSize: 14, fontWeight: 600 }}>Data Import</Box>
        <Box sx={{ fontSize: 12, color: "rgba(0,0,0,0.6)" }}>
          Restore or migrate data from a previous export.
        </Box>
        {/* A real IconButton/Button wrapped in a span, not the Button
            itself, disabled — MUI's Tooltip can't attach its own
            listeners directly to a disabled element (it never fires
            pointer events), so the span is what the hover is actually
            detected on. */}
        <Tooltip title="Not implemented at Level 1">
          <span style={{ alignSelf: "flex-start" }}>
            <Button size="small" variant="outlined" disabled>
              Import…
            </Button>
          </span>
        </Tooltip>
      </Box>

      <Snackbar open={!!snackbarError} autoHideDuration={6000} onClose={() => setSnackbarError(null)}>
        <Alert severity="error" onClose={() => setSnackbarError(null)}>
          {snackbarError}
        </Alert>
      </Snackbar>
    </Box>
  );
}
