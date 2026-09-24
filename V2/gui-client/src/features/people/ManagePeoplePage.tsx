import { useState, type KeyboardEvent } from "react";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
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
import VpnKeyIcon from "@mui/icons-material/VpnKey";
import {
  useAllAttachments,
  useAllRemarks,
  useAllTaskResources,
  useComponents,
  useCreatePerson,
  useDeletePerson,
  usePeople,
  usePersonRoles,
  useProjects,
  useSetPersonPassword,
  useTasks,
  useUpdatePersonField,
} from "../../api/hooks";
import type { PersonRecord } from "../../api/types";
import { useAuth } from "../../auth/AuthContext";
import { DenseDataGrid, DENSE_ROW_HEIGHT, useDenseGridColumns } from "../../components/DenseDataGrid";
import { formatApiError } from "../../lib/apiErrors";
import { useDocumentTitle } from "../../lib/useDocumentTitle";
import { useSingletonWindowIdentity } from "../../lib/windowNav";

// ManagePeoplePlan.md §4.3 — Name and Login up front (both needed before a
// new Person is usable for anything); is_organisation_admin left at its
// server-side default, editable afterward in the grid. (colour used to be
// mentioned here too — it moved to PersonRole/Team Management, D-DM-13/
// D1.4-88, since it's no longer a Person-level field at all.)
function CreatePersonDialog({ onClose }: { onClose: () => void }) {
  const [name, setName] = useState("");
  const [login, setLogin] = useState("");
  const [error, setError] = useState<string | null>(null);
  const createPerson = useCreatePerson();

  async function handleConfirm() {
    if (!name.trim()) {
      setError("A name must be specified.");
      return;
    }
    try {
      await createPerson.mutateAsync({ name: name.trim(), external_login: login.trim() || null });
      onClose();
    } catch (err) {
      setError(formatApiError(err, "please try again."));
    }
  }

  function handleKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key === "Enter") handleConfirm();
  }

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>Add New Person</DialogTitle>
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
          onKeyDown={handleKeyDown}
          error={!!error}
          helperText={error ?? undefined}
        />
        <TextField
          label="Login"
          fullWidth
          margin="normal"
          value={login}
          onChange={(event) => setLogin(event.target.value)}
          onKeyDown={handleKeyDown}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleConfirm} disabled={createPerson.isPending}>
          Create
        </Button>
      </DialogActions>
    </Dialog>
  );
}

// ManagePeoplePlan.md §4.4 — one dialog/action for both a brand-new
// Person's first password and resetting an existing one later; admin-driven
// only (D1.4-83), the admin communicates it out of band afterward.
function SetPasswordDialog({ person, onClose }: { person: PersonRecord; onClose: () => void }) {
  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [error, setError] = useState<string | null>(null);
  const setPersonPassword = useSetPersonPassword();

  async function handleConfirm() {
    if (password.length < 8) {
      setError("The password must be at least 8 characters.");
      return;
    }
    if (password !== confirm) {
      setError("The two passwords don't match.");
      return;
    }
    try {
      await setPersonPassword.mutateAsync({ personId: person.person_id, newPassword: password });
      onClose();
    } catch (err) {
      setError(formatApiError(err, "please try again."));
    }
  }

  function handleKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key === "Enter") handleConfirm();
  }

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>Set Password — {person.name}</DialogTitle>
      <DialogContent>
        <TextField
          label="New Password"
          type="password"
          fullWidth
          margin="normal"
          autoFocus
          value={password}
          onChange={(event) => {
            setPassword(event.target.value);
            setError(null);
          }}
          onKeyDown={handleKeyDown}
        />
        <TextField
          label="Confirm Password"
          type="password"
          fullWidth
          margin="normal"
          value={confirm}
          onChange={(event) => {
            setConfirm(event.target.value);
            setError(null);
          }}
          onKeyDown={handleKeyDown}
          error={!!error}
          helperText={error ?? undefined}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleConfirm} disabled={setPersonPassword.isPending}>
          Set Password
        </Button>
      </DialogActions>
    </Dialog>
  );
}

