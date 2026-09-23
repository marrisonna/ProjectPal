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
  type GridRenderEditCellParams,
  type GridRowParams,
} from "@mui/x-data-grid";
import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import IconButton from "@mui/material/IconButton";
import {
  useAllTaskResources,
  useCreatePersonRole,
  useCreateTeam,
  useDeletePersonRole,
  useDeleteTeam,
  usePeople,
  usePersonRoles,
  useProjects,
  useRenameTeam,
  useTasks,
  useTeams,
  useUpdatePersonRoleField,
} from "../../api/hooks";
import type { PersonRecord, PersonRoleRecord, TeamRecord } from "../../api/types";
import { useAuth } from "../../auth/AuthContext";
import {
  DenseDataGrid,
  DenseSingleSelectEditCell,
  DENSE_ROW_HEIGHT,
  useDenseGridColumns,
} from "../../components/DenseDataGrid";
import { formatApiError } from "../../lib/apiErrors";
import { suggestNextColour } from "../../lib/colourPalette";
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

// A native <input type="color"> edit cell (this app's own "native controls"
// convention, DenseField.tsx) — the one HTML control that naturally
// constrains input to a valid colour. Commits immediately on change, the
// same shape DenseSingleSelectEditCell already uses, and for the same
// reason: `props.api` is the grid's own live API, passed straight in — not
// `useGridApiRef()`, which called fresh here would just be a new,
// disconnected `useRef(null)`. Moved here from ManagePeoplePage.tsx
// (D-DM-13/D1.4-88) — colour is no longer a Person-level field.
function ColourEditCell(props: GridRenderEditCellParams<TeamMemberRow>) {
  const { id, field, value, api } = props;
  return (
    <input
      type="color"
      autoFocus
      value={(value as string | null) ?? "#ffffff"}
      style={{ width: "100%", height: "100%", border: "none", padding: 0, background: "transparent", cursor: "pointer" }}
      onChange={async (event) => {
        await api.setEditCellValue({ id, field, value: event.target.value });
        api.stopCellEditMode({ id, field });
      }}
    />
  );
}

function ColourSwatch({ colour }: { colour: string | null }) {
  return (
    <Box
      sx={{
        width: 14,
        height: 14,
        mx: "auto",
        borderRadius: "2px",
        border: "1px solid rgba(0,0,0,0.3)",
        bgcolor: colour ?? "#fff",
      }}
    />
  );
}

