import { useEffect } from "react";

const ALIVE_KEY_PREFIX = "pp-window-alive:";
function isWindowAlive(windowName: string): boolean {
  return localStorage.getItem(ALIVE_KEY_PREFIX + windowName) === "1";
}
function markWindowAlive(windowName: string): void {
  localStorage.setItem(ALIVE_KEY_PREFIX + windowName, "1");
}
function markWindowGone(windowName: string): void {
  localStorage.removeItem(ALIVE_KEY_PREFIX + windowName);
}

/**
 * Call once, at startup, in *every* window (main.tsx) — a no-op in the
 * main app window itself, since `window.name` is only ever set at this
 * point on windows opened via openNamedWindow below (window.open's own
 * `name` argument). Registering immediately, before React even renders,
 * closes a race a popped-out window would otherwise have: without this,
 * a window wouldn't count as "alive" until its own page component's
 * useSingletonWindowIdentity effect below actually runs, which for a page
 * like TaskDetailPage is only after its data has loaded — a real gap a
 * fast second click could land in.
 *
 * This alone is *not* sufficient for the main app window, though: it can
 * navigate to a singleton-eligible route (e.g. clicking "Tasks" in the app
 * bar) via plain in-place client-side routing, which never touches
 * `window.name` — see useSingletonWindowIdentity below for the other half
 * of this.
 */
export function registerThisWindow(): void {
  const name = window.name;
  if (!name) return;
  markWindowAlive(name);
  window.addEventListener("pagehide", () => markWindowGone(name));
}

/**
 * Call from a singleton-eligible page's own component (TaskListPage.tsx:
 * `useSingletonWindowIdentity("tasks-list")`, TaskDetailPage.tsx:
 * `useSingletonWindowIdentity(\`tasks-${id}\`)`) — this is what makes the
 * registry correct for a window that reached this route by plain in-place
 * client-side navigation (e.g. the main app window's own "Tasks" nav
 * link), not just one `window.open()` created with the name already
 * attached. registerThisWindow above only ever looks at the *static*
 * `window.name` a window was born with; a window can navigate to a
 * singleton route at any point in its life without that ever changing, so
 * relying on birth-time naming alone leaves exactly that window invisible
 * to the registry — openNamedWindow would then (correctly, given what it
 * can see) conclude no such window exists and create a duplicate.
 *
 * On mount: claims this name (`window.name = name`, marks it alive) —
 * overwriting whatever was there before, since only one page at a time
 * can meaningfully own a given name. On unmount (route changed away) or
 * `pagehide` (window closed): releases it, clearing both the registry
 * entry and `window.name` — leaving a stale `window.name` behind would be
 * actively dangerous, not just untidy: a *later* window.open(realUrl,
 * thisName) elsewhere, for a window this registry correctly no longer
 * considers alive, would still find this window by the browser's own
 * native name matching and hijack it — navigating it away from whatever
 * it's actually showing.
 */
export function useSingletonWindowIdentity(name: string): void {
  useEffect(() => {
    window.name = name;
    markWindowAlive(name);
    function handlePageHide() {
      markWindowGone(name);
    }
    window.addEventListener("pagehide", handlePageHide);
    return () => {
      window.removeEventListener("pagehide", handlePageHide);
      markWindowGone(name);
      window.name = "";
    };
  }, [name]);
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
