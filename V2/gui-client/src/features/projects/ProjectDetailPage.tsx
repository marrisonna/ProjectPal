import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import {
  DateField,
  DenseButton,
  FieldLabel,
  FieldSelect,
  FieldStatic,
  FieldTextArea,
  FieldTreePicker,
} from "../../components/DenseField";
import { buildBreadcrumb, type TreeItem } from "../../components/TreePicker";
import {
  useAllAttachments,
  useAllDependencies,
  useAllRemarks,
  useAllTaskResources,
  useAttachments,
  useComponents,
  useDeleteProject,
  useDependencies,
  usePeople,
  usePersonRoles,
  useProject,
  useProjects,
  useRemarks,
  useTasks,
  useUpdateProject,
} from "../../api/hooks";
import { PRIORITY_LEVELS, type ProjectRecord } from "../../api/types";
import { openItemWindow, openListWindow, useSingletonWindowIdentity } from "../../lib/windowNav";
import { useDocumentTitle } from "../../lib/useDocumentTitle";
import { formatApiError } from "../../lib/apiErrors";
import { canEditOwnedRecord, hasRoleAtLeast, isTeamLead } from "../../lib/permissions";
import { personDisplayName } from "../../lib/people";
import { useAuth } from "../../auth/AuthContext";
import { buildScheduleGraph, formatDdMmmYy, getProjectSchedule } from "../../lib/schedule";
import { RemarksPanel } from "../remarks/RemarksPanel";
import { DependenciesPanel } from "../dependencies/DependenciesPanel";
import { AttachmentsPanel } from "../attachments/AttachmentsPanel";
import { Projects } from "./Projects";
import { CreateOrRenameProjectDialog } from "./CreateOrRenameProjectDialog";
import { AddTaskDialog } from "../tasks/AddTaskDialog";

