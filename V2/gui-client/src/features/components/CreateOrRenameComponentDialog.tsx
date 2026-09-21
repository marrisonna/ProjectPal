import { useState, type KeyboardEvent } from "react";
import Button from "@mui/material/Button";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import TextField from "@mui/material/TextField";
import { useCreateComponent, useUpdateComponent } from "../../api/hooks";
import { formatApiError } from "../../lib/apiErrors";

/**
 * ComponentDetailPlan.md §4.5 — a direct copy of
 * `CreateOrRenameProjectDialog.tsx`'s own shape: V1.2's dual-purpose
 * `NewComponent` dialog, creating a new Component or renaming an existing
 * one sharing this one small modal. Parent Component is always read-only
 * context here (the Component the "Add Subcomponent"/rename action was
 * invoked from) — never a field the user picks in this dialog.
 *
 * Mounted only while open (conditionally rendered by its caller), so each
 * open gets a fresh `useUpdateComponent`/`useCreateComponent` bound
 * correctly to whichever Component this particular open is acting on.
 */
export function CreateOrRenameComponentDialog({
  mode,
  teamId,
  parentComponentId,
  parentComponentName,
  componentId,
  initialName,
  onClose,
}: {
  mode: "create" | "rename";
  teamId: number;
  parentComponentId: number;
  parentComponentName: string;
  /** Required, and ignored, for mode "create". */
  componentId?: number;
  initialName?: string;
  onClose: () => void;
}) {
  const [name, setName] = useState(initialName ?? "");
  const [error, setError] = useState<string | null>(null);
  const createComponent = useCreateComponent();
  const updateComponent = useUpdateComponent(componentId ?? -1);
  const pending = createComponent.isPending || updateComponent.isPending;

  async function handleConfirm() {
    // Same check as CreateOrRenameProjectDialog.tsx, verbatim — the only
    // required field for a Component.
    if (!name.trim()) {
      setError("A name for a component must be specified");
      return;
    }
    try {
      if (mode === "create") {
        await createComponent.mutateAsync({
          team_id: teamId,
          name: name.trim(),
          parent_component_id: parentComponentId,
        });
      } else {
        await updateComponent.mutateAsync({ name: name.trim() });
      }
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
      <DialogTitle>{mode === "create" ? "Add New Component" : "Rename Component"}</DialogTitle>
      <DialogContent>
        <TextField
          label="Parent Component"
          fullWidth
          margin="normal"
          value={parentComponentName}
          slotProps={{ input: { readOnly: true } }}
        />
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
