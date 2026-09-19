# TaskGrid — Design and Implementation Plan

**Status: implemented (Level 1), pending user comparison/approval of the migration (§5.4).** Written before any code, per the user's request, following the same before-code-review pattern `ProjectDetailPlan.md` (this folder) already established; everything below reflects a design worked through and explicitly agreed turn-by-turn with the user, not a first draft — see `Plan.md` §10's `D1.4-48`–`D1.4-53` for the condensed decision log this document expands on. Built per §6's own plan: `lib/permissions.ts`'s `canEditTaskField`/`editableTaskStatusValues`, `api/hooks.ts`'s `useUpdateTaskField`/`useDeleteTask`, `features/tasks/TaskGrid.tsx`, `AllTaskPage.tsx` (now live at `/tasks`), `AllTaskOrigPage.tsx` (the renamed original, at `/tasks-orig` behind a temporary nav-bar button), and the matching `TaskDetailPage.tsx` field-permission/Delete-button changes — verified via headless CDP against real seeded data (three permission tiers, T-column visibility, delete-icon gating and its confirmation dialog, and a full inline-edit round-trip persisted through the real API and reverted). Only `DELETE /task/{task_id}` was already a no-op here — the server already had it built, so no server change was actually needed.

**Post-implementation fix:** manual testing surfaced a real gap in §4.9's own "the pre-existing D-Win-5 mechanism is the entire cross-window sync story" claim — true for the *query* layer, but `TaskDetailPage.tsx` keeps its own staged `form` state (D-Win-8) and, before this fix, only ever reset that state on first load of a given Task id, deliberately never on a later refetch, to avoid clobbering an in-progress unsaved edit. That guard also silently blocked a *good* refetch (another window's `TaskGrid` edit to the same Task) from ever reaching an already-open Task Detail window — confirmed one-directional exactly as reported: TaskGrid → open Task Detail didn't update; Task Detail Save → TaskGrid did. Fixed by tracking dirty fields individually (`dirtyFields: Set<string>`, not one whole-form boolean) and merging each refetch into every field the window hasn't itself started editing, leaving a genuinely in-progress edit alone. Verified via headless CDP: an untouched field now picks up an external `TaskGrid` edit live in an already-open Task Detail window with no reload, while a different field mid-edit (not yet saved) survives that same external refresh untouched.

**Post-implementation fix (editing UX):** further manual testing found three real usability problems in `TaskGrid`'s own inline editing, all traced to the same root cause — MUI's own defaults for `singleSelect` columns and `editMode="cell"`'s double-click-to-edit convention, not this design's own choices. (1) Editing a governed cell required a double-click, but that same double-click also fired the row-level double-click handler that opens Task Detail — both actions competed on one gesture. (2) In edit mode, `GridEditSingleSelectCell`'s dropdown renders through a themed Popper portalled directly onto `<body>`, entirely outside `TaskGrid`'s own DOM — so it falls back to the ambient MUI theme's default text size (16px/body1) instead of the grid's own dense font, and doesn't fit the narrow cell. (3) A newly selected value only committed (PATCHed, and so only then live-synced to an open Task Detail window) once the cell lost focus — selecting a value alone wasn't enough. Fixed together: a single click now starts edit mode on a governed, currently-editable cell (`apiRef.current.startCellEditMode`); every `singleSelect` governed column (Status, Priority, Owner, Task Type, Requestor) now uses a custom `DenseSingleSelectEditCell` — a plain native `<select>`, the same "native controls, sized explicitly" convention `DenseField.tsx` already established for this app (Q1.4-17) — which the browser always renders (trigger and dropdown alike) using the trigger element's own computed font, set here to the grid's own `DENSE_FONT_SIZE`, and which commits (`setEditCellValue` + `stopCellEditMode` together) the instant a value is chosen, not on a later blur. Verified via headless CDP: a single click enters edit mode without opening Task Detail; the edit `<select>`'s computed font-size matches the plain cell's; a selection commits to the server and live-syncs immediately with no click-away.

An initial version of this fix also made `onCellDoubleClick` skip opening Task Detail whenever the cell was editable, on the assumption that single click should now own editing entirely — this was an unrequested design addition (only single-click editing had been asked for) and made double-click inconsistent between editable and non-editable columns. Corrected immediately after the user reported it: single click and double-click are independent gestures with independent jobs — single click starts editing a governed, editable cell; double-click *unconditionally* opens Task Detail, exactly as it already does for every non-editable column, with no special-casing either way.

**D1.4-54 — cross-window live-sync latency.** The user reported a genuinely slow (~1.5s estimated) delay before an already-open Task Detail window reflected a `TaskGrid` edit to the same Task, and asked for a same-machine (not cross-user/cross-machine) fast-track that doesn't need a second trip to the database — explicitly scoped to "the local machine being responsive to the user," not fast propagation to other users on other machines. Diagnosis: `invalidateEverywhere` (the original `D-Win-5` mechanism) only ever broadcasts the *changed query key*, never the data — every receiving window, including ones on the same machine, then re-fetches that key from the server itself, a second full network round trip per window per change. `lib/liveSync.ts` gained a second mechanism, `updateEverywhere`, alongside it (not replacing it — structural changes such as create/delete/assign keep using `invalidateEverywhere` unchanged): it broadcasts the mutation's own response — the record's fresh value, already sitting in memory, no extra cost — and every window (including the one that made the edit) applies it directly via `queryClient.setQueryData`, both to the single-record cache entry (e.g. `["tasks", taskId]`, what `TaskDetailPage.tsx` reads) and, patched in place, to the matching row of the bulk list cache entry (e.g. `["tasks"]`, what every open `TaskGrid` reads) — no re-fetch anywhere on the receiving side. Wired into `useUpdateTask`, `useUpdateTaskField`, and `useUpdateProject`, the three "I already have the one changed record's fresh value" mutations.

Verified via headless CDP with `Network` domain tracing: after this change, an already-open Task Detail window makes **zero** network requests when a `TaskGrid` edit to its own Task lands elsewhere — confirming the fast path is genuinely engaged, not just faster by chance. However, timing the same scenario end-to-end still showed roughly the delay originally reported (1.5–2s), which further isolation (timing raw `fetch` calls directly, no React Query/DataGrid/BroadcastChannel involved at all) traced to a different, pre-existing cause: repeated raw `PATCH /task/{id}` calls against the real Docker-hosted `rest-api` varied wildly (22ms–1224ms across 5 successive calls), while raw `GET`s on the same endpoint were consistently fast (~20–30ms). `rest-api/app/db.py` already pools connections (`psycopg_pool.ConnectionPool`, `min_size=1, max_size=10`), ruling out the classic "a new DB connection per request" cause outright. **This is a separate, still-open problem from the one D1.4-54 set out to fix** — this document's own fix is confirmed working and correct on its own terms (the propagation step now costs nothing), but doesn't fully address the user's felt experience, since the dominant cost turned out to be the original mutation's own round trip, not the second one this fix removed. Flagged for a follow-up investigation (§9) rather than guessed at further here.

