import { useMemo, useState } from "react";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import CircularProgress from "@mui/material/CircularProgress";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import MenuItem from "@mui/material/MenuItem";
import TextField from "@mui/material/TextField";
import Snackbar from "@mui/material/Snackbar";
import Alert from "@mui/material/Alert";
import {
  GridActionsCellItem,
  useGridApiRef,
  type GridColDef,
  type GridRowParams,
} from "@mui/x-data-grid";
import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import {
  useComponents,
  useCreateTeam,
  useDeleteTeam,
  usePeople,
  usePersonRoles,
  useProjects,
  useRenameTeam,
  useTeams,
} from "../../api/hooks";
import type { PersonRecord, TeamRecord } from "../../api/types";
import { useAuth } from "../../auth/AuthContext";
import { DenseDataGrid, DENSE_ROW_HEIGHT, clickableCellSx, useDenseGridColumns } from "../../components/DenseDataGrid";
import { formatApiError } from "../../lib/apiErrors";
import { useDocumentTitle } from "../../lib/useDocumentTitle";
import { openItemWindow, useSingletonWindowIdentity } from "../../lib/windowNav";

// org-admin-only create/rename (create_team/rename_team both call
// require_org_admin) — dual-purpose the same way
// CreateOrRenameProjectDialog.tsx already is, since a fresh Team needs an
// initial Team Lead (mode "create") while renaming one doesn't touch
// membership at all (mode "rename").
function CreateOrRenameTeamDialog({
  mode,
  team,
  people,
  onClose,
}: {
  mode: "create" | "rename";
  /** Required, and only its name read, for mode "create". */
  team?: TeamRecord;
  /** Only used for mode "create" — who can be picked as the new Team's initial Team Lead. */
  people: PersonRecord[];
  onClose: () => void;
}) {
  const [name, setName] = useState(mode === "rename" ? (team?.name ?? "") : "");
  const [leadPersonId, setLeadPersonId] = useState<number | "">("");
  const [error, setError] = useState<string | null>(null);
  const createTeam = useCreateTeam();
  const renameTeam = useRenameTeam(team?.team_id ?? -1);
  const pending = createTeam.isPending || renameTeam.isPending;

  async function handleConfirm() {
    if (!name.trim()) {
      setError("A name must be specified.");
      return;
    }
    if (mode === "create" && leadPersonId === "") {
      setError("Choose an initial Team Lead.");
      return;
    }
    try {
      if (mode === "create") {
        await createTeam.mutateAsync({ name: name.trim(), initial_team_lead_person_id: leadPersonId });
      } else {
        await renameTeam.mutateAsync(name.trim());
      }
      onClose();
    } catch (err) {
      setError(formatApiError(err, "please try again."));
    }
  }

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>{mode === "create" ? "New Team" : "Rename Team"}</DialogTitle>
      <DialogContent>
        <TextField
          label="Name"
          fullWidth
          margin="normal"
          autoFocus
          value={name}
          onChange={(event) => {
            setName(event.target.value);
            setError(null);
          }}
        />
        {mode === "create" && (
          <TextField
            select
            label="Initial Team Lead"
            fullWidth
            margin="normal"
            value={leadPersonId}
            onChange={(event) => {
              setLeadPersonId(Number(event.target.value));
              setError(null);
            }}
          >
            {people.map((p) => (
              <MenuItem key={p.person_id} value={p.person_id}>
                {p.name}
              </MenuItem>
            ))}
          </TextField>
        )}
        {error && <Box sx={{ fontSize: 12, color: "error.main", mt: "4px" }}>{error}</Box>}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleConfirm} disabled={pending}>
          {mode === "create" ? "Create" : "Rename"}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