// ManagePeoplePlan.md §5.3 — a Team Lead cannot create Person records at
// all (D-DM-4); this dialog only ever picks among People who already exist
// and aren't already on this Team. `existingColours` (D-DM-13/D1.4-88) is
// every current member's own colour on this Team — used to pre-fill a
// sensible, not-already-used default (suggestNextColour), still fully
// editable before confirming.
function AddTeamMemberDialog({
  teamId,
  candidates,
  existingColours,
  onClose,
}: {
  teamId: number;
  candidates: PersonRecord[];
  existingColours: (string | null)[];
  onClose: () => void;
}) {
  const [personId, setPersonId] = useState<number | "">("");
  const [role, setRole] = useState("NormalUser");
  const [isResource, setIsResource] = useState(false);
  // Computed once, from the Team's membership as it stood when this dialog
  // opened — not re-suggested on every render, so it doesn't change out
  // from under someone who's already looked at or edited it.
  const [colour, setColour] = useState(() => suggestNextColour(existingColours));
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
        colour,
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
        <Box sx={{ display: "flex", alignItems: "center", gap: "10px", mt: "16px" }}>
          <Box sx={{ fontSize: 13 }}>Colour</Box>
          <Box component="input" type="color" value={colour} onChange={(event) => setColour(event.target.value)} />
        </Box>
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

// The same muted row-icon treatment Project.tsx's own rename affordance
// uses (its ROW_ICON_SX/ROW_ICON_BUTTON_SX aren't exported, so redefined
// here rather than reached into).
const ROW_ICON_SX = { fontSize: 15, color: "rgba(0,0,0,0.28)" };
const ROW_ICON_BUTTON_SX = { p: "3px", "&:hover .MuiSvgIcon-root": { color: "primary.main" } };

// org-admin-only create/rename (create_team/rename_team both call
// require_org_admin) — dual-purpose the same way
// CreateOrRenameProjectDialog.tsx already is, since a fresh Team needs an
// initial Team Lead (mode "create") while renaming one doesn't touch
// membership at all (mode "rename"). No prior GUI entry point called either
// endpoint at all before this.
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
        {error && (
          <Box sx={{ fontSize: 12, color: "error.main", mt: "4px" }}>{error}</Box>
        )}
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

// The second, more pointed step of removing a member who still has active
// Tasks (§5.4) — a real Dialog, not another window.confirm, specifically so
// its own confirm button can say what it actually does rather than a bare
// "OK" for something this consequential.
function ActiveTasksWarningDialog({
  name,
  count,
  onConfirm,
  onClose,
}: {
  name: string;
  count: number;
  onConfirm: () => void;
  onClose: () => void;
}) {
  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>Remove Team Member?</DialogTitle>
      <DialogContent>
        <Box sx={{ fontSize: 13 }}>
          Are you sure — "{name}" is still assigned to {count} active Task{count === 1 ? "" : "s"}.
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" color="error" onClick={onConfirm}>
          Remove user with Active Tasks
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
  const { data: projects } = useProjects();
  const { data: allTaskResources } = useAllTaskResources();

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
  const deleteTeam = useDeleteTeam();
  const [addMemberOpen, setAddMemberOpen] = useState(false);
  const [createTeamOpen, setCreateTeamOpen] = useState(false);
  const [renameTarget, setRenameTarget] = useState<TeamRecord | null>(null);
  const [activeTasksWarning, setActiveTasksWarning] = useState<{ row: TeamMemberRow; count: number } | null>(
    null,
  );
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
  // open Tasks on *this Team* still name them as owner, requestor, or
  // assigned Resource, so the confirmation is informative rather than just
  // "are you sure?" with no context. Scoped via each Task's own Project's
  // team_id — a bare `t.owner_person_id === personId` check across every
  // Task org-wide (tried first) counted a Person's *other* Teams' Tasks
  // too, which is what made removing a brand-new Team's own lone member
  // wrongly warn about Tasks that had nothing to do with this Team at all.
  function activeTaskCount(personId: number): number {
    if (id == null || !tasks || !projects || !allTaskResources) return 0;
    const teamProjectIds = new Set(projects.filter((p) => p.team_id === id).map((p) => p.project_id));
    const resourceTaskIds = new Set(
      allTaskResources.filter((r) => r.person_id === personId).map((r) => r.task_id),
    );
    let count = 0;
    for (const t of tasks) {
      if (!teamProjectIds.has(t.project_id)) continue;
      if (!isTaskVisible(t, "Open")) continue;
      if (t.owner_person_id === personId || t.requestor_person_id === personId || resourceTaskIds.has(t.task_id)) {
        count++;
      }
    }
    return count;
  }

  // Two steps, not one: a plain "are you sure?" always comes first: if the
  // Person has no active Tasks on this Team, that's the end of it. Only
  // when they do does a second, more pointed dialog appear — deliberately
  // not another window.confirm (its OK/Cancel can't be relabelled), since
  // the whole point here is a button that says what it actually does
  // rather than a generic "OK" for something this consequential.
  async function handleRemoveMember(row: TeamMemberRow) {
    if (id == null) return;
    if (!window.confirm(`Remove "${row.name}" from this Team?`)) return;
    const count = activeTaskCount(row.person_id);
    if (count > 0) {
      setActiveTasksWarning({ row, count });
      return;
    }
    await removeMember(row);
  }

  async function removeMember(row: TeamMemberRow) {
    if (id == null) return;
    try {
      await deletePersonRole.mutateAsync({ personId: row.person_id, teamId: id });
    } catch (err) {
      setSnackbarError(formatApiError(err, "please try again."));
    }
  }

  // D-DM-14/D1.4-90 — rejected server-side if the Team has any Project or
  // Component; membership itself is cascaded away, not blocking (see
  // teams.py's own module docstring for why — a Team is created *with* a
  // bootstrap TeamLeadUser that can never be fully removed, so blocking on
  // membership would make this unusable for its own stated purpose). Named
  // here in the confirmation, the same "soft warning" shape as removing a
  // single member.
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

  const editableFields = new Set(["nickname", "colour", "role", "is_resource"]);

  const dataColumns: GridColDef<TeamMemberRow>[] = [
    withFilter({ field: "name", headerName: "Name" }, (row) => [row.name], "string"),
    withFilter({ field: "nickname", headerName: "Nickname" }, (row) => [row.nickname ?? ""], "string"),
    // D-DM-13/D1.4-88 — moved here from Manage People: colour is per-Person
    // *per-Team*, not org-wide, the same way nickname/role/is_resource
    // already are.
    withFilter(
      {
        field: "colour",
        headerName: "Colour",
        align: "center",
        sortable: false,
        renderCell: (params) => <ColourSwatch colour={params.row.colour} />,
        renderEditCell: (params) => <ColourEditCell {...params} />,
      },
      (row) => [row.colour ?? ""],
      "string",
    ),
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

  if (!teams || !people || !personRoles || !tasks || !projects || !allTaskResources) {
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
          <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
            <Box sx={{ fontSize: 14, fontWeight: 600 }}>Team Management</Box>
            {isAdmin && (
              <Button size="small" variant="contained" onClick={() => setCreateTeamOpen(true)}>
                New Team
              </Button>
            )}
          </Box>
          {availableTeams.map((t) => (
            <Box key={t.team_id} sx={{ display: "flex", alignItems: "center", gap: "4px" }}>
              <Button
                variant="outlined"
                onClick={() => navigate(`/team-management/${t.team_id}`)}
                sx={{ justifyContent: "flex-start", flex: 1 }}
              >
                {t.name}
              </Button>
              {isAdmin && (
                <IconButton size="small" sx={ROW_ICON_BUTTON_SX} onClick={() => setRenameTarget(t)}>
                  <EditIcon sx={ROW_ICON_SX} />
                </IconButton>
              )}
              {isAdmin && (
                <IconButton size="small" sx={ROW_ICON_BUTTON_SX} onClick={() => handleDeleteTeam(t)}>
                  <DeleteIcon sx={ROW_ICON_SX} />
                </IconButton>
              )}
            </Box>
          ))}
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
        <AddTeamMemberDialog
          teamId={id}
          candidates={addCandidates}
          existingColours={teamMembers.map((m) => m.colour)}
          onClose={() => setAddMemberOpen(false)}
        />
      )}
      {activeTasksWarning && (
        <ActiveTasksWarningDialog
          name={activeTasksWarning.row.name}
          count={activeTasksWarning.count}
          onConfirm={() => {
            const { row } = activeTasksWarning;
            setActiveTasksWarning(null);
            void removeMember(row);
          }}
          onClose={() => setActiveTasksWarning(null)}
        />
      )}

      <Snackbar open={!!snackbarError} autoHideDuration={6000} onClose={() => setSnackbarError(null)}>
        <Alert severity="error" onClose={() => setSnackbarError(null)}>
          {snackbarError}
        </Alert>
      </Snackbar>
    </Box>
  );
}
