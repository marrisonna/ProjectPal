// D1.4-122 — user preferences that live entirely in this browser
// (`localStorage`), never sent to or read from the server: per-Person,
// per-machine, per-browser-profile settings, not Organisation data. Read
// synchronously wherever needed (`windowNav.ts`'s own `openNamedWindow`,
// below), not cached in React state anywhere outside the Settings window
// itself — a change made in one window takes effect the very next time *any*
// window reads it (e.g. the next window opened from anywhere), with no
// cross-window sync mechanism needed, unlike this app's own genuine live-data
// sync (`liveSync.ts`'s `BroadcastChannel`, which solves a different problem:
// keeping already-rendered *data* in sync across windows, not a one-shot
// read at the moment a new window opens).

const NEW_WINDOW_MODE_KEY = "projectpal.settings.newWindowMode";

// "Window"/"Tab" as literal values, not e.g. "window"/"tab" — this setting
// has no database or V1.2 precedent to mirror (it's a genuinely new, V2-only,
// client-side-only concept), so there's no existing naming convention to
// match; matching the dropdown's own display text directly avoids a separate
// value-to-label mapping for no benefit.
export const NEW_WINDOW_MODES = ["Window", "Tab"] as const;
export type NewWindowMode = (typeof NEW_WINDOW_MODES)[number];
const DEFAULT_NEW_WINDOW_MODE: NewWindowMode = "Tab";

export function getNewWindowMode(): NewWindowMode {
  const raw = localStorage.getItem(NEW_WINDOW_MODE_KEY);
  return (NEW_WINDOW_MODES as readonly string[]).includes(raw ?? "")
    ? (raw as NewWindowMode)
    : DEFAULT_NEW_WINDOW_MODE;
}

export function setNewWindowMode(mode: NewWindowMode): void {
  localStorage.setItem(NEW_WINDOW_MODE_KEY, mode);
}
