import { useEffect, useMemo, useRef, useState } from "react";
import {
  useAllAttachments,
  useAllDependencies,
  useAllRemarks,
  useAllTaskResources,
  useComponents,
  usePeople,
  usePersonRoles,
  useProjects,
  useTasks,
  useTeams,
} from "../../api/hooks";
import { TASK_STATUSES } from "../../api/types";
import { useSingletonWindowIdentity } from "../../lib/windowNav";
import { useDocumentTitle } from "../../lib/useDocumentTitle";
import { personDisplayName } from "../../lib/people";
import { isTeamLeadOfAnyTeam } from "../../lib/permissions";
import { useAuth } from "../../auth/AuthContext";
import { buildScheduleGraph } from "../../lib/schedule";
import type { ColumnFilterState } from "../../components/GridColumnFilter";
import { DEFAULT_TASK_GRID_COLUMNS, TaskGrid } from "./TaskGrid";

// The TaskGrid-based All Tasks screen (TaskGridPlan.md §5.2, D1.4-50) — the
// approved replacement for the original grid (D1.4-65). Team-scoping and
// default filter state are this screen's own concern (D-Win-15/16), not
// TaskGrid's, which only ever renders whatever `tasks` it's handed (§4.1).
export function AllTaskPage() {
  useDocumentTitle("All Tasks");
  useSingletonWindowIdentity("tasks-list");
  const { person } = useAuth();
  const { data: tasks, isLoading } = useTasks();
  const { data: projects } = useProjects();
  const { data: components } = useComponents();
  const { data: people } = usePeople();
  const { data: personRoles } = usePersonRoles();
  const { data: teams } = useTeams();
  const { data: allDependencies } = useAllDependencies();
  const { data: allTaskResources } = useAllTaskResources();
  const { data: allRemarks } = useAllRemarks();
  const { data: allAttachments } = useAllAttachments();

  const isTeamLead = isTeamLeadOfAnyTeam(person);

  // Default filter state (D-Win-16): every Status except Closed/Cancelled.
  // This is window-level display
  // configuration (§4.6), not a permission — a different window embedding
  // TaskGrid is free to start with a different default.
  const [filterState, setFilterState] = useState<Record<string, ColumnFilterState>>(() => ({
    status: {
      contains: "",
      exact: new Set(TASK_STATUSES.filter((s) => s !== "Cancelled" && s !== "Closed")),
    },
  }));

  // One-time "default Resources filter to yourself unless you're a Team
  // Lead" behaviour (D-Win-15). TaskGrid only ever reads
  // `initialFilterState` once (a lazy useState initializer, so its own
  // filter UI stays responsive to the user's later edits without fighting
  // a prop that keeps changing underneath it) — so this has to finish
  // updating `filterState` *before* TaskGrid first mounts, not
  // asynchronously afterwards. `defaultFilterReady` (below) gates that
  // first mount on it.
  const appliedDefaultResourceFilter = useRef(false);
  const [defaultFilterReady, setDefaultFilterReady] = useState(false);
  useEffect(() => {
    if (appliedDefaultResourceFilter.current) return;
    if (!person || !people) return;
    appliedDefaultResourceFilter.current = true;
    if (!isTeamLead) {
      const ownName = personDisplayName(person.person_id, undefined, people, personRoles);
      setFilterState((prev) => ({
        ...prev,
        resources: { contains: "", exact: new Set([ownName]) },
      }));
    }
    setDefaultFilterReady(true);
  }, [person, people, personRoles, isTeamLead]);

  const projectsById = useMemo(() => {
    const map = new Map<number, (typeof projects)[number]>();
    for (const p of projects ?? []) map.set(p.project_id, p);
    return map;
  }, [projects]);

  // Every user's All Tasks view is hard-restricted to their own Team(s)
  // (D-Win-17) — a floor under the row set itself, not a clearable filter:
  // a Team Lead previously saw every Task company-wide with no filter at
  // all, and a non-Team-Lead could reach the same thing simply by clearing
  // their own Resources filter, since `useTasks()` itself is never
  // Team-scoped.
  const myTeamIds = useMemo(
    () => new Set(person?.team_roles.map((tr) => tr.team_id) ?? []),
    [person],
  );
  const teamScopedTasks = useMemo(
    () => (tasks ?? []).filter((t) => myTeamIds.has(projectsById.get(t.project_id)?.team_id ?? -1)),
    [tasks, projectsById, myTeamIds],
  );

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
    () => buildScheduleGraph(tasks ?? [], projects ?? [], allDependencies ?? [], resourceCountByTaskId),
    [tasks, projects, allDependencies, resourceCountByTaskId],
  );

  if (
    isLoading ||
    !projects ||
    !components ||
    !people ||
    !personRoles ||
    !teams ||
    !defaultFilterReady ||
    // These four weren't gated here before — the grid could render (and
    // TaskGrid.tsx sees `?? []`, i.e. genuinely empty) a render or two
    // before any of them actually resolved, showing Resources/Remarks/
    // Attachments as blank until whichever later re-render happened to
    // land after they did. Matches the reported "resources are empty
    // straight after logging in, populated after a hard refresh" — a
    // fresh login is exactly the case with the least already-cached data
    // for these to fall back on while they're still in flight.
    !allDependencies ||
    !allTaskResources ||
    !allRemarks ||
    !allAttachments
  ) {
    return null;
  }

  return (
    <TaskGrid
      tasks={teamScopedTasks}
      projects={projects}
      components={components}
      people={people}
      personRoles={personRoles}
      teams={teams}
      scheduleGraph={scheduleGraph}
      resourceIdsByTask={resourceIdsByTask}
      attachmentsCountByTask={attachmentsCountByTask}
      remarksCountByTask={remarksCountByTask}
      columns={DEFAULT_TASK_GRID_COLUMNS}
      showFilters
      initialFilterState={filterState}
    />
  );
}
