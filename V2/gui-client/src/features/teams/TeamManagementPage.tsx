import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Checkbox from "@mui/material/Checkbox";
import CircularProgress from "@mui/material/CircularProgress";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import FormControlLabel from "@mui/material/FormControlLabel";
import MenuItem from "@mui/material/MenuItem";
import TextField from "@mui/material/TextField";
import Snackbar from "@mui/material/Snackbar";
import Alert from "@mui/material/Alert";
import {
  GridActionsCellItem,
  useGridApiRef,
  type GridCellParams,
  type GridColDef,
  type GridRowParams,
} from "@mui/x-data-grid";
import DeleteIcon from "@mui/icons-material/Delete";
import {
  useCreatePersonRole,
  useDeletePersonRole,
  usePeople,
  usePersonRoles,
  useTasks,
  useTeams,
  useUpdatePersonRoleField,
} from "../../api/hooks";
import type { PersonRecord, PersonRoleRecord } from "../../api/types";
import { useAuth } from "../../auth/AuthContext";
import {
  DenseDataGrid,
  DenseSingleSelectEditCell,
  DENSE_ROW_HEIGHT,
  useDenseGridColumns,
} from "../../components/DenseDataGrid";
import { formatApiError } from "../../lib/apiErrors";
import { isTeamLead } from "../../lib/permissions";
import { useDocumentTitle } from "../../lib/useDocumentTitle";
import { useSingletonWindowIdentity } from "../../lib/windowNav";
import { isTaskVisible } from "../projects/Projects";

// permissions.ts's own ROLE_RANK keys, lowest to highest — the same four
// PersonRole.role values that already exist server-side.
const ROLE_VALUES = ["ReadOnlyUser", "NormalUser", "LeadUser", "TeamLeadUser"];

interface TeamMemberRow extends PersonRoleRecord {
  name: string;
}

// ManagePeoplePlan.md §5.3 — a Team Lead cannot create Person records at
// all (D-DM-4); this dialog only ever picks among People who already exist
// and aren't already on this Team.
function AddTeamMemberDialog({
  teamId,
  candidates,
  onClose,
}: {
  teamId: number;
  candidates: PersonRecord[];
  onClose: () => void;
}) {
  const [personId, setPersonId] = useState<number | "">("");
  const [role, setRole] = useState("NormalUser");
  const [isResource, setIsResource] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const createPersonRole = useCreatePersonRole();

  async function handleConfirm() {
    if (personId === "") {
      setError("Choose a Person to add.");
      return;
    }
    try {
      await createPersonRole.mutateAsync({
        person_id: personId,
        team_id: teamId,
        role,
        is_resource: isResource,
      });
      onClose();
    } catch (err) {
      setError(formatApiError(err, "please try again."));
    }
  }

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>Add Person to Team</DialogTitle>
      <DialogContent>
        <TextField
          select
          label="Person"
          fullWidth
          margin="normal"
          value={personId}
          onChange={(event) => {
            setPersonId(Number(event.target.value));
            setError(null);
          }}
          error={!!error}
          helperText={error ?? (candidates.length === 0 ? "Every active Person is already on this Team." : undefined)}
        >
          {candidates.map((p) => (
            <MenuItem key={p.person_id} value={p.person_id}>
              {p.name}
            </MenuItem>
          ))}
        </TextField>
        <TextField
          select
          label="Role"
          fullWidth
          margin="normal"
          value={role}
          onChange={(event) => setRole(event.target.value)}
        >
          {ROLE_VALUES.map((r) => (
            <MenuItem key={r} value={r}>
              {r}
            </MenuItem>
          ))}
        </TextField>
        <FormControlLabel
          control={<Checkbox checked={isResource} onChange={(event) => setIsResource(event.target.checked)} />}
          label="Is Resource"
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleConfirm} disabled={createPersonRole.isPending}>
          Add
        </Button>
      </DialogActions>
    </Dialog>
  );
}

