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
  useTasks,
} from "../../api/hooks";
import { TASK_DRAG_MIME_TYPE } from "../../lib/dnd";

// D1.4-4: an explicit "Add Dependency" search-and-pick dialog, replacing
// V1.2's drag-between-two-listboxes interaction for Level 1.
export function DependenciesPanel({
  taskId,
  hideHeading = false,
}: {
  taskId: number;
  hideHeading?: boolean;
}) {
  const { data: dependencies } = useDependencies(taskId);
  const { data: tasks } = useTasks();
  const createDependency = useCreateDependency(taskId);
  const deleteDependency = useDeleteDependency(taskId);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [direction, setDirection] = useState<"predecessor" | "successor">("predecessor");
  const [selectedTaskId, setSelectedTaskId] = useState<number | null>(null);
  // D1.4-10: which list (if either) a cross-window Ctrl-drag is currently
  // hovering, for the drop-target highlight below.
  const [dragOverZone, setDragOverZone] = useState<"predecessor" | "successor" | null>(null);

  const predecessors = dependencies?.filter((d) => d.post_task_id === taskId) ?? [];
  const successors = dependencies?.filter((d) => d.pre_task_id === taskId) ?? [];

  function isDraggedTask(event: DragEvent): boolean {
    return event.dataTransfer.types.includes(TASK_DRAG_MIME_TYPE);
  }

  // D1.4-10 spike: dropping a dragged Task onto "Depends upon" makes it a
  // predecessor of this Task; onto "Dependants" makes it a successor —
  // same two mutation shapes handleAdd already uses for the explicit "Add
  // Dependency" dialog (D1.4-7), just triggered by a drop instead of a
  // dialog submit. Dropping a Task onto its own Task Detail window is
  // ignored, mirroring V1.2's own `if (preTask == postTask) return;` guard.
  function handleDropOnZone(event: DragEvent, zone: "predecessor" | "successor") {
    event.preventDefault();
    setDragOverZone(null);
    const draggedTaskId = Number(event.dataTransfer.getData(TASK_DRAG_MIME_TYPE));
    if (!draggedTaskId || draggedTaskId === taskId) return;
    if (zone === "predecessor") {
      createDependency.mutateAsync({ pre_task_id: draggedTaskId, post_task_id: taskId });
    } else {
      createDependency.mutateAsync({ pre_task_id: taskId, post_task_id: draggedTaskId });
    }
  }

  function dropZoneSx(zone: "predecessor" | "successor") {
    return dragOverZone === zone
      ? { outline: "2px dashed", outlineColor: "primary.main", outlineOffset: "-2px", borderRadius: "4px" }
      : {};
  }

  function taskDescription(id: number | null) {
    if (id === null) return "(a Project)";
    return tasks?.find((t) => t.task_id === id)?.description ?? `Task #${id}`;
  }

  async function handleAdd() {
    if (selectedTaskId === null) return;
    if (direction === "predecessor") {
      await createDependency.mutateAsync({ pre_task_id: selectedTaskId, post_task_id: taskId });
    } else {
      await createDependency.mutateAsync({ pre_task_id: taskId, post_task_id: selectedTaskId });
    }
    setDialogOpen(false);
    setSelectedTaskId(null);
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
            <ListItemText primary={taskDescription(dep.pre_task_id)} />
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
            <ListItemText primary={taskDescription(dep.post_task_id)} />
          </ListItem>
        ))}
        {successors.length === 0 && (
          <Typography variant="body2" color="text.secondary" sx={{ pl: 2 }}>
            None.
          </Typography>
        )}
      </List>

      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)} fullWidth maxWidth="xs">
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
            options={tasks?.filter((t) => t.task_id !== taskId) ?? []}
            getOptionLabel={(task) => `#${task.task_id} — ${task.description}`}
            onChange={(_event, value) => setSelectedTaskId(value?.task_id ?? null)}
            renderInput={(params) => <TextField {...params} label="Task" autoFocus />}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialogOpen(false)}>Cancel</Button>
          <Button variant="contained" onClick={handleAdd} disabled={selectedTaskId === null}>
            Add
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
