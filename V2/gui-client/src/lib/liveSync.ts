import type { QueryClient, QueryKey } from "@tanstack/react-query";

/**
 * Cross-window live data refresh (UserInterfaceWindows.md's D-Win-5): when
 * a save in one window invalidates a query, every other open window
 * invalidates the same query too, so it refetches and re-renders with the
 * fresh value — without needing to regain OS focus first (React Query's
 * own default `refetchOnWindowFocus` already covers that simpler case,
 * but not two windows genuinely visible side by side, which is the whole
 * point of this app's multi-window model).
 *
 * This does *not* cause a visible flash in the other windows: invalidating
 * a query only triggers a background refetch — React Query keeps the old
 * data on screen until the new data arrives, and React's own rendering
 * only patches the DOM nodes whose value actually changed (a grid cell's
 * text, a select's chosen option), not the whole page. There is nothing
 * to build for that part; it's the default behaviour of both libraries.
 *
 * One shared BroadcastChannel instance (not one per call), same reasoning
 * as windowNav.ts's focus channel: so a window's own broadcast doesn't
 * loop back into its own listener.
 */
const DATA_CHANGED_CHANNEL = "pp-data-changed";
let channel: BroadcastChannel | null = null;
function getChannel(): BroadcastChannel {
  if (!channel) channel = new BroadcastChannel(DATA_CHANGED_CHANNEL);
  return channel;
}

/**
 * Use this in a mutation's onSuccess in place of a bare
 * `queryClient.invalidateQueries({ queryKey })` — every call site in
 * api/hooks.ts does. Invalidates locally exactly as before, and also
 * tells every other open window to invalidate the same key.
 */
export function invalidateEverywhere(queryClient: QueryClient, queryKey: QueryKey): void {
  queryClient.invalidateQueries({ queryKey });
  getChannel().postMessage(queryKey);
}

/** Call once, at startup, in every window (main.tsx). */
export function startLiveSync(queryClient: QueryClient): void {
  getChannel().onmessage = (event) => {
    queryClient.invalidateQueries({ queryKey: event.data as QueryKey });
  };
}