**D1.4-55 — optimistic broadcast, ahead of the PATCH.** Following straight on from D1.4-54: the user asked whether the fast track could run *before* the PATCH rather than after it, reasoning that a real (non-localhost) network's PATCH latency will be both slower and more variable than what D1.4-54's own investigation already found locally. Confirmed yes — this is the standard optimistic-update pattern, and it fully decouples propagation speed from PATCH duration, since the guess is broadcast the instant the edit is made, not once the server confirms it. `lib/liveSync.ts` gained `beginOptimisticUpdate`, called from each of the same three mutations' own `onMutate` (before `mutationFn` fires): it cancels any in-flight fetch for the affected queries, reads whatever's currently cached (the single-record entry, falling back to the matching row of the bulk list), and — if found — immediately applies + broadcasts a guessed record (the cached value overlaid with the fields being changed) via the existing `updateEverywhere`, returning a pre-edit snapshot. `onSuccess` then reconciles the guess with the server's own authoritative value (usually identical, but corrects it if the server computed or normalised something); `onError` broadcasts the snapshot back out, rolling the guess back everywhere, not just in the window that made the edit. The one deliberate trade-off, stated plainly to the user before building this: for the (normally brief) window between the guess and its confirmation, every open window shows a value the server hasn't actually confirmed yet, and if the mutation then fails, every window briefly shows the wrong value before flipping back — standard optimistic-UI behaviour (the same trade Trello/Sheets-style apps make), not a new class of risk, but a genuine change from before this existed.

