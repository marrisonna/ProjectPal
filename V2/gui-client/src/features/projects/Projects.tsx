import { useId, useState } from "react";
import Box from "@mui/material/Box";
import type {
  ComponentRecord,
  PersonRecord,
  PersonRoleRecord,
  ProjectRecord,
  TaskRecord,
} from "../../api/types";
import { getTaskSchedule, isProjectActive, priorityWeight, type ScheduleGraph } from "../../lib/schedule";
import { DENSE_FONT_SIZE } from "../../theme/theme";
import { EMBEDDED_TASK_GRID_COLUMNS, TaskGrid } from "../tasks/TaskGrid";
import { Project } from "./Project";

const TASK_VISIBILITY_OPTIONS: TaskVisibility[] = ["None", "Open", "All"];

// ProjectsGUIComponent.md §4.2's Task-visibility filter (None/Open/All) — a
// pure client-side display filter, never sent to the server, matching the
// same three-value shape V1.2's own GUIProject.TasksDisplayValues used.
export type TaskVisibility = "None" | "Open" | "All";

export function isTaskVisible(task: TaskRecord, visibility: TaskVisibility): boolean {
  if (visibility === "None") return false;
  if (visibility === "All") return true;
  return task.status !== "Closed" && task.status !== "Cancelled"; // "Open"
}

// Sibling Projects (`parentId: null` for the top-level ones) are ordered
// highest Priority first, then alphabetically within the same Priority —
// the same Priority-to-number mapping Urgency already uses, not a
// separately-invented order.
export function childProjectsOf(projects: ProjectRecord[], parentId: number | null): ProjectRecord[] {
  return projects
    .filter((p) => p.parent_project_id === parentId)
    .sort((a, b) => priorityWeight(b.priority) - priorityWeight(a.priority) || a.name.localeCompare(b.name));
}

export function childTasksOf(
  tasks: TaskRecord[],
  projectId: number,
  graph: ScheduleGraph,
  visibility: TaskVisibility,
): TaskRecord[] {
  return tasks
    .filter((t) => t.project_id === projectId && isTaskVisible(t, visibility))
    .sort((a, b) => {
      const sa = getTaskSchedule(graph, a.task_id).startDate?.getTime() ?? Number.POSITIVE_INFINITY;
      const sb = getTaskSchedule(graph, b.task_id).startDate?.getTime() ?? Number.POSITIVE_INFINITY;
      return sa - sb;
    });
}

// Shared by both Project.tsx and this file (ProjectsGUIComponent.md §4.1/
// §4.2) — the reference data and action callbacks every level of the
// recursion passes down unchanged. Deliberately carries no permission-check
// props at all (D1.4-64): Project computes those for itself.
export interface ProjectTreeSharedProps {
  projects: ProjectRecord[];
  tasks: TaskRecord[];
  components: ComponentRecord[];
  people: PersonRecord[];
  personRoles: PersonRoleRecord[];
  scheduleGraph: ScheduleGraph;
  resourceIdsByTask: Map<number, number[]>;
  attachmentsCountByTask: Map<number, number>;
  remarksCountByTask: Map<number, number>;
  onOpenProject: (project: ProjectRecord) => void;
  onRenameProject: (project: ProjectRecord) => void;
  onDeleteProject: (project: ProjectRecord) => void;
  onAddTask: (parent: ProjectRecord) => void;
}

export interface ProjectsProps extends ProjectTreeSharedProps {
  /** null renders every top-level Project (the "Top Level Projects" window). */
  parentProjectId: number | null;
  // When set, this Project's own Tasks render (via the same embedded
  // TaskGrid treatment §4.1's Project uses) before the sibling Project
  // list — for ProjectDetailPage.tsx's "a specific Project is open" mode,
  // where the Project itself isn't repeated as its own row (the page
  // you're already on), but its own Tasks still need to appear somewhere.
  alsoShowTasksForProject?: ProjectRecord;
  /** Default true — false for a Project's own nested sub-Projects list (D1.4-58). */
  showToggle?: boolean;
  defaultTaskVisibility?: TaskVisibility;
  /** Required when showToggle is false. */
  taskVisibility?: TaskVisibility;
  /** Default true — matches V1.2's own "active projects only" checkbox default. */
  defaultActiveOnly?: boolean;
  /** Required when showToggle is false. */
  activeOnly?: boolean;
}

/**
 * ProjectsGUIComponent.md §4.2 — the None/Open/All Task-visibility toggle
 * (only at the outermost nesting level, `showToggle`) followed by one
 * `Project` per sibling. Self-contained/uncontrolled by default: an
 * embedding window never needs to own `taskVisibility` state itself unless
 * it also needs the value for something adjacent to this component
 * (`alsoShowTasksForProject`, used by ProjectDetailPage.tsx for the
 * currently-open Project's own Tasks).
 *
 * Recursion goes back through this component, not a plain inline list
 * (D1.4-58) — `Project`'s own sub-Projects section renders a nested
 * `<Projects showToggle={false} .../>`, so "sort siblings, render one
 * Project per" lives in exactly one place.
 */
