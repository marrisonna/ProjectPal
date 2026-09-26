import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import { DenseButton, FieldLabel, FieldTreePicker } from "../../components/DenseField";
import { buildBreadcrumb, type TreeItem } from "../../components/TreePicker";
import {
  useAllAttachments,
  useAllDependencies,
  useAllRemarks,
  useAllTaskResources,
  useAttachments,
  useComponent,
  useComponents,
  useDeleteComponent,
  usePeople,
  usePersonRoles,
  useProjects,
  useRemarks,
  useTasks,
  useUpdateComponent,
} from "../../api/hooks";
import type { ComponentRecord } from "../../api/types";
import { openItemWindow, openListWindow, useRememberedWindowSize, useSingletonWindowIdentity } from "../../lib/windowNav";
import { PlanPage } from "../plan/PlanPage";
import { useDocumentTitle } from "../../lib/useDocumentTitle";
import { formatApiError } from "../../lib/apiErrors";
import { canEditOwnedRecord, hasRoleAtLeast } from "../../lib/permissions";
import { personDisplayName } from "../../lib/people";
import { useAuth } from "../../auth/AuthContext";
import { buildScheduleGraph } from "../../lib/schedule";
import { RemarksPanel } from "../remarks/RemarksPanel";
import { AttachmentsPanel } from "../attachments/AttachmentsPanel";
import { AddTaskDialog } from "../tasks/AddTaskDialog";
import { Components } from "./Components";
import { CreateOrRenameComponentDialog } from "./CreateOrRenameComponentDialog";

type DialogState =
  | { kind: "create"; parentComponentId: number; parentComponentName: string; teamId: number }
  | {
      kind: "rename";
      componentId: number;
      parentComponentId: number;
      parentComponentName: string;
      teamId: number;
      initialName: string;
    }
  | { kind: "addTask"; componentId: number; teamId: number }
  | null;

/**
 * ComponentDetailPlan.md — singleton-per-component window
 * (`openItemWindow("components", id)`), mirroring `ProjectDetailPage.tsx`'s
 * own structure closely (`D1.4-70`), but genuinely simpler: no Priority,
 * dates, or Detailed Description (Component has none of these fields at
 * all), and no Dependencies tab (Component "plays no role in... dependencies",
 * `DomainModel.md` §2.7). With no id at all (`/components`), it's the same
 * dual-purpose "Top Level Components" browsing mode Project Detail's own
 * `/projects` route already established.
 */