Verified via headless CDP: (1) success path — a shared-wall-clock (`Date.now()`, not per-tab `performance.now()`, whose origin differs per page load) console-log trace showed the broadcast leaving the editing window and arriving in the already-open Task Detail window's own listener within 14–51ms across repeated runs, confirming the mechanism itself is genuinely fast; (2) failure/rollback path — using CDP's own `Fetch` domain to intercept and fail the `PATCH` at the browser level (a plain in-page `window.fetch` override was tried first and silently failed to intercept anything, since `openapi-fetch` calls `fetch(request, requestInitExt)` with an already-captured reference from module load, immune to a later reassignment of `window.fetch` — the `Fetch` domain intercepts below any page JS, so it can't be defeated the same way): the already-open Task Detail window showed the optimistic guess immediately, then rolled back to the original value once the simulated failure was reported, with the server itself confirmed untouched.

A methodological note worth recording: an initial round of "is this actually fast" testing measured *wall-clock time as observed by the CDP test harness's own polling loop* (repeatedly asking the browser "has the DOM updated yet" over the CDP WebSocket) and got misleadingly large, inconsistent figures (478ms–2249ms) — that number is dominated by the test harness's own round-trip overhead, not real in-page latency, and should not be mistaken for it. The reliable technique is a shared-clock, in-page console-log trace (as used above) or, better, the actual felt experience in a real browser session.

**Post-implementation fix (hover colour):** the user reported the hovered row's background turning flat grey, losing its urgency tint entirely. Measured live: MUI DataGrid's own default `.MuiDataGrid-row:hover` rule paints a solid `rgb(245, 245, 245)` straight over whatever the row's own background was. Fixed by giving each urgency bucket's own CSS rule a `:hover` variant blending 50/50 with that measured grey instead of replacing it outright (a row's own urgency class already carries enough extra specificity over DataGrid's plain `.MuiDataGrid-row:hover` to win with no `!important`) — a hovered row now reads as "the same row, highlighted," not "a different, flat-grey row." Verified live: `rgb(255, 128, 128)` hovers to exactly `rgb(250, 187, 187)`, the measured 50/50 blend.

**Post-implementation fix (selection background):** separately, the user reported the *selected* row also turning light grey, and asked that only the focused cell's own border change, never the row's background. Traced to DataGrid's default click-to-select-row behaviour (a separate concept from cell focus) painting its own flat `Mui-selected` background — `TaskGrid` has no use for row selection at all (no checkbox column, no bulk row actions), so `disableRowSelectionOnClick` turns it off outright, leaving cell focus (and its border) untouched. Verified live: a row's background stays fixed at its urgency colour before and after clicking a cell, the row never gains the `Mui-selected` class, and the clicked cell still shows its own solid focus border.

**D1.4-56 — V1.2's own right-click grid menu (Reset All Filters / Copy All / Show Filter).** The user asked for V1.2's three-item right-click context menu, pointing at its own `GridControl` source for the exact behaviour rather than a guess. Read directly from `V1.2/Libs/CustomGUIControls/CustomGUIControls/Grid/GridControl.cs`/`.Designer.cs`:
- The menu (`contextMenuStrip1`) is attached to the *entire* `dataGridView`, not just its filter row — right-clicking anywhere on the grid opens it. Reproduced the same way: `onContextMenu` on `TaskGrid`'s own outer container, using the standard MUI "anchor a `Menu` at the click position" recipe.
- **Reset All Filters** (`resetAllFiltersToolStripMenuItem_Click`): clears every column filter's own value and re-applies. Maps directly onto `setFilterState({})`.
- **Copy All** (`selectAllToolStripMenuItem_Click`, oddly named in the V1.2 source — its own label is "Copy All," not "Select All"): builds a tab-separated, newline-separated copy of every *currently visible* row and column (header row of column names first), using each column's own **formatted display text**, not the raw value — explicitly excluding V1.2's own hidden "column zero" (the underlying-object reference column) — and puts it on the OS clipboard. Reproduced using `apiRef.current.getSortedRowIds()` (respects the current sort, over our own already-filtered `rows`) and `apiRef.current.getCellParams(id, field).formattedValue` (the same "what's actually shown," not `owner_person_id`'s raw numeric value) for every column except the `__delete` actions column (`TaskGrid`'s own equivalent of V1.2's hidden column zero), written via `navigator.clipboard.writeText`.
- **Show Filter** (`showFilterToolStripMenuItem_Click`): a plain visibility toggle (`m_filterIsVisible`) for the filter row itself — reading `ApplyFiltersToRows` directly confirmed this is **independent of whether filters are actually applied**: V1.2 iterates every filter's current value regardless of `m_filterIsVisible`, so hiding the boxes never silently drops whatever was already typed into them. This is a genuine, previously-unnoticed gap from this design's own original behaviour: `TaskGrid`'s `showFilters` prop was, until now, an all-or-nothing switch that also disabled filtering entirely when `false`. Fixed to match V1.2: `showFilters` is now only the prop's *initial* value; an internal `filterVisible` state (toggled by this new menu item, with a check-mark reflecting its current value, exactly like V1.2's own `Checked` property) controls only whether the filter row's own controls render, while `filterState` — and so `passesAllFilters` — is now evaluated unconditionally, matching `ApplyFiltersToRows`'s own independence exactly.

Verified via headless CDP against real seeded data: the menu shows exactly these three items, in this order; toggling "Show Filter" off hides the filter row's own controls without changing which rows are visible (a previously-applied Status filter stays in effect), and its own check-mark reflects the current state correctly both ways; "Reset All Filters" clearing the default Status filter reveals previously-hidden Closed/Cancelled Tasks; "Copy All" (clipboard permissions granted via `Browser.grantPermissions` for the headless test) produces a tab-separated payload with a correct header row, one line per visible row, and real display text (person names, formatted dates, "InProgress"-style status text) rather than raw ids or `undefined`.

## Contents

1. [Purpose and Scope](#purpose-and-scope)
2. [Requirements](#requirements)
   - 2.1 [What All Tasks Does Today](#what-all-tasks-does-today)
   - 2.2 [What's New](#whats-new)
   - 2.3 [The Governing Principle: Window vs. User Behaviour](#governing-principle)
3. [What Already Exists to Build On](#what-already-exists)
   - 3.1 [Server API](#server-api)
   - 3.2 [Reusable Client-Side Pieces](#reusable-client-side-pieces)
   - 3.3 [Cross-Window Live Sync](#cross-window-live-sync)
   - 3.4 [Gaps to Fill](#gaps-to-fill)
4. [Design](#design)
   - 4.1 [Component API](#component-api)
   - 4.2 [Column Catalog](#column-catalog)
   - 4.3 [The Central Field-Permission Rule](#central-field-permission-rule)
   - 4.4 [Editable Columns — Level 1](#editable-columns)
   - 4.5 [Visual Treatment of Non-Editable Cells](#visual-treatment)
   - 4.6 [Filtering](#filtering)
   - 4.7 [Sorting](#sorting)
   - 4.8 [Opening a Task](#opening-a-task)
   - 4.9 [Saving an Edit and Cross-Window Sync](#saving-an-edit)
   - 4.10 [Deleting a Task](#deleting-a-task)
   - 4.11 [Right-Click Menu](#right-click-menu)
5. [Migrating All Tasks](#migrating-all-tasks)
   - 5.1 [AllTaskOrigPage (Renamed, Unchanged)](#all-task-orig-page)
   - 5.2 [AllTaskPage (New, `TaskGrid`-Based)](#all-task-page)
   - 5.3 [Routing and the Comparison Period](#routing-and-comparison)
   - 5.4 [Cleanup Once Approved](#cleanup-once-approved)
6. [Implementation Plan](#implementation-plan)
   - 6.1 [New/Changed Permission Functions](#new-changed-permission-functions)
   - 6.2 [New API Client Hook](#new-api-client-hook)
   - 6.3 [New Components](#new-components)
   - 6.4 [Build Steps](#build-steps)
7. [Deferred to Level 2](#deferred-to-level-2)
8. [Testing Approach](#testing-approach)
9. [Open Items for Review](#open-items-for-review)

<a id="purpose-and-scope"></a>
## 1. Purpose and Scope

This document designs and plans **`TaskGrid`**, a reusable grid component for showing (and, for some fields, directly editing) Tasks — factored out of the current All Tasks screen so that other windows needing a Task list (Project Detail's own embedded list is the first candidate, once it wants a grid rather than its current tree) can reuse the identical look, feel, and editing behaviour rather than rebuilding it.

It also covers migrating All Tasks itself onto `TaskGrid`, via a temporary side-by-side comparison: the current implementation is renamed to `AllTaskOrigPage` (kept, unchanged, on its own route) while a new `AllTaskPage` takes over `/tasks` using `TaskGrid`; `AllTaskOrigPage` is deleted once the user is satisfied the two look and behave identically.

In scope, beyond `TaskGrid` itself: two real REST/Task-Detail changes this design deliberately makes for cross-window consistency and to close real gaps found along the way (§4.3, §4.10) — a new `DELETE /task/{task_id}` endpoint (Task deletion doesn't exist anywhere in V2 yet), and Task Detail's own field-permission rules (Owner, and now Status/Detailed Description for a non-owning assigned Resource) switching to the same central function `TaskGrid` uses. Not in scope: any change to the *shape* of Task Detail's own layout (fields move nowhere, no new fields appear there) beyond a new Delete button (§4.10) and those permission-rule changes.

<a id="requirements"></a>
## 2. Requirements

<a id="what-all-tasks-does-today"></a>
### 2.1 What All Tasks Does Today

Read directly from `features/tasks/TaskListPage.tsx` (about to become `AllTaskOrigPage.tsx`, §5.1, unchanged in every respect below):

- A `@mui/x-data-grid` grid, one row per Task, restricted to Tasks on a Team the caller belongs to (`myTeamIds`, client-side — `GET /task` itself has no Team restriction).
- 22 columns, in a fixed order (§4.2 lists them all): a mix of raw fields (Description, Priority, Effort, …), resolved-name fields (Owner/Requestor/Component/Project, resolved from id to display name), and computed fields (Urgency, Planned Start, End Date, Resources, Attachments/Remarks counts) built from `buildScheduleGraph`/`getTaskSchedule`/`computeUrgency` and bulk `useAll*` queries.
- A **"T" (tentative_resource_assignment) column**, shown only if the signed-in Person is a `TeamLeadUser` on *some* Team (`isTeamLeadOfAnyTeam`) — otherwise omitted entirely.
- **Per-column filtering** (`D-Win-14`, `components/GridColumnFilter.tsx`): a text-contains box plus an exact-match checklist popover under every column header, combined with AND across columns. Two default filters: Status starts excluding Closed/Cancelled; Resources starts restricted to the signed-in Person's own name, unless they're a Team Lead on some Team, in which case it starts unfiltered (`D-Win-15`/`16`).
- **Sorting**: click a header to sort ascending/descending (no third "unsorted" click, `D-Win-16`'s own follow-up); default sort is Urgency descending.
- **Row colour**: the whole row shaded by Urgency, exactly as `computeTaskRowColour` defines it (white → light red, or flat grey for no-Priority/Cancelled/Closed) — identical to Task Detail's own Urgency badge and the Gantt view's own Task bars.
- **Double-click a row** opens that Task's own Task Detail window (`openItemWindow`, singleton-per-Task).
- Dense, WinForms-like row/header sizing (`DENSE_ROW_HEIGHT`, `HEADER_HEIGHT`), no native DataGrid column menu/filter (both switched off — this screen's own custom filtering replaces them), 100-row pagination (the free DataGrid tier's own cap — not true unlimited scrolling).

Nothing here changes for the end user once `AllTaskPage` replaces it (§5) — this section is `TaskGrid`'s own literal starting spec, not a proposal.

<a id="whats-new"></a>
### 2.2 What's New

- **Extracted into a reusable component**, configurable per embedding window for *what it can show*: which columns (a subset/reorder of the same fixed catalog, §4.2), and whether the filter row is shown at all.
- **Inline editing** on 8 of the 22 columns (§4.4), gated per-cell by a new, centralised permission rule (§4.3) — not a per-window setting.
- Reachable at `/tasks` once migrated (§5), alongside a temporary `AllTaskOrigPage` for direct comparison.

<a id="governing-principle"></a>
### 2.3 The Governing Principle: Window vs. User Behaviour

Settled explicitly with the user before any design detail below, and the organising idea for everything that follows: **a window configures what it offers to show; it never decides what a given user is allowed to do with it.** Concretely:

- **Window-level ("what can be seen here"):** which columns this particular embedding of `TaskGrid` offers as candidates, and whether the filter row is shown at all. Two different windows may legitimately show different columns.
- **User-level ("what this person may do"), never window-specific:** whether the "T" column has any content worth showing (a Team Lead sees it; nobody else does, in *any* window that offers it as a candidate column), and whether any given cell is actually editable. These are decided by one central, shared rule (§4.3), called directly by `TaskGrid` itself — never something an embedding window can override or reimplement differently.

A practical consequence: a window's own `columns` prop lists **candidates**, not guarantees — `TaskGrid` still applies the user-level rule on top before deciding what actually renders or accepts an edit.

<a id="what-already-exists"></a>
## 3. What Already Exists to Build On

<a id="server-api"></a>
### 3.1 Server API

No server changes needed for editing. `PATCH /task/{task_id}` (`UpdateTaskRequest`, all fields optional) already accepts every field this document makes editable (§4.4); the existing `require_owner_or_team_lead` server-side check already enforces the general "owner-or-TeamLeadUser" rule that backs most of §4.3's own client-side rule (the server has no field-specific carve-out for `owner_person_id`/`tentative_resource_assignment` today, and no notion of §4.3's own tier 3 at all — see §9's own open item on this).

One server change *is* needed for deletion (§4.10, `D1.4-53`): no `DELETE /task/{task_id}` exists today (`schema.d.ts` confirms `/task/{task_id}` has no `delete` operation) — needed now that Task deletion is in scope for Level 1.

<a id="reusable-client-side-pieces"></a>
### 3.2 Reusable Client-Side Pieces

Reused **unchanged**:

- `components/GridColumnFilter.tsx` — the entire per-column filter mechanism (`FilterableHeader`, `columnFilterPasses`, `isColumnFilterActive`, `sortFilterOptions`) is already generic over a column's own value-getter; nothing here is Task-specific.
- `lib/schedule.ts`'s `buildScheduleGraph`/`getTaskSchedule`/`computeUrgency`/`computeUrgencyColour`/`computeTaskRowColour`/`formatDdMmmYy`.
- `lib/people.ts`'s `personDisplayName`.
- `lib/permissions.ts`'s `isTeamLeadOfAnyTeam` (the "T" column's own visibility rule, already written exactly this way today) and `hasRoleAtLeast`/`canEditOwnedRecord` (§4.3 builds directly on these, not around them).
- `lib/windowNav.ts`'s `openItemWindow("tasks", id)`.
- `DENSE_FONT_SIZE`/`DENSE_ROW_HEIGHT`/`HEADER_HEIGHT`-equivalent sizing constants already in `TaskListPage.tsx` — moving with the code that owns them into `TaskGrid.tsx`.

<a id="cross-window-live-sync"></a>
### 3.3 Cross-Window Live Sync

Read directly from `lib/liveSync.ts` (`D-Win-5`) to confirm before designing around it: every mutation hook in `api/hooks.ts` calls `invalidateEverywhere(queryClient, key)` rather than a bare `invalidateQueries` — this invalidates the local cache *and* posts the same query key over a shared `BroadcastChannel` every other open window is listening on (`startLiveSync`, wired up once per window in `main.tsx`), so any window with an active query on that key refetches and re-renders, with no polling and no manual refresh step.

This already delivers exactly the behaviour the user asked to confirm, in both directions, with **no new sync mechanism required**:

- **A `TaskGrid` edit updates an open Task Detail window for the same Task.** `useTask(id)` (Task Detail's own query) is keyed `["tasks", taskId]`. As long as `TaskGrid`'s own edit mutation invalidates that same key (§6.2), any open Task Detail window for that Task refetches immediately.
- **Task Detail's Save updates every open `TaskGrid`.** `useUpdateTask` (Task Detail's existing Save) already invalidates the bare `["tasks"]` key today. `TaskGrid` owns no query of its own — it's handed a `tasks` prop (§4.1) — so this is really "the *embedding window's* `useTasks()` refetches and passes fresh data down," which already happens today for the current All Tasks screen and needs no new code at all.

<a id="gaps-to-fill"></a>
### 3.4 Gaps to Fill

- No field-level permission function exists yet — only the coarser, whole-record `canEditOwnedRecord`. §4.3/§6.1.
- No mutation hook exists that can update one field of an arbitrary, dynamically-chosen Task — `useUpdateTask(taskId)` is bound to one fixed id at the point it's called, which doesn't fit a grid where the edited row varies per cell edit. §6.2.
- No route/window exists yet for a side-by-side comparison page. §5.3.
- No way to delete a Task exists at all, anywhere in V2 — no server endpoint, no client hook, no button on any screen. §4.10/§6.1/§6.2.

<a id="design"></a>
## 4. Design

<a id="component-api"></a>
### 4.1 Component API

`TaskGrid` is a controlled, data-in component — it fetches nothing itself; every embedding window supplies the data it needs (matching the user's own framing: "each TaskGrid will be given a collection of tasks that it needs to render").

```ts
interface TaskGridProps {
  tasks: TaskRecord[];
  // Reference data TaskGrid's own column value-getters/editors need —
  // the same queries AllTaskOrigPage already fetches today, just handed
  // down instead of fetched internally.
  projects: ProjectRecord[];
  components: ComponentRecord[];
  people: PersonRecord[];
  personRoles: PersonRoleRecord[];
  scheduleGraph: ScheduleGraph;       // buildScheduleGraph(...), built by the caller
  resourceIdsByTask: Map<number, number[]>;
  attachmentsCountByTask: Map<number, number>;
  remarksCountByTask: Map<number, number>;

  // Window-level configuration (§2.3) — "what can be seen here."
  columns?: TaskGridColumnKey[];      // defaults to the full catalog, §4.2, in catalog order
  showFilters?: boolean;              // default true
  initialFilterState?: Record<string, ColumnFilterState>;
  defaultSort?: { field: string; sort: "asc" | "desc" }; // default Urgency desc, matching today

  onRowDoubleClick?: (task: TaskRecord) => void; // default: openItemWindow("tasks", task.task_id)
}
```

Deliberately **absent**: any prop expressing "is this cell editable" or "is the T column visible" — those are user-level, centrally decided (§2.3), and `TaskGrid` calls the shared functions itself (§4.3) rather than accepting an override that could let two windows disagree.

<a id="column-catalog"></a>
### 4.2 Column Catalog

The fixed catalog `columns` chooses from — identical set, identical order, identical value-getters to today's All Tasks (§2.1), just named as stable keys an embedding window's `columns` prop can reference:

`task_id`, `urgency`, `resources`, `status`, `tentative_resource_assignment`, `description`, `component_id`, `project_id`, `priority`, `end_date`, `start_date`, `attachments`, `remarks`, `owner_person_id`, `requestor_person_id`, `date_added`, `effort_in_days`, `effort_type`, `percentage_allocation`, `task_type`, `status_date`, `external_reference_url`, `detailed_description`.

`AllTaskPage`'s own `columns` prop lists every one of these, in this order, matching §2.1's baseline exactly (including `tentative_resource_assignment` unconditionally — its actual visibility is still decided centrally, §2.3).

<a id="central-field-permission-rule"></a>
### 4.3 The Central Field-Permission Rule

New, in `lib/permissions.ts` — alongside `canEditOwnedRecord`, not replacing it (that function still governs whole-record edit access, e.g. Task Detail's own Save button, and Project Detail's, unchanged). Three tiers, not two — `D1.4-52` folded in `V1.2`'s own third tier (`UserInterfaceWindows.md` §3.20's `GUITask.IsReadOnly`) after the user confirmed it's correct behaviour V2 needs too, not an old-app quirk to leave behind:

```ts
export type EditableTaskField =
  | "status"
  | "tentative_resource_assignment"
  | "priority"
  | "owner_person_id"
  | "effort_in_days"
  | "percentage_allocation"
  | "task_type"
  | "requestor_person_id"
  | "detailed_description";

// Fields a Task's own assigned Resource may edit even when they don't own
// it and aren't a Team Lead — V1.2's own third tier (GUITask.cs's
// NormalUserEditableColumns), confirmed as wanted behaviour, not legacy
// cruft (D1.4-52).
const RESOURCE_EDITABLE_FIELDS: readonly EditableTaskField[] = ["status", "detailed_description"];

export function canEditTaskField(
  person: WhoAmI | null,
  teamId: number | null | undefined,
  ownerPersonId: number | null | undefined,
  isAssignedResource: boolean,
  field: EditableTaskField,
): boolean {
  // Owner reassignment, and toggling Tentative Resource Assignment, are
  // both reserved for LeadUser+ specifically, not just "the record's own
  // owner" — matching V1.2's own real precedent exactly, not an
  // independently-invented rule (D1.4-51: V1.2's `GUITaskColumns.
  // ColumnIsReadOnly` restricts both of these columns identically, to
  // SuperUser/PowerUser only — V2's TeamLeadUser/LeadUser).
  if (field === "owner_person_id" || field === "tentative_resource_assignment") {
    return hasRoleAtLeast(person, teamId, "LeadUser");
  }
  if (canEditOwnedRecord(person, teamId, ownerPersonId)) return true;
  // Tier 3 (D1.4-52): not the owner, not a Team Lead, but assigned to the
  // Task as a Resource — Status and Detailed Description only.
  return isAssignedResource && RESOURCE_EDITABLE_FIELDS.includes(field);
}

// V1.2's own further restriction on top of the above (GUITaskColumns.
// AdjustComboEditor, D1.4-52): tier 3's own Status edit can move a Task
// along, but can't close it out — Closed/Cancelled stay off the menu
// unless the editor has full (owner-or-TeamLeadUser) rights.
export function editableTaskStatusValues(
  person: WhoAmI | null,
  teamId: number | null | undefined,
  ownerPersonId: number | null | undefined,
): readonly string[] {
  if (canEditOwnedRecord(person, teamId, ownerPersonId)) return TASK_STATUSES;
  return TASK_STATUSES.filter((s) => s !== "Closed" && s !== "Cancelled");
}
```

`isAssignedResource` is supplied by the caller (`TaskGrid` already has `resourceIdsByTask`, §4.1; Task Detail already has its own staged `resourceIds`) rather than looked up inside `lib/permissions.ts` itself, keeping that module free of any dependency on how resource assignment data happens to be shaped or fetched — the same reason `canEditOwnedRecord` takes a raw `ownerPersonId` rather than a whole `TaskRecord`.

**Used identically everywhere a governed field is editable — not just `TaskGrid`.** Per the user's own explicit instruction ("the same permission module should be used throughout, not just for the TaskGrid"), `TaskDetailPage.tsx`'s own Owner field switches from the blanket `disabled={!canEdit}` to `disabled={!canEditTaskField(...)}` — a deliberate, explicitly-approved behaviour change to already-shipped Task Detail, in both directions: today, a Task's own Normal-User owner can reassign its Owner there (after this change, they can't); and today, a non-owning assigned Resource can't touch Status or Detailed Description there at all (after this change, they can, values-restricted per `editableTaskStatusValues` for Status). Every governed field on Task Detail routes through `canEditTaskField(..., field)`/`editableTaskStatusValues`, not the blanket `canEdit`, so there's exactly one place any of these rules is expressed. Task Detail's *other* fields (Description, Component, Project, the two editable dates) have no `TaskGrid` equivalent and keep using the plain `canEdit` (`canEditOwnedRecord`) exactly as today.

The "T" column's own **visibility** (as opposed to editability) stays exactly as it already is — `isTeamLeadOfAnyTeam(person)`, called by `TaskGrid` itself, no new function needed. Its *editability*, once visible, follows the same LeadUser+ rule as Owner (above), not the general rule every other governed column uses.

<a id="editable-columns"></a>
### 4.4 Editable Columns — Level 1

Of the 22 catalog columns, 9 are ever editable in a cell, each behind `canEditTaskField` — up from the 8 first proposed: **Detailed Description joins the list** (`D1.4-52`), since it's one of V1.2's own two tier-3 (Resource) fields and there's no reason to make it editable for a Resource but not for the Task's own owner, who already had full rights to it in V1.2 too.

| Column | Editor | Notes |
|---|---|---|
| Status | `singleSelect`, `editableTaskStatusValues(...)` (not the full `TASK_STATUSES` unconditionally) | Owner/TeamLeadUser: any value. Assigned-Resource-only (tier 3): every value except Closed/Cancelled — matches V1.2 exactly, `D1.4-52`. |
| T (Tentative Resource Assignment) | boolean checkbox | New capability — read-only until now. LeadUser+ only, above (matching V1.2's own precedent exactly, `D1.4-51`). |
| Priority | `singleSelect`, `PRIORITY_LEVELS` (+ "(none)") | |
| Owner | `singleSelect`, Team-scoped candidate list (same restriction as Task Detail's own Owner field, `D1.4-22`) | LeadUser+ only, above. |
| Effort | number | |
| % Allocation | number, stored as a fraction/displayed as a whole percentage | Same transform Task Detail's own field already does. |
| Task Type | `singleSelect`, `TASK_TYPES` | |
| Requestor | `singleSelect`, org-wide active People (not Team-scoped, matching Task Detail's own Requestor, `D1.4-15`) | |
| Detailed Description | multi-line text | New, `D1.4-52`. Owner/TeamLeadUser or an assigned Resource (tier 3). A long value editing in a single grid cell is an implementation detail to work out (a taller row, or a bigger popup editor) — not a design blocker. |

**Effort Type is deliberately excluded, on purpose, not an oversight** — confirmed explicitly: changing it can have large knock-on effects (it changes what the Effort number even *means*), so it stays a Task-Detail-only edit, to avoid an accidental one-cell change with outsized consequences. Every other column not listed above (computed fields, counts, system-set dates, free text not in this list) is never editable from the grid, Level 1 or otherwise, without a further explicit decision.

<a id="visual-treatment"></a>
### 4.5 Visual Treatment of Non-Editable Cells

Confirmed: a cell the current user isn't permitted to edit should look that way *before* they try to edit it, not just silently refuse the attempt — the same principle Task Detail's own read-only fields already follow (`DenseField.tsx`'s `READONLY_BG`, `rgba(0,0,0,0.06)`). `TaskGrid` applies a `cellClassName` per governed column, computed per-row from `canEditTaskField`, giving a non-editable cell that same muted background — reusing the identical colour, not inventing a second "this is read-only" visual language.

<a id="filtering"></a>
### 4.6 Filtering

`showFilters` (default `true`) is only the row's *initial* visibility (D1.4-56) — the user can toggle it themselves at runtime via the right-click menu's "Show Filter" item (§4.10 area, below), reproducing V1.2's own real `GridControl` behaviour exactly. Filter *mechanics* (text-contains + exact-match checklist) are unchanged, reusing `GridColumnFilter.tsx` exactly as today, and — matching V1.2's own `ApplyFiltersToRows`, confirmed independent of `m_filterIsVisible` — hiding the filter row's own controls never drops whatever filter values were already set: `filterState` keeps being evaluated regardless of whether the row showing it is currently visible.

**Default filter values are also window-level configuration, supplied via `initialFilterState`, not centralised policy** — this is a real, deliberate distinction from §4.3's field-permission rule, worth stating plainly: "which Tasks does this window start filtered to" (today's All-Tasks-specific choice of hiding Closed/Cancelled by default, and restricting Resources to the signed-in Person unless they're a Team Lead, `D-Win-15`/`16`) is an interaction-design choice belonging to the *screen*, not a rule about what any user is allowed to do — a different window embedding `TaskGrid` with different starting filters isn't inconsistent in the way two windows disagreeing about editability would be. `AllTaskPage` computes this exact same initial state itself (unchanged logic, just relocated) and passes it in.

<a id="sorting"></a>
### 4.7 Sorting

Unchanged: click-header ascending/descending toggle (no third "unsorted" state), `defaultSort` defaulting to Urgency descending.

<a id="opening-a-task"></a>
### 4.8 Opening a Task

Double-click a row to open its Task Detail window, exactly as today (`onRowDoubleClick`, defaulting to `openItemWindow("tasks", task.task_id)` — overridable per §4.1, though `AllTaskPage` itself uses the default).

<a id="saving-an-edit"></a>
### 4.9 Saving an Edit and Cross-Window Sync

Confirmed: **immediate per-cell save for Level 1** (§7 records the Level 2 follow-up). Mechanically, via DataGrid's own `processRowUpdate(newRow, oldRow)`: on committing a cell edit (Enter/Tab/click-away), `TaskGrid` diffs the one changed field and calls the new `useUpdateTaskField()` mutation (§6.2) with just that field. A successful response resolves `processRowUpdate` with the updated row (DataGrid then shows it); a failure rejects it (DataGrid reverts the cell to its prior value) and surfaces the server's own message (`formatApiError`) in a dismissible `Snackbar`/`Alert` at the grid's own level — there's no per-row place to show an inline error the way Task Detail's own single-record Alert works, so a transient toast is the closest equivalent.

`useUpdateTaskField()`'s own mutation invalidates the identical `["tasks", taskId]` / `["tasks"]` keys `useUpdateTask` already does (§6.2) — this, plus the pre-existing `D-Win-5` mechanism (§3.3), is the entire cross-window sync story; nothing else is needed.

<a id="deleting-a-task"></a>
### 4.10 Deleting a Task

New for Level 1 (`D1.4-53`) — V1.2 has always supported this (`UserInterfaceWindows.md` §3.20) and V2 currently has no way to delete a Task at all, anywhere, including no server endpoint. Confirmed explicitly: needed now, with the *same permissions* V1.2 uses.

- **Permission**: `canEditOwnedRecord(person, teamId, task.owner_person_id)` — reading V1.2's own rule directly (`Utils.Permissions.IsAllowed(Owner, Task, Delete)`: the Task's own owner, or a SuperUser, and never a mere PowerUser over someone else's Task) shows it's already exactly `canEditOwnedRecord`'s own existing shape (owner-with-NormalUser+, or TeamLeadUser) — no new, delete-specific permission function needed, and no tier-3 Resource carve-out for deleting (§4.3's tier 3 is edit-only, matching V1.2, which never lets a non-owning Resource delete anything).
- **Where it appears**: both `TaskGrid` (a trash icon per row, gated on the same permission check, matching V1.2's own grid-level delete affordance exactly) and `TaskDetailPage.tsx`'s own header (a **Delete** button, matching the pattern `ProjectDetailPage.tsx` already established for Projects) — the user's own framing ("we need to be able to delete tasks... deleting a Task from the TaskGrid should *also* be possible") reads as a general capability with the grid as one of (at least) two entry points, not a grid-only feature.
- **Confirmation is mandatory, explicitly** — a plain `window.confirm()` (the same mechanism `ProjectDetailPage.tsx`'s own Delete already uses, §4.8 of `ProjectDetailPlan.md`), asking to confirm before the mutation fires. No native, silent, or single-click delete path from either the grid or Task Detail.
- **Server**: a new `DELETE /task/{task_id}` endpoint (§6, none exists today), enforcing `require_owner_or_team_lead` — the same server-side check `PATCH /task/{id}`/`DELETE /project/{id}` already use.
- **Client**: a new, generic `useDeleteTask()` hook (§6.2), the same no-fixed-id shape as `useDeleteProject()` (a single shared instance, the target `task_id` supplied at call time — needed for `TaskGrid`'s own per-row use just as it was for Project's per-row delete in its own tree).
- **Referential-integrity failures aren't specially handled here** — the same reasoning `ProjectDetailPlan.md` §4.8 already gives for Project: rely on the database's own foreign-key constraint failing loudly (Dependencies/Attachments/Remarks/Resources still pointing at the Task) rather than a separately-maintained client-side pre-check. §9's own open item flags a real wrinkle in the *message* this produces, worth reading before building this.

<a id="right-click-menu"></a>
### 4.11 Right-Click Menu

New for Level 1 (`D1.4-56`), reproducing V1.2's own `GridControl` right-click menu exactly, read directly from `V1.2/Libs/CustomGUIControls/CustomGUIControls/Grid/GridControl.cs`/`.Designer.cs` rather than guessed at:

- **Scope**: attached to `TaskGrid`'s own outer container, so it opens on a right-click anywhere on the grid, not just its filter row — matching V1.2's own `dataGridView.ContextMenuStrip` assignment, which is on the whole `DataGridView`.
- **Reset All Filters**: clears every column's own filter value (`setFilterState({})`) and re-applies — a direct match for `resetAllFiltersToolStripMenuItem_Click`'s `foreach filter: filter.Clear()`.
- **Copy All**: a tab-separated, newline-separated copy of every *currently visible* row and column (a header row of column names first) to the OS clipboard, in the grid's current sort order — `selectAllToolStripMenuItem_Click`'s own real behaviour (its own label is "Copy All," despite the method's name). Uses each column's own **displayed** text (`apiRef.current.getCellParams(id, field).formattedValue`), not the raw underlying value — matters for Owner/Requestor, whose real value is a `person_id` — and excludes the `__delete` actions column, `TaskGrid`'s own equivalent of V1.2's hidden "column zero" (the underlying-object reference column, also excluded there).
- **Show Filter**: a plain visibility toggle for the filter row (an internal `filterVisible` state, initialised from the `showFilters` prop but user-togglable from here on, §4.6) — matching `showFilterToolStripMenuItem_Click`'s own `Checked` toggle exactly in effect, including the important finding that toggling this **never affects which Tasks are actually filtered** (confirmed by reading `ApplyFiltersToRows` directly, which has no dependency on `m_filterIsVisible` at all) — only whether the boxes used to set those filters are currently shown. The menu item's own label reads "Hide filter" when currently visible and "Show Filter" when hidden (a deliberate departure from V1.2's own fixed-label-plus-checkmark idiom, at the user's own request — the label alone states the toggle's effect, so a separate check-mark icon would only repeat it).

**Post-implementation fix:** the first version of "Show Filter" fell back to DataGrid's own default column-header renderer whenever hidden, instead of `FilterableHeader`. That changed the *label's own* appearance depending on filter visibility — its default renderer centres a single line within the header's fixed height using a lighter font weight, where `FilterableHeader`'s own label is top-aligned and bold — reported directly as "the sort row's height increases and its font goes from bold to not bold" when hiding the filter. Fixed by never switching renderers at all: `FilterableHeader` now always renders (`filterRowVisible` prop replaces the on/off `renderHeader` branch), and only the filter box + button beneath the label toggle `visibility`/`pointerEvents` — kept in the layout, not conditionally rendered, so the label above never shifts position, size, or weight depending on this flag, and `columnHeaderHeight` (a single grid-wide constant) never needs to vary either. Verified via headless CDP: the label's own computed font-weight (700), its position within the header cell, and the header's own total height are all identical whether the filter row is shown or hidden; the filter input itself is confirmed present-but-`visibility:hidden` (not removed) while hidden, and fully interactive again once re-shown.

Verified via headless CDP against real seeded data: exactly these three items appear, in this order; toggling "Show Filter" off/on hides/reshows the filter row without ever changing which rows are visible, and its own check-mark tracks the current state correctly; "Reset All Filters" clearing the default Status filter reveals previously-hidden Closed/Cancelled Tasks; "Copy All" (clipboard permissions granted via `Browser.grantPermissions` for the headless test) produces a correctly-shaped tab-separated payload with real display text throughout.

<a id="migrating-all-tasks"></a>
## 5. Migrating All Tasks

<a id="all-task-orig-page"></a>
### 5.1 AllTaskOrigPage (Renamed, Unchanged)

`features/tasks/TaskListPage.tsx` → `features/tasks/AllTaskOrigPage.tsx`, component renamed `TaskListPage` → `AllTaskOrigPage`. No behavioural change at all — this is the fixed comparison baseline. Reachable at `/tasks-orig`, window identity `tasks-orig-list`.

<a id="all-task-page"></a>
### 5.2 AllTaskPage (New, `TaskGrid`-Based)

New `features/tasks/AllTaskPage.tsx` — fetches the same reference data `AllTaskOrigPage` does today (`useTasks`, `useProjects`, `useComponents`, `usePeople`, `usePersonRoles`, `useAllDependencies`, `useAllTaskResources`, `useAllRemarks`, `useAllAttachments`), builds the same team-scoped `tasks` list and the same default `initialFilterState` (§4.6), and renders a single `<TaskGrid ... />` with the full column catalog (§4.2) and `showFilters` true — reproducing §2.1's baseline exactly, per the user's own explicit acceptance criterion ("the look and feel of the TaskGrid in the AllTask window should be identical to the current AllTask window").

<a id="routing-and-comparison"></a>
### 5.3 Routing and the Comparison Period

`/tasks` → `AllTaskPage` (confirmed: the new page takes the primary route immediately, not after approval). `/tasks-orig` → `AllTaskOrigPage`, plus a second, clearly-labelled nav-bar button (e.g. "Tasks (orig)") so both are reachable without a manually-typed URL, for as long as the comparison period lasts.

<a id="cleanup-once-approved"></a>
### 5.4 Cleanup Once Approved

Once the user is satisfied: delete `AllTaskOrigPage.tsx`, its route, and the temporary nav-bar button. `TaskListPage.test.ts`-equivalent references (none exist today — this screen has no unit tests of its own, consistent with `Plan.md` §7.1) need no cleanup.

<a id="implementation-plan"></a>
## 6. Implementation Plan

<a id="new-changed-permission-functions"></a>
### 6.1 New/Changed Permission Functions

- `lib/permissions.ts`: add `EditableTaskField`, `canEditTaskField`, and `editableTaskStatusValues` (§4.3).
- `TaskDetailPage.tsx`: route Status/Priority/Owner/Effort/% Allocation/Task Type/Requestor/Detailed Description through `canEditTaskField(..., field)` instead of the blanket `canEdit`, with Status's own dropdown built from `editableTaskStatusValues(...)` rather than the full `TASK_STATUSES` unconditionally — per §4.3's own explicit instruction. This is the change that actually lets a non-owning assigned Resource use Task Detail at all today (previously `canEdit` locked the entire form for them); needs its own explicit manual-test pass (§8), not just a visual check, since it's a real new capability, not a refactor of existing behaviour. (Tentative Resource Assignment isn't a real field on Task Detail's own layout today — nothing to change there yet; it becomes relevant once/if Task Detail itself grows a control for it.)

<a id="new-api-client-hook"></a>
### 6.2 New API Client Hooks

`api/hooks.ts` — two additions, both generic siblings to the fixed-id `useUpdateTask(taskId)` (Task Detail keeps using that one, unchanged):

```ts
export function useUpdateTaskField() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ taskId, body }: { taskId: number; body: Record<string, unknown> }) =>
      unwrap<TaskRecord>(
        await apiClient.PATCH("/task/{task_id}", {
          params: { path: { task_id: taskId } },
          body: body as never, // required-field cast, see useCreateTask's own identical comment
        }),
      ),
    onSuccess: (_data, vars) => {
      invalidateEverywhere(queryClient, ["tasks", vars.taskId]);
      invalidateEverywhere(queryClient, ["tasks"]);
    },
  });
}

// §4.10 — a single shared instance for both TaskGrid's own per-row trash
// icon and TaskDetailPage.tsx's header Delete button, the target task_id
// supplied at call time (the same shape useDeleteProject() already uses,
// and for the same reason: the id varies per call, not per hook).
export function useDeleteTask() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (taskId: number) =>
      unwrap<void>(
        await apiClient.DELETE("/task/{task_id}", { params: { path: { task_id: taskId } } }),
      ),
    onSuccess: () => invalidateEverywhere(queryClient, ["tasks"]),
  });
}
```

<a id="new-components"></a>
### 6.3 New Components

- `features/tasks/TaskGrid.tsx` — the component itself (§4).
- `features/tasks/AllTaskPage.tsx` — the new All Tasks screen (§5.2).
- `features/tasks/AllTaskOrigPage.tsx` — the renamed original (§5.1), moved, not rewritten.

<a id="build-steps"></a>
### 6.4 Build Steps

In dependency order:

1. Rename `TaskListPage.tsx` → `AllTaskOrigPage.tsx` (§5.1); update its own route to `/tasks-orig` and window identity to `tasks-orig-list`; add the temporary nav-bar button. Confirm it still behaves identically before touching anything else — this step should be a pure rename/move with zero logic changes.
2. `rest-api`: add `DELETE /task/{task_id}` (§4.10, §3.1), `require_owner_or_team_lead`, mirroring `DELETE /project/{project_id}`'s own shape exactly.
3. `lib/permissions.ts`: add `canEditTaskField` and `editableTaskStatusValues` (§6.1), with unit tests covering all three tiers (owner, TeamLeadUser, a non-owning assigned Resource, and a non-owning *non*-Resource) crossed with each governed field — Owner/T's own stricter LeadUser+ threshold, and Status/Detailed Description's tier-3 carve-out, each need their own explicit case, not just the general rule's.
4. `api/hooks.ts`: add `useUpdateTaskField()` and `useDeleteTask()` (§6.2).
5. Build `TaskGrid.tsx` itself: the full column catalog (§4.2) and filtering (§4.6)/sorting (§4.7) first, read-only, ported directly from `AllTaskOrigPage.tsx`'s own column definitions — get this rendering identically before adding editing.
6. Add inline editing to the 9 governed columns (§4.4/§4.9), `isCellEditable`/`cellClassName` wired to `canEditTaskField` (§4.5), `processRowUpdate` wired to `useUpdateTaskField()`.
7. Add the per-row delete affordance (§4.10) — trash icon, `window.confirm()`, `useDeleteTask()`.
8. Build `AllTaskPage.tsx` (§5.2) as a thin wrapper: fetch the same reference data `AllTaskOrigPage.tsx` does, compute the same `initialFilterState`, render one `<TaskGrid>`. Point `/tasks` at it (§5.3).
9. Update `TaskDetailPage.tsx`'s own governed fields to call `canEditTaskField`/`editableTaskStatusValues` (§6.1), and add its own header Delete button (§4.10).

<a id="deferred-to-level-2"></a>
## 7. Deferred to Level 2

Recorded here, and to be added to `Claude/Level2_Implementation/Scope.md` per this project's own standing convention for an unnamed later-level item: **reconciling `TaskGrid`'s immediate per-cell save with Task Detail's own staged-edits-then-Save model** (`D-Win-8`) is a real, acknowledged inconsistency — two different save conventions for editing the same fields on the same entity, depending only on which screen you're using. The user's own framing: V1.2 avoided this by holding *every* edit in memory until a single "Save All," and Level 2's own undo requirements make this worth solving properly once, rather than patching Level 1's grid to half-match Task Detail's model now. Level 1 ships with the inconsistency accepted, not hidden.

<a id="testing-approach"></a>
## 8. Testing Approach

Same approach already established throughout this phase (`Plan.md` §7): manual testing against the real running `rest-api` and real seeded data, no mocked API layer, plus unit tests for the new pure logic (`canEditTaskField`, alongside `lib/permissions.ts`'s own existing untested-so-far functions — worth adding basic coverage for those too while touching this file, not just the new function). Once built, add a numbered Manual Testing entry to `Plan.md` §7.2 covering: `AllTaskPage` and `AllTaskOrigPage` rendering identically side by side (same rows, same columns, same default sort/filter, same row colours); editing each of the 8 governed columns as an owner, as a non-owner Normal User (should be refused/greyed), as a Team Lead, and Owner specifically as a plain owner-Normal-User (should be greyed, unlike the other seven) versus a LeadUser (should be editable); a `TaskGrid` edit reflecting live in an already-open Task Detail window for the same Task, and vice versa; the "T" column appearing only for a Team Lead. Also update `V2/UserDocumentation/AllTaskView.md` once built, to document the new inline-editing capability, per the standing note in `Plan.md` §7.3 — not done as part of this design document, since it must describe the real, shipped screen, not a plan.

<a id="open-items-for-review"></a>
## 9. Open Items for Review

1. **The server has no field-specific carve-out for `owner_person_id` or `tentative_resource_assignment`.** `require_owner_or_team_lead` (the server-side check `PATCH /task/{id}` actually enforces) is the same general rule for every field — a plain Normal-User owner could still reassign either via a raw API call even after this document's own client-side `canEditTaskField` refuses to show/accept that edit through either Task Detail or `TaskGrid`. This mirrors how the client already goes further than the server in some existing cases (nothing new in kind), but worth a conscious decision: leave the server as the coarser backstop it already is, or add a matching server-side carve-out for these two fields. Not blocking Level 1's own GUI work either way.
2. **`useUpdateTaskField()`'s error surfacing** (§4.9) proposes a `Snackbar`/`Alert` at the grid level, since there's no natural per-row inline-error slot the way Task Detail's own single-record layout has one. Worth confirming that's an acceptable interim answer, given §7's own broader acknowledgement that the save/error model here isn't the final Level 2 answer.
3. **The one generic foreign-key-violation message is worded for the wrong direction when a Delete is blocked.** `rest-api/app/errors.py`'s `_foreign_key_violation_handler` always returns "Refers to a record that doesn't exist" (400) — correct wording for a *create/update* violation (the record being saved points at something that isn't there), but backwards for a *delete* blocked because something else still points at the record being deleted (a Dependency, Attachment, Remark, or Resource assignment still referencing this Task, §4.10). A user who tries to delete a still-referenced Task today would see a message that reads as if their own data is broken, not as "something else depends on this." This isn't new to Task deletion — `ProjectDetailPlan.md` §4.8/§5.5 already relies on the same handler for blocked Project deletes and has the identical wrinkle — but Task deletion is a second, freshly-added case that makes fixing it worth doing once rather than twice. Worth a small server-side fix (distinguishing the two directions, e.g. by which side of the constraint failed, and returning direction-appropriate wording) before or alongside building §4.10, rather than shipping a second screen with a misleading delete-failure message.
4. **A raw `PATCH /task/{id}` against the real `rest-api` is itself intermittently slow (D1.4-54), independent of anything in this document's own design.** Timing 5 successive raw `fetch` calls (no React Query, no DataGrid, no live-sync involved at all) found `PATCH` round trips of 22ms, 1224ms, 272ms, 213ms, 22ms — wildly inconsistent — against a `GET` on the same endpoint, from the same client, consistently landing at 20–30ms every time. `db.py` already pools connections (`min_size=1, max_size=10`), ruling out the obvious "opens a fresh DB connection per request" cause. **No longer user-visible for the cross-window sync case** since D1.4-55's optimistic broadcast means the PATCH's own duration is no longer on the critical path for propagation — but the underlying question (why is `PATCH` specifically inconsistent when `GET` on the same endpoint never is) is still genuinely unanswered, and worth its own investigation (connection-pool sizing under concurrent access, Postgres-side lock contention, or Windows/Docker Desktop's own networking overhead for new-vs-reused connections) at some point, independent of anything TaskGrid-specific.