export function Projects({
  parentProjectId,
  alsoShowTasksForProject,
  projects,
  tasks,
  components,
  people,
  personRoles,
  scheduleGraph,
  resourceIdsByTask,
  attachmentsCountByTask,
  remarksCountByTask,
  onOpenProject,
  onRenameProject,
  onDeleteProject,
  onAddTask,
  showToggle = true,
  defaultTaskVisibility = "None",
  taskVisibility: controlledTaskVisibility,
  defaultActiveOnly = true,
  activeOnly: controlledActiveOnly,
}: ProjectsProps) {
  const [ownTaskVisibility, setOwnTaskVisibility] = useState<TaskVisibility>(defaultTaskVisibility);
  const taskVisibility = showToggle ? ownTaskVisibility : controlledTaskVisibility!;
  const visibilityGroupName = useId();

  const [ownActiveOnly, setOwnActiveOnly] = useState<boolean>(defaultActiveOnly);
  const activeOnly = showToggle ? ownActiveOnly : controlledActiveOnly!;

  const allSiblingProjects = childProjectsOf(projects, parentProjectId);
  const siblingProjects = activeOnly
    ? allSiblingProjects.filter((p) => isProjectActive(scheduleGraph, p.project_id))
    : allSiblingProjects;
  const ownTasks = alsoShowTasksForProject
    ? childTasksOf(tasks, alsoShowTasksForProject.project_id, scheduleGraph, taskVisibility)
    : [];

  return (
    <Box>
      {showToggle && (
        <Box sx={{ display: "flex", alignItems: "center", gap: "10px", mb: "6px" }}>
          <Box
            component="label"
            sx={{
              display: "flex",
              alignItems: "center",
              gap: "4px",
              fontSize: DENSE_FONT_SIZE,
              cursor: "pointer",
              userSelect: "none",
            }}
          >
            <Box
              component="input"
              type="checkbox"
              checked={ownActiveOnly}
              onChange={(event) => setOwnActiveOnly(event.target.checked)}
              sx={{ width: 13, height: 13, m: 0, cursor: "pointer" }}
            />
            Only Active Projects
          </Box>
          <Box sx={{ fontSize: DENSE_FONT_SIZE, ml: "12px" }}>Tasks</Box>
          {TASK_VISIBILITY_OPTIONS.map((v) => (
            <Box
              component="label"
              key={v}
              sx={{
                display: "flex",
                alignItems: "center",
                gap: "3px",
                fontSize: DENSE_FONT_SIZE,
                cursor: "pointer",
              }}
            >
              <Box
                component="input"
                type="radio"
                name={`task-visibility-${visibilityGroupName}`}
                checked={ownTaskVisibility === v}
                onChange={() => setOwnTaskVisibility(v)}
                sx={{ width: 13, height: 13, m: 0, cursor: "pointer" }}
              />
              {v}
            </Box>
          ))}
        </Box>
      )}
      {ownTasks.length > 0 && (
        <Box sx={{ mb: "6px" }}>
          <TaskGrid
            tasks={ownTasks}
            projects={projects}
            components={components}
            people={people}
            personRoles={personRoles}
            scheduleGraph={scheduleGraph}
            resourceIdsByTask={resourceIdsByTask}
            attachmentsCountByTask={attachmentsCountByTask}
            remarksCountByTask={remarksCountByTask}
            columns={EMBEDDED_TASK_GRID_COLUMNS}
            showFilters={false}
          />
        </Box>
      )}
      {siblingProjects.length === 0 && ownTasks.length === 0 ? (
        <Box sx={{ display: "flex", alignItems: "center", gap: "4px", m: "1px 0 1px 18px" }}>
          <Box component="hr" sx={{ width: "18px", height: 0, m: 0, border: "none", borderTop: "1px solid rgba(0,0,0,0.4)" }} />
          <Box sx={{ fontSize: DENSE_FONT_SIZE, color: "rgba(0,0,0,0.4)" }}>no subprojects</Box>
          <Box component="hr" sx={{ width: "18px", height: 0, m: 0, border: "none", borderTop: "1px solid rgba(0,0,0,0.4)" }} />
        </Box>
      ) : (
        siblingProjects.map((p) => (
          <Project
            key={p.project_id}
            project={p}
            projects={projects}
            tasks={tasks}
            components={components}
            people={people}
            personRoles={personRoles}
            scheduleGraph={scheduleGraph}
            resourceIdsByTask={resourceIdsByTask}
            attachmentsCountByTask={attachmentsCountByTask}
            remarksCountByTask={remarksCountByTask}
            taskVisibility={taskVisibility}
            activeOnly={activeOnly}
            onOpenProject={onOpenProject}
            onRenameProject={onRenameProject}
            onDeleteProject={onDeleteProject}
            onAddTask={onAddTask}
          />
        ))
      )}
    </Box>
  );
}
