/**
 * Opens (or refocuses) a browser window for `path`, named `windowName` —
 * window.open's own named-target behaviour re-focuses an already-open
 * window for the same name instead of creating a duplicate or navigating
 * whatever window you called this from, which is what actually delivers
 * V1.2's singleton-per-object re-focusing here (D1.4-8).
 */
export function openNamedWindow(path: string, windowName: string, features?: string): void {
  window.open(path, windowName, features);
}

// window.open ignores `features` on an already-open named window (it only
// focuses/navigates it) — this only sets the size the *first* time a given
// window is opened. Sized for TaskDetailPage.tsx's own fixed 656px-wide
// card (+ its 6px outer margin on each side) plus some slack for the
// browser's own window chrome, so it opens without a horizontal scrollbar
// by default; height is a reasonable default for typical content (a task
// with unusually many Remarks/Dependencies may still need a vertical one).
// Width widened by ~28px on top of that (~half the Save button's own
// rendered width, DenseField.tsx's DenseButton — "Save" at 12px/600 weight
// plus its 12px each-side padding and border, roughly 56px) per feedback
// that 700 still felt tight.
const TASK_DETAIL_WINDOW_FEATURES = "width=728,height=800";

/** One singleton window per (entityType, entityId) — see openNamedWindow. */
export function openItemWindow(entityType: string, entityId: string | number): void {
  const features = entityType === "tasks" ? TASK_DETAIL_WINDOW_FEATURES : undefined;
  openNamedWindow(`/${entityType}/${entityId}`, `${entityType}-${entityId}`, features);
}

/** One singleton window for a whole list view (e.g. "tasks" -> "All Tasks"). */
export function openListWindow(entityType: string): void {
  openNamedWindow(`/${entityType}`, `${entityType}-list`);
}
