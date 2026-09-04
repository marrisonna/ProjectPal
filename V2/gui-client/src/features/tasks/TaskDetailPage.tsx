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
import Radio from "@mui/material/Radio";
import RadioGroup from "@mui/material/RadioGroup";
import TextField from "@mui/material/TextField";
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
  addBusinessDays,
  businessDaysBetween,
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

function toDateInputValue(date: Date | null): string {
  if (!date) return "";
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, "0");
  const d = String(date.getDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

const RESOURCE_NAME_MAX_CHARS = 10;

function truncateResourceName(name: string): string {
  return name.length > RESOURCE_NAME_MAX_CHARS
    ? `${name.slice(0, RESOURCE_NAME_MAX_CHARS)}…`
    : name;
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
  // Resources listbox ordering (D1.4-18, §3.11): checked-first, alphabetical
  // within each group, recomputed on every render off assignedIds so ticking
  // or unticking a resource re-sorts it immediately.
  const sortedResourcePeople = [...activePeople].sort((a, b) => {
    const aAssigned = assignedIds.has(a.person_id);
    const bAssigned = assignedIds.has(b.person_id);
    if (aAssigned !== bAssigned) return aAssigned ? -1 : 1;
    return a.name.localeCompare(b.name);
  });

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

        {resourceError && (
          <Alert severity="error" sx={{ mb: 1 }} onClose={() => setResourceError(null)}>
            {resourceError}
          </Alert>
        )}
        {/* Resources listbox beside rows 1+2 (Priority/... and Effort/...)
            rather than in its own card — it's a fixed-size control (§3.11,
            D1.4-19) and would otherwise leave an empty card around it. */}
        <Box sx={{ display: "flex", gap: 3, alignItems: "stretch", mt: 1 }}>
          <Box sx={{ flexShrink: 0, display: "flex", flexDirection: "column" }}>
            <Typography variant="caption" color="text.secondary" sx={{ mb: 0.5 }}>
              Resources
            </Typography>
            <Box
              sx={{
                width: 160,
                flex: 1,
                minHeight: 0,
                maxHeight: 110,
                overflowY: "auto",
                border: "1px solid",
                borderColor: "divider",
                borderRadius: 1,
                p: 0.5,
              }}
            >
              {sortedResourcePeople.map((person) => (
                <Box
                  key={person.person_id}
                  component="label"
                  sx={{ display: "flex", alignItems: "center", gap: 0.5, cursor: "pointer" }}
                >
                  <Checkbox
                    size="small"
                    sx={{ p: 0.5 }}
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
                  <Typography variant="body2" noWrap title={person.name}>
                    {truncateResourceName(person.name)}
                  </Typography>
                </Box>
              ))}
            </Box>
          </Box>

          <Box sx={{ flex: 1, minWidth: 0, display: "flex", flexDirection: "column", gap: 2 }}>
            {/* Row 1: Priority/Status/Task Type/Requestor/Owner. */}
            <Box sx={{ display: "grid", gridTemplateColumns: "repeat(5, 1fr)", gap: 2 }}>
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
            </Box>

            {/* Row 2: Effort/Effort Type/% Allocation/dates/Duration/Tentative. */}
            <Box sx={{ display: "flex", flexWrap: "wrap", gap: 2 }}>
              <TextField
                label="Effort"
                type="number"
                sx={{ width: 90 }}
                value={field("effort_in_days") ?? ""}
                onChange={(event) =>
                  setField("effort_in_days", event.target.value === "" ? null : Number(event.target.value))
                }
              />
              {/* Two stacked radio buttons, as V1.2 uses, rather than a wider
                  toggle switch — narrower, and needs no group label since each
                  option already carries its own visible text (D1.4-19). */}
              <RadioGroup
                aria-label="Effort Type"
                value={field("effort_type") ?? ""}
                onChange={(event) => setField("effort_type", event.target.value)}
                sx={{ justifyContent: "flex-end" }}
              >
                <FormControlLabel value="PersonDays" control={<Radio size="small" sx={{ p: 0.5 }} />} label="Days" />
                <FormControlLabel value="Duration" control={<Radio size="small" sx={{ p: 0.5 }} />} label="Duration" />
              </RadioGroup>
              <TextField
                label="% Allocation"
                type="number"
                sx={{ width: 140 }}
                value={field("percentage_allocation") ?? ""}
                onChange={(event) =>
                  setField(
                    "percentage_allocation",
                    event.target.value === "" ? null : Number(event.target.value),
                  )
                }
              />
              <TextField
                label="Requested Start Date"
                type="date"
                sx={{ width: 170 }}
                value={toDateInputValue(earliestStartDate)}
                slotProps={{ inputLabel: { shrink: true } }}
                onChange={(event) => {
                  if (!event.target.value || !teamProject?.start_date) return;
                  const chosen = new Date(`${event.target.value}T00:00:00`);
                  const offset = businessDaysBetween(new Date(teamProject.start_date), chosen);
                  setField("start_relative_days_to_project", offset);
                }}
                helperText="Sets the start offset (§4.1, D1.4-19)"
              />
              <TextField
                label="Planned Start Date"
                sx={{ width: 150 }}
                value={formatDate(startDate)}
                slotProps={{ input: { readOnly: true } }}
                helperText="Accounts for Dependencies"
              />
              <TextField
                label="End Date"
                type="date"
                sx={{ width: 150 }}
                value={toDateInputValue(endDate)}
                slotProps={{ inputLabel: { shrink: true } }}
                onChange={(event) => {
                  if (!event.target.value || !teamProject?.start_date || duration == null) return;
                  const chosen = new Date(`${event.target.value}T00:00:00`);
                  // V1.2's Task.EndDate setter: back-compute the Start Date this
                  // End Date implies (subtracting Duration business days), then
                  // convert that to the stored offset — same as editing
                  // Requested Start Date directly, just anchored at the other
                  // end (D1.4-20).
                  const impliedStart = addBusinessDays(chosen, -(Math.ceil(duration) - 1));
                  const offset = businessDaysBetween(new Date(teamProject.start_date), impliedStart);
                  setField("start_relative_days_to_project", offset);
                }}
                helperText="Editing this also sets the start offset (D1.4-20)"
              />
              <TextField
                label="Duration (days)"
                sx={{ width: 130 }}
                value={duration != null ? duration.toFixed(1) : "—"}
                slotProps={{ input: { readOnly: true } }}
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
          </Box>
        </Box>

        <TextField
          label="Detailed Description"
          fullWidth
          multiline
          minRows={4}
          margin="normal"
          value={field("detailed_description") ?? ""}
          onChange={(event) => setField("detailed_description", event.target.value)}
        />

        <TextField
          label="External Reference URL"
          fullWidth
          margin="normal"
          value={field("external_reference_url") ?? ""}
          onChange={(event) => setField("external_reference_url", event.target.value || null)}
        />
      </Paper>

      {/* Project/Component: wide edit boxes of their own row, placed after
          Resources/Description rather than among the short fixed-width
          fields — their values can run long (V1.2 screenshot, D1.4-19). */}
      <Paper sx={{ p: 3, mb: 3 }}>
        <Box sx={{ display: "flex", gap: 2 }}>
          <TextField
            select
            label="Project"
            value={field("project_id")}
            onChange={(event) => setField("project_id", Number(event.target.value))}
            sx={{ flex: 1 }}
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
            sx={{ flex: 1 }}
          >
            <MenuItem value="">(none)</MenuItem>
            {teamComponents.map((c) => (
              <MenuItem key={c.component_id} value={c.component_id}>
                {c.name}
              </MenuItem>
            ))}
          </TextField>
        </Box>
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
