# ProjectPal V2 — Urgency Calculation Phase Plan

## Contents

1. [Status and Purpose](#status-and-purpose)
2. [Scope for Level 1](#scope)
   - 2.1 [In Scope](#in-scope)
   - 2.2 [Out of Scope / Deferred](#deferred)
3. [Algorithm](#algorithm)
   - 3.1 [Source of Truth](#source-of-truth)
   - 3.2 [Inputs Required, and Where They Already Exist](#inputs)
   - 3.3 [Start/End Date Fidelity: Upgraded, Not Merely Inherited](#inherited-limitation)
4. [Design](#design)
   - 4.1 [Module and Function Shape](#module-shape)
   - 4.2 [Priority-Value Mapping](#priority-mapping)
   - 4.3 [Calendar Days, Not Business Days](#calendar-days)
   - 4.4 [Project Ancestor-Chain Walk](#ancestor-walk)
   - 4.5 [Integration Points](#integration-points)
   - 4.6 [Urgency-to-Colour Mapping](#colour-mapping)
   - 4.7 [Recursive Start/End Date Evaluator](#recursive-schedule)
5. [Implementation Plan](#implementation-plan)
   - 5.1 [Files Touched](#files-touched)
   - 5.2 [Build Order](#build-order)
6. [Testing](#testing)
   - 6.1 [Approach](#testing-approach)
   - 6.2 [Manual Testing](#manual-testing)
7. [Definition of Success](#definition-of-success)
8. [Open Questions (Phase-Specific)](#open-questions)
9. [Decisions (Phase-Specific)](#decisions)
10. [Implementation Outcome Summary](#implementation-outcome-summary)

<a id="status-and-purpose"></a>
## 1. Status and Purpose

**Status:** Done.

Implement the Urgency calculation (`Requirements/KeyConcepts.md` §12) for real, replacing the fixed `100` placeholder both `TaskListPage.tsx` (the All Tasks grid's Urgency column) and `TaskDetailPage.tsx` (its header preview) currently show for every Task. Per explicit instruction, this reproduces §12.1's algorithm exactly — the same constants, the same formula, no re-tuning — not a reinterpretation or a simplification.

This phase was expected to fold entirely into `4_GuiClient/Plan.md` once that phase reached it (that document's own §2.1, and this folder's original placeholder, said as much). It's being tracked here as its own phase instead, now that it's actually starting, since Urgency is a self-contained enough unit of work to plan and land on its own — `4_GuiClient/Plan.md` §2.1/§7.1 are corrected to point here rather than restating that expectation.

<a id="scope"></a>
## 2. Scope for Level 1

<a id="in-scope"></a>
### 2.1 In Scope

- The Urgency score itself (`Requirements/KeyConcepts.md` §12.1), computed client-side, exactly as V1.2's `GUITask.Urgency` (`V1.2/Apps/ProjectPal/ProjectPal/Tasks/GUITask.cs`) computes it — same constants, same rounding, no deviation (`D1.5-1`, §9).
- Wiring the real value into both places it's currently faked: the All Tasks grid's Urgency column (`TaskListPage.tsx`) and Task Detail's header preview (`TaskDetailPage.tsx`).
- A shared, pure, presentation-layer function living in `lib/schedule.ts` alongside the Start/End date derivation it depends on (§4.1) — not duplicated per screen.
- Upgrading `lib/schedule.ts`'s Start/End date computation to the full recursive Task/Project dependency graph (`D1.5-2`, §3.3/§4.7) — no longer a known approximation Urgency merely inherits, corrected as part of this phase. Supersedes `4_GuiClient/Plan.md`'s `D1.4-14`.
- §12.2's Urgency-to-colour mapping (`D1.5-5`, §4.6), used everywhere Urgency's own numeric value is shown (both places above).

<a id="deferred"></a>
### 2.2 Out of Scope / Deferred

- **Team-specific configurable weights for the algorithm.** Already deferred, not by this document — `Claude/Level2_Implementation/Scope.md`, `4_GuiClient/Plan.md` §2.1.
- **Re-tuning any of the algorithm's constants** (the 10-day closed-task decay window, the 60-day time-pressure horizon, the priority-weighting exponents). `Requirements/KeyConcepts.md`'s own `Q-KC-3` raised this as worth validating against real usage eventually; `D1.5-1` (§9) answers it for *this* phase specifically — port them unchanged for now, revisit only if real usage says otherwise.
- **Gantt/plan view rendering.** Urgency is a plausible colour-coding input there (`4_GuiClient/Plan.md` §6.3), but that view doesn't exist yet (Stage 3) — nothing here is blocked on it, and nothing here builds it. `4_GuiClient/Plan.md` §2.1 now carries a standing note for whoever builds it (and any other future Task-showing screen) to actually use Urgency/its colour once this phase lands.

<a id="algorithm"></a>
## 3. Algorithm

<a id="source-of-truth"></a>
### 3.1 Source of Truth

`Requirements/KeyConcepts.md` §12.1 ("Current Urgency Algorithm") is the single canonical statement of the formula — symbols, every constant, both branches (Closed/Cancelled decay and the open-Task priority-×-time-pressure product), and three fully worked numeric examples. It is not restated here (`Claude/Guidelines/document-guidelines.md` rule 2); this document covers the *engineering* of porting it, not the mathematics itself. §12.2 covers the separate Urgency-to-colour mapping (§4.6).

<a id="inputs"></a>
### 3.2 Inputs Required, and Where They Already Exist

Every input §12.1's formula needs is already loaded by both screens that will use it — no new REST API endpoint, query parameter, or schema change of any kind:

| Input | Source |
|---|---|
| Task's `status`, `status_date`, `priority` | `TaskRecord`, already fetched (`useTasks()`/`useTask()`) |
| Task's own `StartDate`/`EndDate` | `lib/schedule.ts`'s existing `computeStartDate`/`computeEndDate` — already computed per-Task in both `TaskListPage.tsx` (`scheduleByTask`) and `TaskDetailPage.tsx` |
| Every ancestor Project's `priority` and `parent_project_id` | `ProjectRecord`, from the already-fetched, unfiltered `useProjects()` list both screens already call — sufficient to walk any Task's ancestry regardless of Team |
| `today` | The client's own current date, no time component (matching §12.1's `today`) |

<a id="inherited-limitation"></a>
### 3.3 Start/End Date Fidelity: Upgraded, Not Merely Inherited

Urgency's "time pressure" factor (§12.1 Step 3) is computed from a Task's `StartDate`/`EndDate`. `lib/schedule.ts`'s existing computation of these was a Stage-2-bounded approximation — one level of predecessor-Dependency awareness, not the full recursive Task/Project dependency graph V1.2 itself actually computes (`4_GuiClient/Plan.md` §3.8/`D1.4-14`) — and this phase was initially going to accept that as an inherited limitation rather than fix it (`D1.5-2`, §9, records the decision not to). Full detail of the real algorithm, read from V1.2's own `Task.cs`/`Project.cs`, is in §4.7; in short:

- A Project's `EndDate` is the *max* of every one of its own direct Tasks' `EndDate` and every direct sub-Project's own `EndDate`, recursively down the whole containment tree — not a value read from a stored field (V1.2's `Project.EndDate` getter, `Libs/DBProjectPal/DBProjectPal/Project.cs`).
- A Task's or Project's `StartDate` is the later of its own natural start and "latest predecessor's `EndDate` + 1 business day" (`LatestPreDepenentEndDate` in both `Task.cs` and `Project.cs`) — and when a node has no predecessor of its own, it *inherits* whatever constraint its parent Project is carrying, rather than having none.
- Both of the above are mutually recursive with each other (a predecessor's own `EndDate` is resolved the same way, all the way down/across the graph), so this needs a proper memoized recursive evaluator over the whole Task+Project graph, not a one-hop lookup (§4.7).

This supersedes `4_GuiClient/Plan.md`'s `D1.4-14` (which accepted the one-level bound for Stage 2) — recorded there as **Superseded by:** `D1.5-2`, per `Claude/Guidelines/ImplementationApproach.md` §3.2's convention for a reopened decision. It also means Stage 3's Gantt view, which `D1.4-14` anticipated would eventually need to build this same full graph walk itself, gets it for free once this phase lands — nothing further for that view to build on this front, just reuse of the upgraded `lib/schedule.ts` functions.

Because `computeStartDate`/`computeEndDate` are the exact same functions both screens already call for their own directly-displayed Start/End date fields (not something newly introduced for Urgency), this upgrade also makes those already-shown values more accurate — a real, positive side effect, but also a wider blast radius than "just add a new Urgency value" would have been: existing, working, currently-displayed dates change behaviour for any Task with a dependency chain deeper than one hop.

<a id="design"></a>
## 4. Design

<a id="module-shape"></a>
### 4.1 Module and Function Shape

A new `computeUrgency` in `lib/schedule.ts`, alongside `computeStartDate`/`computeEndDate`/`formatDdMmmYy` — a plain, pure function, no DOM/React dependency, callable identically from both screens:

```ts
export function computeUrgency(
  task: Pick<TaskRecord, "status" | "status_date" | "priority">,
  ancestorPriorityBand: { min: number; max: number },
  startDate: Date | null,
  endDate: Date | null,
  today: Date = new Date(),
): number
```

`startDate`/`endDate` are passed in rather than recomputed inside `computeUrgency` — both call sites already have them (§3.2) from the exact same `computeStartDate`/`computeEndDate` calls they use for display, so there is no reason to compute Start/End dates twice per Task. `ancestorPriorityBand` (§4.4) is likewise passed in rather than recomputed per Task, since it depends only on `project_id`, not on anything about the individual Task.

<a id="priority-mapping"></a>
### 4.2 Priority-Value Mapping

§12.1's `Pr(x)` table (High=5, MedHigh=4, Med=3, MedLow=2, Low=1, Closed=0, Cancelled=−1) maps directly onto `api/types.ts`'s existing `PRIORITY_LEVELS` strings by name — no renaming or reinterpretation needed (V2's Priority strings were already aligned to these exact semantic names, not V1.2's differently-worded GUI dropdown labels — `8_ValidationAndVerification/Plan.md`'s `Q1.8-4`/`D1.8-4`). One shared lookup table, used identically for a Task's own `priority` and for every ancestor Project's own `priority` — both are drawn from the same 7-value vocabulary. A missing/`null` Priority defaults to `Med` (3) in both cases, matching §12.1 exactly.

<a id="calendar-days"></a>
### 4.3 Calendar Days, Not Business Days

§12.1's two day-count computations (`(today − StatusDate).Days` and `(taskDate − today).Days`) are plain calendar-day differences in V1.2's own code — unlike `lib/schedule.ts`'s existing `addBusinessDays`/`businessDaysBetween` helpers, which the Start/End date derivation itself uses. `computeUrgency` must not reach for those existing helpers here; a plain calendar-day diff is a new, small helper (or inlined directly) — reusing the business-day helpers here would silently produce a different, wrong number from V1.2's on any range spanning a weekend, which is worth calling out explicitly since it would be an easy mistake sitting right next to code that does count business days.

<a id="ancestor-walk"></a>
### 4.4 Project Ancestor-Chain Walk

Walk `project_id → parent_project_id` up to `null`, collecting each Project's Priority (defaulting missing to Med, §4.2), then reverse to root-first order — producing §12.1's `P[0 … n−1]`, from which the `[min, max]` effective-priority band (§12.1 Step 1) is computed once per Task's own `project_id`.

Two small deliberate additions beyond a literal port, both purely defensive and behavior-preserving for any well-formed Project tree:

- **A cycle guard** (a visited-`Set<number>`, bailing out if a `project_id` is revisited). V1.2's own equivalent walk has none — a malformed `parent_project_id` cycle would infinite-loop there too — but V1.2's failure mode is one desktop process hanging; a browser tab hanging (or a shared dev/CI environment) is worse, so this costs nothing for correct data and prevents a much worse failure mode for corrupt data (`D1.5-3`, §9).
- **Memoized per-`project_id`**, not recomputed per Task. Many Tasks typically share the same Project, and the All Tasks grid can show every Task in the system at once (`D-Win-13`) — walking the same ancestor chain again for every one of a Project's Tasks would be wasted, easily-avoidable work. A `Map<number, { min: number; max: number }>` built once per render pass (alongside the existing `projectsById` memo both screens already have) (`D1.5-4`, §9).

<a id="integration-points"></a>
### 4.5 Integration Points

- **`TaskListPage.tsx`**: the Urgency column's `valueGetter`/`getValues` (currently the fixed `100`/`["100"]`, D-Win-12) call `computeUrgency` per row instead, using the already-computed `scheduleByTask` entry and a new per-render ancestor-band map (§4.4). No change needed to the column's own filter/sort wiring — `sortType: "number"` is already correct, and D-Win-16's default Urgency-descending sort starts actually doing something meaningful once real values exist instead of every row tying at 100. The cell also renders its background via `computeUrgencyColour` (§4.6).
- **`TaskDetailPage.tsx`**: the header's hardcoded `100` (added purely as a layout preview once Urgency didn't exist yet) is replaced with one `computeUrgency` call for that Task, coloured via `computeUrgencyColour` (§4.6), using the same ancestor-band walk over the already-fetched, unfiltered `projects` list. Its own `useDependencies(id)` (this Task's direct Dependencies only) needs to become `useAllDependencies()` (already an existing hook, already used by `TaskListPage.tsx`) — the recursive evaluator (§4.7) needs every Dependency in the system to resolve a predecessor's own predecessors, not just this Task's direct links.

<a id="colour-mapping"></a>
### 4.6 Urgency-to-Colour Mapping

`Requirements/KeyConcepts.md` §12.2 fully specifies a separate Urgency-to-colour function (`Utils.Colours.UrgencyColour`) for highlighting. In scope for this phase (`D1.5-5`, §9) even though neither screen had any highlighting mechanism to hang it on before now — the formula is fully specified, pure, and the same shape as §12.1, so there's no reason to build `computeUrgency` without its paired colour function. A new `computeUrgencyColour(urgency: number): string` in `lib/schedule.ts`, returning a CSS colour string (`rgb(...)`) per §12.2's lookup-table algorithm; used as both screens' Urgency display's own background colour (§4.5). `4_GuiClient/Plan.md` §2.1 now carries a standing note for every other screen that shows a Task to use both functions wherever appropriate, not just these two.

<a id="recursive-schedule"></a>
### 4.7 Recursive Start/End Date Evaluator

Replaces `lib/schedule.ts`'s current `computeStartDate`/`computeEndDate`/`approximatePredecessorEndDate` (the one-level approximation, §3.3) with a proper recursive evaluator over the whole Task+Project graph, matching V1.2's own `Task.cs`/`Project.cs` (both implement a shared `ITaskOrProject` interface there — `StartDate`, `EndDate`, and `LatestPreDepenentEndDate` all exist on both, with the same recursive relationship):

- **A single internal recursive function**, handling a Task node or a Project node uniformly (mirroring V1.2's own `ITaskOrProject`), computing `{ startDate, endDate }` for either:
  - `EndDate`: for a Task, `StartDate + Duration` (unchanged from today — `computeDuration` isn't affected by this decision). For a Project, the *max* of every direct child Task's `EndDate` and every direct child Project's own `EndDate` (recursive call), or `undefined` if it has no descendants.
  - `StartDate`: the later of the node's own natural start (a Task's `EarliestStartDate`; a Project's own stored `start_date`) and `LatestPreDepenentEndDate + 1` business day, if that's later.
  - `LatestPreDepenentEndDate`: the *max* of every direct Dependency predecessor's own resolved `EndDate` (recursive call — a predecessor can be a Task or a Project), and the node's own parent Project's `LatestPreDepenentEndDate` (recursive call up the containment tree) — whichever is later. This is the "inherits its parent's constraint when it has none of its own" behaviour (§3.3).
- **Memoized** per node (a `Map` keyed by e.g. `` `task:${id}` ``/`` `project:${id}` ``, built fresh once per render pass) — without this, a Task or Project referenced as a predecessor by many other nodes would have its own chain walked once per reference rather than once total, the same reasoning as `D1.5-4`'s ancestor-band memoization, now applied to a bigger graph.
- **A cycle guard** (a visited-`Set` threaded through the recursion, same reasoning as `D1.5-3`) — this graph has two kinds of edges (Project containment via `parent_project_id`, and explicit Dependency links) rather than the ancestor walk's one, so there's correspondingly more surface for a malformed graph to cycle.
- **Data already available, with one exception**: every Task, every Project, and (once `TaskDetailPage.tsx`'s own hook swap lands, §4.5) every Dependency are already fetched unfiltered by both screens — no new REST API endpoint, exactly as §3.2 already established for Urgency's own other inputs.
- **Public signature**: `computeStartDate`/`computeEndDate` keep call sites unchanged in shape as far as possible (still one call per Task, still returning `Date | null`) — the difference is internal, taking the *full* `allDependencies` list (not just one Task's own direct predecessors) so the recursion can resolve a predecessor's own predecessors, and using the memo/cycle-guard machinery above rather than the old single-hop lookup.

<a id="implementation-plan"></a>
## 5. Implementation Plan

Not yet executed — no code has been written for this phase (§1). `Q1.5-2`/`Q1.5-5` are both now answered (§9) — recorded here as the intended shape to actually build.

<a id="files-touched"></a>
### 5.1 Files Touched

```
V2/gui-client/
├── package.json                                (edited — adds Vitest, D1.5-6)
├── vitest.config.ts                            (NEW)
└── src/
    ├── lib/
    │   ├── schedule.ts                         (edited — recursive Start/End date
    │   │                                          evaluator (§4.7) replacing the one-level
    │   │                                          approximation; computeUrgency,
    │   │                                          computeUrgencyColour, priority-weight
    │   │                                          table, ancestor-band walk (§4.1-4.6))
    │   ├── schedule.dates.test.ts               (NEW — recursive evaluator, §6.1)
    │   └── schedule.urgency.test.ts            (NEW — computeUrgency/computeUrgencyColour, §6.1)
    └── features/tasks/
        ├── TaskListPage.tsx                    (edited — real Urgency column + colour, §4.5)
        └── TaskDetailPage.tsx                  (edited — real Urgency header value + colour;
                                                    useDependencies(id) -> useAllDependencies(), §4.5)
```

<a id="build-order"></a>
### 5.2 Build Order

The recursive Start/End date evaluator (§4.7) comes *before* Urgency itself in build order, since Urgency's own "time pressure" factor depends on it (§3.3) — building Urgency against the old one-level dates first would mean redoing it once the evaluator lands.

1. Rewrite `lib/schedule.ts`'s Start/End date computation to the full recursive evaluator (§4.7) — the internal recursive function, memoization, and cycle guard.
2. Swap `TaskDetailPage.tsx`'s `useDependencies(id)` for `useAllDependencies()` (§4.5).
3. Set up Vitest (`D1.5-6`, §9). Write `schedule.dates.test.ts` first, against the recursive evaluator (§6.1) — a multi-level dependency chain and a Project EndDate aggregated from several descendants are the cases the *previous* one-level code could never have passed, so these are the tests that actually prove the upgrade did something.
4. Add the priority-weight table, the ancestor-band walk (with its own cycle guard and memoization, §4.4), `computeUrgency` (§4.1), and `computeUrgencyColour` (§4.6) to `lib/schedule.ts`.
5. Write `schedule.urgency.test.ts` against `KeyConcepts.md` §12.1's three worked examples (§6.1).
6. Wire `TaskListPage.tsx`'s Urgency column to the real value and colour (§4.5).
7. Wire `TaskDetailPage.tsx`'s header value and colour to the real value (§4.5).
8. Typecheck, build, and verify via the same headless-browser ritual used throughout `4_GuiClient`'s own build — confirm real seeded Tasks show plausible, differentiated Urgency values and colours (not everyone still reading `100`/white), that the All Tasks grid's default Urgency-descending sort (D-Win-16) now visibly does something, and that at least one Task with a multi-level dependency chain in the seed data shows a *different* (and correct, by hand-checking) Start/End date than it did before this phase (§6.2 flags checking whether the current seed data actually has such a chain to test against).
9. This phase's own status moves to Done once the above is verified.

<a id="testing"></a>
## 6. Testing

<a id="testing-approach"></a>
### 6.1 Approach

`gui-client` has no test runner yet (`4_GuiClient/Plan.md` §7.1 anticipated this landing "once it exists to test," in Stage 3 — moot now, since this phase is starting ahead of Stage 3, §1). Two pure, deterministic pieces of logic are worth pinning down with real unit tests before either is wired into a screen:

- **The recursive Start/End date evaluator (§4.7)**, in `schedule.dates.test.ts` — this is genuinely new logic the previous one-level code couldn't do at all, so it needs fixtures the old tests (there weren't any) never had reason to cover: a chain of three or more Tasks linked by direct Dependencies (confirming a predecessor's own predecessor correctly propagates through, not just one hop); a Project with several child Tasks and a sub-Project, confirming `EndDate` is the true max across all of them; a Task with no Dependency of its own under a Project that itself has one, confirming the inherited-constraint behaviour (§3.3); and a deliberately cyclic `parent_project_id` chain, confirming the cycle guard bails out rather than hanging.
- **`computeUrgency`/`computeUrgencyColour` (§4.1/§4.6)**, in `schedule.urgency.test.ts` — `KeyConcepts.md` §12.1 already hands over three fully worked, hand-computed examples (closed-task decay; open task not yet due; open task overdue) ready to use as fixtures verbatim, plus the Priority-defaulting and ancestor-chain cycle-guard behaviours (§4.2, §4.4) worth a few more cases beyond those three, and §12.2's own three worked colour examples for `computeUrgencyColour`.

Vitest is the natural choice for a Vite project (`D1.5-6`, §9) — same build tooling, no separate test bundler to configure.

<a id="manual-testing"></a>
### 6.2 Manual Testing

Log in as a seeded Person (`3_Authentication/Plan.md` §4), open All Tasks, and spot-check a handful of real Tasks' displayed Urgency (value and colour) by hand against §12.1/§12.2's formulas (status, priority, the Project ancestor chain's own priorities, and the Task's own computed Start/End date are all visible elsewhere in the same grid row or in Task Detail) — confirming the wiring, not re-deriving the already-unit-tested math. Confirm the default Urgency-descending sort (D-Win-16) now produces a real, non-trivial ordering.

Separately, check whether the current seed data (`V2/database/seed/`) actually has a Task with a dependency chain deeper than one hop, or a Project with more than one Task/sub-Project contributing to its `EndDate` — the recursive evaluator's (§4.7) unit tests exercise this with synthetic fixtures regardless, but a real, visible before/after difference in the running app (a Task's own Planned Start/End Date changing once the upgrade lands) is worth confirming too if the data supports it; if it doesn't, that's worth noting as a seed-data gap rather than assuming the manual check was performed when it couldn't have been.

<a id="definition-of-success"></a>
## 7. Definition of Success

- `computeUrgency` reproduces `Requirements/KeyConcepts.md` §12.1 exactly, including its rounding, for both the Closed/Cancelled and open-Task branches.
- `computeUrgencyColour` reproduces §12.2 exactly.
- `lib/schedule.ts`'s Start/End date computation is the full recursive Task/Project graph (§4.7), not the one-level approximation — matching V1.2's own `Task.cs`/`Project.cs`, including Project `EndDate` aggregation and inherited dependency constraints.
- Unit tests cover §12.1's three worked examples, §12.2's three worked colour examples, and the recursive evaluator's own multi-level/aggregation/inheritance/cycle-guard cases (§6.1) — all pass.
- The All Tasks grid and Task Detail's header both show real, per-Task Urgency values and colours — no screen still shows the fixed `100`/white placeholder.
- No new REST API endpoint, query parameter, or schema change was needed (§3.2 confirmed this) — `TaskDetailPage.tsx` switching to an already-existing bulk hook (`useAllDependencies`, §4.5) is the only data-fetching change either screen needed.
- `4_GuiClient/Plan.md`'s `D1.4-14` is marked superseded by `D1.5-2`.

<a id="open-questions"></a>
## 8. Open Questions (Phase-Specific)

None currently open — see Decisions below.

<a id="decisions"></a>
## 9. Decisions (Phase-Specific)

- **D1.5-1** (decided 2026-09-12)<br>
  **Question:** `Requirements/KeyConcepts.md`'s `Q-KC-3` — are the Urgency algorithm's specific tuned constants (the 10-day closed-task decay window, the 60-day time-pressure horizon, the priority-weighting exponents) worth revisiting when V2 reimplements this, or ported unchanged?<br>
  **Decision:** ported unchanged, exactly as V1.2's own `GUITask.Urgency` computes them — no re-tuning as part of this phase. Recorded as `D-KC-3` in `KeyConcepts.md` (§20 there), pointing back to this entry as the source. Revisiting the constants themselves stays a live possibility later (`Q-KC-3`'s own "validate against real usage" framing still holds) — just not resolved by, or blocking, this phase.
- **D1.5-2** (decided 2026-09-12)<br>
  **Question:** Should this phase also upgrade Start/End date computation to the full recursive Task/Project dependency graph (§3.3), or ship Urgency now against the existing one-level approximation, accepting that Urgency for Tasks with deeper dependency chains inherits the same known imprecision?<br>
  **Decision:** yes — upgrade it now, as part of this phase, to the full recursive graph exactly as V1.2's own `Task.cs`/`Project.cs` compute it (§4.7). Supersedes `4_GuiClient/Plan.md`'s `D1.4-14` (recorded there as **Superseded by:** `D1.5-2`), which had accepted the one-level bound for Stage 2 and anticipated Stage 3's Gantt view eventually building the full graph itself — that work is now done here instead, ahead of Stage 3, which inherits the upgraded `lib/schedule.ts` functions for free rather than needing to build them.
- **D1.5-3** (decided 2026-09-12)<br>
  **Question:** V1.2's own Project ancestor-chain walk has no cycle guard — does V2's port need one?<br>
  **Decision:** yes — a visited-`Set<number>` bail-out, purely defensive. Behaves identically to a literal port for any well-formed Project tree (no cycle ever exists), and turns a malformed one from "browser tab hangs" into a handled, recoverable case — a strictly better failure mode than V1.2's own equivalent risk, at no cost to correct data.
- **D1.5-4** (decided 2026-09-12)<br>
  **Question:** The All Tasks grid can show every Task in the system at once (`D-Win-13`), and many Tasks typically share a Project — should the ancestor-priority-band walk be memoized per `project_id`, rather than re-walked once per Task row?<br>
  **Decision:** yes — a `Map<number, {min,max}>` built once per render pass, alongside the existing `projectsById` memo both screens already build. Avoids re-walking the same ancestor chain redundantly for every Task in a shared Project, with no behavioural difference from computing it fresh per row.
- **D1.5-5** (decided 2026-09-12)<br>
  **Question:** Is §12.2's Urgency-to-colour mapping in scope for this phase? Neither screen has any highlighting mechanism to hang it on yet — build it now anyway (cheap, fully specified), or leave it until a screen actually wants to render Urgency as a colour?<br>
  **Decision:** yes — build `computeUrgencyColour` now (§4.6) and use it in both `TaskListPage.tsx`'s Urgency column and `TaskDetailPage.tsx`'s header value. `4_GuiClient/Plan.md` §2.1 gains a standing note that any future Task-showing screen (the Gantt view most directly, Stage 4's Project/Component Detail Task lists too) should include Urgency and its colour wherever appropriate, rather than treating this pair as specific to these two screens.
- **D1.5-6** (decided 2026-09-12)<br>
  **Question:** `gui-client` has no test runner at all yet (`4_GuiClient/Plan.md` §7.1) — is introducing one worth doing now, just for this phase, rather than waiting for Stage 3 as originally anticipated?<br>
  **Decision:** yes — Vitest, chosen simply because it shares Vite's own build tooling rather than needing a separate bundler configured for tests. `computeUrgency` is pure, deterministic, and already has three ready-made worked examples in `KeyConcepts.md` §12.1 to use as fixtures — a low-effort, high-value first test to have, and there's no reason a project's first unit test has to wait for a specific, unrelated later Stage to arrive.

See `../ImplementationPlan.md` for how this phase fits into the Level 1 plan.

<a id="implementation-outcome-summary"></a>
## 10. Implementation Outcome Summary

**What was implemented:** exactly §2.1's scope, both open questions resolved in favour of the fuller option (§9). `lib/schedule.ts` gained: a full recursive Task/Project schedule evaluator (`buildScheduleGraph`/`getTaskSchedule`/`getProjectSchedule`, §4.7) replacing the old one-level `computeStartDate`/`approximatePredecessorEndDate`, matching V1.2's `Task.cs`/`Project.cs` exactly — Project `EndDate` aggregated bottom-up over every child Task/sub-Project (excluding Cancelled/Closed ones, a real behaviour only found by reading `Project.EndDate` directly, not anticipated in this document's own original design pass), dependency constraints inherited down the Project hierarchy when a node has none of its own, memoized per node, with a cycle guard; `computeUrgency` (§12.1) and `computeUrgencyColour` (§12.2); `computeTaskRowColour`, a second gap found only by reading `GUITask.Colour` directly — the Priority-based grey-override that decides *whether* a Task's row shows the urgency colour at all, missing from `KeyConcepts.md` §12.2 until this phase found it and added it. Both screens (`TaskListPage.tsx`'s Urgency column, `TaskDetailPage.tsx`'s header) show the real value and colour; `TaskDetailPage.tsx` switched from its own single-Task `useDependencies`/`useTaskResources` to the bulk `useAllDependencies`/`useAllTaskResources` hooks the recursive evaluator needs, substituting its own live (unsaved) form state into the graph so the existing live-preview behaviour (D1.4-14) carries over unchanged.

**Testing:** Vitest introduced as this project's first test runner (`D1.5-6`) — `schedule.urgency.test.ts` (`KeyConcepts.md` §12.1's three worked examples plus Priority-defaulting/cycle-guard/row-colour cases) and `schedule.dates.test.ts` (a multi-hop Dependency chain, Project `EndDate` aggregation including the Cancelled/Closed exclusion, inherited-constraint, and a deliberately cyclic `parent_project_id` chain) — 21 tests, all passing. Verified live against the real seeded/regenerated database (headless-browser session, both screens): real, differentiated Urgency values and matching row/header colours render on both screens; the default Urgency-descending sort (D-Win-16) now does something real.

**Issues that arose:**
- **§12.1's own Worked Example 2 was itself imprecise** — stated `Urgency = 89.1`, but computing the same scenario at full precision (rather than the document's own rounded intermediate, `0.891`) gives `89.0`. Caught only by computing an exact expected value for a unit test rather than trusting the document's stated result verbatim — fixed in `KeyConcepts.md` alongside this phase landing.
- **`KeyConcepts.md` §12.2 was missing `GUITask.Colour`'s Priority-based grey-override entirely** — the initial "verify §12.1/§12.2 against V1.2" pass checked `Colours.UrgencyColour` itself (accurate) but not the caller that decides whether it's even used; found only once actually wiring row colouring into the GUI prompted reading `GUITask.Colour` directly. Fixed in the same document.
- **A real cycle-guard gap**, caught by `schedule.dates.test.ts`'s own cyclic-parent-chain test failing with a real stack overflow on the first attempt: `resolveNode`'s `resolving` guard protects against a cyclic *Dependency* graph, but `resolveLatestPreDependentEnd`'s own separate walk *up* the `parent_project_id` chain had no guard of its own and recursed forever on a cyclic containment chain. Fixed with its own `visitedParents` set.
- **A CSS selector mistake in `TaskListPage.tsx`'s row colouring**: `getRowClassName` puts a class on each row, a descendant of the `<DataGrid>` root `sx` is attached to — `&.urgency-row-N` (a compound selector, no space) only ever matches the root element itself having that class, which never happens, so it silently matched nothing. Caught by checking the actual rendered row background colour rather than assuming the class being present was sufficient; fixed to `& .urgency-row-N` (a descendant selector, with a space).

**Further consideration:**
- The recursive evaluator's own worst-case cost (a very deep/wide Task+Project graph) hasn't been profiled — Level 1's data volumes make this a non-concern for now, same reasoning `4_GuiClient/Plan.md` §3.8 already applied to fetching the whole Project tree.
- Real seeded data (post date-shift, per the same session's separate testing-aid script) now sits far enough in the past relative to actual wall-clock "today" that most open Tasks show very large, saturated-red Urgency values — confirmed correct (verified by hand against the exact algorithm), not a defect, but worth knowing before assuming a screenshot of the running app is showing a calculation bug rather than simply overdue-relative-to-now seed data.
