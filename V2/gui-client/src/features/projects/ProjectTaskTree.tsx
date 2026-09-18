import Box from "@mui/material/Box";
import IconButton from "@mui/material/IconButton";
import Typography from "@mui/material/Typography";
import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import { SimpleTreeView } from "@mui/x-tree-view/SimpleTreeView";
import { TreeItem } from "@mui/x-tree-view/TreeItem";
import type { ProjectRecord, TaskRecord } from "../../api/types";
import { computeTaskRowColour, computeUrgency, getTaskSchedule, isProjectActive, priorityWeight, type ScheduleGraph } from "../../lib/schedule";
import { DENSE_FONT_SIZE } from "../../theme/theme";

// ProjectDetailPlan.md §4.4's Task-visibility filter (None/Open/All) — a
// pure client-side display filter, never sent to the server, matching the
// same three-value shape V1.2's own GUIProject.TasksDisplayValues used.
export type TaskVisibility = "None" | "Open" | "All";

function isTaskVisible(task: TaskRecord, visibility: TaskVisibility): boolean {
  if (visibility === "None") return false;
  if (visibility === "All") return true;
  return task.status !== "Closed" && task.status !== "Cancelled"; // "Open"
}

// Sibling Projects (`parentId: null` for the top-level ones) are ordered
// highest Priority first, then alphabetically within the same Priority —
// the same Priority-to-number mapping Urgency already uses (High=5 down
// to Low=1, Closed=0, Cancelled=-1 last), not a separately-invented order.
function childProjectsOf(projects: ProjectRecord[], parentId: number | null): ProjectRecord[] {
  return projects
    .filter((p) => p.parent_project_id === parentId)
    .sort((a, b) => priorityWeight(b.priority) - priorityWeight(a.priority) || a.name.localeCompare(b.name));
}

function childTasksOf(tasks: TaskRecord[], projectId: number, graph: ScheduleGraph, visibility: TaskVisibility): TaskRecord[] {
  return tasks
    .filter((t) => t.project_id === projectId && isTaskVisible(t, visibility))
    .sort((a, b) => {
      const sa = getTaskSchedule(graph, a.task_id).startDate?.getTime() ?? Number.POSITIVE_INFINITY;
      const sb = getTaskSchedule(graph, b.task_id).startDate?.getTime() ?? Number.POSITIVE_INFINITY;
      return sa - sb;
    });
}

interface TreeCallbacks {
  onOpenProject: (project: ProjectRecord) => void;
  onOpenTask: (task: TaskRecord) => void;
  onRenameProject: (project: ProjectRecord) => void;
  onDeleteProject: (project: ProjectRecord) => void;
  canManageTeam: (teamId: number) => boolean;
}

function ProjectRowLabel({ project, active, callbacks }: { project: ProjectRecord; active: boolean; callbacks: TreeCallbacks }) {
  const canManage = callbacks.canManageTeam(project.team_id);
  return (
    <Box sx={{ display: "flex", alignItems: "center", gap: "6px", width: "100%", py: "2px" }}>
      <Box
        component="span"
        onClick={(event: React.MouseEvent) => {
          event.stopPropagation();
          callbacks.onOpenProject(project);
        }}
        sx={{
          fontWeight: 700,
          fontSize: DENSE_FONT_SIZE,
          // Muted, not hidden — a Project with no open Task and no active
          // sub-Project (lib/schedule.ts's isProjectActive) still needs to
          // stay reachable to browse into, per V1.2's own embedded tree
          // showing every sub-Project regardless of activity.
          color: active ? "inherit" : "rgba(0,0,0,0.45)",
          cursor: "pointer",
          "&:hover": { textDecoration: "underline" },
        }}
      >
        {project.name}
      </Box>
      {canManage && (
        <>
          <IconButton
            size="small"
            onClick={(event) => {
              event.stopPropagation();
              callbacks.onRenameProject(project);
            }}
          >
            <EditIcon fontSize="inherit" />
          </IconButton>
          <IconButton
            size="small"
            onClick={(event) => {
              event.stopPropagation();
              callbacks.onDeleteProject(project);
            }}
          >
            <DeleteIcon fontSize="inherit" />
          </IconButton>
        </>
      )}
    </Box>
  );
}

