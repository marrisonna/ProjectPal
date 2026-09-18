import type { QueryClient, QueryKey } from "@tanstack/react-query";

/**
 * Cross-window live data refresh (UserInterfaceWindows.md's D-Win-5): when
 * a save in one window changes a record, every other open window sees the
 * fresh value too — without needing to regain OS focus first (React
 * Query's own default `refetchOnWindowFocus` already covers that simpler
 * case, but not two windows genuinely visible side by side, which is the
 * whole point of this app's multi-window model).
 *
 * Two propagation styles share one channel:
 *
 * - `invalidateEverywhere` (the original mechanism): broadcasts a bare
 *   query key. Every window — including this one — just re-fetches that
 *   key from the server. Simple and always correct, but a real network
 *   round trip per window per change; fine for structural changes (a
 *   record created/deleted, a list membership changed) where there's no
 *   single "new value" to hand over directly.
 * - `updateEverywhere` (D1.4-54): broadcasts the record's own fresh value
 *   — already sitting in memory as the mutation's own response, no extra
 *   cost to produce — so every window (including this one) can apply it
 *   straight into its cache via `setQueryData`, with no re-fetch at all.
 *   This is the fast path for "I just edited one field of one record and
 *   want every window showing that record to update immediately" (the
 *   TaskGrid-edit-reflected-in-an-open-Task-Detail-window case) — confirmed
 *   as the priority to optimise for: same-machine, multi-window
 *   responsiveness for one user, not fast propagation to other users on
 *   other machines (out of scope for this mechanism; would need a genuine
 *   server-push channel instead, since BroadcastChannel never crosses
 *   devices).
 *
 * Neither style causes a visible flash in the other windows: applying new
 * data (however it arrived) only replaces what a subscribed `useQuery`
 * already had — React's own rendering only patches the DOM nodes whose
 * value actually changed, not the whole page.
 *
 * One shared BroadcastChannel instance (not one per call), same reasoning
 * as windowNav.ts's focus channel: so a window's own broadcast doesn't loop
 * back into its own listener.
 */
const DATA_CHANGED_CHANNEL = "pp-data-changed";
let channel: BroadcastChannel | null = null;
function getChannel(): BroadcastChannel {
  if (!channel) channel = new BroadcastChannel(DATA_CHANGED_CHANNEL);
  return channel;
}

type LiveSyncMessage =
  | { kind: "invalidate"; queryKey: QueryKey }
  | {
      kind: "set-record";
      itemQueryKey: QueryKey;
      listQueryKey: QueryKey;
      idField: string;
      data: Record<string, unknown>;
    };

/**
 * Use this in a mutation's onSuccess in place of a bare
 * `queryClient.invalidateQueries({ queryKey })`. Invalidates locally
 * exactly as before, and also tells every other open window to invalidate
 * the same key — for structural changes (create/delete/assign/unassign)
 * where no single updated record can stand in for "what changed."
 */
export function invalidateEverywhere(queryClient: QueryClient, queryKey: QueryKey): void {
  queryClient.invalidateQueries({ queryKey });
  getChannel().postMessage({ kind: "invalidate", queryKey } satisfies LiveSyncMessage);
}

function applyRecordUpdate(
  queryClient: QueryClient,
  itemQueryKey: QueryKey,
  listQueryKey: QueryKey,
  idField: string,
  data: Record<string, unknown>,
): void {
  queryClient.setQueryData(itemQueryKey, data);
  // Only patches a list that's actually cached here (`old` undefined for a
  // window that's never fetched it) — never manufactures a one-row list
  // out of nothing, which would be the wrong shape for a query some other
  // component is relying on filtering/sorting itself.
  queryClient.setQueryData<Record<string, unknown>[]>(listQueryKey, (old) =>
    old ? old.map((item) => (item[idField] === data[idField] ? data : item)) : old,
  );
}

/**
 * The fast path (D1.4-54): `data` is the record's own fresh value — a
 * mutation's own response, not re-fetched — applied directly into both
 * the single-record cache entry (`itemQueryKey`, e.g. `["tasks", taskId]`)
 * and, in place, the matching row of the bulk list cache entry
 * (`listQueryKey`, e.g. `["tasks"]`), locally and in every other open
 * window, with no network round trip on the receiving side at all.
 */
export function updateEverywhere<T extends object>(
  queryClient: QueryClient,
  itemQueryKey: QueryKey,
  listQueryKey: QueryKey,
  idField: keyof T & string,
  data: T,
): void {
  // The declared record types (TaskRecord, ProjectRecord, ...) have no
  // index signature of their own — this cast is only for the generic,
  // by-field-name indexing applyRecordUpdate/the message envelope need,
  // not a claim that arbitrary string keys are actually meaningful on T.
  const record = data as Record<string, unknown>;
  applyRecordUpdate(queryClient, itemQueryKey, listQueryKey, idField, record);
  getChannel().postMessage({
    kind: "set-record",
    itemQueryKey,
    listQueryKey,
    idField,
    data: record,
  } satisfies LiveSyncMessage);
}

/** Call once, at startup, in every window (main.tsx). */
export function startLiveSync(queryClient: QueryClient): void {
  getChannel().onmessage = (event) => {
    const message = event.data as LiveSyncMessage;
    if (message.kind === "set-record") {
      applyRecordUpdate(
        queryClient,
        message.itemQueryKey,
        message.listQueryKey,
        message.idField,
        message.data,
      );
    } else {
      queryClient.invalidateQueries({ queryKey: message.queryKey });
    }
  };
}