export function ComponentDetailPage() {
  const { componentId: componentIdParam } = useParams<{ componentId?: string }>();
  const id = componentIdParam ? Number(componentIdParam) : null;
  const navigate = useNavigate();
  useSingletonWindowIdentity(id != null ? `components-${id}` : "components-list");

  const { person } = useAuth();
  const { data: component, isLoading: componentLoading } = useComponent(id);
  const { data: allComponents, isLoading: componentsLoading } = useComponents();
  const { data: projects, isLoading: projectsLoading } = useProjects();
  const { data: tasks, isLoading: tasksLoading } = useTasks();
  const { data: people } = usePeople();
  const { data: personRoles } = usePersonRoles();
  const { data: allDependencies } = useAllDependencies();
  const { data: allTaskResources } = useAllTaskResources();
  const { data: allRemarks } = useAllRemarks();
  const { data: allAttachments } = useAllAttachments();
  const updateComponent = useUpdateComponent(id ?? -1);
  const deleteComponent = useDeleteComponent();

  // Only Components on a Team the caller belongs to (any role) — same
  // client-side team-scoping ProjectDetailPage.tsx already established
  // (GET /component has no server-side Team restriction of its own).
  const memberTeamIds = useMemo(
    () => new Set((person?.team_roles ?? []).map((tr) => tr.team_id)),
    [person],
  );
  const components = useMemo(
    () => (allComponents ?? []).filter((c) => memberTeamIds.has(c.team_id)),
    [allComponents, memberTeamIds],
  );

  const [form, setForm] = useState<Record<string, unknown> | null>(null);
  const [dirty, setDirty] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [dialog, setDialog] = useState<DialogState>(null);

  // D1.4-113 — see ProjectDetailPage.tsx's own identical comment: "View
  // Gantt" toggles this window in place rather than opening a separate
  // standalone popup (still how a *different* Component's Gantt is reached
  // — Component.tsx's own per-row shortcut icon).
  const [view, setView] = useState<"detail" | "gantt">("detail");
  const [everViewedGantt, setEverViewedGantt] = useState(false);
  // D1.4-115 — restores this window's own last size for whichever of
  // "detail"/"gantt" it's switching back to, best-effort.
  useRememberedWindowSize(view);
  function toggleView() {
    setView((v) => {
      const next = v === "detail" ? "gantt" : "detail";
      if (next === "gantt") setEverViewedGantt(true);
      return next;
    });
  }

  const loadedComponentIdRef = useRef<number | null>(null);
  useEffect(() => {
    if (component && loadedComponentIdRef.current !== component.component_id) {
      setForm({ ...component });
      setDirty(false);
      loadedComponentIdRef.current = component.component_id;
    }
  }, [component]);

  useDocumentTitle(id != null ? `Component - ${(form?.name as string | undefined) ?? id}` : "Top Level Components");

  const resourceIdsByTask = useMemo(() => {
    const map = new Map<number, number[]>();
    for (const r of allTaskResources ?? []) {
      const list = map.get(r.task_id);
      if (list) list.push(r.person_id);
      else map.set(r.task_id, [r.person_id]);
    }
    return map;
  }, [allTaskResources]);

  const remarksCountByTask = useMemo(() => {
    const map = new Map<number, number>();
    for (const r of allRemarks ?? []) {
      if (r.task_id == null) continue;
      map.set(r.task_id, (map.get(r.task_id) ?? 0) + 1);
    }
    return map;
  }, [allRemarks]);

  const attachmentsCountByTask = useMemo(() => {
    const map = new Map<number, number>();
    for (const a of allAttachments ?? []) {
      if (a.task_id == null) continue;
      map.set(a.task_id, (map.get(a.task_id) ?? 0) + 1);
    }
    return map;
  }, [allAttachments]);

  const resourceCountByTaskId = useMemo(() => {
    const map = new Map<number, number>();
    for (const [taskId, ids] of resourceIdsByTask) map.set(taskId, ids.length);
    return map;
  }, [resourceIdsByTask]);

  // Still needed even though Component itself has no dates/scheduling of
  // its own (DomainModel.md §2.7) — its Tasks still need scheduling via
  // their own Project, exactly the same graph ProjectDetailPage.tsx builds.
  const scheduleGraph = useMemo(
    () => buildScheduleGraph(tasks ?? [], projects ?? [], allDependencies ?? [], resourceCountByTaskId),
    [tasks, projects, allDependencies, resourceCountByTaskId],
  );

  if (
    componentsLoading ||
    projectsLoading ||
    tasksLoading ||
    !people ||
    !personRoles ||
    // Same gap AllTaskPage.tsx/ProjectDetailPage.tsx had — without these,
    // the page could render — and Components/Component.tsx see `?? []`,
    // i.e. genuinely empty — a render or two before these bulk queries
    // actually resolved, showing Resources/Remarks/Attachments as blank
    // until whichever later re-render happened to land after they did.
    !allDependencies ||
    !allTaskResources ||
    !allRemarks ||
    !allAttachments
  ) {
    return <CircularProgress sx={{ m: 2 }} />;
  }
  if (id != null && (componentLoading || !form)) {
    return <CircularProgress sx={{ m: 2 }} />;
  }

  function field(name: string): unknown {
    return form?.[name] ?? "";
  }
  function setField(name: string, value: unknown) {
    setForm((prev) => ({ ...prev!, [name]: value }));
    setDirty(true);
  }

  // D1.4-66: canRename and canDelete are deliberately the *same* formula —
  // unlike Project, Component's own delete_component route uses
  // require_owner_or_team_lead, identical to update_component's own rule,
  // not a stricter TeamLead-only rule.
  const canEdit = id != null && !!component && canEditOwnedRecord(person, component.team_id, component.owner_person_id);
  const canDelete = canEdit;
  const canCreateHere = id != null && !!component && hasRoleAtLeast(person, component.team_id, "LeadUser");

  // Reparent picker: every Component on this Component's own Team,
  // excluding itself (a Component can't be its own parent) — same
  // Team-scoped tree-picker shape Project Detail's own Parent Project field
  // already uses.
  const teamComponents = id != null && component ? components.filter((c) => c.team_id === component.team_id) : [];
  const componentTreeItems: TreeItem[] = teamComponents
    .filter((c) => c.component_id !== id)
    .map((c) => ({ id: c.component_id, name: c.name, parentId: c.parent_component_id }));

  // Owner candidate set: same restriction Project Detail's/Task Detail's
  // own Owner field uses — active People holding a Resource role on this
  // Component's own Team.
  const teamResourcePersonIds = new Set(
    personRoles
      .filter((pr) => id != null && component && pr.team_id === component.team_id && pr.is_resource)
      .map((pr) => pr.person_id),
  );
  const activePeople = people.filter((p) => p.is_active);
  const sortedOwnerPeople = [...activePeople]
    .filter((p) => teamResourcePersonIds.has(p.person_id))
    .sort((a, b) =>
      personDisplayName(a.person_id, component?.team_id, people, personRoles).localeCompare(
        personDisplayName(b.person_id, component?.team_id, people, personRoles),
      ),
    );

  const parentComponentId = id != null ? (field("parent_component_id") as number | null) : null;
  const parentComponent =
    parentComponentId != null ? allComponents?.find((c) => c.component_id === parentComponentId) : undefined;

  async function handleSave() {
    if (id == null) return;
    setSaveError(null);
    try {
      await updateComponent.mutateAsync(form!);
      setDirty(false);
    } catch (err) {
      setSaveError(`Save failed — ${formatApiError(err, "check required fields and try again.")}`);
    }
  }

  async function handleDeleteComponent(target: Pick<ComponentRecord, "component_id" | "name">) {
    if (!window.confirm(`Delete Component "${target.name}"? This cannot be undone.`)) return;
    setSaveError(null);
    try {
      await deleteComponent.mutateAsync(target.component_id);
      // Deleting the Component this window is currently showing leaves it
      // with nothing to display — fall back to the Top Level Components
      // browser rather than a dead URL. Deleting some other row (a
      // sub-Component, from the tree below) needs no navigation at all:
      // the mutation's own cache invalidation already refreshes this same
      // tree with that row gone.
      if (target.component_id === id) navigate("/components");
    } catch (err) {
      setSaveError(`Delete failed — ${formatApiError(err, "please try again.")}`);
    }
  }

  // "Top Level Components" (no id) fills the whole window, both axes —
  // same browsing-screen treatment ProjectDetailPage.tsx's own "Top Level
  // Projects" mode already uses, for the same reason: a browsing screen,
  // not a form. Viewing the Gantt (D1.4-113) fills the window too, same
  // reasoning as ProjectDetailPage.tsx's own identical comment (though in
  // practice `view` can only ever become "gantt" when `id != null`, since
  // there's no button to trigger it otherwise — see below).
  const fillWindow = id == null || view === "gantt";

  return (
    <Box sx={{ p: "6px", boxSizing: "border-box", height: "100%", display: "flex", flexDirection: "column" }}>
      {/* Permanent, outside the detail card below (unlike D1.4-109's
          original placement inside its header row) — it has to survive
          being visible in *both* states, so it can't live inside whichever
          one is currently hidden. Only rendered at all when a Component is
          actually open — same "no 'Top Level Components' aggregate mode"
          restriction the button already had (matching V1.2's own
          ComponentWindow, whose Gantt tab is always scoped to one
          already-open Component). */}
      {id != null && (
        <Box sx={{ display: "flex", justifyContent: "flex-start", flexShrink: 0, mb: "6px" }}>
          <DenseButton onClick={toggleView}>{view === "detail" ? "View Gantt" : "View Component"}</DenseButton>
        </Box>
      )}
      <Box
        sx={
          view === "gantt"
            ? { display: "none" }
            : fillWindow
              ? { flex: 1, minHeight: 0, display: "flex", flexDirection: "column" }
              : { width: 700, mx: "auto" }
        }
      >
        <Box
          sx={{
            bgcolor: "#fff",
            border: "1px solid rgba(0,0,0,0.08)",
            borderRadius: "8px",
            boxShadow: "0 1px 3px rgba(0,0,0,0.06)",
            p: "12px",
            display: "flex",
            flexDirection: "column",
            gap: "10px",
            ...(fillWindow && { flex: 1, minHeight: 0 }),
          }}
        >
          {/* Compact identity header, matching ProjectDetailPage.tsx's own
              layout. */}
          <Box sx={{ display: "flex", alignItems: "center", gap: "10px" }}>
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
              C
            </Box>
            <Box sx={{ display: "flex", flexDirection: "column", minWidth: 0, flex: 1 }}>
              <Box sx={{ fontSize: 9, color: "rgba(0,0,0,0.5)" }}>{id != null ? `COMPONENT #${id}` : "COMPONENTS"}</Box>
              {id != null ? (
                <Box
                  component="input"
                  value={field("name") as string}
                  readOnly={!canEdit}
                  onChange={(event: React.ChangeEvent<HTMLInputElement>) => setField("name", event.target.value)}
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
              ) : (
                <Box sx={{ fontSize: 14, fontWeight: 600 }}>Top Level Components</Box>
              )}
            </Box>
            {parentComponent && (
              <Box
                sx={{
                  display: "flex",
                  flexDirection: "column",
                  alignItems: "flex-start",
                  px: "6px",
                  borderLeft: "1px solid rgba(0,0,0,0.1)",
                }}
              >
                <FieldLabel>Parent</FieldLabel>
                <Box
                  component="button"
                  onClick={() => navigate(`/components/${parentComponent.component_id}`)}
                  sx={{
                    border: "none",
                    background: "none",
                    cursor: "pointer",
                    fontSize: 11,
                    fontWeight: 500,
                    color: "primary.dark",
                    p: 0,
                    fontFamily: "inherit",
                    "&:hover": { textDecoration: "underline" },
                  }}
                >
                  {parentComponent.name}
                </Box>
              </Box>
            )}
            {id != null && (
              <Box
                sx={{
                  display: "flex",
                  flexDirection: "column",
                  alignItems: "flex-start",
                  px: "6px",
                  borderLeft: "1px solid rgba(0,0,0,0.1)",
                }}
              >
                <FieldLabel>Owner</FieldLabel>
                <Box
                  component="select"
                  value={(field("owner_person_id") as number | "") ?? ""}
                  disabled={!canEdit}
                  onChange={(event: React.ChangeEvent<HTMLSelectElement>) =>
                    setField("owner_person_id", event.target.value === "" ? null : Number(event.target.value))
                  }
                  sx={{ border: "none", outline: "none", bgcolor: "transparent", fontFamily: "inherit", fontSize: 11, fontWeight: 500 }}
                >
                  <option value="">(none)</option>
                  {sortedOwnerPeople.map((p) => (
                    <option key={p.person_id} value={p.person_id}>
                      {personDisplayName(p.person_id, component?.team_id, people, personRoles)}
                    </option>
                  ))}
                </Box>
              </Box>
            )}
            {id != null && <DenseButton onClick={() => openListWindow("components")}>All Components</DenseButton>}
            {canDelete && component && (
              <DenseButton onClick={() => handleDeleteComponent(component)} disabled={deleteComponent.isPending}>
                Delete
              </DenseButton>
            )}
            {canEdit && (
              <DenseButton variant="filled" onClick={handleSave} disabled={!dirty || updateComponent.isPending}>
                Save
              </DenseButton>
            )}
          </Box>

          {saveError && (
            <Alert severity="error" onClose={() => setSaveError(null)}>
              {saveError}
            </Alert>
          )}

          {/* No Priority/dates/Detailed Description — Component has none of
              these fields (ComponentDetailPlan.md §2.1/§4.1). Parent is
              still reparentable via the tree picker, same as Project. */}
          {id != null && (
            <FieldTreePicker
              label="Parent Component"
              flex={1}
              items={componentTreeItems}
              selectedId={parentComponentId}
              breadcrumb={buildBreadcrumb(componentTreeItems, parentComponentId)}
              onSelect={(cid) => setField("parent_component_id", cid)}
              readOnly={!canEdit}
              allowNone
              // D1.4-123 (UserInteractionPlan.md) — previously had no click
              // action at all; now opens the parent Component's own Detail
              // window, matching Task Detail's own Project/Component
              // pickers.
              onBreadcrumbClick={
                parentComponentId != null ? () => openItemWindow("components", parentComponentId) : undefined
              }
              breadcrumbHint="Click: Open the parent Component's own window."
            />
          )}

          {canCreateHere && component && (
            <Box sx={{ display: "flex", justifyContent: "flex-end", gap: "6px" }}>
              <DenseButton
                onClick={() =>
                  setDialog({
                    kind: "create",
                    parentComponentId: component.component_id,
                    parentComponentName: component.name,
                    teamId: component.team_id,
                  })
                }
              >
                Add Subcomponent
              </DenseButton>
              <DenseButton
                onClick={() => setDialog({ kind: "addTask", componentId: component.component_id, teamId: component.team_id })}
              >
                Add Task
              </DenseButton>
            </Box>
          )}

          <Box
            sx={{
              border: "1px solid rgba(0,0,0,0.12)",
              borderRadius: "6px",
              p: "6px",
              overflowY: "auto",
              ...(fillWindow ? { flex: 1, minHeight: 0 } : { maxHeight: 320 }),
            }}
          >
            <Components
              parentComponentId={id}
              alsoShowTasksForComponent={id != null ? component : undefined}
              components={components}
              projects={projects ?? []}
              tasks={tasks ?? []}
              people={people}
              personRoles={personRoles}
              scheduleGraph={scheduleGraph}
              resourceIdsByTask={resourceIdsByTask}
              attachmentsCountByTask={attachmentsCountByTask}
              remarksCountByTask={remarksCountByTask}
              // Opens (or refocuses) that Component's own singleton window,
              // rather than navigating this window away from whatever it's
              // currently showing — same as Project's own onOpenProject.
              onOpenComponent={(c) => openItemWindow("components", c.component_id)}
              onRenameComponent={(c) =>
                setDialog({
                  kind: "rename",
                  componentId: c.component_id,
                  parentComponentId: c.parent_component_id ?? -1,
                  parentComponentName:
                    components.find((cc) => cc.component_id === c.parent_component_id)?.name ?? "(top level)",
                  teamId: c.team_id,
                  initialName: c.name,
                })
              }
              onDeleteComponent={(c) => handleDeleteComponent(c)}
              onAddTask={(c) => setDialog({ kind: "addTask", componentId: c.component_id, teamId: c.team_id })}
            />
          </Box>

          {id != null && component && <ComponentSubTabs componentId={id} teamId={component.team_id} />}
        </Box>
      </Box>
      {everViewedGantt && id != null && (
        <Box sx={{ display: view === "gantt" ? "flex" : "none", flexDirection: "column", flex: 1, minHeight: 0 }}>
          <PlanPage embedded={{ mode: "component", componentId: id }} />
        </Box>
      )}

      {dialog?.kind === "create" && (
        <CreateOrRenameComponentDialog
          mode="create"
          teamId={dialog.teamId}
          parentComponentId={dialog.parentComponentId}
          parentComponentName={dialog.parentComponentName}
          onClose={() => setDialog(null)}
        />
      )}
      {dialog?.kind === "rename" && (
        <CreateOrRenameComponentDialog
          mode="rename"
          teamId={dialog.teamId}
          parentComponentId={dialog.parentComponentId}
          parentComponentName={dialog.parentComponentName}
          componentId={dialog.componentId}
          initialName={dialog.initialName}
          onClose={() => setDialog(null)}
        />
      )}
      {dialog?.kind === "addTask" && (
        <AddTaskDialog
          openedFrom={{ kind: "component", componentId: dialog.componentId, teamId: dialog.teamId }}
          defaultRequestorPersonId={person?.person_id ?? null}
          onClose={() => setDialog(null)}
          onCreated={(task) => {
            setDialog(null);
            openItemWindow("tasks", task.task_id);
          }}
        />
      )}
    </Box>
  );
}

