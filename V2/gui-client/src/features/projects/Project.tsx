import { useState } from "react";
import Box from "@mui/material/Box";
import IconButton from "@mui/material/IconButton";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import AddIcon from "@mui/icons-material/Add";
import type { ProjectRecord } from "../../api/types";
import { useAuth } from "../../auth/AuthContext";
import { canEditOwnedRecord, hasRoleAtLeast, isTeamLead } from "../../lib/permissions";
import { isProjectActive } from "../../lib/schedule";
import { DENSE_FONT_SIZE } from "../../theme/theme";
import { EMBEDDED_TASK_GRID_COLUMNS, TaskGrid } from "../tasks/TaskGrid";
import { childTasksOf, Projects, type ProjectTreeSharedProps, type TaskVisibility } from "./Projects";

// The rename/delete/add-task row icons default to a fairly large, fully
// black rendering (IconButton's own "small" size is still ~20px, and an
// icon's colour defaults to inherit — near-black body text) — busier than a
// row of secondary actions next to a Project's name needs. Smaller and a
// lighter grey reads as "available, not shouting for attention," darkening
// to the theme's primary colour on hover so the affordance is still clear.
const ROW_ICON_BUTTON_SX = {
  p: "3px",
  "&:hover .MuiSvgIcon-root": { color: "primary.main" },
};
const ROW_ICON_SX = { fontSize: 15, color: "rgba(0,0,0,0.28)" };

// The expand/collapse control (VS Code's own file-tree disclosure triangle,
// not a bordered +/- button — that read as too prominent for something
// clicked this often): a single chevron that rotates 90° open, rather than
// swapping between two different glyphs.
const EXPAND_TOGGLE_SX = {
  p: "1px",
  "&:hover .MuiSvgIcon-root": { color: "rgba(0,0,0,0.75)" },
};
function expandChevronSx(expanded: boolean) {
  return {
    fontSize: 16,
    color: "rgba(0,0,0,0.45)",
    transform: expanded ? "rotate(90deg)" : "none",
    transition: "transform 0.1s ease",
  };
}

export interface ProjectProps extends ProjectTreeSharedProps {
  project: ProjectRecord;
  taskVisibility: TaskVisibility;
  initiallyExpanded?: boolean;
}

/**
 * ProjectsGUIComponent.md §4.1 — one Project row: its name (plus rename/
 * delete/add-task icons, each gated by its own server-matching permission
 * rule computed here, D1.4-64 — never passed in from the embedding window),
 * an embedded TaskGrid for its own Tasks, and a nested Projects instance
 * (`showToggle={false}`, D1.4-58) for its own sub-Projects, recursing to any
 * depth. Collapsed by default (`initiallyExpanded`), matching the previous
 * tree-based browsing UI's own default.
 */
export function Project({
  project,
  projects,
  tasks,
  components,
  people,
  personRoles,
  scheduleGraph,
  resourceIdsByTask,
  attachmentsCountByTask,
  remarksCountByTask,
  taskVisibility,
  onOpenProject,
  onRenameProject,
  onDeleteProject,
  onAddTask,
  initiallyExpanded = false,
}: ProjectProps) {
  const [expanded, setExpanded] = useState(initiallyExpanded);
  const { person } = useAuth();

  // D1.4-64: computed here, matching the server's own rules exactly
  // (rest-api/app/routes/projects.py) — never injected from the embedding
  // window, which is what let a single wrong callback (the old tree's
  // `canManageTeam`) gate both actions with neither's real rule.
  const canRename = canEditOwnedRecord(person, project.team_id, project.owner_person_id);
  const canDelete = isTeamLead(person, project.team_id);
  const canAddTaskHere = hasRoleAtLeast(person, project.team_id, "LeadUser");

  const active = isProjectActive(scheduleGraph, project.project_id);
  const visibleTasks = childTasksOf(tasks, project.project_id, scheduleGraph, taskVisibility);

  return (
    <Box>
      <Box sx={{ display: "flex", alignItems: "center", gap: "4px", py: "2px" }}>
        <IconButton
          size="small"
          sx={EXPAND_TOGGLE_SX}
          onClick={() => setExpanded((e) => !e)}
          aria-label={expanded ? "Collapse" : "Expand"}
        >
          <ChevronRightIcon sx={expandChevronSx(expanded)} />
        </IconButton>
        <Box
          component="span"
          onClick={() => onOpenProject(project)}
          sx={{
            fontWeight: 700,
            fontSize: DENSE_FONT_SIZE,
            // Muted, not hidden — a Project with no open Task and no active
            // sub-Project still needs to stay reachable to browse into.
            color: active ? "inherit" : "rgba(0,0,0,0.45)",
            cursor: "pointer",
            "&:hover": { textDecoration: "underline" },
          }}
        >
          {project.name}
        </Box>
        {canRename && (
          <IconButton size="small" sx={ROW_ICON_BUTTON_SX} onClick={() => onRenameProject(project)}>
            <EditIcon sx={ROW_ICON_SX} />
          </IconButton>
        )}
        {canDelete && (
          <IconButton size="small" sx={ROW_ICON_BUTTON_SX} onClick={() => onDeleteProject(project)}>
            <DeleteIcon sx={ROW_ICON_SX} />
          </IconButton>
        )}
        {canAddTaskHere && (
          <IconButton size="small" sx={ROW_ICON_BUTTON_SX} onClick={() => onAddTask(project)} title="Add Task">
            <AddIcon sx={ROW_ICON_SX} />
          </IconButton>
        )}
      </Box>
      {expanded && (
        <Box sx={{ pl: "18px" }}>
          {visibleTasks.length > 0 && (
            <Box sx={{ mb: "6px" }}>
              <TaskGrid
                tasks={visibleTasks}
                projects={projects}
                components={components}
                people={people}
                personRoles={personRoles}
                scheduleGraph={scheduleGraph}
                resourceIdsByTask={resourceIdsByTask}
                attachmentsCountByTask={attachmentsCountByTask}
                remarksCountByTask={remarksCountByTask}
                columns={EMBEDDED_TASK_GRID_COLUMNS}
              />
            </Box>
          )}
          <Projects
            parentProjectId={project.project_id}
            projects={projects}
            tasks={tasks}
            components={components}
            people={people}
            personRoles={personRoles}
            scheduleGraph={scheduleGraph}
            resourceIdsByTask={resourceIdsByTask}
            attachmentsCountByTask={attachmentsCountByTask}
            remarksCountByTask={remarksCountByTask}
            onOpenProject={onOpenProject}
            onRenameProject={onRenameProject}
            onDeleteProject={onDeleteProject}
            onAddTask={onAddTask}
            showToggle={false}
            taskVisibility={taskVisibility}
          />
        </Box>
      )}
    </Box>
  );
}
