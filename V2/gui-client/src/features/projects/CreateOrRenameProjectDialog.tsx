import { useState, type KeyboardEvent } from "react";
import Button from "@mui/material/Button";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import TextField from "@mui/material/TextField";
import { useCreateProject, useUpdateProject } from "../../api/hooks";
import { formatApiError } from "../../lib/apiErrors";

/**
 * ProjectDetailPlan.md §4.6 — V1.2's dual-purpose `NewProject` dialog:
 * creating a new Project, or renaming an existing one, share this one
 * small modal. Parent Project is always read-only context here (the
 * Project the "Add New Project"/rename action was invoked from) — never a
 * field the user picks in this dialog.
 *
 * Mounted only while open (conditionally rendered by its caller), so each
 * open gets a fresh `useUpdateProject`/`useCreateProject` bound correctly
 * to whichever Project this particular open is acting on — not a
 * long-lived instance reused across different target Projects.
 */
export function CreateOrRenameProjectDialog({
  mode,
  teamId,
  parentProjectId,
  parentProjectName,
  projectId,
  initialName,
  onClose,
}: {
  mode: "create" | "rename";
  teamId: number;
  parentProjectId: number;
  parentProjectName: string;
  /** Required, and ignored, for mode "create". */
  projectId?: number;
  initialName?: string;
  onClose: () => void;
}) {
  const [name, setName] = useState(initialName ?? "");
  const [error, setError] = useState<string | null>(null);
  const createProject = useCreateProject();
  const updateProject = useUpdateProject(projectId ?? -1);
  const pending = createProject.isPending || updateProject.isPending;

  async function handleConfirm() {
    // V1.2's own check, verbatim (ProjectDetail.cs's toolStripButton1_Click,
    // 4_GuiClient/Plan.md D1.4-43) — the only required field for a Project.
    if (!name.trim()) {
      setError("A name for a project must be specified");
      return;
    }
    try {
      if (mode === "create") {
        await createProject.mutateAsync({ team_id: teamId, name: name.trim(), parent_project_id: parentProjectId });
      } else {
        await updateProject.mutateAsync({ name: name.trim() });
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
      <DialogTitle>{mode === "create" ? "Add New Project" : "Rename Project"}</DialogTitle>
      <DialogContent>
        <TextField label="Parent Project" fullWidth margin="normal" value={parentProjectName} slotProps={{ input: { readOnly: true } }} />
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
