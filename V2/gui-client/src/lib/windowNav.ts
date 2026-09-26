import { useEffect, useLayoutEffect, useRef } from "react";
import { getNewWindowMode } from "./settings";

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
 * Call from a singleton-eligible page's own component (AllTaskPage.tsx:
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
// `name: null` (D1.4-110) is a genuine no-op — for a page component that's
// sometimes rendered as its own routed window and sometimes embedded
// inside a *different* page's own window (PlanPage.tsx's own `embedded`
// prop, embedded in AllTaskPage.tsx/ProjectDetailPage.tsx/
// ComponentDetailPage.tsx). The embedded case
// must not claim any window identity at all: doing so would overwrite the
// *outer* page's own already-claimed name (e.g. "tasks-list") the moment
// the embedded one mounted, breaking the outer window's own singleton
// behaviour. A hook can't be called conditionally (Rules of Hooks), so the
// no-op has to live inside this hook itself, letting every caller call it
// unconditionally either way.
export function useSingletonWindowIdentity(name: string | null): void {
  useEffect(() => {
    if (name == null) return;
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
 * D1.4-115 — remembers this window's own outer size separately per `view`
 * value (e.g. ProjectDetailPage.tsx's/ComponentDetailPage.tsx's own
 * "detail"/"gantt" toggle, D1.4-113), and tries to restore it with
 * `window.resizeTo` whenever `view` changes back to a value seen before.
 * Motivating case: a user grows the window while viewing the Gantt, then
 * flips back to the compact detail card, which is now left sitting inside
 * an oversized window — this puts it back to whatever size it was at
 * last time, and does the same for the Gantt side switching back the other
 * way, rather than a single "shrink to fit" applied on demand.
 *
 * The *first* time `view` ever becomes a given value, there's nothing
 * remembered for it yet, so no resize happens at all — the window is left
 * exactly as-is (no worse than not having this feature).
 *
 * `window.resizeTo` only works at all on a window opened by script
 * (`window.open`, which every window this hook is used in already is —
 * `openItemWindow`/`openNamedWindow` above) and is otherwise governed by
 * rules that vary by browser and can tighten mid-session (e.g. Chromium
 * revokes it once a second tab is opened in the same window) — there is no
 * reliable way to ask in advance whether a given call will actually do
 * anything. Deliberately not tested for up front: a blocked call is a
 * silent no-op with no visible sign it was ever attempted, so there is
 * nothing to gate on — no button, no feature-detection probe, just an
 * attempt every time `view` changes that either helps or does nothing.
 *
 * Capturing the *outgoing* view's size happens here, in this same effect,
 * rather than inside whatever click handler changed `view` — the window's
 * own `outerWidth`/`outerHeight` only change via an actual resize (native
 * or `resizeTo`), never merely because React re-rendered with new content,
 * so reading them here, right after `view` has changed but before this
 * value's own restore call below, still reflects the size the window
 * genuinely was at while the *previous* view was showing.
 */
export function useRememberedWindowSize<View extends string>(view: View): void {
  const sizesRef = useRef<Partial<Record<View, { width: number; height: number }>>>({});
  const previousViewRef = useRef(view);
  useLayoutEffect(() => {
    const previousView = previousViewRef.current;
    if (previousView === view) return;
    sizesRef.current[previousView] = { width: window.outerWidth, height: window.outerHeight };
    const remembered = sizesRef.current[view];
    if (remembered) window.resizeTo(remembered.width, remembered.height);
    previousViewRef.current = view;
  }, [view]);
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
  // D1.4-122 — the "New Window" setting (Settings.tsx) decides tab vs.
  // window here, at the one real choke point every caller already goes
  // through, rather than each `windowFeaturesFor` caller checking it itself.
  // A `features` string with sizing (`width=`/`height=`) is what makes a
  // browser open a genuine separate window rather than a new tab — passing
  // `undefined` instead (this app's own previous, and only, behaviour was to
  // always pass one) is what "Tab" actually means here. Read fresh on every
  // call, not cached anywhere — a change made in the Settings window takes
  // effect starting with the very next window opened, from any window.
  const effectiveFeatures = getNewWindowMode() === "Tab" ? undefined : features;
  const win = window.open(window.location.origin + path, windowName, effectiveFeatures);
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

// ProjectDetailPage.tsx's own card is a similar width to Task Detail's, but
// its embedded sub-Project/Task tree (ProjectDetailPlan.md §4.4) needs more
// vertical room to actually be useful than Task Detail's flat field layout
// does — same width, taller default height.
const PROJECT_DETAIL_WINDOW_FEATURES = "width=728,height=900";

// ComponentDetailPage.tsx's own card (ComponentDetailPlan.md) — same
// embedded-tree shape as Project Detail's, so the same sizing.
const COMPONENT_DETAIL_WINDOW_FEATURES = "width=728,height=900";

// SearchPage.tsx (SearchPlan.md §4.1) — wider than Project/Component
// Detail's own card (a results grid with several columns wants more
// horizontal room), shorter (a search screen wants less vertical room).
const SEARCH_WINDOW_FEATURES = "width=900,height=600";

// ManagePeoplePage.tsx/TeamManagementPage.tsx (ManagePeoplePlan.md §4.1/
// §5.1) — the same shape as Search's own window: a results/members grid
// wants horizontal room, neither needs much vertical room.
const PEOPLE_WINDOW_FEATURES = "width=900,height=600";
const TEAM_MANAGEMENT_WINDOW_FEATURES = "width=900,height=600";
// TeamsManagementPage.tsx (plural, D1.4-92) — the admin-facing list of every
// Team (create/rename/delete); a narrower, shorter card is enough for it,
// unlike the singular per-Team member grid above.
const TEAMS_MANAGEMENT_WINDOW_FEATURES = "width=420,height=600";
// AdminPage.tsx (D1.4-103) — two stacked panels (integrity check, export/
// import), each with a small grid at most — Teams Management's own
// narrower shape fits better than the wider People/Search one.
const ADMIN_WINDOW_FEATURES = "width=560,height=700";
// DashboardPage.tsx's own double-click-a-Resource-row action (D1.4-105) —
// a full All Tasks grid (many columns), so wider than Search/People's own
// windows above.
const TASKS_FOR_RESOURCE_WINDOW_FEATURES = "width=1100,height=700";
// SettingsPage.tsx (D1.4-122) — a single labelled dropdown, nothing else;
// smaller than every other window here. Still given real sizing features
// even though the "New Window" setting it itself controls might currently be
// "Tab" — `windowFeaturesFor` doesn't know or care what the setting is, that
// switch lives entirely in `openNamedWindow` above, applied uniformly to
// every caller including this one.
const SETTINGS_WINDOW_FEATURES = "width=380,height=300";

function windowFeaturesFor(entityType: string): string | undefined {
  if (entityType === "tasks") return TASK_DETAIL_WINDOW_FEATURES;
  if (entityType === "projects") return PROJECT_DETAIL_WINDOW_FEATURES;
  if (entityType === "components") return COMPONENT_DETAIL_WINDOW_FEATURES;
  if (entityType === "search") return SEARCH_WINDOW_FEATURES;
  if (entityType === "people") return PEOPLE_WINDOW_FEATURES;
  if (entityType === "team-management") return TEAM_MANAGEMENT_WINDOW_FEATURES;
  if (entityType === "teams-management") return TEAMS_MANAGEMENT_WINDOW_FEATURES;
  if (entityType === "settings") return SETTINGS_WINDOW_FEATURES;
  if (entityType === "admin") return ADMIN_WINDOW_FEATURES;
  return undefined;
}

/** One singleton window per (entityType, entityId) — see openNamedWindow. */
export function openItemWindow(entityType: string, entityId: string | number): void {
  openNamedWindow(`/${entityType}/${entityId}`, `${entityType}-${entityId}`, windowFeaturesFor(entityType));
}

/**
 * All Tasks filtered to one Resource (DashboardPage.tsx's own double-click
 * on a Resource row, D1.4-105) — a *separate* window per `resourceKey`
 * ("unassigned", or a Person id as a string), not the single "tasks-list"
 * singleton the plain All Tasks window shares. Opening this for several
 * different Resources from the Dashboard doesn't keep replacing the same
 * window, and the Dashboard's own window is left exactly as it was rather
 * than being navigated away from. AllTaskPage.tsx's own
 * `useSingletonWindowIdentity` call varies its own name the same way,
 * keyed off the same `?resource=` query param, so this and that stay in
 * sync about what "already open" means for a given Resource.
 */
export function openTasksForResource(resourceKey: string): void {
  openNamedWindow(
    `/tasks?resource=${encodeURIComponent(resourceKey)}`,
    `tasks-resource-${resourceKey}`,
    TASKS_FOR_RESOURCE_WINDOW_FEATURES,
  );
}

/** One singleton window for a whole list view (e.g. "tasks" -> "All Tasks"). */
export function openListWindow(entityType: string): void {
  openNamedWindow(`/${entityType}`, `${entityType}-list`, windowFeaturesFor(entityType));
}
