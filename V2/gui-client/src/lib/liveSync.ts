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
 * `updateEverywhere` alone still waits for the mutation's own PATCH to
 * resolve before anyone hears about the change — invisible in the window
 * that made the edit (DataGrid applies the new value to its own row
 * immediately, before `processRowUpdate`'s promise settles) but very
 * visible everywhere else, since D1.4-54's broadcast only fires from
 * `onSuccess`. `beginOptimisticUpdate` (D1.4-55) closes that gap: called
 * from a mutation's own `onMutate`, *before* the network request is even
 * sent, it broadcasts a *guessed* new record (the last-known value overlaid
 * with the fields being changed) immediately, so every window updates in
 * lockstep with the originating one regardless of how long the PATCH
 * itself actually takes on the wire — the whole point, given a real
 * network's PATCH latency is both slower and far less predictable than
 * anything seen against the local Docker-hosted API (`TaskGridPlan.md` §9,
 * item 4). The guess is then reconciled once the real response arrives:
 * `onSuccess` calls `updateEverywhere` again with the server's own
 * authoritative value (corrects the guess if the server computed or
 * normalised something), and `onError` calls it with the pre-edit snapshot
 * `beginOptimisticUpdate` returned, rolling the guess back out to every
 * window, not just the one that made the edit. For the — normally brief —
 * time between the guess and its confirmation, every open window shows a
 * value the server hasn't actually confirmed yet; if the mutation then
 * fails, every window briefly shows the wrong value before flipping back.
 * This is standard optimistic-UI behaviour (the same trade every
 * spreadsheet/Trello-style app makes), not a new class of risk, but is a
 * genuine behaviour change from before this existed, worth stating plainly.
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

/**
 * Call from a mutation's own `onMutate(variables)`, before its `mutationFn`
 * fires (D1.4-55). Cancels any in-flight fetch for the single-record query
 * first, so a refetch that was already on its way can't land after this and
 * clobber the guess with stale pre-edit data; reads whatever's currently
 * cached (checking the single-record entry, then falling back to the
 * matching row of the bulk list — TaskGrid's own edits usually only have
 * the row cached via the list, never having fetched the record on its own)
 * and, if found, immediately applies + broadcasts a guessed record (those
 * fields overlaid with `patch`) via `updateEverywhere`. Returns the
 * pre-edit snapshot so the caller's own `onError` can hand it straight back
 * to `updateEverywhere` to roll the guess back out to every window if the
 * mutation ultimately fails. No snapshot (nothing cached anywhere for this
 * id) means nothing to guess from or roll back to — the caller's `onSuccess`
 * still broadcasts the real value once it arrives, same as before this
 * existed, just without the head start.
 */
export async function beginOptimisticUpdate<T extends object>(
  queryClient: QueryClient,
  itemQueryKey: QueryKey,
  listQueryKey: QueryKey,
  idField: keyof T & string,
  id: unknown,
  patch: Record<string, unknown>,
): Promise<{ snapshot: T | undefined }> {
  await Promise.all([
    queryClient.cancelQueries({ queryKey: itemQueryKey }),
    queryClient.cancelQueries({ queryKey: listQueryKey }),
  ]);
  const current =
    queryClient.getQueryData<T>(itemQueryKey) ??
    queryClient
      .getQueryData<Record<string, unknown>[]>(listQueryKey)
      ?.find((item) => item[idField] === id) as T | undefined;
  if (current) {
    updateEverywhere(queryClient, itemQueryKey, listQueryKey, idField, {
      ...current,
      ...patch,
    } as T);
  }
  return { snapshot: current };
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
