import { useMemo, useState } from "react";
import { useParams } from "react-router";
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
import {
  useAllTaskResources,
  useCreatePersonRole,
  useDeletePersonRole,
  usePeople,
  usePersonRoles,
  useProjects,
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

// D1.4-92/D1.4-93 — split from what used to combine both modes: this is now
// purely the *singular* per-Team membership view (add/remove members, edit
// nickname/colour/role/is_resource), reachable only at
// /team-management/:teamId — a required param, unlike before. One instance
// per Team (not more), matching Task/Project/Component's own singleton-per-
// item windows. No in-window Team switcher (D1.4-93) — picking a different
// Team always means TeamsManagementPage.tsx's own list, which opens/focuses
// that Team's own separate window; a switcher living *inside* one Team's
// own window to jump to another felt unnatural once Teams already had
// their own separate windows (the same reason Task Detail has no "jump to
// a different Task" control of its own either).
export function TeamManagementPage() {
  const { teamId: teamIdParam } = useParams<{ teamId: string }>();
  const id = Number(teamIdParam);
  useSingletonWindowIdentity(`team-management-${id}`);

  const { person: caller } = useAuth();
  const { data: teams } = useTeams();
  const { data: people } = usePeople();
  const { data: personRoles } = usePersonRoles();
  const { data: tasks } = useTasks();
  const { data: projects } = useProjects();
  const { data: allTaskResources } = useAllTaskResources();

  const isAdmin = !!caller?.is_organisation_admin;

  const canManageThisTeam = Number.isFinite(id) && (isAdmin || isTeamLead(caller, id));

  const updatePersonRoleField = useUpdatePersonRoleField();
  const deletePersonRole = useDeletePersonRole();
  const [addMemberOpen, setAddMemberOpen] = useState(false);
  const [activeTasksWarning, setActiveTasksWarning] = useState<{ row: TeamMemberRow; count: number } | null>(
    null,
  );
  const [snackbarError, setSnackbarError] = useState<string | null>(null);
  const apiRef = useGridApiRef();

  const currentTeam = teams?.find((t) => t.team_id === id);
  useDocumentTitle(currentTeam ? `Team Management — ${currentTeam.name}` : "Team Management");

  const teamMembers = useMemo<TeamMemberRow[]>(() => {
    if (!Number.isFinite(id) || !personRoles || !people) return [];
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
    if (!tasks || !projects || !allTaskResources) return 0;
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
    if (!window.confirm(`Remove "${row.name}" from this Team?`)) return;
    const count = activeTaskCount(row.person_id);
    if (count > 0) {
      setActiveTasksWarning({ row, count });
      return;
    }
    await removeMember(row);
  }

  async function removeMember(row: TeamMemberRow) {
    try {
      await deletePersonRole.mutateAsync({ personId: row.person_id, teamId: id });
    } catch (err) {
      setSnackbarError(formatApiError(err, "please try again."));
    }
  }

  async function processRowUpdate(newRow: TeamMemberRow, oldRow: TeamMemberRow): Promise<TeamMemberRow> {
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

  // Two icons' worth of width (D1.4-92, matching ManagePeoplePage.tsx's own
  // ACTIONS_COLUMN_WIDTH) even though there's only one icon here — sized to
  // fit the "Actions" header text comfortably, not just the icon, and kept
  // consistent with Manage People's own actions column rather than a
  // narrower one-off value.
  const ACTIONS_COLUMN_WIDTH = DENSE_ROW_HEIGHT * 2 + 14;

  const actionsColumn: GridColDef<TeamMemberRow> = {
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
    getActions: (params: GridRowParams<TeamMemberRow>) => [
      // Darker than the muted rgba(0,0,0,0.28) row-action icons used
      // elsewhere (Project/Component's own row icons, TeamsManagementPage's
      // rename/delete icons before D1.4-92 darkened those too) —
      // deliberately so: this icon *is* the whole cell's content, not a
      // secondary affordance beside other content, matching
      // ManagePeoplePage.tsx's own reasoning exactly.
      <GridActionsCellItem
        key="remove"
        icon={<DeleteIcon fontSize="inherit" sx={{ color: "rgba(0,0,0,0.87)" }} />}
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

  if (!canManageThisTeam) {
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
        <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", flexWrap: "wrap", gap: "10px" }}>
          <Box sx={{ fontSize: 14, fontWeight: 600 }}>Team: {currentTeam?.name ?? `#${id}`}</Box>
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
