import { useId, useState } from "react";
import Box from "@mui/material/Box";
import type {
  ComponentRecord,
  PersonRecord,
  PersonRoleRecord,
  ProjectRecord,
  TaskRecord,
} from "../../api/types";
import { getTaskSchedule, type ScheduleGraph } from "../../lib/schedule";
import { isTaskVisible, type TaskVisibility } from "../projects/Projects";
import { DENSE_FONT_SIZE } from "../../theme/theme";
import { COMPONENT_EMBEDDED_TASK_GRID_COLUMNS, TaskGrid } from "../tasks/TaskGrid";
import { Component } from "./Component";

export type { TaskVisibility };

const TASK_VISIBILITY_OPTIONS: TaskVisibility[] = ["None", "Open", "All"];

// Sibling Components (`parentId: null` for the top-level ones) are ordered
// alphabetically only — unlike Project's own priority-then-name order
// (`Projects.tsx`'s `childProjectsOf`), Component has no Priority field at
// all to weight by (`ComponentDetailPlan.md` §2.1, `D1.4-67`), matching
// V1.2's own `SortComponentName`/`SortComponents`, both a plain
// `string.Compare(a.Name, b.Name)`.
export function childComponentsOf(components: ComponentRecord[], parentId: number | null): ComponentRecord[] {
  return components
    .filter((c) => c.parent_component_id === parentId)
    .sort((a, b) => a.name.localeCompare(b.name));
}

export function childTasksOfComponent(
  tasks: TaskRecord[],
  componentId: number,
  graph: ScheduleGraph,
  visibility: TaskVisibility,
): TaskRecord[] {
  return tasks
    .filter((t) => t.component_id === componentId && isTaskVisible(t, visibility))
    .sort((a, b) => {
      const sa = getTaskSchedule(graph, a.task_id).startDate?.getTime() ?? Number.POSITIVE_INFINITY;
      const sb = getTaskSchedule(graph, b.task_id).startDate?.getTime() ?? Number.POSITIVE_INFINITY;
      return sa - sb;
    });
}

// Shared by both Component.tsx and this file (ComponentDetailPlan.md §4.3,
// mirroring ProjectTreeSharedProps) — the reference data and action
// callbacks every level of the recursion passes down unchanged.
// Deliberately carries no permission-check props at all (D1.4-64/D1.4-66):
// Component computes those for itself.
export interface ComponentTreeSharedProps {
  components: ComponentRecord[];
  projects: ProjectRecord[];
  tasks: TaskRecord[];
  people: PersonRecord[];
  personRoles: PersonRoleRecord[];
  scheduleGraph: ScheduleGraph;
  resourceIdsByTask: Map<number, number[]>;
  attachmentsCountByTask: Map<number, number>;
  remarksCountByTask: Map<number, number>;
  onOpenComponent: (component: ComponentRecord) => void;
  onRenameComponent: (component: ComponentRecord) => void;
  onDeleteComponent: (component: ComponentRecord) => void;
  onAddTask: (parent: ComponentRecord) => void;
}

export interface ComponentsProps extends ComponentTreeSharedProps {
  /** null renders every top-level Component (the "Top Level Components" window). */
  parentComponentId: number | null;
  // When set, this Component's own Tasks render (via the same embedded
  // TaskGrid treatment §4.1's Component uses) before the sibling Component
  // list — for ComponentDetailPage.tsx's "a specific Component is open"
  // mode, where the Component itself isn't repeated as its own row (the
  // page you're already on), but its own Tasks still need to appear
  // somewhere.
  alsoShowTasksForComponent?: ComponentRecord;
  /** Default true — false for a Component's own nested sub-Components list. */
  showToggle?: boolean;
  defaultTaskVisibility?: TaskVisibility;
  /** Required when showToggle is false. */
  taskVisibility?: TaskVisibility;
}

/**
 * ComponentDetailPlan.md §4.3/§5.2 (`D1.4-70`) — a new, parallel pair
 * mirroring `Projects.tsx`'s own conventions closely, not a generic reuse
 * of it: the None/Open/All Task-visibility toggle (only at the outermost
 * nesting level, `showToggle`) followed by one `Component` per sibling.
 * Self-contained/uncontrolled by default, same as `Projects`. No "Only
 * Active" checkbox — Component has no Priority/activity concept to filter
 * by at all (`D1.4-67`).
 *
 * Recursion goes back through this component, not a plain inline list,
 * exactly like `Projects`/`Project` — `Component`'s own sub-Components
 * section renders a nested `<Components showToggle={false} .../>`.
 */
export function Components({
  parentComponentId,
  alsoShowTasksForComponent,
  components,
  projects,
  tasks,
  people,
  personRoles,
  scheduleGraph,
  resourceIdsByTask,
  attachmentsCountByTask,
  remarksCountByTask,
  onOpenComponent,
  onRenameComponent,
  onDeleteComponent,
  onAddTask,
  showToggle = true,
  defaultTaskVisibility = "None",
  taskVisibility: controlledTaskVisibility,
}: ComponentsProps) {
  const [ownTaskVisibility, setOwnTaskVisibility] = useState<TaskVisibility>(defaultTaskVisibility);
  const taskVisibility = showToggle ? ownTaskVisibility : controlledTaskVisibility!;
  const visibilityGroupName = useId();

  const siblingComponents = childComponentsOf(components, parentComponentId);
  const ownTasks = alsoShowTasksForComponent
    ? childTasksOfComponent(tasks, alsoShowTasksForComponent.component_id, scheduleGraph, taskVisibility)
    : [];

  return (
    <Box>
      {showToggle && (
        <Box sx={{ display: "flex", alignItems: "center", gap: "10px", mb: "6px" }}>
          <Box sx={{ fontSize: DENSE_FONT_SIZE }}>Tasks</Box>
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
            columns={COMPONENT_EMBEDDED_TASK_GRID_COLUMNS}
            showFilters={false}
          />
        </Box>
      )}
      {siblingComponents.length === 0 && ownTasks.length === 0 ? null : (
        siblingComponents.map((c) => (
          <Component
            key={c.component_id}
            component={c}
            components={components}
            projects={projects}
            tasks={tasks}
            people={people}
            personRoles={personRoles}
            scheduleGraph={scheduleGraph}
            resourceIdsByTask={resourceIdsByTask}
            attachmentsCountByTask={attachmentsCountByTask}
            remarksCountByTask={remarksCountByTask}
            taskVisibility={taskVisibility}
            onOpenComponent={onOpenComponent}
            onRenameComponent={onRenameComponent}
            onDeleteComponent={onDeleteComponent}
            onAddTask={onAddTask}
          />
        ))
      )}
    </Box>
  );
}
