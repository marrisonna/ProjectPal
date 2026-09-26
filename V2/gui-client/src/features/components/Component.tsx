import { useState } from "react";
import Box from "@mui/material/Box";
import IconButton from "@mui/material/IconButton";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import AddIcon from "@mui/icons-material/Add";
import TimelineIcon from "@mui/icons-material/Timeline";
import type { ComponentRecord } from "../../api/types";
import { useAuth } from "../../auth/AuthContext";
import { CLICKABLE_SX } from "../../components/DenseField";
import { HintTooltip } from "../../components/HintTooltip";
import { canEditOwnedRecord, hasRoleAtLeast } from "../../lib/permissions";
import { openItemWindow } from "../../lib/windowNav";
import { DENSE_FONT_SIZE } from "../../theme/theme";
import { COMPONENT_EMBEDDED_TASK_GRID_COLUMNS, TaskGrid } from "../tasks/TaskGrid";
import { childTasksOfComponent, Components, type ComponentTreeSharedProps, type TaskVisibility } from "./Components";

// Same treatment as Project.tsx's own row icons — see that file's own
// comment for why (smaller, lighter grey than the IconButton/icon
// defaults, darkening to the theme's primary colour on hover).
const ROW_ICON_BUTTON_SX = {
  p: "3px",
  "&:hover .MuiSvgIcon-root": { color: "primary.main" },
};
const ROW_ICON_SX = { fontSize: 15, color: "rgba(0,0,0,0.28)" };

// Same VS Code-style rotating chevron as Project.tsx's own expand/collapse
// control — see that file's own comment for why (a bordered +/- button
// read as too prominent for something clicked this often).
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

export interface ComponentProps extends ComponentTreeSharedProps {
  component: ComponentRecord;
  taskVisibility: TaskVisibility;
  initiallyExpanded?: boolean;
}

/**
 * ComponentDetailPlan.md §4.3/§5.2 (`D1.4-70`) — one Component row: its name
 * (plus rename/delete/add-task icons, each gated by its own server-matching
 * permission rule computed here, `D1.4-66` — never passed in from the
 * embedding window), an embedded TaskGrid for its own Tasks, and a nested
 * Components instance (`showToggle={false}`) for its own sub-Components,
 * recursing to any depth. Collapsed by default (`initiallyExpanded`).
 *
 * A new, parallel component mirroring `Project.tsx`'s own conventions
 * closely, not a generic reuse of it (`D1.4-70`) — Component's own rules
 * genuinely differ in three places: `canDelete` below (owner-or-team-lead,
 * not team-lead-only, `D1.4-66`), no "is this Component active" concept to
 * mute an inactive row's name by (Component has no Priority field at all),
 * and its embedded TaskGrid uses `COMPONENT_EMBEDDED_TASK_GRID_COLUMNS`
 * (shows Project, not Component — the inverse of `Project`'s own grid).
 */
export function Component({
  component,
  components,
  projects,
  tasks,
  people,
  personRoles,
  scheduleGraph,
  resourceIdsByTask,
  attachmentsCountByTask,
  remarksCountByTask,
  taskVisibility,
  onOpenComponent,
  onRenameComponent,
  onDeleteComponent,
  onAddTask,
  initiallyExpanded = false,
}: ComponentProps) {
  const [expanded, setExpanded] = useState(initiallyExpanded);
  const { person } = useAuth();

  // D1.4-66: computed here, matching the server's own rules exactly
  // (rest-api/app/routes/components.py) — never injected from the
  // embedding window (D1.4-64's own principle, applied to Component too).
  // canRename and canDelete are deliberately the *same* formula here —
  // unlike Project, where delete is team-lead-only — because
  // delete_component uses require_owner_or_team_lead, identical to
  // update_component's own rule. Confirmed by reading the route directly,
  // not assumed by analogy with Project.
  const canRename = canEditOwnedRecord(person, component.team_id, component.owner_person_id);
  const canDelete = canEditOwnedRecord(person, component.team_id, component.owner_person_id);
  const canAddTaskHere = hasRoleAtLeast(person, component.team_id, "LeadUser");

  const visibleTasks = childTasksOfComponent(tasks, component.component_id, scheduleGraph, taskVisibility);

  return (
    <Box>
      <Box sx={{ display: "flex", alignItems: "center", gap: "4px", py: "2px" }}>
        <HintTooltip hint={expanded ? "Click: Collapse." : "Click: Expand."}>
          <IconButton
            size="small"
            sx={EXPAND_TOGGLE_SX}
            onClick={() => setExpanded((e) => !e)}
            aria-label={expanded ? "Collapse" : "Expand"}
          >
            <ChevronRightIcon sx={expandChevronSx(expanded)} />
          </IconButton>
        </HintTooltip>
        <HintTooltip hint="Click: Open this Component's own window.">
          <Box
            component="span"
            onClick={() => onOpenComponent(component)}
            sx={{
              fontWeight: 700,
              fontSize: DENSE_FONT_SIZE,
              ...CLICKABLE_SX,
            }}
          >
            {component.name}
          </Box>
        </HintTooltip>
        {canRename && (
          <IconButton size="small" sx={ROW_ICON_BUTTON_SX} onClick={() => onRenameComponent(component)}>
            <EditIcon sx={ROW_ICON_SX} />
          </IconButton>
        )}
        {canDelete && (
          <IconButton size="small" sx={ROW_ICON_BUTTON_SX} onClick={() => onDeleteComponent(component)}>
            <DeleteIcon sx={ROW_ICON_SX} />
          </IconButton>
        )}
        {canAddTaskHere && (
          <IconButton size="small" sx={ROW_ICON_BUTTON_SX} onClick={() => onAddTask(component)} title="Add Task">
            <AddIcon sx={ROW_ICON_SX} />
          </IconButton>
        )}
        {/* D1.4-109 — this Component's own Gantt view (the Component→
            SubComponent tree, distinct from Project's own Gantt — see
            lib/ganttLayout.ts's buildComponentGanttLayout). Read-only, no
            permission gate, same reasoning as Project.tsx's own shortcut. */}
        <IconButton
          size="small"
          sx={ROW_ICON_BUTTON_SX}
          onClick={() => openItemWindow("plan-component", component.component_id)}
          title="Gantt Display"
        >
          <TimelineIcon sx={ROW_ICON_SX} />
        </IconButton>
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
                columns={COMPONENT_EMBEDDED_TASK_GRID_COLUMNS}
                showFilters={false}
              />
            </Box>
          )}
          <Components
            parentComponentId={component.component_id}
            components={components}
            projects={projects}
            tasks={tasks}
            people={people}
            personRoles={personRoles}
            scheduleGraph={scheduleGraph}
            resourceIdsByTask={resourceIdsByTask}
            attachmentsCountByTask={attachmentsCountByTask}
            remarksCountByTask={remarksCountByTask}
            onOpenComponent={onOpenComponent}
            onRenameComponent={onRenameComponent}
            onDeleteComponent={onDeleteComponent}
            onAddTask={onAddTask}
            showToggle={false}
            taskVisibility={taskVisibility}
          />
        </Box>
      )}
    </Box>
  );
}
