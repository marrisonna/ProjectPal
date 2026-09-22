# Search / Find — Design and Implementation Plan

**Status: implemented and verified against the real REST API/database (`D1.4-73`–`D1.4-76`).** Written before any code, per the user's own request, following the same before-code-review pattern `ProjectDetailPlan.md`/`TaskGridPlan.md`/`ComponentDetailPlan.md` (this folder) already established. All open items (§8) are answered: the search trigger is explicit, not live (`D1.4-74`); `TaskGrid.tsx`'s dense-grid chrome (sizing, borders, header styling, right-click menu, text-measurement/auto-sizing) is extracted into a shared `DenseDataGrid` (`D1.4-73`, §5) that both `TaskGrid` and this screen's own results grid build on; multi-term search is a server-side AND-across-words fix in `search.py` (`D1.4-75`, §6.1); and Task-row Urgency tinting is adopted in the results grid (`D1.4-76`, §4.4/§8).

## Contents

1. [Purpose and Scope](#purpose-and-scope)
2. [Requirements](#requirements)
   - 2.1 [What V1.2 Did](#what-v12-did)
   - 2.2 [What's Different in V2](#whats-different-in-v2)
   - 2.3 [Visibility — the Team-Scoping Floor](#visibility)
3. [What Already Exists to Build On](#what-already-exists)
   - 3.1 [Server API](#server-api)
   - 3.2 [Reusable Client-Side Pieces](#reusable-pieces)
   - 3.3 [Gaps to Fill](#gaps-to-fill)
4. [Design](#design)
   - 4.1 [Screen Structure and Navigation](#screen-structure)
   - 4.2 [Search Controls](#search-controls)
   - 4.3 [Data Flow](#data-flow)
   - 4.4 [Results Grid](#results-grid)
   - 4.5 [Opening a Result](#opening-a-result)
   - 4.6 [Empty and Error States](#empty-error-states)
5. [Shared Grid Chrome: Extracting `DenseDataGrid` From `TaskGrid`](#dense-data-grid)
   - 5.1 [What's Generic vs. Task-Specific](#generic-vs-specific)
   - 5.2 [The New `components/DenseDataGrid.tsx`](#new-dense-data-grid-file)
   - 5.3 [How `TaskGrid` Changes](#how-taskgrid-changes)
   - 5.4 [How the Search Results Grid Uses It](#how-search-uses-it)
6. [Implementation Plan](#implementation-plan)
   - 6.1 [Server Change](#server-change)
   - 6.2 [New API Client Hook](#new-hook)
   - 6.3 [New Files](#new-files)
   - 6.4 [Build Steps](#build-steps)
7. [Testing Approach](#testing-approach)
8. [Open Items for Review](#open-items)

<a id="purpose-and-scope"></a>
## 1. Purpose and Scope

`4_GuiClient/Plan.md` §6.4 (Stage 4 — Remaining Screens) lists Search/Find as one of the four screens still unbuilt now that Project and Component List/Detail are both done. `Requirements/UseCases.md` §6 and `Requirements/UserInterfaceWindows.md` §3.11 both describe it as a global text search across Task, Project, Component, and Remark, with Attachment coverage folded in (metadata only — full-text search of attachment *contents* is explicitly deferred, per both documents). This plan designs it having read V1.2's real `FormFind`/`FindResult` source directly and the V2 REST API's own `GET /search` (`rest-api/app/routes/search.py`, already built and tested — see §3.1), rather than assuming either one straightforwardly carries over.

Out of scope: full-text search inside Attachment file contents (deferred, per both requirements documents above) and any drag-and-drop (Stage 5, `D1.4-40` — V1.2's own `FindResult.PopulateDragDropDataObject` is a no-op stub, so there's nothing to carry forward here anyway).

<a id="requirements"></a>
## 2. Requirements

<a id="what-v12-did"></a>
### 2.1 What V1.2 Did

`FormFind.cs`: a modeless window (opened from `MainWindow`'s "Find" button) with a search text box, a checkbox per source type (Tasks/Components/Projects/Remarks — four; **not** Attachments, see below), an "Include Closed Items" checkbox, and a results grid. Clicking "Find":

1. Splits the search text on spaces into separate terms, lower-cased.
2. Collects every in-memory instance (`DBProjectPal.Task/Component/Project/Remark.AllInstances` — V1.2 is a single desktop process with the whole object graph already loaded) from the checked source types.
3. Skips any item where `IsClosed` is true, unless "Include Closed Items" is checked. `IsClosed` is a plain, type-specific property: Task/Project → `Priority` is `Cancelled`/`Closed`; Component → `ActiveTaskCount == 0`; Remark → its owning Task's own `IsClosed`.
4. For each remaining item, requires **every** search term to be found via that type's own `ContainsText` (Task: `Description` or `DetailedDescription`; Project/Component: `Name`; Remark: `RemarkText`) — a plain case-insensitive substring test, ANDed across terms.
5. **Attachments are never their own result type in V1.2.** They're only consulted as a *fallback*: if an item didn't match on its own fields, V1.2 opens each of its attached files and checks whether the remaining unmatched terms appear in the file's own *content* (`Attachment.ContainsText`, a real parser for `.msg`/etc. via `Utils.DocumentSearch`) — if so, the original item (not the attachment) is still the result. There is no "Attachment" `FindResult.TypeString` anywhere in `FindResult.cs`.

   This same `CustomGUIControls.Grid` control is what backs V1.2's `FormFind` results grid — the same generic, `IGridItem`-driven grid engine `TaskWindow`/`ProjectControl`/`ComponentControl` also use, just handed a different `IGridItem` implementation (`FindResult` vs. `GUITask`) and a different declarative column list (`FindResultColumns` vs. `GUITask`'s own). This is the observation that prompted §5 below: V1.2 already shares one grid *engine* across every entity type it displays, including Find's own results — V2's `TaskGrid` hasn't had a reason to split "generic grid chrome" from "Task-specific column/editing logic" apart until now, since it's only ever shown Tasks.
6. Results are shown as `FindResult` rows — `Type`, `Description`, `Date` (Task: computed end date, or `StatusDate` if closed; Project: `StartDate`; Remark: `ModifiedTime`; Component: none), `Person` (Task: comma-joined resource names; Project: owner; Remark: owner; Component: none) — sorted active-items-first, then by a fixed type order (Task, Component, Project, Remark), then alphabetically by Description.
7. Double-clicking a result opens the matching detail window by type; a Remark result opens its owning Task's `TaskDetail` and additionally calls `ShowRemark` to surface that specific remark within it.

<a id="whats-different-in-v2"></a>
### 2.2 What's Different in V2

- **No single in-memory object graph.** Each V2 window is its own browser tab/process with its own React Query cache (`ComponentDetailPlan.md`'s own precedent already established this isn't new territory) — there is nothing V2-equivalent to V1.2's `AllInstances`. `GET /search` (§3.1) exists specifically to give a fresh window a fast, single-round-trip way to find matching rows without first pulling every table down in full.
- **Attachment is already its own first-class result type in V2's `/search`.** Since full attachment-*content* search is deferred anyway (both requirements docs agree), matching directly on Attachment *metadata* (`name`, `url`, `mail_from`) as its own row is a simpler, already-implemented substitute for V1.2's "open the file and search inside it as a fallback for another item" behaviour — not a gap to fill, an intentional V2 simplification building on work already done.
- **Urgency didn't exist in V1.2's search results at all** (V1.2 predates it as a concept here). `Plan.md` §2 is explicit that "Any remaining screen this phase builds that shows a Task ... should include Urgency and colour-code it wherever doing so is actually appropriate" — Search shows Tasks, so Task-typed result rows get the same urgency tint `TaskGrid.tsx` already uses (`D1.4-76`, §4.4).
- **No `Requestor`/`ShowRemark`-style deep link into a specific Remark.** V2's Remarks render inline in a tab on the owning Task/Project/Component's own detail page, not a separate popped-out `RemarkWindow` — there's nothing to `ShowRemark` into. A Remark (or Attachment) result opens its owner's detail window; landing on the exact Remark/Attachment row within that window's own tab is not attempted (a small, deliberately accepted regression from V1.2 — see §4.5).
- **Component now has an `owner_person_id`** (unlike V1.2, where Component had no owner concept at all and `FindResult.Person` returns `""` for it) — a small, natural V2 improvement: Component results can show an owner the way Project results always have.
- **V1.2 had one shared grid engine for every entity type; V2's `TaskGrid` is currently Task-only.** This screen is the reason to split TaskGrid's generic chrome out at last (§5), rather than a gap being carried forward unaddressed.

<a id="visibility"></a>
### 2.3 Visibility — the Team-Scoping Floor

`Requirements/UseCases.md` §6 says Search is "scoped to what the user can see." Neither `GET /search` nor any other list endpoint (`GET /task`, `GET /project`, `GET /component`) applies any server-side Team restriction — confirmed by reading `search.py`/`tasks.py` directly, not assumed. What actually enforces this today is a **client-side floor**, already established identically in three places: `AllTaskPage.tsx` (`D-Win-17` — "Every user's All Tasks view is hard-restricted to their own Team(s) ... a floor under the row set itself, not a clearable filter"), `ProjectDetailPage.tsx`, and `ComponentDetailPage.tsx` (both: "Only Projects/Components on a Team the caller belongs to (any role) — same client-side team-scoping already established for Tasks/the Gantt view"). All three compute `memberTeamIds` from `person.team_roles` and filter the raw list before it ever reaches the screen.

**Search must apply this exact same floor**, extended to every type it covers: Task via its Project's `team_id`, Project/Component via their own `team_id`, and Remark/Attachment via whichever of `task_id`/`project_id`/`component_id` is set on them (resolved the same way, one level removed). This is not a new policy decision — it's applying an already-settled one to a fourth and fifth screen.

<a id="what-already-exists"></a>
## 3. What Already Exists to Build On

<a id="server-api"></a>
### 3.1 Server API

`GET /search?q=...` (`rest-api/app/routes/search.py`, `D1-4`) already exists, is already tested (`tests/test_search.py`), and already covers Task (`description`), Project/Component (`name`), Remark (`remark_text`), and Attachment (`name`/`url`/`mail_from`) via one `UNION ALL` query, returning `{type, id, label}` rows. It has **no response model** (untyped in `schema.d.ts` — `search_search_get` resolves to `unknown`, the same situation `client.ts` already documents for `WhoAmI`/`Team`), and does **not** split `q` on whitespace the way V1.2 did (a single `%q%` substring per branch, not an AND of multiple terms) — see §6.1.

<a id="reusable-pieces"></a>
### 3.2 Reusable Client-Side Pieces

Everything this screen needs already exists, built for other screens:

- **Reference-data hooks** (`api/hooks.ts`): `useTasks`, `useProjects`, `useComponents`, `usePeople`, `usePersonRoles`, `useAllDependencies`, `useAllTaskResources`, `useAllRemarks`, `useAllAttachments` — the exact same bundle `AllTaskPage.tsx`/`ProjectDetailPage.tsx`/`ComponentDetailPage.tsx` already fetch in full on every load.
- **`buildScheduleGraph`** (`lib/schedule.ts`) — needed for Task results' computed end date and Urgency, built from the same hooks above (`AllTaskPage.tsx`'s own `useMemo` call is the exact pattern to copy).
- **`isTaskVisible`** (`features/projects/Projects.tsx`) and **`isProjectActive`** (`lib/schedule.ts`) — already the established "is this closed" test for Task and Project respectively (the latter already backs Projects.tsx's own "Only Active Projects" checkbox).
- **`personDisplayName`** (`lib/people.ts`) — resolves a `person_id` (+ optional Team, for nickname) to a display name; used identically for every "Person" column elsewhere in the app.
- **`computeUrgency` / `computeTaskRowColour`** (`lib/schedule.ts`) — already back `TaskGrid.tsx`'s own per-row urgency tint; reusable as-is for Task-typed result rows (`D1.4-76`, §4.4).
- **`formatDdMmmYy`** (`lib/schedule.ts`) — the shared date-display format used everywhere else a schedule date is shown.
- **`openItemWindow(entityType, id)`** (`lib/windowNav.ts`) — already the single, generic way every other screen opens a Task/Project/Component's own singleton window; needs no change, just calling with the right `entityType`/`id` per result type (§4.5).
- **The `memberTeamIds` pattern** (§2.3) — copy `ProjectDetailPage.tsx`'s own `useMemo(() => new Set((person?.team_roles ?? []).map((tr) => tr.team_id)), [person])` verbatim.
- **`DenseDataGrid`** (§5, new in this plan) — the shared dense-grid chrome extracted from `TaskGrid.tsx`, used by this screen's own results grid (§4.4).

<a id="gaps-to-fill"></a>
### 3.3 Gaps to Fill

- `search.py` doesn't AND multiple search terms (§6.1).
- No API client hook wraps `GET /search` yet (§6.2) — and since it has no server-side response model, this hook needs its own hand-written return type, the same way `usePeople`/`useTeams`-equivalent hooks already do for other unmodelled endpoints.
- No route, nav entry, or window-size entry exists for a Search screen yet (`App.tsx`, `AppShell.tsx`, `windowNav.ts`).
- Nothing today resolves "which Team does this Remark/Attachment belong to" (§2.3) — a small local helper, not previously needed since Remarks/Attachments have only ever been shown already-scoped to one owner's own detail page.
- `TaskGrid.tsx`'s dense-grid chrome isn't separated from its Task-specific logic yet (§5) — the one piece of groundwork this plan does that isn't Search-specific at all, but that Search is what finally needs.

<a id="design"></a>
## 4. Design

<a id="screen-structure"></a>
### 4.1 Screen Structure and Navigation

A new popped-out singleton window, matching the precedent `Plan`/`Projects`/`Components` already set (`D1.4-8`) rather than `AllTaskPage`'s in-place-navigation ("Tasks" link) precedent — Search is a secondary, on-demand utility, not a landing page. `AppShell.tsx` gets a fourth nav button: `<Button color="inherit" onClick={() => openListWindow("search")}>Search</Button>`, opening `/search` in a window named `search-list`, chrome-less (`BareAuthenticatedLayout`, same as Plan/Project Detail/Component Detail — no repeated app bar in a small utility popup). `windowNav.ts` gets a `SEARCH_WINDOW_FEATURES` entry — wider than Project/Component Detail's `728×900` (a results grid with several columns wants more horizontal room, a search screen wants less vertical); proposing `width=900,height=600`, wired into `windowFeaturesFor` the same way.

The page shell copies `ProjectDetailPage.tsx`'s own "Top Level Projects" (`fillWindow`) shape exactly: an outer `Box` at `height:"100%"`, `display:"flex"`, `flexDirection:"column"`, a small title (`Box sx={{ fontSize:14, fontWeight:600 }}` — "Search"), the controls row (§4.2) below it, then the results grid (§4.4) taking the remaining flex space (`flex:1, minHeight:0`).

<a id="search-controls"></a>
### 4.2 Search Controls

Per `Requirements/UserInterfaceWindows.md` §3.11 ("Search text box, checkboxes for which types to search and whether to include closed items ... a read-only results grid"):

- A search text `<input>` (native, matching `DenseField.tsx`'s own stated app-wide preference for native controls over MUI `TextField`).
- Five type checkboxes — Task, Project, Component, Remark, Attachment — all default-checked (V1.2 defaulted its four to checked; Attachment is new in V2 but follows the same default since it's no longer an expensive opt-in content-scan, just an ordinary metadata match, §2.2).
- One "Include Closed Items" checkbox, default **unchecked** (matches V1.2's own default, and `AllTaskPage.tsx`'s `D-Win-16` default of excluding Closed/Cancelled).
- **No** "search attachment contents" checkbox — nothing exists behind it (deferred per §1); shipping a checkbox with no effect would be inert UI, which this codebase has consistently avoided elsewhere (e.g. Component has no "Only Active" checkbox at all, per `D1.4-67`, rather than showing one that does nothing).
- No progress indicator (V1.2's `FormProgress`) — Level 1 data volumes are small enough (`Plan.md` §2's own repeated reasoning) that a search among already-small reference lists is effectively instant; nothing here justifies async progress UI.

The search re-runs only on an explicit trigger — Enter in the text box, or a "Find" button — not live-as-you-type (`D1.4-74`, confirmed by the user): matches V1.2's own model, and avoids a network round-trip to `/search` on every keystroke (unlike TaskGrid's own filter-row inputs, which filter already-loaded rows locally rather than calling the server).

<a id="data-flow"></a>
### 4.3 Data Flow

1. **Fetch candidates**: call `GET /search?q=<text>` (only once the text is non-empty) via the new hook (§6.2) — a single round trip that does the actual substring/AND-term matching in Postgres, covering `mail_from` correctly (the one field no client-side reference list carries at all, since `useAllAttachments`'s own `_LIST_COLUMNS` deliberately excludes it, matching what `GET /attachment` itself returns).
2. **Fetch reference data** unconditionally on mount — the exact bundle listed in §3.2, the same one every other detail screen already fetches in full regardless of Search. This is not extra cost specific to Search: opening any of Task/Project/Component Detail already pays for fetching this same set; Search pays it too, once, on window open.
3. **Resolve each candidate** (`{type, id, label}`) against the matching reference list by id — a `Map<number, T>` per type, built once via `useMemo`, exactly the `projectsById`-style pattern `AllTaskPage.tsx`/`ProjectDetailPage.tsx` already use. A candidate whose id isn't found (a race between the search call and a concurrent delete elsewhere) is silently dropped, not shown as broken.
4. **Apply the Team-scoping floor** (§2.3) — drop any resolved row whose owning Team isn't in `memberTeamIds`.
5. **Apply the type checkboxes** — drop any row whose `type` is unchecked.
6. **Apply "Include Closed Items"** — per type: Task → `isTaskVisible(task, "Open")` (inverted: keep if this is true, or if the checkbox is on); Project → `isProjectActive(scheduleGraph, project_id)`; Component → at least one of its own Tasks (by `component_id`) is not Closed/Cancelled (Component has no `isProjectActive`-equivalent helper today — a small inline check, mirroring V1.2's own `ActiveTaskCount == 0` rule); Remark/Attachment → the same test applied to whichever of Task/Project/Component owns it (mirroring V1.2's `Remark.IsClosed => m_task.IsClosed`, generalised to V2's three possible owners).
7. **Build display rows**: `{ type, id, label, date, person, teamId }` per §4.4/V1.2's own `FindResult` shape, using `getTaskSchedule`/`formatDdMmmYy`/`personDisplayName` as listed in §3.2.

All of steps 3–7 are one `useMemo`, recomputed when the search results or any reference list changes — not a separate "Find" action's worth of imperative code (a deliberate difference from V1.2's `buttonFind_Click`, since none of this is expensive enough here to need to be structured as a one-shot batch job the way it did there).

<a id="results-grid"></a>
### 4.4 Results Grid

One flat grid, built on the new shared `DenseDataGrid` (§5) rather than a bespoke plain `<DataGrid>` or V1.2's own single unified engine reused wholesale — read-only (no inline editing — nothing here is ever edited from a search result), matching V1.2's own single unified grid rather than splitting by type. Columns, matching `FindResult` exactly: **Type, Description, Date, Person** — Component's Date/Person cells are simply blank where nothing applies (Component has no date concept at all, §2.2), same as V1.2. No Team column — nothing in the GUI currently displays a Team name anywhere (confirmed: no `useTeams`-style hook or Team-name lookup exists yet), and adding that plumbing solely for this one column is out of scope here.

**Per-column filtering, after all (`D1.4-77`, revising this section's original design).** The four columns run through `useDenseGridColumns` (§5.1) the same way `TaskGrid`'s own columns do — each gets a real filter box via `withFilter`, and `DenseDataGrid`'s right-click context menu shows the full set: Copy All, Reset All Filters, Show Filter/Hide filter. The original reasoning here (the search box/type checkboxes/Include Closed checkbox already narrow the row set, so a second filtering layer seemed redundant) was overridden by the user specifically because that behaviour is shared chrome — once `DenseDataGrid` already knows how to do per-column filtering, a grid opting out of it needs its own justification, not the other way round, and "these four columns are short and predictable" isn't a strong enough one. Default sort: Type, then Description — simpler than V1.2's three-key active-first/type-order/alphabetical sort, since the urgency tint (below) already gives Task rows their own visual "this one's live" cue without needing to also physically reorder the grid.

**Description is a `flex` column, not a fixed width (`D1.4-78`).** With all four columns auto-sized to fixed pixel widths, MUI DataGrid left the leftover horizontal space as its own "filler" element after the last column — reported (correctly) as looking like a genuine fifth column: no header, no filter, no data, and it visibly stretched as the window resized. `withFilter` (§5.1/`DenseDataGrid.tsx`) gained an optional 5th `flex` argument for exactly this: Description passes `flex: 1`, absorbing all leftover width itself, while Type/Date/Person stay at their own auto-sized widths — eliminating the filler entirely rather than just hiding it. `flex` only applies while the user hasn't manually resized that column (MUI DataGrid always ignores `width` when `flex` is set, so the two are mutually exclusive) — the moment the user drags Description to a specific width, `withFilter`'s existing manual-width override takes over verbatim, same as any other column, and `flex` is dropped for good. Generic, not Search-specific: any future `DenseDataGrid` consumer with the same "one column should fill the rest" need can pass `flex` on its own designated column the same way.

**Task-row Urgency tinting (`D1.4-76`, confirmed by the user).** Task-typed result rows get the same per-row background tint `TaskGrid.tsx` already computes — `getRowClassName` resolving to `computeTaskRowColour(row.priority, computeUrgency(...))`'s palette-of-CSS-classes approach, passed into `DenseDataGrid` the same way `TaskGrid` itself passes its own `urgencyRowClassName`/`urgencyRowSx` (§5.1/§5.3). Project/Component/Remark/Attachment rows get no tint (no urgency concept for those types) — plain white, same as any non-urgency row already renders elsewhere in this app.

<a id="opening-a-result"></a>
### 4.5 Opening a Result

Double-click (matching every other grid in this app) calls `openItemWindow(entityType, id)`:

- Task → `openItemWindow("tasks", task_id)`
- Project → `openItemWindow("projects", project_id)`
- Component → `openItemWindow("components", component_id)`
- Remark → resolve its owner (whichever of `task_id`/`project_id`/`component_id` is set) and open **that** window instead — there's no separate Remark window in V2 to open (§2.2)
- Attachment → same owner-resolution as Remark

Landing on the specific Remark/Attachment row *within* the opened window's own tab (V1.2's `ShowRemark`) is not attempted — a deliberately accepted small regression (§2.2), not a build step here.

<a id="empty-error-states"></a>
### 4.6 Empty and Error States

Empty search text: show no grid at all (not "0 results" — there's no search yet to have produced zero of anything), matching `Components.tsx`'s own established "show nothing, not a placeholder message" precedent from the most recent styling round. A non-empty search producing zero rows: MUI DataGrid's own default "No rows" treatment is sufficient — nothing bespoke needed. A failed `/search` call: a plain inline error message above the grid, same shallow-error-handling level as the rest of this phase (no retry/backoff machinery).

<a id="dense-data-grid"></a>
## 5. Shared Grid Chrome: Extracting `DenseDataGrid` From `TaskGrid`

**Decided (`D1.4-73`).** Prompted by the user's own observation that V1.2's `FormFind` results grid is the *same* generic `CustomGUIControls.Grid` engine `TaskWindow`/`ProjectControl`/`ComponentControl` all use (§2.1, point 5) — V2's `TaskGrid.tsx` has simply never had a second consumer to force the same split. Read `TaskGrid.tsx` in full (1330 lines) to separate what's genuinely generic display chrome from what's Task-specific business logic, rather than guessing from its name.

Two options were weighed: rebuild a full V1.2-style generic grid engine (one component parameterised over an `IGridItem`-equivalent interface, serving *every* column/editing concern for *any* row type) vs. extracting only the *chrome* — sizing, borders, header styling, the right-click menu, text measurement — and leaving column definitions, editability, and urgency firmly in each consumer. The former was rejected for the same reason `D1.4-70` (Component vs. Project) rejected a generic `Project`/`Component` pair: Search's rows are read-only and mix five unrelated record shapes, while `TaskGrid`'s rows are single-shaped and centrally governed by per-field edit rules (`canEditTaskField`) — forcing both through one parameterised "any entity, any column" engine would mean re-deriving V1.2's `IGridItem`/`GetFieldValue`/`IsReadOnly` abstraction in React for a benefit (one engine instead of two) that's aesthetic more than functional at this scale. The chrome-only split avoids that: it shares exactly the part that's identical between them (how a dense grid looks and behaves as a grid) without coupling Search's rows to Task's editing model.

<a id="generic-vs-specific"></a>
### 5.1 What's Generic vs. Task-Specific

Generic (moves to `DenseDataGrid.tsx`, §5.2):

- The dense sizing constants (`DENSE_ROW_HEIGHT`, `HEADER_HEIGHT`, `HEADER_HEIGHT_NO_FILTER_ROW`, `HEADER_LABEL_PADDING`, `CELL_PADDING`, `BOOLEAN_COLUMN_WIDTH`) and the header/cell font constants (`HEADER_FONT_WEIGHT`, `CELL_FONT_WEIGHT`, `HEADER_FONT`, `CELL_FONT`) — none of these reference Task at all today; they're sized against `DENSE_FONT_SIZE`/`branding.fontFamily` only.
- `measureTextWidth` and its module-level offscreen-`<span>` singleton, and the `fontsReady`/`document.fonts.load` logic (renamed `useDenseFontsReady`) — both already fully generic, just currently declared inside `TaskGrid.tsx` because nothing else needed them yet.
- The filtering/auto-sizing machinery — `filterState`/`filterVisible`/`setContains`/`setExact`/`passesAllFilters`/`getOptionsForField`/`manualColumnWidthsRef`/`measureColumnContentWidth`/today's `withFilter` — generic over row type `T` once its few `TaskRecord`-typed signatures become `T`-typed. Packaged as one hook, `useDenseGridColumns<T>({ rows, initialFilterState, showFilters })`, returning `{ withFilter, filteredRows, filterVisible, setFilterVisible, resetFilters }` — `TaskGrid` calls `withFilter` per column exactly as it does today; a lightweight consumer with few, fixed columns (Search, §4.4) can skip this hook entirely and build plain `GridColDef`s instead.
- `DenseSingleSelectEditCell` — already written generically against `GridRenderEditCellParams<T>`; only pinned to `TaskRecord` today because it's the sole consumer. Moves as-is, generic.
- The `<DataGrid>` wrapper itself: `rowHeight`/`columnHeaderHeight`, `disableColumnMenu`/`disableColumnFilter`/`disableRowSelectionOnClick`, `showColumnVerticalBorder`/`showCellVerticalBorder`, `autoHeight` + `hideFooter` under 100 rows, `sortingOrder`, the base `sx` (the dark-grey outer border, white `.MuiDataGrid-columnHeaders` fill, `fontSize`), `onColumnResize` wiring into `manualColumnWidthsRef`, and the right-click context menu (Copy All always; Reset All Filters/Show-Hide Filter only when a `filtering` prop is supplied, §4.4) — none of this reads a Task field.

Stays in `TaskGrid.tsx` (Task-specific):

- The column catalog itself — `TaskGridColumnKey`, `DEFAULT_TASK_GRID_COLUMNS`/`EMBEDDED_TASK_GRID_COLUMNS`/`COMPONENT_EMBEDDED_TASK_GRID_COLUMNS`, `allColumnDefs`, `defaultMaxWidths` (Description/Ref URL/Detailed Description's own width caps — a Task-column-specific concern, threaded into `withFilter` as a plain `capsForField` argument now rather than a closed-over constant).
- `GOVERNED_FIELDS`, `governed()`, `canEditCell`/`canEditTaskField`/`canDeleteRow`/`isAssignedResource`/`projectTeamId` — Task's own per-field/per-row permission rules.
- `scheduledUrgency`/`taskUrgency`/`urgencyRowClassName`/`urgencyRowSx` — Urgency is a Task-only concept; the *palette-of-CSS-classes* mechanism this uses is generic in principle, but with only one consumer today there's no forced abstraction here — `TaskGrid` passes its own `getRowClassName`/extra `sx` into `DenseDataGrid` as plain props (already how row-class/sx works in the design below), so nothing about this needs to move.
- `processRowUpdate`/`useUpdateTaskField`/`useDeleteTask`/`handleDeleteTask`/the delete actions column — Task's own mutations.

<a id="new-dense-data-grid-file"></a>
### 5.2 The New `components/DenseDataGrid.tsx`

```ts
export const DENSE_ROW_HEIGHT = 22;
export const HEADER_HEIGHT = 36;
export const HEADER_HEIGHT_NO_FILTER_ROW = 16;
// ...HEADER_LABEL_PADDING, CELL_PADDING, BOOLEAN_COLUMN_WIDTH, font constants — moved verbatim.

export function measureTextWidth(text: string, fontWeight: number): number { /* moved verbatim */ }
export function useDenseFontsReady(): boolean { /* moved verbatim, generalised name */ }
export function DenseSingleSelectEditCell<T>(props: GridRenderEditCellParams<T>) { /* moved verbatim, generic */ }

export function useDenseGridColumns<T>(opts: {
  rows: T[];
  initialFilterState?: Record<string, ColumnFilterState>;
  showFilters?: boolean;
}): {
  withFilter: (
    col: GridColDef<T>,
    getValues: (row: T) => string[],
    sortType: FilterSortType,
    widthCap?: number,
  ) => GridColDef<T>;
  filteredRows: T[];
  filterVisible: boolean;
  setFilterVisible: (v: boolean) => void;
  resetFilters: () => void;
} { /* today's filterState/withFilter/passesAllFilters/manualColumnWidthsRef, generalised over T */ }

export interface DenseDataGridProps<T> {
  rows: T[];
  columns: GridColDef<T>[];
  getRowId: (row: T) => number | string;
  onRowDoubleClick?: (row: T) => void;
  getRowClassName?: (row: T) => string;
  sx?: SxProps; // merged after the base chrome sx (e.g. TaskGrid's urgencyRowSx)
  defaultSort?: { field: string; sort: "asc" | "desc" };
  // Present only for a grid that has column filtering (TaskGrid) — see §4.4 for a grid that doesn't (Search).
  filtering?: { filterVisible: boolean; onToggleFilterVisible: () => void; onResetFilters: () => void };
  // Pass-throughs for an editable grid (TaskGrid only) — absent/no-op for a read-only one (Search).
  isCellEditable?: (params: GridCellParams<T>) => boolean;
  onCellClick?: (params: GridCellParams<T>) => void;
  processRowUpdate?: (newRow: T, oldRow: T) => Promise<T>;
  onProcessRowUpdateError?: (err: unknown) => void;
}

export function DenseDataGrid<T>(props: DenseDataGridProps<T>) {
  /* today's <Box onContextMenu><DataGrid .../><Snackbar/><Menu/></Box>, generalised over T,
     with the Reset-Filters/Show-Hide-Filter menu items conditional on `filtering` being supplied
     and Copy All always present */
}
```

<a id="how-taskgrid-changes"></a>
### 5.3 How `TaskGrid` Changes

`TaskGrid.tsx` keeps every one of §5.1's Task-specific pieces, now imported from `DenseDataGrid.tsx` instead of declared locally: `useDenseGridColumns<TaskRecord>` replaces its own inline `filterState`/`withFilter`/etc. (its `governed()` wrapper still applies on top of `withFilter`'s result, unchanged), and the final `return` swaps its own `<Box onContextMenu><DataGrid ...` block for `<DenseDataGrid<TaskRecord> columns={columns} rows={filteredTasks} getRowId={(r) => r.task_id} onRowDoubleClick={openTask} getRowClassName={urgencyRowClassName} sx={urgencyRowSx} filtering={{ filterVisible, onToggleFilterVisible: () => setFilterVisible(v => !v), onResetFilters: () => setFilterState({}) }} isCellEditable={...} onCellClick={...} processRowUpdate={processRowUpdate} onProcessRowUpdateError={...} />`. This is a pure refactor — no behaviour, styling, or sizing changes for any existing `TaskGrid` consumer (`AllTaskPage`, `Project`/`Project`'s embedded grid, `Component`'s embedded grid); the point is moving code, not changing what it does, so the existing manual QA pass for each of those screens (§7) is a regression check, not new testing.

<a id="how-search-uses-it"></a>
### 5.4 How the Search Results Grid Uses It

`SearchPage.tsx` renders `<DenseDataGrid<SearchDisplayRow> columns={resultColumns} rows={displayRows} getRowId={(r) => `${r.type}-${r.id}`} onRowDoubleClick={openResult} />` — plain fixed `GridColDef`s (§4.4), no `filtering` prop (so the context menu shows only Copy All), no editing props at all. It gets the same dense row height, the same dark-grey border, the same header fill, and the same "Copy All" clipboard behaviour as `TaskGrid` for free, with zero duplicated styling code — exactly the payoff this extraction is for.

<a id="implementation-plan"></a>
## 6. Implementation Plan

<a id="server-change"></a>
### 6.1 Server Change

`search.py`: split `q` on whitespace into words (matching V1.2's own tokenising) and require **every** word to match **somewhere** on the row — AND across words, but OR across that type's own searchable columns *within* each word, exactly mirroring `Task.ContainsText`'s own `Description OR DetailedDescription` shape (a word found in Description and a different word found only in DetailedDescription still matches, same as V1.2). Concretely, per word `w`: `(description ILIKE %w% OR detailed_description ILIKE %w%)` for Task, `(name ILIKE %w% OR url ILIKE %w% OR mail_from ILIKE %w%)` for Attachment, a single `ILIKE %w%` for Project/Component/Remark (one searchable column each) — each type's own per-word clause ANDed together across every word `q` splits into. This replaces the current single `%q%` substring per branch — the one place V2's current behaviour is a real, worth-fixing gap relative to V1.2, since doing this client-side instead would be unable to see `mail_from` at all (§3.1). Add a regenerated `openapi.json` afterward per this repo's usual process; no other endpoints touched.

<a id="new-hook"></a>
### 6.2 New API Client Hook

`api/hooks.ts`, alongside the existing `useComponents()`-style reference hooks:

```ts
export interface SearchResultRecord {
  type: "Task" | "Project" | "Component" | "Remark" | "Attachment";
  id: number;
  label: string;
}

export function useSearch(q: string) {
  return useQuery({
    queryKey: ["search", q],
    enabled: q.trim().length > 0,
    queryFn: async () =>
      unwrap<SearchResultRecord[]>(
        await apiClient.GET("/search", { params: { query: { q } } }),
      ),
  });
}
```

(Response typed by hand, matching the existing `WhoAmI`/`Team` precedent in `client.ts` for endpoints with no server-side `response_model`.)

<a id="new-files"></a>
### 6.3 New Files

- `components/DenseDataGrid.tsx` — the shared chrome extracted from `TaskGrid.tsx` (§5.2).
- `features/search/SearchPage.tsx` — the whole screen: controls (§4.2), data flow (§4.3), results grid (§4.4/§4.5/§5.4), following `ProjectDetailPage.tsx`'s "Top Level Projects" page-shell shape (§4.1). One file — this screen has no sub-tree of its own components the way Project/Component Detail's tree does, so there's no `Search`/`Searches` pair to split out.

<a id="build-steps"></a>
### 6.4 Build Steps

1. `components/DenseDataGrid.tsx` (§5.2), extracted from `TaskGrid.tsx`.
2. `TaskGrid.tsx` refactored to build on it (§5.3) — verify every existing consumer (All Tasks, Project's/Component's embedded grids) is visually and behaviourally unchanged before moving on.
3. `search.py` multi-term AND matching (§6.1), plus its own test update in `test_search.py`.
4. `api/hooks.ts`: `useSearch` (§6.2).
5. `features/search/SearchPage.tsx` (§6.3), built on `DenseDataGrid` (§5.4).
6. `windowNav.ts` (`SEARCH_WINDOW_FEATURES`, `windowFeaturesFor`), `App.tsx` (`/search` route under `BareAuthenticatedLayout`), `AppShell.tsx` ("Search" nav button) — all three mirroring `ComponentDetailPlan.md`'s own §5.4 final step exactly.

<a id="testing-approach"></a>
## 7. Testing Approach

Same as every other screen this phase (`Plan.md` §7.1/§7.2): no automated GUI tests yet; manual verification against the real running `rest-api`/seeded data. `test_search.py` gets one new case for multi-term AND matching (e.g. two words that each appear somewhere in a description but never together, expected to *not* match). The `DenseDataGrid` extraction (§5) is a refactor of already-shipped, manually-verified behaviour — re-run the existing manual pass for All Tasks and Project's/Component's embedded grids (filtering, sorting, in-place editing, urgency tinting, delete, Copy All, Reset Filters/Show-Hide Filter) and confirm nothing changed, rather than writing that coverage fresh. For Search itself: search text matching each of the five types individually and in combination; confirm a Team-mismatched item (belonging to a Team the logged-in Person isn't on) never appears regardless of how well it matches; confirm "Include Closed Items" toggles Cancelled/Closed Tasks/Projects (and their Remarks/Attachments) in and out; confirm double-click opens the right window for all five types, including a Remark/Attachment opening its *owner's* window; confirm a Component result's Person column shows its owner (a genuine new capability vs. V1.2, §2.2).

<a id="open-items"></a>
## 8. Open Items for Review

None open — all four were answered by the user and are recorded as decisions where they arise in the design above (also cross-referenced in `Plan.md` §10):

- **`D1.4-73`** (§5): extract `TaskGrid.tsx`'s dense-grid chrome into a shared `DenseDataGrid`, used by both `TaskGrid` and Search's own results grid — not a full V1.2-style generic "any entity" grid engine.
- **`D1.4-74`** (§4.2): the search re-runs only on an explicit trigger (Enter/"Find" button), not live-as-you-type.
- **`D1.4-75`** (§6.1): multi-term matching is a server-side fix in `search.py` — every space-separated word must match somewhere on the row (AND across words), each word itself OR'd across that type's own searchable columns, exactly mirroring V1.2's `Task.ContainsText`'s `Description OR DetailedDescription` shape.
- **`D1.4-76`** (§4.4): Task-typed result rows get the same Urgency-based background tint `TaskGrid.tsx` already computes for its own rows; other types render untinted.
