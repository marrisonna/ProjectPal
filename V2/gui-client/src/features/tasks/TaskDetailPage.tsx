import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Checkbox from "@mui/material/Checkbox";
import CircularProgress from "@mui/material/CircularProgress";
import Divider from "@mui/material/Divider";
import FormControlLabel from "@mui/material/FormControlLabel";
import IconButton from "@mui/material/IconButton";
import MenuItem from "@mui/material/MenuItem";
import Paper from "@mui/material/Paper";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import ToggleButton from "@mui/material/ToggleButton";
import ToggleButtonGroup from "@mui/material/ToggleButtonGroup";
import Typography from "@mui/material/Typography";
import OpenInNewIcon from "@mui/icons-material/OpenInNew";
import {
  useAssignResource,
  useComponents,
  useDependencies,
  usePeople,
  useProjects,
  useTask,
  useTaskResources,
  useTasks,
  useUnassignResource,
  useUpdateTask,
} from "../../api/hooks";
import { PRIORITY_LEVELS, TASK_STATUSES, TASK_TYPES } from "../../api/types";
import { openItemWindow } from "../../lib/windowNav";
import {
  computeDuration,
  computeEarliestStartDate,
  computeEndDate,
  computeStartDate,
} from "../../lib/schedule";
import { RemarksPanel } from "../remarks/RemarksPanel";
import { DependenciesPanel } from "../dependencies/DependenciesPanel";
import { AttachmentsPanel } from "../attachments/AttachmentsPanel";

function formatDate(date: Date | null): string {
  return date ? date.toLocaleDateString(undefined, { day: "2-digit", month: "short", year: "numeric" }) : "—";
}