// ManagePeoplePlan.md §4 — organisation-admin-only, org-wide Person
// management: create, edit every Person-level field in place, set/reset a
// password, deactivate, or (narrowly, D-DM-12) delete.
export function ManagePeoplePage() {
  useDocumentTitle("Manage People");
  useSingletonWindowIdentity("people-list");
  const { person: caller } = useAuth();
  const apiRef = useGridApiRef();

  const { data: people } = usePeople();
  // D-DM-12's own eight-table reference check, replicated client-side
  // (§4.5's own delete gate) so the trash icon only shows for a Person it
  // will actually succeed for, rather than always showing it and
  // explaining a rejection after the fact — the same "hide it, don't just
  // reject it" convention TaskGrid's own delete icon already uses
  // (canDeleteRow). The server's own check remains the authoritative
  // backstop (handleDeletePerson's own catch below) for the rare case
  // where this client-side snapshot is stale by the time the click lands.
  const { data: tasks } = useTasks();
  const { data: allTaskResources } = useAllTaskResources();
  const { data: projects } = useProjects();
  const { data: components } = useComponents();
  const { data: allRemarks } = useAllRemarks();
  const { data: allAttachments } = useAllAttachments();
  const { data: personRoles } = usePersonRoles();
  const updatePersonField = useUpdatePersonField();
  const deletePerson = useDeletePerson();
  const [createOpen, setCreateOpen] = useState(false);
  const [passwordTarget, setPasswordTarget] = useState<PersonRecord | null>(null);
  const [snackbarError, setSnackbarError] = useState<string | null>(null);

  const { withFilter, getFilteredRows, filterVisible, setFilterVisible, resetFilters, onColumnResize } =
    useDenseGridColumns<PersonRecord>({
      rows: people ?? [],
      getRowId: (row) => row.person_id,
      showFilters: false,
      apiRef,
    });

  // §4.6 — an admin may demote a *different* admin, but never remove their
  // own flag; enforced authoritatively server-side (update_person), this is
  // just the client-side UX half so the rejection is never actually hit in
  // the normal case.
  function isEditableCell(row: PersonRecord, field: string): boolean {
    if (field === "is_organisation_admin" && row.person_id === caller?.person_id) return false;
    return true;
  }

  // Mirrors teams.py's own _PERSON_REFERENCE_CHECKS exactly (same eight
  // tables, same columns) — see the useX() calls above for where each list
  // comes from.
  function isPersonDeletable(personId: number): boolean {
    if ((tasks ?? []).some((t) => t.owner_person_id === personId || t.requestor_person_id === personId)) {
      return false;
    }
    if ((allTaskResources ?? []).some((r) => r.person_id === personId)) return false;
    if ((projects ?? []).some((p) => p.owner_person_id === personId)) return false;
    if ((components ?? []).some((c) => c.owner_person_id === personId)) return false;
    if ((allRemarks ?? []).some((r) => r.created_by_person_id === personId)) return false;
    if ((allAttachments ?? []).some((a) => a.owner_person_id === personId)) return false;
    if ((personRoles ?? []).some((pr) => pr.person_id === personId)) return false;
    return true;
  }

  async function handleDeletePerson(target: PersonRecord) {
    if (!window.confirm(`Delete Person "${target.name}"? This cannot be undone.`)) return;
    try {
      await deletePerson.mutateAsync(target.person_id);
    } catch (err) {
      setSnackbarError(formatApiError(err, "please try again."));
    }
  }

  async function processRowUpdate(newRow: PersonRecord, oldRow: PersonRecord): Promise<PersonRecord> {
    const changedField = (Object.keys(newRow) as (keyof PersonRecord)[]).find(
      (key) => newRow[key] !== oldRow[key],
    );
    if (!changedField) return oldRow;
    return updatePersonField.mutateAsync({
      personId: oldRow.person_id,
      body: { [changedField]: newRow[changedField] },
    });
  }

  // Two icons side by side need more room than TaskGrid's own single-icon
  // actions column (DENSE_ROW_HEIGHT alone) — that was clipping both icons
  // against the cell's own edges (MUI centres an actions cell's content and
  // clips whatever doesn't fit). Icon buttons also get a tighter inline
  // `style` padding below for the same reason (GridActionsCellItemProps
  // doesn't expose `sx` in its own TS types, even though the runtime
  // component happily forwards it — `style` is both typed and sufficient
  // here), rather than widening the column to fit MUI's own default
  // IconButton padding.
  const ACTIONS_COLUMN_WIDTH = DENSE_ROW_HEIGHT * 2 + 14;

  const actionsColumn: GridColDef<PersonRecord> = {
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
    getActions: (params: GridRowParams<PersonRecord>) => {
      const actions = [
        // Darker than the muted rgba(0,0,0,0.28) row-action icons used
        // elsewhere (TaskGrid/Team Management's delete, Project/Component's
        // row icons) — deliberately so: those sit beside other content
        // (a name, a row) they're secondary to, while here the icon *is*
        // the whole cell, so it should read as the primary content rather
        // than a muted afterthought.
        <GridActionsCellItem
          key="password"
          icon={<VpnKeyIcon fontSize="inherit" sx={{ color: "rgba(0,0,0,0.87)" }} />}
          label="Set Password"
          style={{ padding: "2px" }}
          onClick={() => setPasswordTarget(params.row)}
        />,
      ];
      // §4.5/D1.4-100 — always shown now; disabled (a real no-op click) and
      // a lighter grey for a Person D-DM-12's own reference check (mirrored
      // client-side above) wouldn't actually let this succeed for, rather
      // than hiding the icon entirely — the same "always present" treatment
      // TeamsManagementPage.tsx's/TaskGrid.tsx's own Delete icons use
      // (D1.4-98/D1.4-99), for a consistent look rather than some rows
      // having one icon and some having two.
      const deletable = isPersonDeletable(params.row.person_id);
      actions.push(
        <GridActionsCellItem
          key="delete"
          icon={<DeleteIcon fontSize="inherit" sx={{ color: deletable ? "rgba(0,0,0,0.87)" : "rgba(0,0,0,0.18)" }} />}
          label="Delete"
          style={{ padding: "2px" }}
          disabled={!deletable}
          onClick={deletable ? () => handleDeletePerson(params.row) : undefined}
        />,
      );
      return actions;
    },
  };

  // Every data column is always editable (governed elsewhere: this whole
  // screen is already gated on is_organisation_admin, §4.1's access check
  // below, so there's no "reachable but read-only" viewer to design a
  // per-cell grey-out for the way TaskGrid's own governed columns need —
  // only the self-demotion exception, handled by isEditableCell above).
  const dataColumns: GridColDef<PersonRecord>[] = [
    withFilter({ field: "name", headerName: "Name" }, (row) => [row.name], "string"),
    withFilter(
      { field: "is_active", headerName: "Is Active", type: "boolean", align: "center", headerAlign: "center" },
      (row) => [row.is_active ? "✓" : ""],
      "string",
    ),
    withFilter({ field: "external_login", headerName: "Login" }, (row) => [row.external_login ?? ""], "string"),
    // flex: 1 (last argument) — this has to be the *trailing* column
    // (DenseDataGrid.tsx's own withFilter doc comment): without a flex
    // column, MUI DataGrid leaves whatever width the fixed columns don't
    // use as dead space after the last one (its own "filler" element),
    // which reads as a genuine extra, unlabelled column once the window is
    // wider than the grid's own content — exactly what SearchPage.tsx's
    // Description column already fixed the same way (D1.4-78). Colour used
    // to be this trailing column; it moved to Team Management (D-DM-13/
    // D1.4-88), so Is Organisation Admin takes over the role instead.
    withFilter(
      {
        field: "is_organisation_admin",
        headerName: "Is Organisation Admin",
        type: "boolean",
        align: "center",
        headerAlign: "center",
      },
      (row) => [row.is_organisation_admin ? "✓" : ""],
      "string",
      undefined,
      1,
    ),
  ].map((col) => ({ ...col, editable: true }));

  const columns: GridColDef<PersonRecord>[] = [actionsColumn, ...dataColumns];

  const filteredPeople = getFilteredRows();

  if (!caller?.is_organisation_admin) {
    return (
      <Box sx={{ p: 2, fontSize: 13 }}>You don't have access to this page.</Box>
    );
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
          <Box sx={{ fontSize: 14, fontWeight: 600 }}>Manage People</Box>
          <Button size="small" variant="contained" onClick={() => setCreateOpen(true)}>
            New Person
          </Button>
        </Box>

        <DenseDataGrid<PersonRecord>
          apiRef={apiRef}
          rows={filteredPeople}
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
          isCellEditable={(params: GridCellParams<PersonRecord>) =>
            isEditableCell(params.row, params.field)
          }
          onCellClick={(params) => {
            if (params.field === "__actions") return;
            if (!isEditableCell(params.row, params.field)) return;
            if (apiRef.current.getCellMode(params.id, params.field) === "edit") return;
            apiRef.current.startCellEditMode({ id: params.id, field: params.field });
          }}
        />
      </Box>

      {createOpen && <CreatePersonDialog onClose={() => setCreateOpen(false)} />}
      {passwordTarget && (
        <SetPasswordDialog person={passwordTarget} onClose={() => setPasswordTarget(null)} />
      )}

      <Snackbar open={!!snackbarError} autoHideDuration={6000} onClose={() => setSnackbarError(null)}>
        <Alert severity="error" onClose={() => setSnackbarError(null)}>
          {snackbarError}
        </Alert>
      </Snackbar>
    </Box>
  );
}
