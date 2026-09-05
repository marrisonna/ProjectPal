/**
 * Opens (or refocuses) a browser window for `path`, named `windowName` —
 * window.open's own named-target behaviour re-focuses an already-open
 * window for the same name instead of creating a duplicate or navigating
 * whatever window you called this from, which is what actually delivers
 * V1.2's singleton-per-object re-focusing here (D1.4-8).
 */
export function openNamedWindow(path: string, windowName: string): void {
  window.open(path, windowName);
}

/** One singleton window per (entityType, entityId) — see openNamedWindow. */
export function openItemWindow(entityType: string, entityId: string | number): void {
  openNamedWindow(`/${entityType}/${entityId}`, `${entityType}-${entityId}`);
}

/** One singleton window for a whole list view (e.g. "tasks" -> "All Tasks"). */
export function openListWindow(entityType: string): void {
  openNamedWindow(`/${entityType}`, `${entityType}-list`);
}
