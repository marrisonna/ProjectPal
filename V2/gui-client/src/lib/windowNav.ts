const ALIVE_KEY_PREFIX = "pp-window-alive:";
function isWindowAlive(windowName: string): boolean {
  return localStorage.getItem(ALIVE_KEY_PREFIX + windowName) === "1";
}

/**
 * Call once, at startup, in *every* window (main.tsx) — a no-op in the
 * main app window itself, since `window.name` is only ever set on windows
 * opened via openNamedWindow below (window.open's own `name` argument).
 *
 * Registers this window as "alive" under its own name in localStorage —
 * shared, live, and synchronously readable by every same-origin window
 * (unlike window.open, which requires actually opening something to find
 * out whether a name is taken — see openNamedWindow's comment for why
 * that matters).
 */
export function registerThisWindow(): void {
  const name = window.name;
  if (!name) return;
  localStorage.setItem(ALIVE_KEY_PREFIX + name, "1");
  window.addEventListener("pagehide", () => {
    localStorage.removeItem(ALIVE_KEY_PREFIX + name);
  });
}

/**
 * Opens (or refocuses) a browser window for `path`, named `windowName` —
 * V1.2's singleton-per-object re-focusing here (D1.4-8): a window already
 * open under this name is brought to the front instead of a duplicate
 * being created.
 *
 * Deliberately *not* `window.open(path, windowName, features)` for the
 * refocus case: that always re-navigates the target window even when
 * it's already showing `path` (a same-origin full navigation, not SPA
 * client routing, so refocusing an open window visibly reloads it —
 * blank, then redraw).
 *
 * A version that asked the *target* window to focus itself on receiving a
 * BroadcastChannel message was tried instead, and quietly did nothing:
 * `window.focus()` called from inside a message handler is an async,
 * script-initiated call with no direct user gesture in that window's own
 * context, and Chromium's anti-focus-stealing protection generally
 * ignores exactly that (a background window can't just decide to bring
 * itself forward on its own) — it's not the same as this window's own
 * user gesture, even though a person did just click something.
 *
 * So: check the (synchronous, no window.open() needed) localStorage
 * registry from registerThisWindow first. Already alive — a single
 * `window.open("", windowName)` call: this returns the existing window
 * without navigating/reloading it (an empty URL means "don't navigate"),
 * and because it's a direct, synchronous consequence of *this* window's
 * own click — not a message from elsewhere — the browser treats bringing
 * it to the front as legitimate, the same native behaviour a plain
 * `target="name"` link has always had. Not alive — a single
 * `window.open(path, ...)` call, with the real URL from the very start,
 * so Chromium correctly recognises it as in-scope for the installed PWA
 * and opens it as a standalone app window rather than a browser tab (this
 * broke, silently, in an earlier version that opened a blank window
 * first and navigated it afterwards — Chromium decides standalone-vs-tab
 * from the URL at creation time, not on a later script-driven
 * navigation). Deliberately never more than one window.open() call per
 * click either way: a version that probed for an existing window with a
 * blank-URL call and then opened a second, real one when needed hit
 * Chromium's popup blocker, which treats a *second* window.open() call in
 * the same click handler as an unrequested popup and silently blocks it.
 *
 * Known limitation: the localStorage flag is cleared on `pagehide`, which
 * fires reliably for every normal close (the window's own close button,
 * `window.close()`, navigating away) but not a hard crash/force-kill —
 * in that rare case the flag is stuck "alive" and `window.open("", name)`
 * creates a fresh, blank, wrongly-named window instead of finding nothing
 * (there's nothing left with that name to find). Not solved here — rare
 * enough, for a Level 1 Demonstrator, not to justify the complexity of
 * detecting and recovering from it.
 */
export function openNamedWindow(path: string, windowName: string, features?: string): void {
  if (isWindowAlive(windowName)) {
    const win = window.open("", windowName);
    win?.focus();
    return;
  }
  const win = window.open(window.location.origin + path, windowName, features);
  win?.focus();
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
