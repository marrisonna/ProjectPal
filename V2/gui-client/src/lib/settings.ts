import { useEffect, useState } from "react";

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

// D1.4-124 (UserInteractionPlan.md §10) — "Hints": whether every interactive
// element in the app shows a tooltip naming its own available gesture(s) and
// what each does (e.g. "Single-click: edit. Double-click: open Task
// Detail."). Unlike "New Window" above, this one genuinely needs to update
// *already-open* windows the moment it changes, not just the next window
// opened — a hint tooltip is rendered continuously by whatever window is
// currently showing it, not read once at some single "do the thing" moment.
// `useHintsEnabled` (below) is what every hint-rendering call site actually
// uses, not `getHintsEnabled` directly, for exactly that reason.
const HINTS_KEY = "projectpal.settings.hints";
export const HINT_MODES = ["On", "Off"] as const;
export type HintMode = (typeof HINT_MODES)[number];
const DEFAULT_HINT_MODE: HintMode = "On";

export function getHintMode(): HintMode {
  const raw = localStorage.getItem(HINTS_KEY);
  return (HINT_MODES as readonly string[]).includes(raw ?? "") ? (raw as HintMode) : DEFAULT_HINT_MODE;
}

export function setHintMode(mode: HintMode): void {
  localStorage.setItem(HINTS_KEY, mode);
}

// Live-reactive: a change made in the Settings window (a separate window)
// fires the browser's own native `storage` event in every *other* open
// window — exactly the windows whose already-rendered hint tooltips need to
// react — with no new pub/sub of this app's own needed. `storage` never
// fires in the window that made the change itself, which is fine here: the
// Settings window doesn't render any hint tooltips of its own.
export function useHintsEnabled(): boolean {
  const [enabled, setEnabled] = useState(() => getHintMode() === "On");
  useEffect(() => {
    function handleStorage(event: StorageEvent) {
      if (event.key === HINTS_KEY || event.key === null) setEnabled(getHintMode() === "On");
    }
    window.addEventListener("storage", handleStorage);
    return () => window.removeEventListener("storage", handleStorage);
  }, []);
  return enabled;
}

// Stage5 — "Show User name in window title": primarily a testing aid (the
// user's own stated reason) — with several `ProjectPal` app windows open at
// once, each logged in as a different Person to exercise multi-user
// scenarios, the OS taskbar/Alt-Tab switcher otherwise shows an identical
// title for every one of them. Same live-reactive shape as "Hints" above
// (`lib/useDocumentTitle.ts`'s effect re-runs on the very next
// `storage` event, so an already-open window's own title updates
// immediately, not just the next window opened) rather than "New Window"'s
// read-once-at-open-time shape.
const SHOW_USER_NAME_IN_TITLE_KEY = "projectpal.settings.showUserNameInTitle";
export const SHOW_USER_NAME_IN_TITLE_MODES = ["On", "Off"] as const;
export type ShowUserNameInTitleMode = (typeof SHOW_USER_NAME_IN_TITLE_MODES)[number];
const DEFAULT_SHOW_USER_NAME_IN_TITLE_MODE: ShowUserNameInTitleMode = "On";

export function getShowUserNameInTitleMode(): ShowUserNameInTitleMode {
  const raw = localStorage.getItem(SHOW_USER_NAME_IN_TITLE_KEY);
  return (SHOW_USER_NAME_IN_TITLE_MODES as readonly string[]).includes(raw ?? "")
    ? (raw as ShowUserNameInTitleMode)
    : DEFAULT_SHOW_USER_NAME_IN_TITLE_MODE;
}

export function setShowUserNameInTitleMode(mode: ShowUserNameInTitleMode): void {
  localStorage.setItem(SHOW_USER_NAME_IN_TITLE_KEY, mode);
}

export function useShowUserNameInTitle(): boolean {
  const [enabled, setEnabled] = useState(() => getShowUserNameInTitleMode() === "On");
  useEffect(() => {
    function handleStorage(event: StorageEvent) {
      if (event.key === SHOW_USER_NAME_IN_TITLE_KEY || event.key === null) {
        setEnabled(getShowUserNameInTitleMode() === "On");
      }
    }
    window.addEventListener("storage", handleStorage);
    return () => window.removeEventListener("storage", handleStorage);
  }, []);
  return enabled;
}

// UserInteractionPlan.md §4.2 (`D1.4-131`/`D1.4-132`/`D1.4-133`) — how
// strongly `TaskGrid`'s own non-editable cells (`components/DenseDataGrid.
// tsx`'s `notEditableCellPaletteSx`) are dimmed, requested as its own
// setting after two rounds of "this specific fixed amount is wrong" (first
// too strong, then still too strong). `"Low"` is today's own shipped amount
// (kept as the default, so this setting's own introduction changes nothing
// for anyone who hasn't touched it); `"Medium"` is the *background* amount
// from one round earlier (`D1.4-132`, before it was halved again at
// `D1.4-133`) — the font colour doesn't get its own level, since only the
// background was ever reported as wrong a second time; `"Max"` doubles
// `"Medium"`'s own background amount again, continuing the same halving/
// doubling ladder this value has already been through twice; `"None"`
// disables the whole affordance, every cell rendered identically regardless
// of whether it's actually editable. Same live-reactive shape as "Hints"
// above — a `TaskGrid` window's own cell styling is continuously visible,
// not a one-shot "at the moment this opens" concern.
const UNEDITABLE_DIMMING_KEY = "projectpal.settings.uneditableDimming";
export const UNEDITABLE_DIMMING_LEVELS = ["None", "Low", "Medium", "Max"] as const;
export type UneditableDimmingLevel = (typeof UNEDITABLE_DIMMING_LEVELS)[number];
const DEFAULT_UNEDITABLE_DIMMING_LEVEL: UneditableDimmingLevel = "Low";

export function getUneditableDimmingLevel(): UneditableDimmingLevel {
  const raw = localStorage.getItem(UNEDITABLE_DIMMING_KEY);
  return (UNEDITABLE_DIMMING_LEVELS as readonly string[]).includes(raw ?? "")
    ? (raw as UneditableDimmingLevel)
    : DEFAULT_UNEDITABLE_DIMMING_LEVEL;
}

export function setUneditableDimmingLevel(level: UneditableDimmingLevel): void {
  localStorage.setItem(UNEDITABLE_DIMMING_KEY, level);
}

export function useUneditableDimmingLevel(): UneditableDimmingLevel {
  const [level, setLevel] = useState<UneditableDimmingLevel>(() => getUneditableDimmingLevel());
  useEffect(() => {
    function handleStorage(event: StorageEvent) {
      if (event.key === UNEDITABLE_DIMMING_KEY || event.key === null) setLevel(getUneditableDimmingLevel());
    }
    window.addEventListener("storage", handleStorage);
    return () => window.removeEventListener("storage", handleStorage);
  }, []);
  return level;
}
