import { useEffect, useState } from "react";
import { useParams } from "react-router";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import IconButton from "@mui/material/IconButton";
import OpenInNewIcon from "@mui/icons-material/OpenInNew";
import {
  DateField,
  DenseButton,
  FieldInput,
  FieldLabel,
  FieldSelect,
  FieldStatic,
  FieldTextArea,
} from "../../components/DenseField";
import {
  useAssignResource,
  useAttachments,
  useComponents,
  useDependencies,
  usePeople,
  usePersonRoles,
  useProjects,
  useRemarks,
  useTask,
  useTaskResources,
  useTasks,
  useUnassignResource,
  useUpdateTask,
} from "../../api/hooks";
import { PRIORITY_LEVELS, TASK_STATUSES, TASK_TYPES } from "../../api/types";
import { openItemWindow, openListWindow } from "../../lib/windowNav";
import { useDocumentTitle } from "../../lib/useDocumentTitle";
import { personDisplayName } from "../../lib/people";
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

const MONTHS = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

// Shared display format for all three scheduling dates (mockup 1a used a
// second, longer format for Planned Start with no stated reason — unified
// here rather than carrying that inconsistency forward). Deliberately not
// the browser's locale-controlled native date-input format.
function formatDdMmmYy(date: Date | null): string {
  if (!date) return "—";
  const yy = String(date.getFullYear()).slice(-2);
  return `${String(date.getDate()).padStart(2, "0")}-${MONTHS[date.getMonth()]}-${yy}`;
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

const TABS = ["DEPENDENCIES", "ATTACHMENTS", "REMARKS"] as const;

export function TaskDetailPage() {
  const { taskId } = useParams<{ taskId: string }>();
  const id = Number(taskId);

  const { data: task, isLoading } = useTask(id);
  const { data: projects } = useProjects();
  const { data: components } = useComponents();
  const { data: people } = usePeople();
  const { data: personRoles } = usePersonRoles();
  const { data: assignedResources } = useTaskResources(id);
  const { data: allTasks } = useTasks();
  const { data: dependencies } = useDependencies(id);
  const { data: attachments } = useAttachments({ task_id: id });
  const { data: remarks } = useRemarks({ task_id: id });
  const updateTask = useUpdateTask(id);
  const assignResource = useAssignResource(id);
  const unassignResource = useUnassignResource(id);

  const [form, setForm] = useState<Record<string, unknown> | null>(null);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [resourceError, setResourceError] = useState<string | null>(null);
  // Shared tab strip for the low-frequency sub-panels, each labelled with a
  // count (§3.11) — only one is ever visible, matching V1.2's Remarks/
  // Attachments/Links tabs.
  const [subTab, setSubTab] = useState(0);

  useEffect(() => {
    if (task) setForm({ ...task });
  }, [task]);

  // Live off the form, not the fetched task, so this stays in sync while
  // the Description field (in the header, below) is being edited.
  useDocumentTitle(`Task - ${(form?.description as string | undefined) ?? id}`);

  // Also wait on the reference lists the form's select options come from —
  // rendering a select with no options yet (before they load) would briefly
  // show an empty box rather than the real placeholder.
  if (isLoading || !form || !projects || !components || !people || !personRoles) {
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
  // V1.2's Owner/Requestor pickers list Person.AllActiveInstances, not every
  // Person on record (V1.2/Libs/DBProjectPal/DBProjectPal/GUITaskColumns
  // usage) — matched here (D1.4-15) for Requestor. Owner was narrowed to the
  // task's own Team, same candidate set as Resources (D1.4-22).
  const activePeople = people?.filter((p) => p.is_active) ?? [];
  // Resources, unlike Requestor, is scoped to the task's own Team: only
  // people with an is_resource person_role there can actually be assigned
  // (the server already rejects anyone else — see the assignResource catch
  // below), so the listbox should only ever offer that set. Owner uses the
  // same candidate set (D1.4-22).
  const teamResourcePersonIds = new Set(
    personRoles
      ?.filter((pr) => pr.team_id === teamProject?.team_id && pr.is_resource)
      .map((pr) => pr.person_id),
  );
  const resourceCandidates = activePeople.filter((p) => teamResourcePersonIds.has(p.person_id));
  function byDisplayName(a: { person_id: number }, b: { person_id: number }) {
    return personDisplayName(a.person_id, teamProject?.team_id, people, personRoles).localeCompare(
      personDisplayName(b.person_id, teamProject?.team_id, people, personRoles),
    );
  }
  // Resources listbox ordering (D1.4-18, §3.11): checked-first, alphabetical
  // within each group, recomputed on every render off assignedIds so ticking
  // or unticking a resource re-sorts it immediately. Sorted and displayed by
  // each Person's Team-scoped display name (D1.4-21: nickname if this Team
  // set one, else their plain name), matching what's actually shown.
  const sortedResourcePeople = [...resourceCandidates].sort((a, b) => {
    const aAssigned = assignedIds.has(a.person_id);
    const bAssigned = assignedIds.has(b.person_id);
    if (aAssigned !== bAssigned) return aAssigned ? -1 : 1;
    return byDisplayName(a, b);
  });
  // Owner dropdown: same team-scoped candidate set as Resources (D1.4-22),
  // plain alphabetical (no checked-first grouping — there's only one Owner).
  const sortedOwnerPeople = [...resourceCandidates].sort(byDisplayName);

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
    // Fixed, narrow width rather than filling the browser — matches the
    // multi-window goal (D1.4-8): a Task Detail opened in its own window is
    // meant to be small enough to sit alongside several others. Rebuilt
    // directly against the Claude Design mockup's own markup/CSS (project
    // b721f06c-e472-46b7-8b29-fad6315ab723, "Task Detail Compact Mockups.dc.html",
    // option 1a) rather than approximated from screenshots (Q1.4-17).
    <Box sx={{ width: 624, mx: "auto" }}>
      <Box
        sx={{
          bgcolor: "#fff",
          border: "1px solid rgba(0,0,0,0.08)",
          borderRadius: "8px",
          boxShadow: "0 1px 3px rgba(0,0,0,0.06)",
          p: "12px",
        }}
      >
        {/* Compact identity header: a slim icon+title strip folding in the
            Task's own Description as its title, instead of a full labelled
            field (§3.11's "collapse identity into one compact header"). */}
        <Box sx={{ mb: 1, display: "flex", alignItems: "center", gap: "10px" }}>
          <Box
            sx={{
              width: 24,
              height: 24,
              borderRadius: "5px",
              bgcolor: "primary.dark",
              color: "primary.contrastText",
              fontSize: 12,
              fontWeight: 700,
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              flexShrink: 0,
            }}
          >
            T
          </Box>
          <Box sx={{ display: "flex", flexDirection: "column", minWidth: 0, flex: 1 }}>
            <Box sx={{ fontSize: 9, color: "rgba(0,0,0,0.5)" }}>
              TASK #{id}
              {teamProject ? ` · ${teamProject.name.toUpperCase()}` : ""}
            </Box>
            <Box
              component="input"
              value={field("description") as string}
              onChange={(event: React.ChangeEvent<HTMLInputElement>) => setField("description", event.target.value)}
              sx={{
                fontSize: 14,
                fontWeight: 600,
                border: "none",
                outline: "none",
                bgcolor: "transparent",
                fontFamily: "inherit",
                width: "100%",
                p: 0,
                whiteSpace: "nowrap",
                overflow: "hidden",
                textOverflow: "ellipsis",
              }}
            />
          </Box>
          {/* Urgency isn't shown here: it's real, computed client-side
              (D1.2-2), but that computation isn't built until Stage 3 (see
              TaskListPage.tsx) — showing a placeholder number would be worse
              than not showing one. */}
          <Box sx={{ display: "flex", flexDirection: "column", alignItems: "flex-start", px: "6px", borderLeft: "1px solid rgba(0,0,0,0.1)" }}>
            <FieldLabel>Owner</FieldLabel>
            <Box
              component="select"
              value={(field("owner_person_id") as number | "") ?? ""}
              onChange={(event: React.ChangeEvent<HTMLSelectElement>) =>
                setField("owner_person_id", event.target.value === "" ? null : Number(event.target.value))
              }
              sx={{
                border: "none",
                outline: "none",
                bgcolor: "transparent",
                fontFamily: "inherit",
                fontSize: 11,
                fontWeight: 500,
              }}
            >
              <option value="">(none)</option>
              {sortedOwnerPeople.map((p) => (
                <option key={p.person_id} value={p.person_id}>
                  {personDisplayName(p.person_id, teamProject?.team_id, people, personRoles)}
                </option>
              ))}
            </Box>
          </Box>
          <IconButton
            title="Open in new window"
            size="small"
            sx={{ p: "2px" }}
            onClick={() => openItemWindow("tasks", id)}
          >
            <OpenInNewIcon sx={{ fontSize: 14 }} />
          </IconButton>
          <DenseButton onClick={() => openListWindow("tasks")}>All Tasks</DenseButton>
          <DenseButton variant="filled" onClick={handleSave} disabled={updateTask.isPending}>
            Save
          </DenseButton>
        </Box>

        {saveError && (
          <Alert severity="error" sx={{ mb: 1 }}>
            {saveError}
          </Alert>
        )}
        {resourceError && (
          <Alert severity="error" sx={{ mb: 1 }} onClose={() => setResourceError(null)}>
            {resourceError}
          </Alert>
        )}

        {/* Resources beside the scheduling fields — a fixed-size control
            (§3.11, D1.4-18) sized so its box lines up with Priority's box. */}
        <Box sx={{ mb: 1, display: "flex", gap: "12px", alignItems: "stretch" }}>
          <Box sx={{ flexShrink: 0, display: "flex", flexDirection: "column", gap: "2px" }}>
            <FieldLabel>Resources</FieldLabel>
            {/* Fixed height, tuned to land exactly on the Effort row's
                bottom (measured, not guessed — see D1.4-18): flex:1 doesn't
                work here since the sibling column has no intrinsic bound of
                its own to stretch against, so an unbounded resources list
                would inflate the whole row's height instead of scrolling. */}
            <Box
              sx={{
                width: 116,
                height: 70,
                overflowY: "auto",
                border: "1px solid rgba(0,0,0,0.15)",
                borderRadius: "4px",
                p: "4px 0",
              }}
            >
              {sortedResourcePeople.map((person) => (
                <Box
                  key={person.person_id}
                  component="label"
                  sx={{ display: "flex", alignItems: "center", gap: "5px", fontSize: 11, px: "6px", py: "3px", cursor: "pointer" }}
                >
                  <Box
                    component="input"
                    type="checkbox"
                    sx={{ width: 12, height: 12, m: 0, flexShrink: 0 }}
                    checked={assignedIds.has(person.person_id)}
                    onChange={async (event: React.ChangeEvent<HTMLInputElement>) => {
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
                  <Box
                    component="span"
                    title={person.name}
                    sx={{ overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}
                  >
                    {truncateResourceName(
                      personDisplayName(person.person_id, teamProject?.team_id, people, personRoles),
                    )}
                  </Box>
                </Box>
              ))}
            </Box>
          </Box>

          <Box sx={{ flex: 1, minWidth: 0, display: "flex", flexDirection: "column", gap: "8px" }}>
            {/* Row 1: Priority/Status/Task Type/Requestor, spread per the
                mockup — widths widened a little past its own (e.g. Status
                88 -> 108) since the mockup's demo strings ("Med", "Ready")
                are shorter than our real enum values ("MedHigh",
                "InProgress"); §3.11's "size fields to content" applies to
                our actual content, not the mockup's placeholder text. */}
            <Box sx={{ display: "flex", flexWrap: "wrap", gap: "8px", justifyContent: "space-between" }}>
              <FieldSelect
                label="Priority"
                width={84}
                value={(field("priority") as string) ?? ""}
                onChange={(v) => setField("priority", v || null)}
              >
                <option value="">(none)</option>
                {PRIORITY_LEVELS.map((p) => (
                  <option key={p} value={p}>
                    {p}
                  </option>
                ))}
              </FieldSelect>
              <FieldSelect
                label="Status"
                width={108}
                value={field("status") as string}
                onChange={(v) => setField("status", v)}
              >
                {TASK_STATUSES.map((s) => (
                  <option key={s} value={s}>
                    {s}
                  </option>
                ))}
              </FieldSelect>
              <FieldSelect
                label="Task Type"
                width={118}
                value={(field("task_type") as string) ?? ""}
                onChange={(v) => setField("task_type", v || null)}
              >
                <option value="">(none)</option>
                {TASK_TYPES.map((t) => (
                  <option key={t} value={t}>
                    {t}
                  </option>
                ))}
              </FieldSelect>
              <FieldSelect
                label="Requestor"
                width={114}
                value={(field("requestor_person_id") as number | "") ?? ""}
                onChange={(v) => setField("requestor_person_id", v === "" ? null : Number(v))}
              >
                <option value="">(none)</option>
                {activePeople.map((p) => (
                  <option key={p.person_id} value={p.person_id}>
                    {personDisplayName(p.person_id, teamProject?.team_id, people, personRoles)}
                  </option>
                ))}
              </FieldSelect>
            </Box>

            {/* Row 2: Effort/Effort Type/% Allocation/dates, sized and spread
                exactly per the mockup. */}
            <Box sx={{ display: "flex", flexWrap: "wrap", gap: "8px", justifyContent: "space-between" }}>
              <FieldInput
                label="Effort"
                width={42}
                type="number"
                center
                value={(field("effort_in_days") as number) ?? ""}
                onChange={(v) => setField("effort_in_days", v === "" ? null : Number(v))}
              />
              {/* Two stacked radio buttons, as V1.2 uses, rather than a wider
                  toggle switch — bottom-aligned with the row's field-boxes
                  rather than top-aligned like a labelled field (D1.4-19). */}
              <Box sx={{ width: 66, display: "flex", flexDirection: "column", justifyContent: "flex-end", gap: "2px" }}>
                {(["PersonDays", "Duration"] as const).map((opt) => (
                  <Box
                    key={opt}
                    component="label"
                    sx={{ display: "flex", alignItems: "center", gap: "4px", fontSize: 11, cursor: "pointer" }}
                  >
                    <Box
                      component="input"
                      type="radio"
                      name="effortType"
                      sx={{ width: 12, height: 12, m: 0, flexShrink: 0 }}
                      checked={field("effort_type") === opt}
                      onChange={() => setField("effort_type", opt)}
                    />
                    {opt === "PersonDays" ? "Days" : "Duration"}
                  </Box>
                ))}
              </Box>
              <FieldInput
                label="% Alloc"
                width={48}
                type="number"
                center
                // Stored/computed (schedule.ts's computeDuration) as a
                // fraction, 1 = 100% — displayed here as a whole percentage.
                value={
                  field("percentage_allocation") != null
                    ? (field("percentage_allocation") as number) * 100
                    : ""
                }
                onChange={(v) =>
                  setField("percentage_allocation", v === "" ? null : Number(v) / 100)
                }
              />
              <DateField
                label="Requested Start"
                width={92}
                value={toDateInputValue(earliestStartDate)}
                display={formatDdMmmYy(earliestStartDate)}
                onChange={(v) => {
                  if (!v || !teamProject?.start_date) return;
                  const chosen = new Date(`${v}T00:00:00`);
                  const offset = businessDaysBetween(new Date(teamProject.start_date), chosen);
                  setField("start_relative_days_to_project", offset);
                }}
              />
              <FieldStatic label="Planned Start" width={82}>
                {formatDdMmmYy(startDate)}
              </FieldStatic>
              <DateField
                label="End Date"
                width={92}
                value={toDateInputValue(endDate)}
                display={formatDdMmmYy(endDate)}
                onChange={(v) => {
                  if (!v || !teamProject?.start_date || duration == null) return;
                  const chosen = new Date(`${v}T00:00:00`);
                  // V1.2's Task.EndDate setter: back-compute the Start Date
                  // this End Date implies (subtracting Duration business
                  // days), then convert that to the stored offset — same as
                  // editing Requested Start Date directly, anchored at the
                  // other end (D1.4-20).
                  const impliedStart = addBusinessDays(chosen, -(Math.ceil(duration) - 1));
                  const offset = businessDaysBetween(new Date(teamProject.start_date), impliedStart);
                  setField("start_relative_days_to_project", offset);
                }}
              />
            </Box>
          </Box>
        </Box>

        <Box sx={{ mb: 1 }}>
          <FieldTextArea
            label="Detailed Description"
            value={(field("detailed_description") as string) ?? ""}
            onChange={(v) => setField("detailed_description", v)}
          />
        </Box>

        {/* Project/Component: wide edit boxes of their own row, placed after
            Resources/Description rather than among the short fixed-width
            fields — their values can run long (V1.2 screenshot, D1.4-19). */}
        <Box sx={{ mb: 1, display: "flex", gap: "8px" }}>
          <FieldSelect
            label="Project"
            flex={1}
            value={field("project_id") as number}
            onChange={(v) => setField("project_id", Number(v))}
          >
            {projects?.map((p) => (
              <option key={p.project_id} value={p.project_id}>
                {p.name}
              </option>
            ))}
          </FieldSelect>
          <FieldSelect
            label="Component"
            flex={1}
            value={(field("component_id") as number | "") ?? ""}
            onChange={(v) => setField("component_id", v === "" ? null : Number(v))}
          >
            <option value="">(none)</option>
            {teamComponents.map((c) => (
              <option key={c.component_id} value={c.component_id}>
                {c.name}
              </option>
            ))}
          </FieldSelect>
        </Box>

        {/* Dependencies/Attachments/Remarks share one tab strip, each
            labelled with a count, rather than three permanently-expanded
            cards (§3.11 — matches V1.2's Remarks/Attachments/Links tabs). */}
        <Box sx={{ bgcolor: "#fff", border: "1px solid rgba(0,0,0,0.12)", borderRadius: "6px", overflow: "hidden" }}>
          <Box sx={{ display: "flex", borderBottom: "1px solid rgba(0,0,0,0.12)" }}>
            {TABS.map((label, index) => {
              const count = [dependencies?.length ?? 0, attachments?.length ?? 0, remarks?.length ?? 0][index];
              const active = subTab === index;
              return (
                <Box
                  key={label}
                  onClick={() => setSubTab(index)}
                  sx={{
                    flex: 1,
                    textAlign: "center",
                    py: "8px",
                    px: "4px",
                    fontSize: 10,
                    fontWeight: 600,
                    letterSpacing: "0.3px",
                    cursor: "pointer",
                    borderBottom: active ? "2px solid" : "2px solid transparent",
                    borderBottomColor: active ? "primary.dark" : "transparent",
                    color: active ? "primary.dark" : "rgba(0,0,0,0.55)",
                  }}
                >
                  {label} ({count})
                </Box>
              );
            })}
          </Box>
          <Box sx={{ p: "10px 12px" }}>
            {subTab === 0 && <DependenciesPanel taskId={id} hideHeading />}
            {subTab === 1 && <AttachmentsPanel owner={{ task_id: id }} hideHeading />}
            {subTab === 2 && (
              <RemarksPanel owner={{ task_id: id }} hideHeading teamId={teamProject?.team_id} />
            )}
          </Box>
        </Box>
      </Box>
    </Box>
  );
}
