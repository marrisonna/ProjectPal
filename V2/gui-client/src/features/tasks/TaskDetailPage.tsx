import { useEffect, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router";
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
import { HintTooltip } from "../../components/HintTooltip";
import {
  useAllDependencies,
  useAllTaskResources,
  useAssignResource,
  useAttachments,
  useComponents,
  useDeleteTask,
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
import { PRIORITY_LEVELS, TASK_TYPES } from "../../api/types";
import { openItemWindow, openListWindow, useSingletonWindowIdentity } from "../../lib/windowNav";
import { DRAG_HANDLE_SX, TASK_DRAG_MIME_TYPE } from "../../lib/dnd";
import { useDocumentTitle } from "../../lib/useDocumentTitle";
import { formatApiError } from "../../lib/apiErrors";
import { canEditOwnedRecord, canEditTaskField, editableTaskStatusValues } from "../../lib/permissions";
import { personDisplayName } from "../../lib/people";
import { useAuth } from "../../auth/AuthContext";
import {
  addBusinessDays,
  buildScheduleGraph,
  businessDaysBetween,
  computeDuration,
  computeEarliestStartDate,
  computeEndDate,
  computeTaskRowColour,
  computeUrgency,
  formatDdMmmYy,
  getTaskSchedule,
} from "../../lib/schedule";
import { RemarksPanel } from "../remarks/RemarksPanel";
import { DependenciesPanel } from "../dependencies/DependenciesPanel";
import { AttachmentsPanel } from "../attachments/AttachmentsPanel";

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
  const navigate = useNavigate();
  const { person } = useAuth();
  // See windowNav.ts's own doc comment for why this is needed even though
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
  const { data: dependencies } = useDependencies({ task_id: id });
  // Bulk, unfiltered — the recursive Start/End date evaluator
  // (lib/schedule.ts's buildScheduleGraph, D1.5-2/§4.7) needs every
  // Dependency and every Task's real assigned-Resource count to resolve a
  // predecessor's own predecessors, not just this Task's own direct ones.
  const { data: allDependencies } = useAllDependencies();
  const { data: allTaskResources } = useAllTaskResources();
  const { data: attachments } = useAttachments({ task_id: id });
  const { data: remarks } = useRemarks({ task_id: id });
  const updateTask = useUpdateTask(id);
  const assignResource = useAssignResource(id);
  const unassignResource = useUnassignResource(id);
  const deleteTask = useDeleteTask();

  const [form, setForm] = useState<Record<string, unknown> | null>(null);
  // Staged, not written on click (D-Win-8): the set of person_ids the
  // Resources checklist *would* show checked if saved right now. Compared
  // against the query's own assignedResources at Save time to work out
  // which assign/unassign calls actually need to happen.
  const [resourceIds, setResourceIds] = useState<Set<number> | null>(null);
  // Which `form` fields this window has itself started editing since the
  // last load/Save — not a single whole-form boolean, so an *external*
  // change (e.g. a TaskGrid edit to this same Task from another window,
  // delivered here via D-Win-5 live sync) can be merged into every field
  // the user hasn't touched, below, without clobbering a field they have.
  const [dirtyFields, setDirtyFields] = useState<Set<string>>(new Set());
  const dirtyFieldsRef = useRef<Set<string>>(dirtyFields);
  useEffect(() => {
    dirtyFieldsRef.current = dirtyFields;
  }, [dirtyFields]);
  // Resources has no per-field granularity to merge (it's a single staged
  // Set, not individual form fields) — a plain touched flag, same as
  // before, is enough since nothing outside this window's own Save can
  // change resource assignment (TaskGrid doesn't edit Resources).
  const [resourcesTouched, setResourcesTouched] = useState(false);
  const dirty = dirtyFields.size > 0 || resourcesTouched;
  const [saveError, setSaveError] = useState<string | null>(null);
  // Shared tab strip for the low-frequency sub-panels, each labelled with a
  // count (§3.11) — only one is ever visible, matching V1.2's Remarks/
  // Attachments/Links tabs.
  const [subTab, setSubTab] = useState(0);

  // Fully reset the edit form (and staged Resources) when this task's own
  // id changes (first load, or navigating to a different task).
  const loadedTaskIdRef = useRef<number | null>(null);
  useEffect(() => {
    if (task && assignedResources && loadedTaskIdRef.current !== task.task_id) {
      setForm({ ...task });
      setResourceIds(new Set(assignedResources.map((r) => r.person_id)));
      setDirtyFields(new Set());
      setResourcesTouched(false);
      loadedTaskIdRef.current = task.task_id;
    }
  }, [task, assignedResources]);

  // Same Task, but the query itself refetched with new data — most notably,
  // another window editing a cell on this same Task via TaskGrid, delivered
  // here through the D-Win-5 live-sync mechanism (TaskGridPlan.md §4.9).
  // Merge the fresh value into every field this window hasn't itself
  // started editing, so an outside change becomes visible immediately
  // without clobbering an in-progress, unsaved edit here (D-Win-8) — a
  // plain "reset the whole form on every refetch" would show outside
  // changes too, but at the cost of silently discarding whatever the user
  // was themselves mid-way through typing.
  useEffect(() => {
    if (!task || loadedTaskIdRef.current !== task.task_id) return;
    setForm((prev) => {
      if (!prev) return prev;
      const next = { ...prev };
      for (const [key, value] of Object.entries(task)) {
        if (!dirtyFieldsRef.current.has(key)) next[key] = value;
      }
      return next;
    });
  }, [task]);

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
    setDirtyFields((prev) => new Set(prev).add(name));
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
      setDirtyFields(new Set());
      setResourcesTouched(false);
    } catch (err) {
      setSaveError(
        `Save failed — ${formatApiError(err, "check required fields and try again.")}`,
      );
    }
  }

  // Same permission (and the same rationale for reading it) as Project
  // Detail's own Delete button (ProjectDetailPlan.md §4.8/D1.4-53) —
  // confirmed to already match V1.2's own Task-delete rule exactly, no
  // separate delete-specific check needed.
  async function handleDeleteTask() {
    if (!task || !window.confirm(`Delete Task "${task.description}"? This cannot be undone.`)) {
      return;
    }
    setSaveError(null);
    try {
      await deleteTask.mutateAsync(task.task_id);
      // This window has nothing left to show once its own Task is gone —
      // a popped-out Task Detail window (openItemWindow, the normal way
      // this page is reached) closes itself; if that's refused (this
      // happens to be the main, non-popup window), fall back to All Tasks.
      window.close();
      navigate("/tasks");
    } catch (err) {
      setSaveError(`Delete failed — ${formatApiError(err, "please try again.")}`);
    }
  }

  const teamProject = projects?.find((p) => p.project_id === field("project_id"));
  // Mirrors rest-api's require_owner_or_team_lead exactly (see
  // lib/permissions.ts) — decided against the Task's *original* owner
  // (task.owner_person_id), not form's, since that's what the server will
  // actually check against on save regardless of a pending, unsaved
  // reassignment in this form.
  const canEdit = canEditOwnedRecord(person, teamProject?.team_id, task?.owner_person_id);
  // Whether the signed-in Person is an assigned Resource on this Task, per
  // the query's own last-known assignment (assignedResources), not the
  // staged/unsaved `resourceIds` — same "decide against what the server
  // will actually check" reasoning as `canEdit` above (lib/permissions.ts's
  // canEditTaskField's own `isAssignedResource` is caller-supplied for
  // exactly this reason, TaskGridPlan.md §4.3).
  const isAssignedResource = !!person && !!assignedResources?.some((r) => r.person_id === person.person_id);
  const canEditField = (fieldName: Parameters<typeof canEditTaskField>[4]) =>
    canEditTaskField(person, teamProject?.team_id, task?.owner_person_id, isAssignedResource, fieldName);
  const canDelete = canEdit;
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
  // A Task can end up assigned to someone who's no longer a valid Resource
  // candidate at all (no longer an active team Resource — e.g. removed
  // from the Team since being assigned, the reported case) — that person
  // must still show up here, checked, so a Lead User can actually see and
  // remove them; without this they were invisible in the checklist
  // entirely, with no way to unassign them from this screen. Only widens
  // the *Resources* checklist, not `resourceCandidates` itself, so the
  // Owner dropdown (sortedOwnerPeople, below) keeps its own stricter
  // "genuine current Resource" candidate set unaffected — reassigning
  // Owner *to* someone in this situation was never the ask. Once
  // unassigned here, they stop appearing (correctly) since they're still
  // not a real candidate — this only ever surfaces someone who's
  // *currently* checked, never offers them as a fresh pick.
  const staleAssignedPeople = (people ?? []).filter(
    (p) => resourceIds.has(p.person_id) && !resourceCandidates.some((c) => c.person_id === p.person_id),
  );
  const resourceListCandidates = [...resourceCandidates, ...staleAssignedPeople];
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
  const sortedResourcePeople = [...resourceListCandidates].sort((a, b) => {
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

  // The full recursive Task/Project schedule graph (D1.5-2/§4.7) needs
  // this Task's own *live* form state, not the last-saved copy already
  // sitting in allTasks — substituted in here so a predecessor chain
  // resolved through this Task (were one to exist) sees the same
  // in-progress edits the rest of this page already does, and so this
  // Task's own StartDate reflects a live-edited start offset immediately.
  // Same substitution for its own staged Resource count (D-Win-8: toggling
  // a Resource updates Duration/dates before Save, not just on the
  // query's own last-saved assignedResources).
  const tasksForSchedule = (allTasks ?? []).map((t) => (t.task_id === id && formTask ? formTask : t));
  const resourceCountByTaskId = new Map<number, number>();
  for (const r of allTaskResources ?? []) {
    resourceCountByTaskId.set(r.task_id, (resourceCountByTaskId.get(r.task_id) ?? 0) + 1);
  }
  resourceCountByTaskId.set(id, resourceIds.size);
  const scheduleGraph = buildScheduleGraph(
    tasksForSchedule,
    projects ?? [],
    allDependencies ?? [],
    resourceCountByTaskId,
  );
  const startDate = formTask ? getTaskSchedule(scheduleGraph, id).startDate : null;
  // Live off the staged resourceIds, not the query's own assignedResources
  // count, so toggling a Resource updates Duration/dates before Save too
  // (D-Win-8) — same "recompute from what's on screen, not last-saved"
  // principle already applied to Effort/the start offset above.
  const duration = formTask ? computeDuration(formTask, resourceIds.size) : null;
  const endDate = computeEndDate(startDate, duration);
  const urgency = formTask
    ? computeUrgency(formTask, new Map(projects?.map((p) => [p.project_id, p]) ?? []), startDate, endDate)
    : 100;

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
      <Box
        sx={{ width: 656, mx: "auto" }}
        onDragOver={(event) => {
          // D1.4-10: confirmed (manually) that native HTML5 drag-and-drop
          // crosses two separate window.open()'d Task Detail windows, not
          // just tabs — see 4_GuiClient/Plan.md §6.2. Switch to the
          // Dependencies tab as soon as a dragged Task enters this window,
          // so the "Depends upon"/"Dependants" drop targets are visible
          // without the user needing to have pre-selected that tab.
          // Deliberately doesn't switch back on dragleave/drag-cancel —
          // dragenter/dragleave fire spuriously as the pointer crosses
          // child element boundaries, and there's no need to revert once
          // shown.
          if (!event.dataTransfer.types.includes(TASK_DRAG_MIME_TYPE)) return;
          event.preventDefault();
          if (subTab !== 0) setSubTab(0);
        }}
      >
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
          <HintTooltip hint="Ctrl+drag onto another Task's own Dependencies tab: Create a Dependency on this Task.">
            <Box
              draggable
              onDragStart={(event) => {
                // D1.4-10: Ctrl-drag this Task's icon badge (standing in for
                // its title — the description field next to it is a
                // live-editable <input>, not a plain label, so it can't
                // double as the drag source without colliding with native
                // text-selection drag) onto a different, already open Task
                // Detail window's Dependencies tab, matching V1.2's own
                // Ctrl-drag-creates-a-Dependency convention
                // (Requirements/UserInterfaceWindows.md §4).
                if (!event.ctrlKey) {
                  event.preventDefault();
                  return;
                }
                event.dataTransfer.setData(TASK_DRAG_MIME_TYPE, String(id));
                event.dataTransfer.effectAllowed = "link";
              }}
              // UserInteractionPlan.md §5.3 / Stage5-B2 — the shared
              // "discrete drag handle" cursor cue (`lib/dnd.ts`), not a
              // private `cursor: "grab"` here.
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
                ...DRAG_HANDLE_SX,
              }}
            >
              T
            </Box>
          </HintTooltip>
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
              disabled={!canEditField("owner_person_id")}
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
          {/* Real, computed client-side (D1.2-2/D1.5-1) — the same
              computeUrgency TaskGrid.tsx's Urgency column uses.
              Background is V1.2's own row colour (KeyConcepts.md §12.2's
              "Applying it to a Task's row colour", D1.5-5): white when not
              urgent, fading to light red as Urgency climbs past 100, or a
              fixed grey when this Task's own Priority is unset/Cancelled/
              Closed regardless of the number. Position/layout otherwise
              unchanged from the Claude Design mockup ("Task Detail Compact
              Mockups.dc.html", option 1a)'s own Urgency display. */}
          <Box sx={{ display: "flex", flexDirection: "column", alignItems: "flex-start", px: "6px", borderLeft: "1px solid rgba(0,0,0,0.1)" }}>
            <FieldLabel>Urgency</FieldLabel>
            <Box
              sx={{
                fontSize: 12,
                fontWeight: 700,
                color: "rgba(0,0,0,0.75)",
                px: "6px",
                borderRadius: "3px",
                bgcolor: computeTaskRowColour(formTask?.priority ?? null, urgency),
              }}
            >
              {urgency.toFixed(1)}
            </Box>
          </Box>
          <DenseButton onClick={() => openListWindow("tasks")}>All Tasks</DenseButton>
          {canDelete && (
            <DenseButton onClick={handleDeleteTask} disabled={deleteTask.isPending}>
              Delete
            </DenseButton>
          )}
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
                readOnly={!canEditField("status")}
              >
                {editableTaskStatusValues(person, teamProject?.team_id, task?.owner_person_id).map(
                  (s) => (
                    <option key={s} value={s}>
                      {s}
                    </option>
                  ),
                )}
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
                      setResourcesTouched(true);
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
            readOnly={!canEditField("detailed_description")}
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
            onBreadcrumbClick={
              form.project_id != null ? () => openItemWindow("projects", form.project_id as number) : undefined
            }
            breadcrumbHint="Click: Open this Project's own window."
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
            onBreadcrumbClick={
              form.component_id != null ? () => openItemWindow("components", form.component_id as number) : undefined
            }
            breadcrumbHint="Click: Open this Component's own window."
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
            {subTab === 0 && <DependenciesPanel owner={{ task_id: id }} hideHeading />}
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