const TABS = ["ATTACHMENTS", "REMARKS"] as const;

// Its own component, not inlined into ComponentDetailPage's own body,
// purely so its data hooks are only ever called while a real Component is
// open (mounted only when id != null) — same reasoning as
// ProjectDetailPage.tsx's own ProjectSubTabs. No Dependencies tab —
// Component "plays no role in... dependencies" (DomainModel.md §2.7).
function ComponentSubTabs({ componentId, teamId }: { componentId: number; teamId: number }) {
  const [subTab, setSubTab] = useState(0);
  const { data: attachments } = useAttachments({ component_id: componentId });
  const { data: remarks } = useRemarks({ component_id: componentId });

  return (
    <Box sx={{ bgcolor: "#fff", border: "1px solid rgba(0,0,0,0.12)", borderRadius: "6px", overflow: "hidden" }}>
      <Box sx={{ display: "flex", borderBottom: "1px solid rgba(0,0,0,0.12)" }}>
        {TABS.map((label, index) => {
          const count = [attachments?.length ?? 0, remarks?.length ?? 0][index];
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
        {subTab === 0 && <AttachmentsPanel owner={{ component_id: componentId }} hideHeading />}
        {subTab === 1 && <RemarksPanel owner={{ component_id: componentId }} hideHeading teamId={teamId} />}
      </Box>
    </Box>
  );
}
