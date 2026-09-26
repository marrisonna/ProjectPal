// Shared between TaskDetailPage.tsx (drag source) and DependenciesPanel.tsx
// (drop targets) for the D1.4-10 cross-window drag-and-drop spike — kept in
// one place so the two sides can't drift out of sync on the MIME type
// string. Confirmed (manually, across two separate window.open()'d Task
// Detail windows) that native HTML5 drag-and-drop crosses browser windows
// opened this way, not just tabs — see 4_GuiClient/Plan.md §6.2.
export const TASK_DRAG_MIME_TYPE = "application/x-projectpal-task";

// UserInteractionPlan.md §5.2 — the app-wide "this is a valid drop target,
// right now" cue, originated by DependenciesPanel.tsx's own two drop zones
// (D1.4-10) and adopted here as the shared standard: a dashed outline (not a
// border, so it never shifts layout by taking up space) in the theme's own
// primary colour, applied only while a compatible drag is actually
// hovering — never a persistent cue. `Stage5-B1` — refactored out of
// DependenciesPanel.tsx's own private copy, no visual/behavioural change.
export const DROP_TARGET_ACTIVE_SX = {
  outline: "2px dashed",
  outlineColor: "primary.main",
  outlineOffset: "-2px",
  borderRadius: "4px",
};

// UserInteractionPlan.md §5.3 — the "discrete, small drag handle" half of
// the app's two deliberately-different draggable cues (§2.3): a `cursor:
// "grab"` cursor change, for a small badge/icon that is not also the row's
// own primary content (Task Detail's own title badge, D1.4-10, is the
// original example) — as opposed to `draggableRowHighlightSx` below, used
// for a whole draggable row instead. `Stage5-B2` — refactored out of Task
// Detail's own private `cursor: "grab"`, no visual/behavioural change.
export const DRAG_HANDLE_SX = { cursor: "grab" as const };

// UserInteractionPlan.md §5.3 — the "whole draggable row in a dense list"
// cue (the Gantt view's own bars, D1.4-27/D1.4-32): a dotted hover-highlight
// rather than a cursor change, since changing the cursor for an entire row
// the user might be hovering for other reasons (reading its label,
// right-clicking it) is a heavier-handed signal than the row-highlight
// already gives for free. Returns the SVG attributes a `<rect>` overlay
// needs — not a `sx` object, since the Gantt draws this as plain SVG, not a
// MUI `Box` — `isHovered` false yields a fully invisible outline (no stroke)
// rather than omitting attributes, so a future *always-mounted* row overlay
// (toggling this per row on every render, unlike PlanPage.tsx's own
// today, which only mounts the element at all while a row is hovered) can
// use this the same way without needing its own separate "hidden" case.
// `Stage5-B3` — refactored out of PlanPage.tsx's own private `<rect>`
// attributes, no visual/behavioural change.
export function draggableRowHighlightSx(isHovered: boolean) {
  if (!isHovered) return { fill: "none", stroke: "none", style: { pointerEvents: "none" as const } };
  return {
    fill: "none",
    stroke: "rgba(0,0,0,0.6)",
    strokeWidth: 1,
    strokeDasharray: "2,2",
    style: { pointerEvents: "none" as const },
  };
}
