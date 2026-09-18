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

// The new, TaskGrid-based All Tasks screen (TaskGridPlan.md §5.2, D1.4-50),
// taking over the primary /tasks route immediately for direct side-by-side
// comparison with AllTaskOrigPage.tsx (still reachable at /tasks-orig until
// approved, §5.3/5.4). Reproduces AllTaskOrigPage.tsx's own data-fetching,
// Team-scoping, and default filter state exactly (the user's own
// acceptance criterion: "the look and feel... should be identical to the
// current AllTask window") — everything specific to *this* screen lives
// here, not in TaskGrid itself, which only ever renders whatever `tasks`
// it's handed (§4.1).
export function AllTaskPage() {
  useDocumentTitle("All Tasks");
  useSingletonWindowIdentity("tasks-list");
  const { person } = useAuth();
  const { data: tasks, isLoading } = useTasks();
  const { data: projects } = useProjects();
  const { data: components } = useComponents();
  const { data: people } = usePeople();
  const { data: personRoles } = usePersonRoles();
  const { data: allDependencies } = useAllDependencies();
  const { data: allTaskResources } = useAllTaskResources();
  const { data: allRemarks } = useAllRemarks();
  const { data: allAttachments } = useAllAttachments();

  const isTeamLead = isTeamLeadOfAnyTeam(person);

  // Same default filter state AllTaskOrigPage.tsx computes (D-Win-16): every
  // Status except Closed/Cancelled. This is window-level display
  // configuration (§4.6), not a permission — a different window embedding
  // TaskGrid is free to start with a different default.
  const [filterState, setFilterState] = useState<Record<string, ColumnFilterState>>(() => ({
    status: {
      contains: "",
      exact: new Set(TASK_STATUSES.filter((s) => s !== "Cancelled" && s !== "Closed")),
    },
  }));

  // Same one-time "default Resources filter to yourself unless you're a
  // Team Lead" behaviour as AllTaskOrigPage.tsx (D-Win-15). TaskGrid only
  // ever reads `initialFilterState` once (a lazy useState initializer, so
  // its own filter UI stays responsive to the user's later edits without
  // fighting a prop that keeps changing underneath it) — so this has to
  // finish updating `filterState` *before* TaskGrid first mounts, not
  // asynchronously afterwards the way the single-component original could.
  // `defaultFilterReady` (below) gates that first mount on it.
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
  // (D-Win-17) — see AllTaskOrigPage.tsx's own comment for why this is a
  // floor under the row set itself, not a clearable filter.
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

  if (isLoading || !projects || !components || !people || !personRoles || !defaultFilterReady) {
    return null;
  }

  return (
    <TaskGrid
      tasks={teamScopedTasks}
      projects={projects}
      components={components}
      people={people}
      personRoles={personRoles}
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
