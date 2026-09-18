import { useState } from "react";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import FormHelperText from "@mui/material/FormHelperText";
import MenuItem from "@mui/material/MenuItem";
import TextField from "@mui/material/TextField";
import { FieldTreePicker } from "../../components/DenseField";
import { buildBreadcrumb, type TreeItem } from "../../components/TreePicker";
import { useComponents, useCreateTask, usePeople, usePersonRoles } from "../../api/hooks";
import { PRIORITY_LEVELS, TASK_TYPES, type TaskRecord } from "../../api/types";
import { personDisplayName } from "../../lib/people";
import { formatApiError } from "../../lib/apiErrors";

/**
 * ProjectDetailPlan.md §4.7 (`D1.4-43`/`D1.4-45`) — V1.2's `TaskDetail.cs`
 * refuses to save a newly-created Task without a Component, Project,
 * Description, Priority, Task Type, and Requestor. Opened from a
 * Project's own context (as this dialog always is), Project/Priority/
 * Requestor already arrive pre-filled with a sensible default; only
 * Description, Component, and Task Type are genuinely required here.
 */
export function AddTaskDialog({
  projectId,
  teamId,
  defaultRequestorPersonId,
  onClose,
  onCreated,
}: {
  projectId: number;
  teamId: number;
  defaultRequestorPersonId: number | null;
  onClose: () => void;
  onCreated: (task: TaskRecord) => void;
}) {
  const { data: components } = useComponents();
  const { data: people } = usePeople();
  const { data: personRoles } = usePersonRoles();
  const createTask = useCreateTask();

  const [description, setDescription] = useState("");
  const [componentId, setComponentId] = useState<number | null>(null);
  const [taskType, setTaskType] = useState("");
  const [priority, setPriority] = useState("Med");
  const [requestorId, setRequestorId] = useState<number | null>(defaultRequestorPersonId);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [saveError, setSaveError] = useState<string | null>(null);

  const teamComponents = components?.filter((c) => c.team_id === teamId) ?? [];
  const componentTreeItems: TreeItem[] = teamComponents.map((c) => ({
    id: c.component_id,
    name: c.name,
    parentId: c.parent_component_id,
  }));
  // Requestor is org-wide, not Team-scoped — matches TaskDetailPage.tsx's
  // own existing Requestor field exactly (D1.4-15), unlike Owner.
  const activePeople = people?.filter((p) => p.is_active) ?? [];

  async function handleAdd() {
    const newErrors: Record<string, string> = {};
    if (!description.trim()) newErrors.description = "The task must have a Description";
    if (componentId == null) newErrors.component = "A Component must be specified";
    if (!taskType) newErrors.taskType = "The task must have a Task Type";
    setErrors(newErrors);
    if (Object.keys(newErrors).length > 0) return;

    setSaveError(null);
    try {
      const task = await createTask.mutateAsync({
        project_id: projectId,
        description: description.trim(),
        component_id: componentId,
        task_type: taskType,
        priority,
        requestor_person_id: requestorId,
      });
      onCreated(task);
    } catch (err) {
      setSaveError(formatApiError(err, "please try again."));
    }
  }

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>Add Task</DialogTitle>
      <DialogContent>
        <TextField
          label="Description"
          fullWidth
          margin="normal"
          autoFocus
          value={description}
          onChange={(event) => {
            setDescription(event.target.value);
            setErrors((prev) => ({ ...prev, description: "" }));
          }}
          error={!!errors.description}
          helperText={errors.description}
        />
        <Box sx={{ mt: 2, mb: 1 }}>
          <FieldTreePicker
            label="Component"
            flex={1}
            items={componentTreeItems}
            selectedId={componentId}
            breadcrumb={buildBreadcrumb(componentTreeItems, componentId)}
            onSelect={(id) => {
              setComponentId(id);
              setErrors((prev) => ({ ...prev, component: "" }));
            }}
          />
          {errors.component && (
            <FormHelperText error>{errors.component}</FormHelperText>
          )}
        </Box>
        <TextField
          select
          label="Task Type"
          fullWidth
          margin="normal"
          value={taskType}
          onChange={(event) => {
            setTaskType(event.target.value);
            setErrors((prev) => ({ ...prev, taskType: "" }));
          }}
          error={!!errors.taskType}
          helperText={errors.taskType}
        >
          {TASK_TYPES.map((t) => (
            <MenuItem key={t} value={t}>
              {t}
            </MenuItem>
          ))}
        </TextField>
        <TextField
          select
          label="Priority"
          fullWidth
          margin="normal"
          value={priority}
          onChange={(event) => setPriority(event.target.value)}
        >
          {PRIORITY_LEVELS.map((p) => (
            <MenuItem key={p} value={p}>
              {p}
            </MenuItem>
          ))}
        </TextField>
        <TextField
          select
          label="Requestor"
          fullWidth
          margin="normal"
          value={requestorId ?? ""}
          onChange={(event) => setRequestorId(event.target.value === "" ? null : Number(event.target.value))}
        >
          <MenuItem value="">(none)</MenuItem>
          {activePeople.map((p) => (
            <MenuItem key={p.person_id} value={p.person_id}>
              {personDisplayName(p.person_id, teamId, people, personRoles)}
            </MenuItem>
          ))}
        </TextField>
        {saveError && <FormHelperText error>{saveError}</FormHelperText>}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleAdd} disabled={createTask.isPending}>
          Add
        </Button>
      </DialogActions>
    </Dialog>
  );
}
