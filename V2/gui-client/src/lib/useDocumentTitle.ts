import { useEffect } from "react";
import { useAuth } from "../auth/AuthContext";
import { usePeople, usePersonRoles } from "../api/hooks";
import { useShowUserNameInTitle } from "./settings";

// Whether this window is running as an installed PWA ("standalone"), not a
// plain browser tab — `display-mode: standalone` is the standard media-query
// form (Chromium/Edge/Firefox); `navigator.standalone` is Safari's own older,
// non-standard equivalent (iOS only, never set at all elsewhere, so this is
// harmless dead weight on every other browser). Read fresh on every call
// rather than cached in state — display mode can't realistically change
// mid-session, so there's nothing to react to, and `matchMedia` itself is
// cheap enough not to need memoising.
function isAppMode(): boolean {
  return (
    window.matchMedia?.("(display-mode: standalone)").matches === true ||
    (window.navigator as { standalone?: boolean }).standalone === true
  );
}

// Stage5 ("Show User name in window title") — the current Person's own
// nickname if they have one set on *any* Team (this is a window-title-wide
// concern, not scoped to one particular Team the way `lib/people.ts`'s own
// `personDisplayName` deliberately is), falling back to their plain
// `name`. Returns `null` before the reference data it needs has loaded, or
// once logged out entirely — `useDocumentTitle`'s own effect below treats
// that identically to the setting being "Off": no prefix, not a broken one.
function useCurrentUserDisplayName(): string | null {
  const { person } = useAuth();
  const { data: people } = usePeople();
  const { data: personRoles } = usePersonRoles();
  if (!person || !people) return null;
  const nicknameRole = personRoles?.find((pr) => pr.person_id === person.person_id && pr.nickname);
  if (nicknameRole?.nickname) return nicknameRole.nickname;
  return people.find((p) => p.person_id === person.person_id)?.name ?? null;
}

/**
 * Sets the current window's title. Each Task/Task List window opened via
 * windowNav.ts is a real, separate top-level OS window (not a browser tab),
 * so a distinct document.title per window is what Alt-Tab actually shows
 * for it — otherwise every window falls back to index.html's static
 * "ProjectPal" title.
 *
 * Stage5 — while running as an installed app (`isAppMode`), every window's
 * title gets a mandatory "ProjectPal - " prefix, and — while "Show User name
 * in window title" is "On" (default) and the current Person's own name is
 * known — the logged-in Person's own name/nickname between that and the
 * page's own title (e.g. "ProjectPal - Ruth - All Tasks"), primarily so
 * several app windows logged in as different People for testing are
 * distinguishable in the OS taskbar/Alt-Tab switcher, where they'd otherwise
 * look identical. A plain browser tab gets neither prefix — its own favicon
 * and the browser's own tab strip already identify it as ProjectPal, so
 * repeating that in the title text there would be redundant.
 */
export function useDocumentTitle(title: string): void {
  const showUserName = useShowUserNameInTitle();
  const userDisplayName = useCurrentUserDisplayName();
  useEffect(() => {
    if (!isAppMode()) {
      document.title = title;
      return;
    }
    const namePart = showUserName && userDisplayName ? `${userDisplayName} - ` : "";
    document.title = `ProjectPal - ${namePart}${title}`;
  }, [title, showUserName, userDisplayName]);
}
