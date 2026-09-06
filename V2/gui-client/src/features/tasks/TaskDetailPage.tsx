import { useEffect, useRef, useState } from "react";
import { useParams } from "react-router";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import {
  DateField,
  DenseButton,
  FieldInput,
  FieldLabel,
  FieldSelect,
  FieldStatic,
  FieldTextArea,
  FieldTreePicker,
} from "../../components/DenseField";
import { buildBreadcrumb, type TreeItem } from "../../components/TreePicker";
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
import { openListWindow, useSingletonWindowIdentity } from "../../lib/windowNav";
import { useDocumentTitle } from "../../lib/useDocumentTitle";
import { formatApiError } from "../../lib/apiErrors";
import { canEditOwnedRecord } from "../../lib/permissions";
import { personDisplayName } from "../../lib/people";
import { useAuth } from "../../auth/AuthContext";
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
  const { person } = useAuth();
  // See TaskListPage.tsx's own call for why this is needed even though
  // this page is currently only ever reached via a window already named
  // at creation (openItemWindow) — this closes the same gap for any
  // future in-place link to a Task, and its own cleanup-on-navigate-away
  // (which registerThisWindow alone doesn't do) is correct regardless.
  useSingletonWindowIdentity(`tasks-${id}`);

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
  // Staged, not written on click (D-Win-8): the set of person_ids the
  // Resources checklist *would* show checked if saved right now. Compared
  // against the query's own assignedResources at Save time to work out
  // which assign/unassign calls actually need to happen.
  const [resourceIds, setResourceIds] = useState<Set<number> | null>(null);
  const [dirty, setDirty] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);
  // Shared tab strip for the low-frequency sub-panels, each labelled with a
  // count (§3.11) — only one is ever visible, matching V1.2's Remarks/
  // Attachments/Links tabs.
  const [subTab, setSubTab] = useState(0);

  // Reset the edit form (and staged Resources) when this task's own id
  // changes (first load, or navigating to a different task) — deliberately
  // *not* on every `task`/`assignedResources` change, since a cross-window
  // live refresh (D-Win-5) re-fetches these same queries in the background
  // whenever anything invalidates them, and resetting on every one of those
  // would silently overwrite in-progress, unsaved edits in this window with
  // whatever the server has right now.
  const loadedTaskIdRef = useRef<number | null>(null);
  useEffect(() => {
    if (task && assignedResources && loadedTaskIdRef.current !== task.task_id) {
      setForm({ ...task });
      setResourceIds(new Set(assignedResources.map((r) => r.person_id)));
      setDirty(false);
      loadedTaskIdRef.current = task.task_id;
    }
  }, [task, assignedResources]);

  // Live off the form, not the fetched task, so this stays in sync while
  // the Description field (in the header, below) is being edited.
  useDocumentTitle(`Task - ${(form?.description as string | undefined) ?? id}`);

  // Also wait on the reference lists the form's select options come from —
  // rendering a select with no options yet (before they load) would briefly
  // show an empty box rather than the real placeholder.
  if (isLoading || !form || !resourceIds || !projects || !components || !people || !personRoles) {
    return <CircularProgress />;
  }

  function field(name: string) {
    return form![name] ?? "";
  }
  function setField(name: string, value: unknown) {
    setForm((prev) => ({ ...prev!, [name]: value }));
    setDirty(true);
  }

  async function handleSave() {
    setSaveError(null);
    try {
      await updateTask.mutateAsync(form!);
      // Resources are staged, not written on click (D-Win-8) — diff against
      // the query's own last-known assignment and only now actually call
      // assign/unassign for whatever changed, one at a time so a failure
      // partway through stops rather than firing the rest blind.
      const originalIds = new Set(assignedResources?.map((r) => r.person_id) ?? []);
      for (const personId of resourceIds!) {
        if (!originalIds.has(personId)) await assignResource.mutateAsync(personId);
      }
      for (const personId of originalIds) {
        if (!resourceIds!.has(personId)) await unassignResource.mutateAsync(personId);
      }
      setDirty(false);
    } catch (err) {
      setSaveError(
        `Save failed — ${formatApiError(err, "check required fields and try again.")}`,
      );
    }
  }

  const teamProject = projects?.find((p) => p.project_id === field("project_id"));
  // Mirrors rest-api's require_owner_or_team_lead exactly (see
  // lib/permissions.ts) — decided against the Task's *original* owner
  // (task.owner_person_id), not form's, since that's what the server will
  // actually check against on save regardless of a pending, unsaved
  // reassignment in this form.
  const canEdit = canEditOwnedRecord(person, teamProject?.team_id, task?.owner_person_id);
  const teamComponents = components?.filter((c) => c.team_id === teamProject?.team_id) ?? [];
  // Project/Component tree pickers (D-Win-9): scoped to the Task's own
  // Team, same as Component's flat list always was — a Task can only move
  // to a Project on its own Team anyway (D-DM-10), so the flat Project
  // dropdown's org-wide list was already wider than what a move could
  // actually succeed against.
  const teamProjects = projects?.filter((p) => p.team_id === teamProject?.team_id) ?? [];
  const projectTreeItems: TreeItem[] = teamProjects.map((p) => ({
    id: p.project_id,
    name: p.name,
    parentId: p.parent_project_id,
  }));
  const componentTreeItems: TreeItem[] = teamComponents.map((c) => ({
    id: c.component_id,
    name: c.name,
    parentId: c.parent_component_id,
  }));
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
  // within each group, recomputed on every render off the staged
  // resourceIds (not the query's own assignedResources — D-Win-8) so
  // ticking or unticking a resource re-sorts it immediately, before Save.
  // Sorted and displayed by each Person's Team-scoped display name
  // (D1.4-21: nickname if this Team set one, else their plain name),
  // matching what's actually shown.
  const sortedResourcePeople = [...resourceCandidates].sort((a, b) => {
    const aAssigned = resourceIds.has(a.person_id);
    const bAssigned = resourceIds.has(b.person_id);
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
  // Live off the staged resourceIds, not the query's own assignedResources
  // count, so toggling a Resource updates Duration/dates before Save too
  // (D-Win-8) — same "recompute from what's on screen, not last-saved"
  // principle already applied to Effort/the start offset above.
  const duration = formTask ? computeDuration(formTask, resourceIds.size) : null;
  const endDate = computeEndDate(startDate, duration);

  return (
    // Fixed, narrow width rather than filling the browser — matches the
    // multi-window goal (D1.4-8): a Task Detail opened in its own window is
    // meant to be small enough to sit alongside several others. Rebuilt
    // directly against the Claude Design mockup's own markup/CSS (project
    // b721f06c-e472-46b7-8b29-fad6315ab723, "Task Detail Compact Mockups.dc.html",
    // option 1a) rather than approximated from screenshots (Q1.4-17).
    // No AppShell wrapper on this route (see App.tsx's BareAuthenticatedLayout)
    // to keep this window's own chrome minimal, so this page supplies its
    // own (small) margin to the window edge directly, rather than relying
    // on AppShell's <main> padding. The padding lives on this outer Box,
    // not the fixed-width one below, so it doesn't eat into the card's own
    // width (CssBaseline's border-box sizing would otherwise shrink it by
    // the padding amount).
    <Box sx={{ p: "6px" }}>
      {/* Wide enough to keep Row 2 (Effort/.../Requested Start/Planned
          Start/End Date, all fixed-width fields) on one line: card interior
          is width-24(padding), the Resources+fields row leaves that
          -116-12(gap) for the second column, and Row 2's own fixed
          widths+gaps (42+66+48+108+82+108 + 5*8) sum to exactly 494 — so
          this needs to be >= 24+116+12+494 = 646. Deliberately a few px
          over that exact break-even: browsers' flex-width math isn't
          guaranteed to land on a whole pixel, so a zero-slack fit can wrap
          anyway on sub-pixel rounding. */}
      <Box sx={{ width: 656, mx: "auto" }}>
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
              readOnly={!canEdit}
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
          <Box sx={{ display: "flex", flexDirection: "column", alignItems: "flex-start", px: "6px", borderLeft: "1px solid rgba(0,0,0,0.1)" }}>
            <FieldLabel>Owner</FieldLabel>
            <Box
              component="select"
              value={(field("owner_person_id") as number | "") ?? ""}
              disabled={!canEdit}
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
          {/* Preview only: Urgency is real, computed client-side (D1.2-2),
              but that computation isn't built until Stage 3 (see
              TaskListPage.tsx) — this hardcoded 100 is just so the mockup's
              header layout can be seen with something in this slot, not a
              stand-in for the real value. Styled exactly per the Claude
              Design mockup ("Task Detail Compact Mockups.dc.html", option
              1a)'s own Urgency display. */}
          <Box sx={{ display: "flex", flexDirection: "column", alignItems: "flex-start", px: "6px", borderLeft: "1px solid rgba(0,0,0,0.1)" }}>
            <FieldLabel>Urgency</FieldLabel>
            <Box sx={{ fontSize: 12, fontWeight: 700, color: "#b45309" }}>100</Box>
          </Box>
          <DenseButton onClick={() => openListWindow("tasks")}>All Tasks</DenseButton>
          {canEdit && (
            <DenseButton
              variant="filled"
              onClick={handleSave}
              disabled={!dirty || updateTask.isPending}
            >
              Save
            </DenseButton>
          )}
        </Box>

        {saveError && (
          <Alert severity="error" sx={{ mb: 1 }} onClose={() => setSaveError(null)}>
            {saveError}
          </Alert>
        )}

        {/* Resources beside the scheduling fields, at the row's right edge
            (fields column shifted left into the space it used to occupy).
            height:84 is fixed (not auto) so the row's own contribution to
            the page's flow stays exactly what the fields column alone
            needs (38+8+38), regardless of how tall Resources' own box
            grows to — otherwise Resources would just drag Detailed
            Description down by the same amount it grows, and could never
            actually reach it. alignItems is flex-start, not stretch:
            stretch would force Resources' height to match the row's own
            (now fixed) box, shrinking it right back down instead of
            letting it overflow past it. */}
        <Box sx={{ mb: 1, display: "flex", gap: "12px", alignItems: "flex-start", height: 84 }}>
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
                readOnly={!canEdit}
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
                readOnly={!canEdit}
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
                readOnly={!canEdit}
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
                readOnly={!canEdit}
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
                readOnly={!canEdit}
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
                      disabled={!canEdit}
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
                readOnly={!canEdit}
              />
              <DateField
                label="Requested Start"
                // Wider than the plain FieldStatic boxes (e.g. Planned
                // Start's 82) for the same "dd-Mmm-yy" text, since DateField
                // also reserves room for its calendar-icon glyph — at 92
                // this was truncating the display text with an ellipsis.
                width={108}
                value={toDateInputValue(earliestStartDate)}
                display={formatDdMmmYy(earliestStartDate)}
                onChange={(v) => {
                  if (!v || !teamProject?.start_date) return;
                  const chosen = new Date(`${v}T00:00:00`);
                  const offset = businessDaysBetween(new Date(teamProject.start_date), chosen);
                  setField("start_relative_days_to_project", offset);
                }}
                readOnly={!canEdit}
              />
              <FieldStatic label="Planned Start" width={82}>
                {formatDdMmmYy(startDate)}
              </FieldStatic>
              <DateField
                label="End Date"
                width={108}
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
                readOnly={!canEdit}
              />
            </Box>
          </Box>

          <Box sx={{ flexShrink: 0, display: "flex", flexDirection: "column", gap: "2px" }}>
            <FieldLabel>Resources</FieldLabel>
            {/* 83 = 99 (16 down to the box's own top, +83) reaching the
                vertical centre of the "Detailed Description" label below
                (row bottom 84 + row's 8px margin + half that label's own
                14px height) — deliberately overflows past the row's own
                fixed-height box rather than enlarging it (see the row's
                own comment above). */}
            <Box
              sx={{
                width: 116,
                height: 83,
                overflowY: "auto",
                border: "1px solid rgba(0,0,0,0.15)",
                borderRadius: "4px",
                p: "4px 0",
                bgcolor: canEdit ? "transparent" : "rgba(0,0,0,0.06)",
              }}
            >
              {sortedResourcePeople.map((person) => (
                <Box
                  key={person.person_id}
                  component="label"
                  sx={{ display: "flex", alignItems: "center", gap: "5px", fontSize: 11, px: "6px", py: "3px", cursor: canEdit ? "pointer" : "default" }}
                >
                  <Box
                    component="input"
                    type="checkbox"
                    disabled={!canEdit}
                    sx={{ width: 12, height: 12, m: 0, flexShrink: 0 }}
                    checked={resourceIds.has(person.person_id)}
                    // Staged only, not written on click (D-Win-8) — the
                    // actual assign/unassign calls happen in handleSave,
                    // alongside the task PATCH, only once Save is pressed.
                    onChange={(event: React.ChangeEvent<HTMLInputElement>) => {
                      setResourceIds((prev) => {
                        const next = new Set(prev);
                        if (event.target.checked) next.add(person.person_id);
                        else next.delete(person.person_id);
                        return next;
                      });
                      setDirty(true);
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
        </Box>

        <Box sx={{ mb: 1 }}>
          <FieldTextArea
            label="Detailed Description"
            value={(field("detailed_description") as string) ?? ""}
            onChange={(v) => setField("detailed_description", v)}
            readOnly={!canEdit}
          />
        </Box>

        {/* Project/Component: wide edit boxes of their own row, placed after
            Resources/Description rather than among the short fixed-width
            fields — their values can run long (V1.2 screenshot, D1.4-19). */}
        <Box sx={{ mb: 1, display: "flex", gap: "8px" }}>
          <FieldTreePicker
            label="Project"
            flex={1}
            items={projectTreeItems}
            selectedId={(form.project_id as number | null | undefined) ?? null}
            breadcrumb={buildBreadcrumb(projectTreeItems, (form.project_id as number) ?? null)}
            onSelect={(id) => id != null && setField("project_id", id)}
            readOnly={!canEdit}
          />
          <FieldTreePicker
            label="Component"
            flex={1}
            items={componentTreeItems}
            selectedId={(form.component_id as number | null | undefined) ?? null}
            breadcrumb={buildBreadcrumb(componentTreeItems, (form.component_id as number | null) ?? null)}
            onSelect={(id) => setField("component_id", id)}
            readOnly={!canEdit}
            allowNone
          />
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
    </Box>
  );
}