function TaskRowLabel({ task, graph, callbacks }: { task: TaskRecord; graph: ScheduleGraph; callbacks: TreeCallbacks }) {
  const { startDate, endDate } = getTaskSchedule(graph, task.task_id);
  const urgency = computeUrgency(task, graph.projectsById, startDate, endDate);
  return (
    <Box
      onClick={(event: React.MouseEvent) => {
        event.stopPropagation();
        callbacks.onOpenTask(task);
      }}
      sx={{
        display: "flex",
        alignItems: "center",
        gap: "6px",
        width: "100%",
        py: "2px",
        cursor: "pointer",
        "&:hover": { textDecoration: "underline" },
      }}
    >
      <Box component="span" sx={{ fontSize: DENSE_FONT_SIZE }}>
        {task.description}
      </Box>
      <Box
        component="span"
        sx={{
          fontSize: 10,
          fontWeight: 700,
          px: "4px",
          borderRadius: "3px",
          bgcolor: computeTaskRowColour(task.priority, urgency),
        }}
      >
        {urgency.toFixed(1)}
      </Box>
    </Box>
  );
}

function ProjectNode({
  project,
  projects,
  tasks,
  graph,
  taskVisibility,
  callbacks,
}: {
  project: ProjectRecord;
  projects: ProjectRecord[];
  tasks: TaskRecord[];
  graph: ScheduleGraph;
  taskVisibility: TaskVisibility;
  callbacks: TreeCallbacks;
}) {
  const subProjects = childProjectsOf(projects, project.project_id);
  const visibleTasks = childTasksOf(tasks, project.project_id, graph, taskVisibility);
  return (
    <TreeItem
      itemId={`project:${project.project_id}`}
      label={<ProjectRowLabel project={project} active={isProjectActive(graph, project.project_id)} callbacks={callbacks} />}
    >
      {visibleTasks.map((t) => (
        <TreeItem key={t.task_id} itemId={`task:${t.task_id}`} label={<TaskRowLabel task={t} graph={graph} callbacks={callbacks} />} />
      ))}
      {subProjects.map((sp) => (
        <ProjectNode
          key={sp.project_id}
          project={sp}
          projects={projects}
          tasks={tasks}
          graph={graph}
          taskVisibility={taskVisibility}
          callbacks={callbacks}
        />
      ))}
    </TreeItem>
  );
}

/**
 * ProjectDetailPlan.md §4.4 — the sub-Project/Task browsing tree, built
 * with `@mui/x-tree-view` (chosen specifically for this, `4_GuiClient/
 * Plan.md` D1.4-1), not the existing bespoke `TreePicker.tsx` (that's
 * shaped for a dropdown single-selection picker, not an always-visible
 * browsing/management tree with per-row actions).
 *
 * `rootProjectId: null` is the "Top Level Projects" browsing mode
 * (ProjectDetailPage.tsx's own no-Project route) — every top-level Project
 * across the caller's own Teams becomes a top-level tree row. Otherwise
 * (a specific Project open) the tree shows that Project's own direct
 * sub-Projects and Tasks as its top-level rows — the Project itself is the
 * page you're already on, not repeated as its own tree node. Either way,
 * whether Tasks appear at all, at any depth, is governed purely by the
 * Task-visibility filter (`taskVisibility`) — there is no separate,
 * mode-based rule hiding them.
 */
export function ProjectTaskTree({
  projects,
  tasks,
  rootProjectId,
  graph,
  taskVisibility,
  callbacks,
}: {
  projects: ProjectRecord[];
  tasks: TaskRecord[];
  rootProjectId: number | null;
  graph: ScheduleGraph;
  taskVisibility: TaskVisibility;
  callbacks: TreeCallbacks;
}) {
  const topLevelProjects = childProjectsOf(projects, rootProjectId);
  const topLevelTasks = rootProjectId == null ? [] : childTasksOf(tasks, rootProjectId, graph, taskVisibility);

  if (topLevelProjects.length === 0 && topLevelTasks.length === 0) {
    return (
      <Typography variant="body2" color="text.secondary" sx={{ py: 1 }}>
        Nothing to show.
      </Typography>
    );
  }

  return (
    <SimpleTreeView sx={{ flexGrow: 1, overflowY: "auto" }}>
      {topLevelTasks.map((t) => (
        <TreeItem key={t.task_id} itemId={`task:${t.task_id}`} label={<TaskRowLabel task={t} graph={graph} callbacks={callbacks} />} />
      ))}
      {topLevelProjects.map((p) => (
        <ProjectNode
          key={p.project_id}
          project={p}
          projects={projects}
          tasks={tasks}
          graph={graph}
          taskVisibility={taskVisibility}
          callbacks={callbacks}
        />
      ))}
    </SimpleTreeView>
  );
}
