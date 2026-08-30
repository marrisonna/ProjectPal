/**
 * Opens (or refocuses) a single, per-item browser window, per
 * Claude/Level1_Implementation/4_GuiClient/Plan.md D1.4-8 — window.open's
 * own named-target behaviour re-focuses an already-open window for the
 * same name instead of creating a duplicate, which is what actually
 * delivers V1.2's singleton-per-object re-focusing here.
 */
export function openItemWindow(entityType: string, entityId: string | number): void {
  const path = `/${entityType}/${entityId}`;
  window.open(path, `${entityType}-${entityId}`);
}