// ManagePeoplePlan.md §5 — Team-Lead-facing (or organisation-admin, for any
// Team) membership management, scoped to one Team at a time. Routes mirror
// ProjectDetailPage.tsx's own no-id/with-id duality, but the "no id" mode
// resolves differently depending on who's looking (§5.1): an admin always
// sees a picker of every Team; a Team Lead of exactly one Team is sent
// straight through; a Team Lead of several sees a picker of just those.
export function TeamManagementPage() {
  const { teamId: teamIdParam } = useParams<{ teamId?: string }>();
  const id = teamIdParam ? Number(teamIdParam) : null;
  const navigate = useNavigate();
  useSingletonWindowIdentity(id != null ? `team-management-${id}` : "team-management-list");
  useDocumentTitle("Team Management");

  const { person: caller } = useAuth();
  const { data: teams } = useTeams();
  const { data: people } = usePeople();
  const { data: personRoles } = usePersonRoles();
  const { data: tasks } = useTasks();

  const isAdmin = !!caller?.is_organisation_admin;
  const ledTeamIds = useMemo(
    () => new Set((caller?.team_roles ?? []).filter((tr) => tr.role === "TeamLeadUser").map((tr) => tr.team_id)),
    [caller],
  );

  // Every Team available to switch/pick among, from this viewer's own
  // perspective — every Team for an admin, only the ones they lead for a
  // Team Lead (§5.1).
  const availableTeams = useMemo(() => {
    if (!teams) return [];
    return isAdmin ? teams : teams.filter((t) => ledTeamIds.has(t.team_id));
  }, [teams, isAdmin, ledTeamIds]);

  // No-id mode, non-admin: a Team Lead of exactly one Team skips the picker
  // entirely — no extra click for the common case (§5.1). An admin always
  // sees the picker regardless of count, since "manage any Team" has no
  // single obvious default.
  useEffect(() => {
    if (id != null || isAdmin || !teams) return;
    if (availableTeams.length === 1) {
      navigate(`/team-management/${availableTeams[0].team_id}`, { replace: true });
    }
  }, [id, isAdmin, teams, availableTeams, navigate]);

  const canManageThisTeam = id != null && (isAdmin || isTeamLead(caller, id));

  const updatePersonRoleField = useUpdatePersonRoleField();
  const deletePersonRole = useDeletePersonRole();
  const [addMemberOpen, setAddMemberOpen] = useState(false);
  const [snackbarError, setSnackbarError] = useState<string | null>(null);
  const apiRef = useGridApiRef();

  const teamMembers = useMemo<TeamMemberRow[]>(() => {
    if (id == null || !personRoles || !people) return [];
    return personRoles
      .filter((pr) => pr.team_id === id)
      .map((pr) => ({ ...pr, name: people.find((p) => p.person_id === pr.person_id)?.name ?? `Person #${pr.person_id}` }));
  }, [id, personRoles, people]);

  const { withFilter, getFilteredRows, filterVisible, setFilterVisible, resetFilters, onColumnResize } =
    useDenseGridColumns<TeamMemberRow>({
      rows: teamMembers,
      getRowId: (row) => row.person_id,
      showFilters: false,
      apiRef,
    });

  const addCandidates = useMemo(() => {
    if (!people) return [];
    const memberIds = new Set(teamMembers.map((m) => m.person_id));
    return people.filter((p) => p.is_active && !memberIds.has(p.person_id)).sort((a, b) => a.name.localeCompare(b.name));
  }, [people, teamMembers]);

  // §5.4 — removing a member is a soft warning, not a hard block: how many
  // open Tasks on this Team still name them as owner, requestor, or
  // resource, so the confirmation is informative rather than just "are you
  // sure?" with no context.
  function openReferenceCount(personId: number): number {
    if (id == null || !tasks) return 0;
    let count = 0;
    for (const t of tasks) {
      if (!isTaskVisible(t, "Open")) continue;
      if (t.owner_person_id === personId || t.requestor_person_id === personId) count++;
    }
    return count;
  }

  async function handleRemoveMember(row: TeamMemberRow) {
    if (id == null) return;
    const refCount = openReferenceCount(row.person_id);
    const warning =
      refCount > 0
        ? ` They still own or requested ${refCount} open Task${refCount === 1 ? "" : "s"} on this Team.`
        : "";
    if (!window.confirm(`Remove "${row.name}" from this Team?${warning}`)) return;
    try {
      await deletePersonRole.mutateAsync({ personId: row.person_id, teamId: id });
    } catch (err) {
      setSnackbarError(formatApiError(err, "please try again."));
    }
  }

  async function processRowUpdate(newRow: TeamMemberRow, oldRow: TeamMemberRow): Promise<TeamMemberRow> {
    if (id == null) return oldRow;
    const changedField = (Object.keys(newRow) as (keyof TeamMemberRow)[]).find(
      (key) => newRow[key] !== oldRow[key],
    );
    if (!changedField) return oldRow;
    const updated = await updatePersonRoleField.mutateAsync({
      personId: oldRow.person_id,
      teamId: id,
      body: { [changedField]: newRow[changedField] },
    });
    return { ...updated, name: oldRow.name };
  }

  const actionsColumn: GridColDef<TeamMemberRow> = {
    field: "__actions",
    type: "actions",
    headerName: "",
    width: DENSE_ROW_HEIGHT,
    minWidth: DENSE_ROW_HEIGHT,
    maxWidth: DENSE_ROW_HEIGHT,
    sortable: false,
    filterable: false,
    hideSortIcons: true,
    getActions: (params: GridRowParams<TeamMemberRow>) => [
      <GridActionsCellItem
        key="remove"
        icon={<DeleteIcon fontSize="inherit" sx={{ color: "rgba(0,0,0,0.28)" }} />}
        label="Remove from Team"
        onClick={() => handleRemoveMember(params.row)}
      />,
    ],
  };

  const editableFields = new Set(["nickname", "role", "is_resource"]);

  const dataColumns: GridColDef<TeamMemberRow>[] = [
    withFilter({ field: "name", headerName: "Name" }, (row) => [row.name], "string"),
    withFilter({ field: "nickname", headerName: "Nickname" }, (row) => [row.nickname ?? ""], "string"),
    withFilter(
      {
        field: "role",
        headerName: "Role",
        type: "singleSelect",
        valueOptions: ROLE_VALUES,
        renderEditCell: DenseSingleSelectEditCell,
      },
      (row) => [row.role],
      "string",
    ),
    // flex: 1 (last argument) — this has to be the *trailing* column
    // (DenseDataGrid.tsx's own withFilter doc comment): without a flex
    // column, MUI DataGrid leaves whatever width the fixed columns don't
    // use as dead space after the last one (its own "filler" element),
    // which reads as a genuine extra, unlabelled column once the window is
    // wider than the grid's own content — exactly what SearchPage.tsx's
    // Description column already fixed the same way (D1.4-78).
    withFilter(
      { field: "is_resource", headerName: "Is Resource", type: "boolean", align: "center", headerAlign: "center" },
      (row) => [row.is_resource ? "✓" : ""],
      "string",
      undefined,
      1,
    ),
  ].map((col) => ({ ...col, editable: editableFields.has(col.field) }));

  const columns: GridColDef<TeamMemberRow>[] = [actionsColumn, ...dataColumns];
  const filteredMembers = getFilteredRows();

  if (!teams || !people || !personRoles || !tasks) {
    return <CircularProgress sx={{ m: 2 }} />;
  }

  // No-id mode: either a picker, or (a lone-led-Team redirect firing above)
  // a brief flash of this same loading state until it navigates through.
  if (id == null) {
    if (!isAdmin && ledTeamIds.size === 0) {
      return <Box sx={{ p: 2, fontSize: 13 }}>You don't have access to this page.</Box>;
    }
    if (!isAdmin && availableTeams.length === 1) {
      return <CircularProgress sx={{ m: 2 }} />;
    }
    return (
      <Box sx={{ p: "6px", boxSizing: "border-box" }}>
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
            maxWidth: 320,
          }}
        >
          <Box sx={{ fontSize: 14, fontWeight: 600 }}>Team Management</Box>
          {availableTeams.map((t) => (
            <Button
              key={t.team_id}
              variant="outlined"
              onClick={() => navigate(`/team-management/${t.team_id}`)}
              sx={{ justifyContent: "flex-start" }}
            >
              {t.name}
            </Button>
          ))}
        </Box>
      </Box>
    );
  }

  if (!canManageThisTeam) {
    return <Box sx={{ p: 2, fontSize: 13 }}>You don't have access to this page.</Box>;
  }

  const currentTeam = teams.find((t) => t.team_id === id);

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
        <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", flexWrap: "wrap", gap: "10px" }}>
          <Box sx={{ display: "flex", alignItems: "center", gap: "10px" }}>
            <Box sx={{ fontSize: 14, fontWeight: 600 }}>Team Management — {currentTeam?.name ?? `Team #${id}`}</Box>
            {/* A switcher only when there's genuinely more than one Team to
                switch to (§5.1) — a single-Team Lead sees no switcher at all. */}
            {availableTeams.length > 1 && (
              <TextField
                select
                size="small"
                value={id}
                onChange={(event) => navigate(`/team-management/${event.target.value}`)}
                sx={{ minWidth: 160 }}
              >
                {availableTeams.map((t) => (
                  <MenuItem key={t.team_id} value={t.team_id}>
                    {t.name}
                  </MenuItem>
                ))}
              </TextField>
            )}
          </Box>
          <Button size="small" variant="contained" onClick={() => setAddMemberOpen(true)}>
            Add Person
          </Button>
        </Box>

        <DenseDataGrid<TeamMemberRow>
          apiRef={apiRef}
          rows={filteredMembers}
          columns={columns}
          getRowId={(row) => row.person_id}
          onColumnResize={onColumnResize}
          defaultSort={[{ field: "name", sort: "asc" }]}
          filtering={{
            filterVisible,
            onToggleFilterVisible: () => setFilterVisible((prev) => !prev),
            onResetFilters: resetFilters,
          }}
          processRowUpdate={processRowUpdate}
          onProcessRowUpdateError={(err) => setSnackbarError(formatApiError(err, "please try again."))}
          isCellEditable={(params: GridCellParams<TeamMemberRow>) => editableFields.has(params.field)}
          onCellClick={(params) => {
            if (!editableFields.has(params.field)) return;
            if (apiRef.current.getCellMode(params.id, params.field) === "edit") return;
            apiRef.current.startCellEditMode({ id: params.id, field: params.field });
          }}
        />
      </Box>

      {addMemberOpen && (
        <AddTeamMemberDialog teamId={id} candidates={addCandidates} onClose={() => setAddMemberOpen(false)} />
      )}

      <Snackbar open={!!snackbarError} autoHideDuration={6000} onClose={() => setSnackbarError(null)}>
        <Alert severity="error" onClose={() => setSnackbarError(null)}>
          {snackbarError}
        </Alert>
      </Snackbar>
    </Box>
  );
}
