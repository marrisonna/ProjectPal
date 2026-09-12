// Shared between TaskDetailPage.tsx (drag source) and DependenciesPanel.tsx
// (drop targets) for the D1.4-10 cross-window drag-and-drop spike — kept in
// one place so the two sides can't drift out of sync on the MIME type
// string. Confirmed (manually, across two separate window.open()'d Task
// Detail windows) that native HTML5 drag-and-drop crosses browser windows
// opened this way, not just tabs — see 4_GuiClient/Plan.md §6.2.
export const TASK_DRAG_MIME_TYPE = "application/x-projectpal-task";