function toDateInputValue(date: Date | null): string {
  if (!date) return "";
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, "0");
  const d = String(date.getDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

type DialogState =
  | { kind: "create"; parentProjectId: number; parentProjectName: string; teamId: number }
  | {
      kind: "rename";
      projectId: number;
      parentProjectId: number;
      parentProjectName: string;
      teamId: number;
      initialName: string;
    }
  | { kind: "addTask"; projectId: number; teamId: number }
  | null;

// D1.4-24-equivalent for Project: singleton-per-project window
// (ProjectDetailPlan.md §4.1), opened via openItemWindow("projects", id).
// With no id at all (/projects), it's V1.2's own dual-purpose behaviour —
// the "Top Level Projects" browser, since no separate Project List grid
// exists yet.
export function ProjectDetailPage() {
  const { projectId: projectIdParam } = useParams<{ projectId?: string }>();
  const id = projectIdParam ? Number(projectIdParam) : null;
  const navigate = useNavigate();
  useSingletonWindowIdentity(id != null ? `projects-${id}` : "projects-list");

  const { person } = useAuth();
  const { data: project, isLoading: projectLoading } = useProject(id);
  const { data: allProjects, isLoading: projectsLoading } = useProjects();
  const { data: tasks, isLoading: tasksLoading } = useTasks();
  const { data: components } = useComponents();
  const { data: people } = usePeople();
  const { data: personRoles } = usePersonRoles();
  const { data: allDependencies } = useAllDependencies();
  const { data: allTaskResources } = useAllTaskResources();
  const { data: allRemarks } = useAllRemarks();
  const { data: allAttachments } = useAllAttachments();
  const updateProject = useUpdateProject(id ?? -1);
  const deleteProject = useDeleteProject();

  // Only Projects on a Team the caller belongs to (any role) — same
  // client-side team-scoping already established for Tasks/the Gantt view
  // (ProjectDetailPlan.md §2.3 — GET /project has no server-side Team
  // restriction of its own).
  const memberTeamIds = useMemo(
    () => new Set((person?.team_roles ?? []).map((tr) => tr.team_id)),
    [person],
  );
  const projects = useMemo(
    () => (allProjects ?? []).filter((p) => memberTeamIds.has(p.team_id)),
    [allProjects, memberTeamIds],
  );

  const [form, setForm] = useState<Record<string, unknown> | null>(null);
  const [dirty, setDirty] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [dialog, setDialog] = useState<DialogState>(null);

  const loadedProjectIdRef = useRef<number | null>(null);
  useEffect(() => {
    if (project && loadedProjectIdRef.current !== project.project_id) {
      setForm({ ...project });
      setDirty(false);
      loadedProjectIdRef.current = project.project_id;
    }
  }, [project]);

  useDocumentTitle(id != null ? `Project - ${(form?.name as string | undefined) ?? id}` : "Top Level Projects");

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

  const scheduleGraph = useMemo(
    () => buildScheduleGraph(tasks ?? [], projects, allDependencies ?? [], resourceCountByTaskId),
    [tasks, projects, allDependencies, resourceCountByTaskId],
  );

  if (
    projectsLoading ||
    tasksLoading ||
    !components ||
    !people ||
    !personRoles ||
    // Same gap AllTaskPage.tsx had (D1.4-65-adjacent fix): without these,
    // the page could render — and Projects/Project.tsx see `?? []`, i.e.
    // genuinely empty — a render or two before these bulk queries actually
    // resolved, showing Resources/Remarks/Attachments as blank until
    // whichever later re-render happened to land after they did.
    !allDependencies ||
    !allTaskResources ||
    !allRemarks ||
    !allAttachments
  ) {
    return <CircularProgress sx={{ m: 2 }} />;
  }
  if (id != null && (projectLoading || !form)) {
    return <CircularProgress sx={{ m: 2 }} />;
  }

  function field(name: string): unknown {
    return form?.[name] ?? "";
  }
  function setField(name: string, value: unknown) {
    setForm((prev) => ({ ...prev!, [name]: value }));
    setDirty(true);
  }

  const canEdit = id != null && !!project && canEditOwnedRecord(person, project.team_id, project.owner_person_id);
  const canDelete = id != null && !!project && isTeamLead(person, project.team_id);
  const canCreateHere = id != null && !!project && hasRoleAtLeast(person, project.team_id, "LeadUser");

  // Reparent picker (§4.3): every Project on this Project's own Team,
  // excluding itself (a Project can't be its own parent) — same
  // Team-scoped tree-picker shape Task Detail already builds for its own
  // Project field.
  const teamProjects = id != null && project ? projects.filter((p) => p.team_id === project.team_id) : [];
  const projectTreeItems: TreeItem[] = teamProjects
    .filter((p) => p.project_id !== id)
    .map((p) => ({ id: p.project_id, name: p.name, parentId: p.parent_project_id }));

  // Owner candidate set (§4.2): same restriction Task Detail's own Owner
  // field uses — active People holding a Resource role on this Project's
  // own Team (D1.4-22's own precedent, reused rather than "any role").
  const teamResourcePersonIds = new Set(
    personRoles
      .filter((pr) => id != null && project && pr.team_id === project.team_id && pr.is_resource)
      .map((pr) => pr.person_id),
  );
  const activePeople = people.filter((p) => p.is_active);
  const sortedOwnerPeople = [...activePeople]
    .filter((p) => teamResourcePersonIds.has(p.person_id))
    .sort((a, b) =>
      personDisplayName(a.person_id, project?.team_id, people, personRoles).localeCompare(
        personDisplayName(b.person_id, project?.team_id, people, personRoles),
      ),
    );

  const parentProjectId = id != null ? (field("parent_project_id") as number | null) : null;
  const parentProject = parentProjectId != null ? allProjects?.find((p) => p.project_id === parentProjectId) : undefined;

  async function handleSave() {
    if (id == null) return;
    setSaveError(null);
    try {
      await updateProject.mutateAsync(form!);
      setDirty(false);
    } catch (err) {
      setSaveError(`Save failed — ${formatApiError(err, "check required fields and try again.")}`);
    }
  }

  async function handleDeleteProject(target: Pick<ProjectRecord, "project_id" | "name">) {
    if (!window.confirm(`Delete Project "${target.name}"? This cannot be undone.`)) return;
    setSaveError(null);
    try {
      await deleteProject.mutateAsync(target.project_id);
      // Deleting the Project this window is currently showing leaves it
      // with nothing to display — fall back to the Top Level Projects
      // browser rather than a dead URL. Deleting some other row (a
      // sub-Project, from the tree below) needs no navigation at all: the
      // mutation's own cache invalidation already refreshes this same
      // tree with that row gone.
      if (target.project_id === id) navigate("/projects");
    } catch (err) {
      setSaveError(`Delete failed — ${formatApiError(err, "please try again.")}`);
    }
  }

  const startDate = id != null && form?.start_date ? new Date(form.start_date as string) : null;
  const dueDate = id != null && form?.due_date ? new Date(form.due_date as string) : null;
  const endDate = id != null ? getProjectSchedule(scheduleGraph, id).endDate : null;

  // "Top Level Projects" (no id) fills the whole window, both axes — a
  // browsing screen, not a form, so there's no reason to leave it capped at
  // a Task-Detail-style card width/height the way a single Project's own
  // fields view still is.
  const fillWindow = id == null;

  return (
    <Box sx={{ p: "6px", boxSizing: "border-box", ...(fillWindow && { height: "100%", display: "flex", flexDirection: "column" }) }}>
      <Box sx={fillWindow ? { flex: 1, minHeight: 0, display: "flex", flexDirection: "column" } : { width: 700, mx: "auto" }}>
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
          {/* Compact identity header, matching TaskDetailPage.tsx's own
              layout (ProjectDetailPlan.md §4.1/§4.2). */}
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
              P
            </Box>
            <Box sx={{ display: "flex", flexDirection: "column", minWidth: 0, flex: 1 }}>
              <Box sx={{ fontSize: 9, color: "rgba(0,0,0,0.5)" }}>{id != null ? `PROJECT #${id}` : "PROJECTS"}</Box>
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
                <Box sx={{ fontSize: 14, fontWeight: 600 }}>Top Level Projects</Box>
              )}
            </Box>
            {parentProject && (
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
                  onClick={() => navigate(`/projects/${parentProject.project_id}`)}
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
                  {parentProject.name}
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
                      {personDisplayName(p.person_id, project?.team_id, people, personRoles)}
                    </option>
                  ))}
                </Box>
              </Box>
            )}
            {id != null && <DenseButton onClick={() => openListWindow("projects")}>All Projects</DenseButton>}
            {canDelete && project && (
              <DenseButton onClick={() => handleDeleteProject(project)} disabled={deleteProject.isPending}>
                Delete
              </DenseButton>
            )}
            {canEdit && (
              <DenseButton variant="filled" onClick={handleSave} disabled={!dirty || updateProject.isPending}>
                Save
              </DenseButton>
            )}
          </Box>

          {saveError && (
            <Alert severity="error" onClose={() => setSaveError(null)}>
              {saveError}
            </Alert>
          )}

          {id != null && (
            <>
              <Box sx={{ display: "flex", flexWrap: "wrap", gap: "8px", justifyContent: "space-between" }}>
                <FieldSelect
                  label="Priority"
                  width={90}
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
                <DateField
                  label="Start Date"
                  width={108}
                  value={toDateInputValue(startDate)}
                  display={formatDdMmmYy(startDate)}
                  onChange={(v) => setField("start_date", v || null)}
                  readOnly={!canEdit}
                />
                <DateField
                  label="Due Date"
                  width={108}
                  value={toDateInputValue(dueDate)}
                  display={formatDdMmmYy(dueDate)}
                  onChange={(v) => setField("due_date", v || null)}
                  readOnly={!canEdit}
                />
                <FieldStatic label="End Date" width={82}>
                  {formatDdMmmYy(endDate)}
                </FieldStatic>
              </Box>

              <FieldTreePicker
                label="Parent Project"
                flex={1}
                items={projectTreeItems}
                selectedId={parentProjectId}
                breadcrumb={buildBreadcrumb(projectTreeItems, parentProjectId)}
                onSelect={(pid) => setField("parent_project_id", pid)}
                readOnly={!canEdit}
                allowNone
              />

              <FieldTextArea
                label="Detailed Description"
                value={(field("detailed_description") as string) ?? ""}
                onChange={(v) => setField("detailed_description", v)}
                readOnly={!canEdit}
              />
            </>
          )}

          {canCreateHere && project && (
            <Box sx={{ display: "flex", justifyContent: "flex-end", gap: "6px" }}>
              <DenseButton
                onClick={() =>
                  setDialog({
                    kind: "create",
                    parentProjectId: project.project_id,
                    parentProjectName: project.name,
                    teamId: project.team_id,
                  })
                }
              >
                Add Subproject
              </DenseButton>
              <DenseButton
                onClick={() => setDialog({ kind: "addTask", projectId: project.project_id, teamId: project.team_id })}
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
            <Projects
              parentProjectId={id}
              alsoShowTasksForProject={id != null ? project : undefined}
              // A specific Project's own detail view defaults to "Open" —
              // "Top Level Projects" (id == null) keeps Projects's own
              // default of "None".
              defaultTaskVisibility={id != null ? "Open" : undefined}
              projects={projects}
              tasks={tasks ?? []}
              components={components}
              people={people}
              personRoles={personRoles}
              scheduleGraph={scheduleGraph}
              resourceIdsByTask={resourceIdsByTask}
              attachmentsCountByTask={attachmentsCountByTask}
              remarksCountByTask={remarksCountByTask}
              // Opens (or refocuses) that Project's own singleton window
              // (windowNav.ts's openItemWindow, same as a Task row) rather
              // than navigating this window away from whatever it's
              // currently showing.
              onOpenProject={(p) => openItemWindow("projects", p.project_id)}
              onRenameProject={(p) =>
                setDialog({
                  kind: "rename",
                  projectId: p.project_id,
                  parentProjectId: p.parent_project_id ?? -1,
                  parentProjectName: projects.find((pp) => pp.project_id === p.parent_project_id)?.name ?? "(top level)",
                  teamId: p.team_id,
                  initialName: p.name,
                })
              }
              onDeleteProject={(p) => handleDeleteProject(p)}
              onAddTask={(p) => setDialog({ kind: "addTask", projectId: p.project_id, teamId: p.team_id })}
            />
          </Box>

          {id != null && project && <ProjectSubTabs projectId={id} teamId={project.team_id} />}
        </Box>
      </Box>

      {dialog?.kind === "create" && (
        <CreateOrRenameProjectDialog
          mode="create"
          teamId={dialog.teamId}
          parentProjectId={dialog.parentProjectId}
          parentProjectName={dialog.parentProjectName}
          onClose={() => setDialog(null)}
        />
      )}
      {dialog?.kind === "rename" && (
        <CreateOrRenameProjectDialog
          mode="rename"
          teamId={dialog.teamId}
          parentProjectId={dialog.parentProjectId}
          parentProjectName={dialog.parentProjectName}
          projectId={dialog.projectId}
          initialName={dialog.initialName}
          onClose={() => setDialog(null)}
        />
      )}
      {dialog?.kind === "addTask" && (
        <AddTaskDialog
          openedFrom={{ kind: "project", projectId: dialog.projectId, teamId: dialog.teamId }}
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

const TABS = ["DEPENDENCIES", "ATTACHMENTS", "REMARKS"] as const;

// Its own component, not inlined into ProjectDetailPage's own body, purely
// so its three data hooks are only ever called while a real Project is
// open (mounted only when id != null) — calling them unconditionally in
// the parent would mean firing a real request for a nonsense id whenever
// the page is in its "Top Level Projects" (no Project) mode instead.
function ProjectSubTabs({ projectId, teamId }: { projectId: number; teamId: number }) {
  const [subTab, setSubTab] = useState(0);
  const { data: dependencies } = useDependencies({ project_id: projectId });
  const { data: attachments } = useAttachments({ project_id: projectId });
  const { data: remarks } = useRemarks({ project_id: projectId });

  return (
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
        {subTab === 0 && <DependenciesPanel owner={{ project_id: projectId }} hideHeading />}
        {subTab === 1 && <AttachmentsPanel owner={{ project_id: projectId }} hideHeading />}
        {subTab === 2 && <RemarksPanel owner={{ project_id: projectId }} hideHeading teamId={teamId} />}
      </Box>
    </Box>
  );
}
