import { useState, type DragEvent } from "react";
import Autocomplete from "@mui/material/Autocomplete";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import IconButton from "@mui/material/IconButton";
import List from "@mui/material/List";
import ListItem from "@mui/material/ListItem";
import ListItemText from "@mui/material/ListItemText";
import TextField from "@mui/material/TextField";
import ToggleButton from "@mui/material/ToggleButton";
import ToggleButtonGroup from "@mui/material/ToggleButtonGroup";
import Typography from "@mui/material/Typography";
import DeleteIcon from "@mui/icons-material/Delete";
import {
  useCreateDependency,
  useDeleteDependency,
  useDependencies,
  useProjects,
  useTasks,
  type DependencyOwner,
} from "../../api/hooks";
import type { DependencyRecord } from "../../api/types";
import { formatApiError } from "../../lib/apiErrors";
import { TASK_DRAG_MIME_TYPE } from "../../lib/dnd";

// Either side of a Dependency can be a Task or a Project (KeyConcepts.md's
// Dependency entry) — one option list combining both, for the "Add
// Dependency" search dialog (ProjectDetailPlan.md §5.2, generalising this
// panel beyond its original Task-only shape).
interface DependencyOption {
  kind: "task" | "project";
  id: number;
  label: string;
}

// D1.4-4: an explicit "Add Dependency" search-and-pick dialog, replacing
// V1.2's drag-between-two-listboxes interaction for Level 1.
export function DependenciesPanel({
  owner,
  hideHeading = false,
}: {
  owner: DependencyOwner;
  hideHeading?: boolean;
}) {
  const { data: dependencies } = useDependencies(owner);
  const { data: tasks } = useTasks();
  const { data: projects } = useProjects();
  const createDependency = useCreateDependency();
  const deleteDependency = useDeleteDependency();
  const [dialogOpen, setDialogOpen] = useState(false);
  const [direction, setDirection] = useState<"predecessor" | "successor">("predecessor");
  const [selectedOption, setSelectedOption] = useState<DependencyOption | null>(null);
  const [addError, setAddError] = useState<string | null>(null);
  // D1.4-10: which list (if either) a cross-window Ctrl-drag is currently
  // hovering, for the drop-target highlight below.
  const [dragOverZone, setDragOverZone] = useState<"predecessor" | "successor" | null>(null);

  const ownerTaskId = "task_id" in owner ? owner.task_id : null;
  const ownerProjectId = "project_id" in owner ? owner.project_id : null;

  function isThisOwner(taskId: number | null, projectId: number | null): boolean {
    return (
      (ownerTaskId != null && taskId === ownerTaskId) ||
      (ownerProjectId != null && projectId === ownerProjectId)
    );
  }

  const predecessors = dependencies?.filter((d) => isThisOwner(d.post_task_id, d.post_project_id)) ?? [];
  const successors = dependencies?.filter((d) => isThisOwner(d.pre_task_id, d.pre_project_id)) ?? [];

  function isDraggedTask(event: DragEvent): boolean {
    return event.dataTransfer.types.includes(TASK_DRAG_MIME_TYPE);
  }

  // D1.4-10 spike: dropping a dragged Task onto "Depends upon" makes it a
  // predecessor of this owner; onto "Dependants" makes it a successor —
  // same two mutation shapes handleAdd already uses for the explicit "Add
  // Dependency" dialog (D1.4-7), just triggered by a drop instead of a
  // dialog submit. Dropping a Task onto its own window does nothing,
  // mirroring V1.2's own `if (preTask == postTask) return;` guard. Only a
  // Task can be dragged in today (Project Detail isn't a drag source yet —
  // deferred to Stage 5, Plan.md D1.4-40), but this owner-generic body
  // works unchanged once it is.
  function handleDropOnZone(event: DragEvent, zone: "predecessor" | "successor") {
    event.preventDefault();
    setDragOverZone(null);
    const draggedTaskId = Number(event.dataTransfer.getData(TASK_DRAG_MIME_TYPE));
    if (!draggedTaskId || (ownerTaskId != null && draggedTaskId === ownerTaskId)) return;
    const ownerField = ownerTaskId != null ? "task" : "project";
    const ownerId = ownerTaskId ?? ownerProjectId!;
    if (zone === "predecessor") {
      createDependency.mutateAsync({
        pre_task_id: draggedTaskId,
        [`post_${ownerField}_id`]: ownerId,
      });
    } else {
      createDependency.mutateAsync({
        [`pre_${ownerField}_id`]: ownerId,
        post_task_id: draggedTaskId,
      });
    }
  }

  function dropZoneSx(zone: "predecessor" | "successor") {
    return dragOverZone === zone
      ? { outline: "2px dashed", outlineColor: "primary.main", outlineOffset: "-2px", borderRadius: "4px" }
      : {};
  }

  function otherSideLabel(dep: DependencyRecord, side: "pre" | "post"): string {
    const taskId = side === "pre" ? dep.pre_task_id : dep.post_task_id;
    const projectId = side === "pre" ? dep.pre_project_id : dep.post_project_id;
    if (taskId != null) return tasks?.find((t) => t.task_id === taskId)?.description ?? `Task #${taskId}`;
    if (projectId != null) {
      const name = projects?.find((p) => p.project_id === projectId)?.name;
      return `Project — ${name ?? `#${projectId}`}`;
    }
    return "(unknown)";
  }

  const options: DependencyOption[] = [
    ...(tasks ?? [])
      .filter((t) => !(ownerTaskId != null && t.task_id === ownerTaskId))
      .map((t): DependencyOption => ({ kind: "task", id: t.task_id, label: `Task #${t.task_id} — ${t.description}` })),
    ...(projects ?? [])
      .filter((p) => !(ownerProjectId != null && p.project_id === ownerProjectId))
      .map((p): DependencyOption => ({ kind: "project", id: p.project_id, label: `Project — ${p.name}` })),
  ];

  async function handleAdd() {
    if (!selectedOption) return;
    setAddError(null);
    const ownerField = ownerTaskId != null ? "task" : "project";
    const ownerId = ownerTaskId ?? ownerProjectId!;
    const otherField = selectedOption.kind;
    try {
      if (direction === "predecessor") {
        // The selected item is the predecessor (pre); this owner is the successor (post).
        await createDependency.mutateAsync({
          [`pre_${otherField}_id`]: selectedOption.id,
          [`post_${ownerField}_id`]: ownerId,
        });
      } else {
        // This owner is the predecessor (pre); the selected item is the successor (post).
        await createDependency.mutateAsync({
          [`pre_${ownerField}_id`]: ownerId,
          [`post_${otherField}_id`]: selectedOption.id,
        });
      }
      setDialogOpen(false);
      setSelectedOption(null);
    } catch (err) {
      // Found missing entirely while verifying ProjectDetailPlan.md's own
      // generalisation of this panel: creating a Dependency requires being
      // owner-or-TeamLeadUser on *both* sides (dependencies.py's
      // create_dependency) — a real, common rejection (e.g. picking a Task
      // on a Team the caller has no standing on) that silently vanished as
      // an unhandled promise rejection before this, leaving the dialog
      // just sitting there with no visible feedback.
      setAddError(formatApiError(err, "please try again."));
    }
  }

  return (
    <Box>
      <Box sx={{ display: "flex", justifyContent: hideHeading ? "flex-end" : "space-between", alignItems: "center" }}>
        {!hideHeading && <Typography variant="subtitle1">Dependencies</Typography>}
        <Button size="small" onClick={() => setDialogOpen(true)}>
          Add Dependency
        </Button>
      </Box>

      <Typography variant="caption" color="text.secondary">
        Depends upon (predecessors)
      </Typography>
      <List
        dense
        sx={dropZoneSx("predecessor")}
        onDragOver={(event) => {
          if (!isDraggedTask(event)) return;
          event.preventDefault();
          setDragOverZone("predecessor");
        }}
        onDragLeave={() => setDragOverZone((zone) => (zone === "predecessor" ? null : zone))}
        onDrop={(event) => {
          if (!isDraggedTask(event)) return;
          handleDropOnZone(event, "predecessor");
        }}
      >
        {predecessors.map((dep) => (
          <ListItem
            key={dep.dependency_id}
            secondaryAction={
              <IconButton
                edge="end"
                size="small"
                onClick={() => deleteDependency.mutate(dep.dependency_id)}
              >
                <DeleteIcon fontSize="small" />
              </IconButton>
            }
          >
            <ListItemText primary={otherSideLabel(dep, "pre")} />
          </ListItem>
        ))}
        {predecessors.length === 0 && (
          <Typography variant="body2" color="text.secondary" sx={{ pl: 2 }}>
            None.
          </Typography>
        )}
      </List>

      <Typography variant="caption" color="text.secondary">
        Dependants (successors)
      </Typography>
      <List
        dense
        sx={dropZoneSx("successor")}
        onDragOver={(event) => {
          if (!isDraggedTask(event)) return;
          event.preventDefault();
          setDragOverZone("successor");
        }}
        onDragLeave={() => setDragOverZone((zone) => (zone === "successor" ? null : zone))}
        onDrop={(event) => {
          if (!isDraggedTask(event)) return;
          handleDropOnZone(event, "successor");
        }}
      >
        {successors.map((dep) => (
          <ListItem
            key={dep.dependency_id}
            secondaryAction={
              <IconButton
                edge="end"
                size="small"
                onClick={() => deleteDependency.mutate(dep.dependency_id)}
              >
                <DeleteIcon fontSize="small" />
              </IconButton>
            }
          >
            <ListItemText primary={otherSideLabel(dep, "post")} />
          </ListItem>
        ))}
        {successors.length === 0 && (
          <Typography variant="body2" color="text.secondary" sx={{ pl: 2 }}>
            None.
          </Typography>
        )}
      </List>

      <Dialog
        open={dialogOpen}
        onClose={() => {
          setDialogOpen(false);
          setAddError(null);
        }}
        fullWidth
        maxWidth="xs"
      >
        <DialogTitle>Add Dependency</DialogTitle>
        <DialogContent>
          <ToggleButtonGroup
            exclusive
            value={direction}
            onChange={(_event, value) => value && setDirection(value)}
            size="small"
            sx={{ mb: 2, mt: 1 }}
          >
            <ToggleButton value="predecessor">Depends upon</ToggleButton>
            <ToggleButton value="successor">Is depended upon by</ToggleButton>
          </ToggleButtonGroup>
          <Autocomplete
            options={options}
            getOptionLabel={(option) => option.label}
            isOptionEqualToValue={(a, b) => a.kind === b.kind && a.id === b.id}
            onChange={(_event, value) => {
              setSelectedOption(value);
              setAddError(null);
            }}
            renderInput={(params) => <TextField {...params} label="Task or Project" autoFocus />}
          />
          {addError && (
            <Typography variant="caption" color="error" sx={{ display: "block", mt: 1 }}>
              {addError}
            </Typography>
          )}
        </DialogContent>
        <DialogActions>
          <Button
            onClick={() => {
              setDialogOpen(false);
              setAddError(null);
            }}
          >
            Cancel
          </Button>
          <Button variant="contained" onClick={handleAdd} disabled={!selectedOption || createDependency.isPending}>
            Add
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