export function TaskDetailPage() {
  const { taskId } = useParams<{ taskId: string }>();
  const id = Number(taskId);
  const navigate = useNavigate();

  const { data: task, isLoading } = useTask(id);
  const { data: projects } = useProjects();
  const { data: components } = useComponents();
  const { data: people } = usePeople();
  const { data: assignedResources } = useTaskResources(id);
  const { data: allTasks } = useTasks();
  const { data: dependencies } = useDependencies(id);
  const updateTask = useUpdateTask(id);
  const assignResource = useAssignResource(id);
  const unassignResource = useUnassignResource(id);

  const [form, setForm] = useState<Record<string, unknown> | null>(null);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [resourceError, setResourceError] = useState<string | null>(null);

  useEffect(() => {
    if (task) setForm({ ...task });
  }, [task]);

  // Also wait on the reference lists the form's <TextField select> options
  // come from — rendering a select with no options yet (before they load)
  // is a real MUI warning, not just a cosmetic flash.
  if (isLoading || !form || !projects || !components || !people) {
    return <CircularProgress />;
  }

  function field(name: string) {
    return form![name] ?? "";
  }
  function setField(name: string, value: unknown) {
    setForm((prev) => ({ ...prev!, [name]: value }));
  }

  async function handleSave() {
    setSaveError(null);
    try {
      await updateTask.mutateAsync(form!);
    } catch {
      setSaveError("Save failed — check required fields and try again.");
    }
  }

  const teamProject = projects?.find((p) => p.project_id === field("project_id"));
  const teamComponents = components?.filter((c) => c.team_id === teamProject?.team_id) ?? [];
  const assignedIds = new Set(assignedResources?.map((r) => r.person_id));
  // V1.2's Owner/Requestor/Resource pickers list Person.AllActiveInstances,
  // not every Person on record (V1.2/Libs/DBProjectPal/DBProjectPal/GUITaskColumns
  // usage) — matched here (D1.4-15).
  const activePeople = people?.filter((p) => p.is_active) ?? [];

  // Computed display dates (D1.4-14) — see src/lib/schedule.ts for the V1.2
  // logic this reproduces. Uses the live form values so editing Effort/the
  // start offset updates these before Save, matching V1.2's live recalculation.
  const formTask = task ? ({ ...task, ...form } as typeof task) : null;
  const earliestStartDate = formTask ? computeEarliestStartDate(formTask, teamProject) : null;
  const predecessorDependencies = dependencies?.filter((d) => d.post_task_id === id) ?? [];
  const startDate = formTask
    ? computeStartDate(formTask, teamProject, predecessorDependencies, allTasks ?? [], projects ?? [])
    : null;
  const duration = formTask ? computeDuration(formTask, assignedResources?.length ?? 0) : null;
  const endDate = computeEndDate(startDate, duration);

  return (
    <Box>
      <Box sx={{ display: "flex", alignItems: "center", gap: 2, mb: 2 }}>
        <Typography variant="h5" component="h1">
          Task #{id}
        </Typography>
        <IconButton
          size="small"
          title="Open in new window"
          onClick={() => openItemWindow("tasks", id)}
        >
          <OpenInNewIcon fontSize="small" />
        </IconButton>
        <Box sx={{ flexGrow: 1 }} />
        <Button variant="outlined" onClick={() => navigate("/tasks")}>
          Back to list
        </Button>
        <Button variant="contained" onClick={handleSave} disabled={updateTask.isPending}>
          Save
        </Button>
      </Box>

      {saveError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {saveError}
        </Alert>
      )}

      <Paper sx={{ p: 3, mb: 3 }}>
        <TextField
          label="Description"
          fullWidth
          margin="normal"
          value={field("description")}
          onChange={(event) => setField("description", event.target.value)}
        />

        <Box sx={{ display: "grid", gridTemplateColumns: "repeat(4, 1fr)", gap: 2, mt: 1 }}>
          <TextField
            select
            label="Project"
            value={field("project_id")}
            onChange={(event) => setField("project_id", Number(event.target.value))}
          >
            {projects?.map((p) => (
              <MenuItem key={p.project_id} value={p.project_id}>
                {p.name}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            select
            label="Component"
            value={field("component_id") ?? ""}
            onChange={(event) =>
              setField("component_id", event.target.value === "" ? null : Number(event.target.value))
            }
          >
            <MenuItem value="">(none)</MenuItem>
            {teamComponents.map((c) => (
              <MenuItem key={c.component_id} value={c.component_id}>
                {c.name}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            select
            label="Priority"
            value={field("priority") ?? ""}
            onChange={(event) => setField("priority", event.target.value || null)}
          >
            <MenuItem value="">(none)</MenuItem>
            {PRIORITY_LEVELS.map((p) => (
              <MenuItem key={p} value={p}>
                {p}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            select
            label="Status"
            value={field("status")}
            onChange={(event) => setField("status", event.target.value)}
          >
            {TASK_STATUSES.map((s) => (
              <MenuItem key={s} value={s}>
                {s}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            select
            label="Task Type"
            value={field("task_type") ?? ""}
            onChange={(event) => setField("task_type", event.target.value || null)}
          >
            <MenuItem value="">(none)</MenuItem>
            {TASK_TYPES.map((t) => (
              <MenuItem key={t} value={t}>
                {t}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            select
            label="Owner"
            value={field("owner_person_id") ?? ""}
            onChange={(event) =>
              setField("owner_person_id", event.target.value === "" ? null : Number(event.target.value))
            }
          >
            <MenuItem value="">(none)</MenuItem>
            {activePeople.map((p) => (
              <MenuItem key={p.person_id} value={p.person_id}>
                {p.name}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            select
            label="Requestor"
            value={field("requestor_person_id") ?? ""}
            onChange={(event) =>
              setField(
                "requestor_person_id",
                event.target.value === "" ? null : Number(event.target.value),
              )
            }
          >
            <MenuItem value="">(none)</MenuItem>
            {activePeople.map((p) => (
              <MenuItem key={p.person_id} value={p.person_id}>
                {p.name}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            label="Start offset (business days from Project start)"
            type="number"
            value={field("start_relative_days_to_project") ?? ""}
            onChange={(event) =>
              setField(
                "start_relative_days_to_project",
                event.target.value === "" ? null : Number(event.target.value),
              )
            }
          />
        </Box>

        <Box sx={{ display: "grid", gridTemplateColumns: "repeat(4, 1fr)", gap: 2, mt: 2 }}>
          <TextField
            label="Requested Start Date"
            value={formatDate(earliestStartDate)}
            slotProps={{ input: { readOnly: true } }}
            helperText="Computed from the start offset — not stored (§4.1)"
          />
          <TextField
            label="Planned Start Date"
            value={formatDate(startDate)}
            slotProps={{ input: { readOnly: true } }}
            helperText="Also accounts for predecessor Dependencies"
          />
          <TextField
            label="End Date"
            value={formatDate(endDate)}
            slotProps={{ input: { readOnly: true } }}
            helperText="Planned Start + Duration"
          />
          <TextField
            label="Duration (calendar days)"
            value={duration != null ? duration.toFixed(1) : "—"}
            slotProps={{ input: { readOnly: true } }}
            helperText="Effort split across assigned Resources"
          />
        </Box>

        <Box sx={{ display: "grid", gridTemplateColumns: "repeat(4, 1fr)", gap: 2, mt: 2 }}>
          <TextField
            label="Effort"
            type="number"
            value={field("effort_in_days") ?? ""}
            onChange={(event) =>
              setField("effort_in_days", event.target.value === "" ? null : Number(event.target.value))
            }
          />
          <ToggleButtonGroup
            exclusive
            value={field("effort_type") ?? null}
            onChange={(_event, value) => setField("effort_type", value)}
            size="small"
            sx={{ alignSelf: "center" }}
          >
            <ToggleButton value="PersonDays">Person Days</ToggleButton>
            <ToggleButton value="Duration">Duration</ToggleButton>
          </ToggleButtonGroup>
          <TextField
            label="% Allocation"
            type="number"
            value={field("percentage_allocation") ?? ""}
            onChange={(event) =>
              setField(
                "percentage_allocation",
                event.target.value === "" ? null : Number(event.target.value),
              )
            }
          />
          <FormControlLabel
            sx={{ alignSelf: "center" }}
            control={
              <Checkbox
                checked={Boolean(field("tentative_resource_assignment"))}
                onChange={(event) => setField("tentative_resource_assignment", event.target.checked)}
              />
            }
            label="Tentative"
          />
        </Box>

        <TextField
          label="External Reference URL"
          fullWidth
          margin="normal"
          value={field("external_reference_url") ?? ""}
          onChange={(event) => setField("external_reference_url", event.target.value || null)}
        />
        <TextField
          label="Detailed Description"
          fullWidth
          multiline
          minRows={4}
          margin="normal"
          value={field("detailed_description") ?? ""}
          onChange={(event) => setField("detailed_description", event.target.value)}
        />
      </Paper>

      <Paper sx={{ p: 3, mb: 3 }}>
        <Typography variant="subtitle1" gutterBottom>
          Resources
        </Typography>
        {resourceError && (
          <Alert severity="error" sx={{ mb: 1 }} onClose={() => setResourceError(null)}>
            {resourceError}
          </Alert>
        )}
        <Typography variant="caption" color="text.secondary" sx={{ display: "block", mb: 1 }}>
          Shows every active Person — the API rejects assigning someone who isn't a resource on
          this Task's Team.
        </Typography>
        <Stack direction="row" sx={{ flexWrap: "wrap", gap: 1 }}>
          {activePeople.map((person) => (
            <FormControlLabel
              key={person.person_id}
              control={
                <Checkbox
                  size="small"
                  checked={assignedIds.has(person.person_id)}
                  onChange={async (event) => {
                    setResourceError(null);
                    try {
                      if (event.target.checked) {
                        await assignResource.mutateAsync(person.person_id);
                      } else {
                        await unassignResource.mutateAsync(person.person_id);
                      }
                    } catch {
                      setResourceError(
                        `Couldn't assign ${person.name} — they may not be a resource on this Task's Team.`,
                      );
                    }
                  }}
                />
              }
              label={person.name}
            />
          ))}
        </Stack>
      </Paper>

      <Paper sx={{ p: 3, mb: 3 }}>
        <DependenciesPanel taskId={id} />
      </Paper>

      <Paper sx={{ p: 3, mb: 3 }}>
        <AttachmentsPanel owner={{ task_id: id }} />
      </Paper>

      <Paper sx={{ p: 3 }}>
        <RemarksPanel owner={{ task_id: id }} />
      </Paper>

      <Divider sx={{ my: 3 }} />
    </Box>
  );
}