// D1.4-92 — split out of what used to be TeamManagementPage.tsx's own
// "no id" mode: this is now purely the admin-facing (or multi-Team-Lead-
// facing) list of every Team, reachable at /teams-management, always a
// singleton window (at most one instance, ever — unlike the singular
// Team Management window below, which allows one *per Team*). D1.4-94 —
// rebuilt on DenseDataGrid (a leading Actions column, matching
// ManagePeoplePage.tsx's own shape) rather than a plain list of Buttons,
// for consistency with every other list screen in this app. Double-
// clicking a row opens (or focuses) that Team's own separate Team
// Management window via openItemWindow, the same singleton-per-item
// mechanism Tasks/Projects/Components already use — it does not navigate
// this window away from the list.
export function TeamsManagementPage() {
  useSingletonWindowIdentity("teams-management-list");
  useDocumentTitle("Teams Management");
  const apiRef = useGridApiRef();

  const { person: caller } = useAuth();
  const { data: teams } = useTeams();
  const { data: people } = usePeople();
  const { data: personRoles } = usePersonRoles();
  const { data: projects } = useProjects();
  const { data: components } = useComponents();
  const deleteTeam = useDeleteTeam();

  const [createTeamOpen, setCreateTeamOpen] = useState(false);
  const [renameTarget, setRenameTarget] = useState<TeamRecord | null>(null);
  const [snackbarError, setSnackbarError] = useState<string | null>(null);

  const isAdmin = !!caller?.is_organisation_admin;
  const ledTeamIds = useMemo(
    () => new Set((caller?.team_roles ?? []).filter((tr) => tr.role === "TeamLeadUser").map((tr) => tr.team_id)),
    [caller],
  );

  // Every Team available to this viewer — every Team for an admin, only the
  // ones they lead for a Team Lead (matching the old "no id" mode's own
  // rule verbatim).
  const availableTeams = useMemo(() => {
    if (!teams) return [];
    return isAdmin ? teams : teams.filter((t) => ledTeamIds.has(t.team_id));
  }, [teams, isAdmin, ledTeamIds]);

  const { withFilter, getFilteredRows, filterVisible, setFilterVisible, resetFilters, onColumnResize } =
    useDenseGridColumns<TeamRecord>({
      rows: availableTeams,
      getRowId: (row) => row.team_id,
      showFilters: false,
      apiRef,
    });

  // D-DM-14/D1.4-90's own server-side check, mirrored client-side so the
  // bin icon only shows for a Team it will actually succeed for, the same
  // "hide it, don't just reject it" convention ManagePeoplePage.tsx's own
  // Delete icon already uses (isPersonDeletable). Membership deliberately
  // isn't checked here — it's cascaded away, not blocking.
  function isTeamDeletable(teamId: number): boolean {
    if ((projects ?? []).some((p) => p.team_id === teamId)) return false;
    if ((components ?? []).some((c) => c.team_id === teamId)) return false;
    return true;
  }

  // D-DM-14/D1.4-90 — rejected server-side if the Team has any Project or
  // Component; membership itself is cascaded away, not blocking (see
  // teams.py's own module docstring for why). Named here in the
  // confirmation, the same "soft warning" shape as removing a single
  // member (TeamManagementPage.tsx).
  async function handleDeleteTeam(team: TeamRecord) {
    const memberCount = (personRoles ?? []).filter((pr) => pr.team_id === team.team_id).length;
    const warning = memberCount > 0 ? ` This will also remove ${memberCount} member${memberCount === 1 ? "" : "s"} from it.` : "";
    if (!window.confirm(`Delete Team "${team.name}"?${warning} This cannot be undone.`)) return;
    try {
      await deleteTeam.mutateAsync(team.team_id);
    } catch (err) {
      setSnackbarError(formatApiError(err, "please try again."));
    }
  }

  // Two icons' worth of width, matching ManagePeoplePage.tsx's own
  // ACTIONS_COLUMN_WIDTH exactly, sized to fit the "Actions" header text
  // comfortably as well as the two icons.
  const ACTIONS_COLUMN_WIDTH = DENSE_ROW_HEIGHT * 2 + 14;

  const actionsColumn: GridColDef<TeamRecord> = {
    field: "__actions",
    type: "actions",
    headerName: "Actions",
    headerAlign: "center",
    width: ACTIONS_COLUMN_WIDTH,
    minWidth: ACTIONS_COLUMN_WIDTH,
    maxWidth: ACTIONS_COLUMN_WIDTH,
    sortable: false,
    filterable: false,
    hideSortIcons: true,
    getActions: (params: GridRowParams<TeamRecord>) => {
      if (!isAdmin) return [];
      // D1.4-98 — always shown now (reverting D1.4-96's own "hide it
      // entirely" approach): with a mix of one-icon and two-icon rows, the
      // ragged layout read worse than a consistently-present, greyed-out
      // icon does. A genuinely disabled MUI IconButton, not just a lighter
      // colour on an otherwise-live one — `disabled` makes a click on it a
      // real no-op (the button never fires `onClick` at all), matching
      // "nothing happens" exactly, not just discouraging the click.
      const deletable = isTeamDeletable(params.row.team_id);
      return [
        // Darker than the muted rgba(0,0,0,0.28) row-action icons used
        // elsewhere (Project/Component's own row icons) — this icon is
        // the whole cell's content here, not a secondary affordance
        // beside other content, the same reasoning
        // ManagePeoplePage.tsx's own actions column already used.
        <GridActionsCellItem
          key="rename"
          icon={<EditIcon fontSize="inherit" sx={{ color: "rgba(0,0,0,0.87)" }} />}
          label="Rename"
          onClick={() => setRenameTarget(params.row)}
        />,
        <GridActionsCellItem
          key="delete"
          icon={<DeleteIcon fontSize="inherit" sx={{ color: deletable ? "rgba(0,0,0,0.87)" : "rgba(0,0,0,0.18)" }} />}
          label="Delete"
          disabled={!deletable}
          onClick={deletable ? () => handleDeleteTeam(params.row) : undefined}
        />,
      ];
    },
  };

  // flex: 1 — the only data column, so it's also the trailing one; without
  // it MUI DataGrid leaves unused window width as an unlabelled "phantom"
  // filler column (DenseDataGrid.tsx's own withFilter doc comment).
  const nameColumn = withFilter({ field: "name", headerName: "Name" }, (row) => [row.name], "string", undefined, 1);

  const columns: GridColDef<TeamRecord>[] = [actionsColumn, nameColumn];
  const filteredTeams = getFilteredRows();

  if (!teams || !people || !personRoles || !projects || !components) {
    return <CircularProgress sx={{ m: 2 }} />;
  }

  if (!isAdmin && ledTeamIds.size === 0) {
    return <Box sx={{ p: 2, fontSize: 13 }}>You don't have access to this page.</Box>;
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
          flex: 1,
          minHeight: 0,
          overflow: "auto",
        }}
      >
        <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
          <Box sx={{ fontSize: 14, fontWeight: 600 }}>Teams Management</Box>
          {isAdmin && (
            <Button size="small" variant="contained" onClick={() => setCreateTeamOpen(true)}>
              New Team
            </Button>
          )}
        </Box>

        <DenseDataGrid<TeamRecord>
          apiRef={apiRef}
          rows={filteredTeams}
          columns={columns}
          getRowId={(row) => row.team_id}
          // A single click, not TaskGrid/other grids' own double-click —
          // deliberately, per the user's own request: this grid has no
          // in-cell editing to conflict with (unlike TaskGrid, where a
          // single click on a governed cell starts editing it), so there's
          // no ambiguity a single click could be mistaken for. Skips the
          // Actions cell so clicking Rename/Delete doesn't also open the
          // Team's own window underneath it.
          onCellClick={(params) => {
            if (params.field === "__actions") return;
            openItemWindow("team-management", params.row.team_id);
          }}
          // Link-like affordance on the one clickable cell, since a plain
          // grid cell otherwise gives no visual hint it opens something —
          // the app-wide shared primitive this screen's own original
          // version of this rule was generalised into (D1.4-123,
          // UserInteractionPlan.md §5.1); scoped to just the Name column so
          // it never touches the Actions cell beside it.
          sx={clickableCellSx("name")}
          hint="Click: Open the Team's own Team Management window."
          onColumnResize={onColumnResize}
          defaultSort={[{ field: "name", sort: "asc" }]}
          filtering={{
            filterVisible,
            onToggleFilterVisible: () => setFilterVisible((prev) => !prev),
            onResetFilters: resetFilters,
          }}
        />
      </Box>

      {createTeamOpen && (
        <CreateOrRenameTeamDialog
          mode="create"
          people={people.filter((p) => p.is_active).sort((a, b) => a.name.localeCompare(b.name))}
          onClose={() => setCreateTeamOpen(false)}
        />
      )}
      {renameTarget && (
        <CreateOrRenameTeamDialog mode="rename" team={renameTarget} people={[]} onClose={() => setRenameTarget(null)} />
      )}

      <Snackbar open={!!snackbarError} autoHideDuration={6000} onClose={() => setSnackbarError(null)}>
        <Alert severity="error" onClose={() => setSnackbarError(null)}>
          {snackbarError}
        </Alert>
      </Snackbar>
    </Box>
  );
}
