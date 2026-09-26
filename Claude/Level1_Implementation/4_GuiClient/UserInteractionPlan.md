# Stage 5 — Drag and Drop, Click Semantics, and Interaction Affordances

## Contents

1. [Purpose and Scope](#purpose-and-scope)
2. [What Already Exists (the audit)](#what-already-exists)
    - [Click-to-open: single vs. double, audited screen by screen](#click-to-open-single-vs-double)
    - [The two modifier-key conventions already in play](#modifier-key-conventions)
    - [Two different "this is draggable" affordances, already shipped, already different](#draggable-affordances-already-shipped)
    - [The one existing drop-target affordance](#existing-drop-target-affordance)
    - [The one existing "this row can be clicked to navigate" affordance](#existing-click-to-navigate-affordance)
3. [Design Principle](#design-principle)
4. [Click Semantics Policy](#click-semantics-policy)
    - [A known side effect on `TaskGrid`, confirmed but not yet addressed](#taskgrid-double-click-side-effect)
    - [Editable-cell visual affordance — requested design, not yet built](#editable-cell-visual-affordance)
5. [Interaction Affordances — a shared visual vocabulary](#interaction-affordances)
    - ["This can be clicked to open something"](#clickable-to-open)
    - ["This is a valid drop target, right now"](#valid-drop-target)
    - ["This is draggable"](#this-is-draggable)
6. [Drag Gesture Vocabulary](#drag-gesture-vocabulary)
7. [Stage 5's Actual New Drag-and-Drop Features](#stage-5-new-features)
    - [Reparent a Project (Move)](#reparent-a-project)
    - [Reparent a Component (Move)](#reparent-a-component)
    - [Move a Task into a different Project or Component (Move)](#move-a-task)
    - [Dependency creation, extended to Project/Component Dependencies tabs (Link)](#dependency-creation-extended)
    - [OS-file drag onto an Attachments grid (Attach)](#os-file-drag-attachments)
    - [Already-built, unaffected by this stage](#already-built-unaffected)
    - [Port the Gantt view's Shift+drag bar reschedule from V1.2 (Adjust)](#gantt-bar-reschedule)
8. [Open Questions for Confirmation](#open-questions)
9. [Status Tracker](#status-tracker)
10. [Hints — a fourth affordance, on demand](#hints)
11. [Implementation Plan](#implementation-plan)

<a id="purpose-and-scope"></a>
## 1. Purpose and Scope

Stage 4 (`4_GuiClient/Plan.md` §6.4) is now complete. Its own windows were each built with Stage 2's explicit-control pattern (`D1.4-4`/`D1.4-7`) rather than V1.2's drag-and-drop, deliberately deferring that surface to this stage (`D1.4-40`) once the windows to drag between actually existed.

This document was requested with three explicit requirements beyond "build the drag-and-drop interactions themselves," raised because the app's own history — audited below — already shows the failure mode they're worried about actually happening in already-shipped screens, not just a hypothetical risk:

1. **Gesture consistency** — plain drag, Ctrl+drag, and Shift+drag should each mean a *comparable* thing everywhere they appear, not a different thing per window.
2. **Click consistency** — the same operation shouldn't be a single click in one window and a double click in another.
3. **A consistent visual language** for "this is draggable," "this is a valid drop target," and "this is clickable" — hover feedback and/or a persistent cue, applied the same way everywhere.

All three are treated as prerequisites for Stage 5's own new work, not an afterthought: §4/§5/§6 below establish the vocabulary and shared primitives first, and §7 (the actual new drag-and-drop features) is built on top of them, not built ad hoc again and reconciled later.

<a id="what-already-exists"></a>
## 2. What Already Exists (the audit)

Three real interaction patterns already ship today, built independently, at different points in this project, without a shared convention to check against. Reading them side by side is what actually surfaces the inconsistency the request is worried about — it isn't hypothetical.

<a id="click-to-open-single-vs-double"></a>
### 2.1 Click-to-open: single vs. double, audited screen by screen

| Screen / grid | Click to open | Why |
|---|---|---|
| `TaskGrid.tsx` (All Tasks, Project/Component-embedded Task lists) | **Double** | Single click is already claimed — it starts inline editing on a governed cell (`isCellEditable`/`processRowUpdate`, `TaskGridPlan.md`) |
| Search results (`SearchPage.tsx`) | **Double** | No inline editing at all — this is a plain read-only results list |
| Admin Tools' two integrity-check grids (`AdminPage.tsx`) | **Double** | No inline editing |
| Dashboard's report grid (`DashboardPage.tsx`) | **Double** | No inline editing |
| Teams Management list (`TeamsManagementPage.tsx`) | **Single** | Deliberately changed from double (`D1.4-95`): "this grid has no in-cell editing to conflict with" |
| Sub-Project rows (`Project.tsx`, embedded in Project Detail) | **Single** | No inline editing (`onClick`, not `onDoubleClick`) |
| Sub-Component rows (`Component.tsx`, embedded in Component Detail) | **Single** | Mirrors `Project.tsx` exactly |

Four screens use double-click though none of them have any inline editing to protect — the *only* reason `TaskGrid` needs double-click doesn't apply to them at all. `TeamsManagementPage`, `Project.tsx`, and `Component.tsx` already independently arrived at single-click, for the same underlying reason (`D1.4-95`'s own stated logic), without ever being generalised into a rule the other four could have followed. This is the concrete instance of "the same operation is single-click on one window and double-click on another" the request named — it's already true of *opening an item*, the single most common interaction in the app.

<a id="modifier-key-conventions"></a>
### 2.2 The two modifier-key conventions already in play

- **V1.2's own rule** (`Requirements/UserInterfaceWindows.md` §4, "Ctrl-modified drag effect"): no modifier → Move effect; Ctrl held → Link effect. Per-target, not per-source — a target only ever accepts one of the two. One documented exception: Component reparenting "ignores Ctrl and always offers every effect."
- **V2's own shipped precedent** (`D1.4-10`, Stage 2's cross-window spike, built out for real): Ctrl+drag a Task's title badge onto another Task Detail window's Dependencies tab creates a Dependency. A plain drag (no Ctrl) is cancelled outright at the source (`event.preventDefault()` unless `event.ctrlKey`) — there's no Move counterpart wired up here at all, since this particular source has no Move operation to offer (reparenting a Task already goes through the explicit tree-picker, `D-Win-9`).
- **Shift+drag has no existing V2 use at all — correction, found while actually building `D1.4-124`'s hint tooltips (added 2026-09-26):** this section previously claimed the Gantt view's own bar-reschedule was an "existing V2 use," citing `UserInterfaceWindows.md` §3.7. Checked directly against the real code (no `shiftKey`-gated drag handler anywhere in `PlanPage.tsx`, or anywhere else in `V2/gui-client/src`) while wiring up a hint tooltip for it — there's nothing there to hint at. Re-reading §3.7 itself confirms why: it documents this as a real, working feature of **V1.2** ("This is a real, working, persisting feature," §3.7's own words) — a description of the *old* app, not a record of something already ported to V2. It isn't a Windows-drag-and-drop gesture at all there either: a local mouse-move "rubber-band" gesture (V1.2's own `StartMove`/`FinishMove`) that shifts the bar's own Start Date by the number of business days dragged, floored at dependency predecessors' own latest end date. Nothing in V2 uses Shift for anything today. Moved to `§7`'s own list of new features to build, not left in `§7.6`'s "already built" one — see the new `§7.7` below.

So V2 currently has only *one* of the three gestures genuinely exercised (Ctrl+drag, `D1.4-10`) — plain drag doesn't yet do anything anywhere in V2, and Shift+drag doesn't either — but no written-down rule tying what they'll mean together once built, and this document is the first place that rule is stated.

<a id="draggable-affordances-already-shipped"></a>
### 2.3 Two different "this is draggable" affordances, already shipped, already different

- **Task Detail's own drag source** (`D1.4-10`): a small 24×24 coloured badge next to the Task's title (its own live-editable `<input>` can't double as a drag source), styled `cursor: "grab"` — a persistent, discoverable-on-hover cue via cursor shape.
- **The Gantt view's own draggable rows** (`D1.4-27`/`D1.4-32`): a *dotted hover-highlight* on the row, with `cursor: "default"` — a code comment there explains this was a deliberate choice: "the dotted hover-highlight is what shows a row is draggable here, not the cursor shape."

Both are reasonable in isolation, but they're two different visual languages for the exact same underlying fact ("you can pick this up"), each invented independently for its own screen. A user who learns one doesn't get any benefit from that knowledge on the other screen.

<a id="existing-drop-target-affordance"></a>
### 2.4 The one existing drop-target affordance

`DependenciesPanel.tsx`'s "Depends upon"/"Dependants" lists (the `D1.4-10` drop targets): while a valid drag hovers over them, `outline: "2px dashed", outlineColor: "primary.main", outlineOffset: "-2px", borderRadius: "4px"`. This is the only drop-target affordance in the app today, and it's a good one — MUI-idiomatic, uses the theme's own primary colour, doesn't fight the element's own layout (a dashed *outline*, not a border, so it doesn't shift anything by taking up layout space). §5.2 adopts it as the app-wide standard rather than inventing a new one.

<a id="existing-click-to-navigate-affordance"></a>
### 2.5 The one existing "this row can be clicked to navigate" affordance

`TeamsManagementPage.tsx`'s own Name column: `cursor: pointer` plus `text-decoration: underline` on hover, added specifically because "a plain grid cell otherwise gives no visual hint it opens something." This is the only click-affordance precedent in the app and, like the drag ones above, was invented once, locally, and never generalised.

<a id="design-principle"></a>
## 3. Design Principle

**A gesture's meaning is fixed app-wide by what it *is*, not by which window it happens to run in.** Concretely: before adding any new click or drag interaction, ask "which of the fixed vocabulary items below does this belong to," not "what feels right for this screen." Where an existing screen's own behaviour doesn't match the vocabulary once it's fixed (§2.1's four double-click grids, §2.3's two different draggable cues), that's a defect to fix as part of this stage, not a difference to preserve for compatibility — none of them are load-bearing on any particular gesture; nothing else in the app depends on Search or Admin Tools specifically needing a double click.

<a id="click-semantics-policy"></a>
## 4. Click Semantics Policy

**Rule: opening an item is a single click, everywhere, with exactly one standing exception — a grid whose single click is already claimed by inline cell editing uses double-click instead, and only because of that conflict.**

Today, `TaskGrid` is the only grid in the app with inline cell editing, so it's the only screen the exception actually applies to. If a future screen ever gains inline editing, the same exception applies to it too, automatically, by the same reasoning — this is a *rule*, not a hand-maintained list of screens.

**Changes required** (retrofitting the four grids from §2.1 that don't actually need double-click) — **built, D1.4-123 (added 2026-09-26)**:

| Screen | Change |
|---|---|
| `SearchPage.tsx` | `onRowDoubleClick` → `onCellClick` (no actions-type column to skip, confirmed by checking — the whole row stays the click target) |
| `AdminPage.tsx` (both grids) | Same |
| `DashboardPage.tsx` | Same |
| `TaskGrid.tsx` | **No change** — keeps double-click, for the one reason that actually justifies it |
| `TeamsManagementPage.tsx`, `Project.tsx`, `Component.tsx` | **No change** — already correct |

This does *not* touch `TaskGrid`'s own single-click-starts-edit behaviour on a governed cell, which is unrelated and unaffected.

<a id="taskgrid-double-click-side-effect"></a>
### 4.1 A known side effect on `TaskGrid`, confirmed but not yet addressed (added 2026-09-26)

Double-clicking a governed (editable) cell in `TaskGrid` doesn't just open Task Detail — it does *both* things at once, confirmed directly against the real code (`TaskGrid.tsx`'s own comment beside `onCellClick`): the first of the double-click's own two constituent clicks already starts inline editing (independent of, and unaware of, the double-click that follows it), and the double-click itself still opens Task Detail on top of that. Neither handler suppresses the other — they're two independent listeners reacting to the same physical gesture.

**Is it possible to suppress the single-click's own action once a double-click is recognised?** Only via one of two known techniques, and both cost something real:

- **Delay, then execute** — hold the click's own action behind a short timer (roughly the browser's own double-click threshold, ~200-300ms), cancelling it if a second click arrives in time. This is the only way to *prevent* the edit from starting at all, but it adds felt latency to every ordinary single click too, not just the rare double-click case — directly against this grid's own stated design value of an immediate, responsive inline-edit loop (`TaskGridPlan.md`'s "immediate per-cell save," `D1.4-49`). Not recommended for that reason.
- **Let it start, then immediately undo it** — keep today's instant single-click edit start, but if a `dblclick` follows shortly after, programmatically exit edit mode again (`apiRef.current.stopCellEditMode`) right as Task Detail opens. No added latency for the ordinary case, but produces a brief visible flicker (the cell's own editor appears, then disappears) on the rare double-click-an-editable-cell case. The same "let both things happen, then reconcile the outcome afterward" shape this same file already uses for a different double-click side effect — the sort-history undo (`D1.4-79`, `components/DenseDataGrid.tsx`) — so it wouldn't be a new pattern in this codebase, just applied to a second, unrelated conflict.

Neither is built. Recorded here as a confirmed, real side effect and the two ways to address it, so Stage 5 can decide on it deliberately rather than rediscover it from scratch.

<a id="editable-cell-visual-affordance"></a>
### 4.2 Editable-cell visual affordance — **built (`D1.4-131`, §11 group C)**, corrected 2026-09-26

Follows directly from §4.1's own finding: `TaskGrid` had no visual cue at all distinguishing an editable cell from a non-editable one — not per-*column* (only 8 of the grid's columns are ever inline-editable, `GOVERNED_FIELDS` — 9 briefly, until Detailed Description was removed again at `D1.4-129`, see §10.7), and not per-*row* within those 8 either (`canEditTaskField`'s tiered permission rule, `lib/permissions.ts` — the record's own owner can edit every governed field, someone merely assigned as a Resource can only edit Status *in the grid* (Detailed Description is also part of that same tier-3 permission, but is Task-Detail-only since `D1.4-129`, not inline-editable here at all any more), Owner/Tentative Resource Assignment are Team-Lead-only, everyone else gets nothing). The only cursor change a user saw before this — an I-beam over rendered text — was native browser default text-hover behaviour, unrelated to `isCellEditable`, and appeared over *any* cell's text regardless of whether it was actually editable. Reported as genuinely confusing, since there was no way to tell which is which without clicking and finding out.

**Design, two parts, confirmed with the user, now built:**

1. **A persistent (non-hover) cue**, driven by the same `isCellEditable`/`isEditableCell` result already computed today (`TaskGrid.tsx`) — not a second, separate permission concept to keep in sync with it. A cell that isn't editable (whether because its whole column is never governed at all — Description, Project, Component, Resources, ... — or because *this row's* permission check fails for an otherwise-governed column, e.g. Status on a colleague's Task) is subtly dimmed: its text colour desaturated, and whatever urgency-tint background colour is showing through that cell (`computeTaskRowColour`) desaturated along with it. A genuinely editable cell is left completely unstyled — full-strength text, full-strength urgency tint — so it's the *editable* cells that look normal, and the (for most users, on most rows, most) non-editable ones read as visibly, if subtly, inert. **Built as** a single CSS `filter: grayscale(1) opacity(0.55)` on the cell (`components/DenseDataGrid.tsx`'s new `NOT_EDITABLE_CELL_CLASS`/`notEditableCellPaletteSx()`) — desaturates *anything* rendered inside it, text and inherited background alike, in one declaration, replacing the old `task-grid-readonly-cell` grey-background treatment entirely.
2. **A hover-only cursor swap, layered on top of (1), not replacing it.** Every cell gets an explicit resting cursor — not the browser's own native default — since every cell, editable or not, already supports the *row's* own double-click-to-open-Task-Detail (`onRowDoubleClick`/`openTask`). A cell where `isEditableCell` is true swaps that to `cursor: text` instead, specifically flagging the one *additional* action available only there (a single click starts editing it). This gives a genuine two-level cursor language matching the grid's own two-level click behaviour — text means "single-click also edits this one," the plain arrow means it doesn't — rather than one generic affordance that can't tell the two apart. **Built as** a local `task-grid-editable-cell` class (`TaskGrid.tsx`'s own `urgencyRowSx`), applied alongside the shared class above.

**Refinements after first use (`D1.4-132`, added 2026-09-26)** — both points above were built once, tried, and reported back on directly:

- **The urgency-tint background wasn't visibly dimming at all**, only the text was. Root cause: point 1 above was first built as a single `filter: grayscale(1) opacity(0.55)` on the cell — but the urgency tint is painted on the *row* (`getRowClassName`/`urgencyRowPaletteSx`), an ancestor of the cell, and `filter` only ever affects an element's *own* rendered box; a transparent cell showing a colour through from an ancestor behind it has nothing of that colour in its own filtered layer to desaturate. Text dimmed (it's the cell's own content) while the tint, painted a layer further back, didn't. Fixed by splitting into two independent CSS properties instead of one `filter`: `color` (dims the cell's own text — every `TaskGrid` cell's text is plain, near-black, so a lower-alpha black reads as dimmed without needing an actual grayscale) and `bgcolor`, a real paint on the cell's *own* box this time, laid as a translucent grey wash *over* whatever the row's tint shows through underneath it — greying it by ordinary alpha compositing, regardless of what that colour actually is.
- **The text dimming itself was too strong.** Halved the distance back toward full strength: `notEditableCellPaletteSx()` now sets `color: "rgba(0,0,0,0.6)"` (was an effective ~0.48 alpha under the old filter) and a "slightly" greyed `bgcolor: "rgba(120,120,120,0.22)"` for the background, per the user's own two separate asks. **Halved again at `D1.4-133`**, to `bgcolor: "rgba(120,120,120,0.11)"` — still too strong even at 0.22. **Made a Settings preference at `D1.4-137`** ("Uneditable dimming," `None`/`Low`/`Medium`/`Max`) rather than guessing at a fourth fixed amount — `Low` is `D1.4-133`'s own `0.11`, `Medium` is `D1.4-132`'s original `0.22`, `Max` doubles it again to `0.44`, and `None` turns the whole affordance off.
- **The resting cursor read wrong.** `cursor: pointer` — a hand with a pointing finger — implies "this itself is a clickable control," which isn't true of most cells; changed to `cursor: default` (the plain arrow), leaving the editable-cell `cursor: text` override unchanged.

**Scope:** every cell in `TaskGrid`, not just the 8 governed columns — a column that's never inline-editable at all is exactly as "not editable" as a governed column this row's own permissions happen to block, and reads the same way; distinguishing "never editable" from "not editable for you, here" visually wasn't asked for and would only add noise for no benefit. The leading Actions/delete column is the one deliberate exception — it's a button, not a field, and already has its own two-state (enabled/disabled) icon styling (`D1.4-98`-`D1.4-100`) this would only muddy. Applies identically to every `TaskGrid` instance (All Tasks and the embedded Project/Component ones) — `isEditableCell` is already the single shared source of truth for this everywhere, and neither embedding passes any prop that could override or bypass it.

**Implementation shape, as built**: a single `cellClassName` applied uniformly to every column (except the actions column) where `columns` is assembled (`TaskGrid.tsx`), returning the editable or not-editable class per `isEditableCell(params.row, params.field)` — not repeated per column inside `allColumnDefs`. The dimming class itself lives in `components/DenseDataGrid.tsx`, alongside its other shared chrome constants, so any future grid built on the same chrome can reuse it; the cursor-swap classes are local to `TaskGrid.tsx`, since no other grid combines a row-level and a cell-level click action the way this one does.

`tsc -b`/`vitest` (94, unchanged — a styling/cell-classing change, no new pure logic)/`build`/`lint` all clean. Verified interactively (scripted Playwright, computed styles read directly from the DOM, both before and after `D1.4-132`'s revision): on All Tasks, an owned Task's Status/Priority cells come back `color: rgba(0,0,0,0.87)`/`bgcolor: transparent`/`cursor: text`, while Resources/Description/ID come back `color: rgba(0,0,0,0.6)`/`bgcolor: rgba(120,120,120,0.22)`/`cursor: default`, screenshotted to confirm the tint now visibly greys on the non-editable columns. The embedded Project/Component `TaskGrid` instances weren't separately screenshotted this session — confirmed instead by reading both call sites (`Project.tsx`/`Component.tsx`), neither of which passes `sx` or any column override that could affect this — you've since verified C4 directly (§11).

<a id="interaction-affordances"></a>
## 5. Interaction Affordances — a shared visual vocabulary

Three states, one consistent treatment for each, built as shared primitives so a new screen reuses them rather than inventing a third variant of something the app already has two of.

<a id="clickable-to-open"></a>
### 5.1 "This can be clicked to open something" — built (D1.4-123, added 2026-09-26)

Adopt `TeamsManagementPage.tsx`'s own existing pattern as the app-wide standard: `cursor: pointer` plus `text-decoration: underline` on hover, scoped via `data-field` to just the cell(s) that actually open something (never the whole row indiscriminately, since an Actions cell alongside it isn't a "click to open" target).

**Correction, found while actually building this:** the paragraph originally here claimed `Project.tsx`/`Component.tsx`'s own sub-item rows had "no hover cue at all." Checked directly against the real code before touching it — that was wrong. Both already had `cursor: "pointer"` plus `"&:hover": { textDecoration: "underline" }` on the Project/Component name. They were refactored onto the new shared primitive below (identical appearance, no behaviour change) rather than left as-is, purely for the DRY benefit — not because they were broken.

**Shared primitives built**, two of them (a grid cell and a plain element need different mechanisms — a grid cell has no `sx` of its own, so the rule has to live on the grid's own root via a `data-field` selector; a plain element just takes the style directly):
- `clickableCellSx(field?: string)` in `components/DenseDataGrid.tsx`. `field` omitted means "every cell in the row" — used by Search/Admin Tools/Dashboard (§4's retrofit, below), none of which single out one column the way `TeamsManagementPage.tsx`'s own Name-only affordance does; a specific field keeps that narrower scope for `TeamsManagementPage.tsx` itself, refactored onto this primitive too.
- `CLICKABLE_SX` in `components/DenseField.tsx` — the plain-element form, used by `Project.tsx`/`Component.tsx` (refactored) and `FieldTreePicker`'s own breadcrumb (new, below).

**Retrofit, all built:** `SearchPage.tsx`, `AdminPage.tsx` (both grids), `DashboardPage.tsx` converted from `onRowDoubleClick` to `onCellClick` + `clickableCellSx()` (none of the three have an actions-type column to skip, confirmed by checking, so the whole row stays the click target exactly as before). `TeamsManagementPage.tsx`, `Project.tsx`, `Component.tsx` refactored onto the shared primitives with no behaviour change.

**Extended beyond the original scope of this section, per a direct user request to action the app's own navigation consistency as a whole (not just this section's original click-semantics retrofit):**
- `FieldTreePicker` (`components/DenseField.tsx`) gained `onBreadcrumbClick` (replacing the double-click-only `onBreadcrumbDoubleClick` `D1.4-119` had added), styled with `CLICKABLE_SX` whenever given. `TaskDetailPage.tsx`'s own Project/Component fields converted from double- to single-click — nothing else competes for a single click on that field, so there was no `TaskGrid`-style conflict forcing double-click there.
- `ProjectDetailPage.tsx`'s/`ComponentDetailPage.tsx`'s own "Parent" pickers gained `onBreadcrumbClick` for the first time — previously no click action at all, now single-click opens the parent Project's/Component's own Detail window, matching Task Detail's own pickers exactly.
- **`PlanPage.tsx`'s own Gantt bar/label double-click was deliberately left as double-click** — a new, second stated exception alongside `TaskGrid`'s own, and for the identical underlying reason: that same row's label already uses a single click (`mousedown`) to start a drag-to-reorder (`D1.4-27`), so single-click can't also mean "navigate" without colliding with the drag-start gesture. Confirmed with the user rather than assumed.

`tsc -b`/`vitest` (94, unchanged — no new tests, a UI-affordance/click-wiring change with no new pure logic to test)/`build`/`lint` all clean; no server changes.

<a id="valid-drop-target"></a>
### 5.2 "This is a valid drop target, right now"

Adopt `DependenciesPanel.tsx`'s own existing pattern (§2.4) as the app-wide standard: `outline: "2px dashed", outlineColor: "primary.main", outlineOffset: "-2px", borderRadius: "4px"`, applied only while a compatible drag is actually hovering (`onDragOver`/`onDragLeave` toggling local state), never as a persistent cue — a drop target only needs to announce itself once a drag that could actually land there is already in progress.

**New shared primitive**: `DROP_TARGET_ACTIVE_SX` exported from `lib/dnd.ts` (which already exists, currently holding just `TASK_DRAG_MIME_TYPE`) — a plain constant, not a hook, since the actual `onDragOver`/`onDragLeave`/`onDrop` wiring differs enough per target (what MIME type it accepts, what mutation it calls, whether it needs the auto-tab-switch behaviour) that a one-size hook would just be a thin wrapper fighting each call site's own real differences. `DependenciesPanel.tsx` itself is refactored to import this constant rather than keep its own private copy, so there's exactly one definition once this stage lands, not one original plus new callers that merely match it by eye.

<a id="this-is-draggable"></a>
### 5.3 "This is draggable"

**Resolve the §2.3 split.** Recommendation: keep both cursor treatments, but tie the *choice* between them to a stated rule rather than per-screen taste, so the next screen doesn't have to guess: a **discrete, small drag handle** (an icon/badge that is not also the row's own primary content — Task Detail's own title badge is exactly this shape) gets `cursor: "grab"`, since the cursor change is scoped to a small, deliberate target the user is already looking at; a **whole draggable row** in a dense list (the Gantt view's own bars) keeps `cursor: "default"` plus the dotted hover-highlight, since changing the cursor for an entire row the user might also be hovering for other reasons (reading its label, right-clicking it) is a heavier-handed signal than the row-highlight already gives for free. This isn't declaring one of the two existing choices wrong — it's naming *why* each one is right for its own shape, so future screens pick the same way rather than by feel.

**New shared primitives**, both in `lib/dnd.ts`:
- `DRAG_HANDLE_SX` — the small-badge treatment (`cursor: "grab"`), for any future discrete drag handle (e.g. a Project/Component row's own drag handle, §7.1).
- `draggableRowHighlightSx(isHovered: boolean)` — the Gantt's own dotted-highlight-on-hover treatment, extracted so a future dense-list drag source (e.g. reordering rows somewhere other than the Gantt, should that ever come up) doesn't reinvent it.

Neither existing call site (`TaskDetailPage.tsx`'s badge, `PlanPage.tsx`'s row highlight) is behaviourally changed — both are refactored to import the shared constant instead of keeping their own private, now-duplicate definition.

<a id="drag-gesture-vocabulary"></a>
## 6. Drag Gesture Vocabulary

A fixed, small set of *intents* — not "what does dragging do on this screen," but "what does holding this key, while dragging, always mean, regardless of which screen it happens on."

| Gesture | Intent | What it does |
|---|---|---|
| **Plain drag** | **Move** | Relocates the dragged item into the drop target, removing it from wherever it was before (reparenting, moving a Task into a different Project/Component) |
| **Ctrl+drag** | **Link** | Creates an association/reference between the dragged item and the drop target, without moving the dragged item at all (Dependency creation) |
| **Shift+drag** | **Adjust** | Changes a property of the dragged item *itself*, in place, via the gesture's own motion — no hierarchy change, no link created (the Gantt's own bar reschedule, a V1.2 feature **not yet ported to V2** — corrected 2026-09-26, see `§2.2`/`§7.7`) |
| OS file drag (Attachments) | **Attach** | A deliberate, stated exception — dragging *from the operating system*, not from elsewhere in the app, so there's no app-internal modifier-key vocabulary to apply; a user dragging a file out of Explorer isn't thinking in this app's own Move/Link/Adjust terms at all |

**This table is the actual deliverable of the "comparable behaviour" request** — not "every drag interaction supports all three gestures," which isn't true and doesn't need to be (Dependency creation is Link-only; reparenting is Move-only; nothing currently needs both on the same drag source at once, unlike V1.2's own per-source "Ctrl picks Move vs. Link" branching, which V2 doesn't need to reproduce since none of the new interactions share a single drag source with two different valid targets the way V1.2's grid rows did). What matters is that **wherever a gesture *is* supported, it always means the same thing**: Ctrl always means "create a link, don't move anything"; plain drag always means "relocate this"; Shift always means "adjust this item's own properties in place." A user who learns Ctrl+drag creates a Dependency in Task Detail (already true today) can walk up to a Project's own Dependencies tab once Stage 5 extends the same gesture there (§7.4) and already know what it does, without re-learning anything.

**V1.2's own Component-reparenting exception (§2.2, "ignores Ctrl") is deliberately not carried forward.** Making Component reparenting behave identically to Project reparenting (plain drag only, since Ctrl is reserved for Link everywhere else) removes a special case rather than preserving one — keeping it would be exactly the kind of per-window inconsistency this whole document exists to close, for a V1.2 quirk that was never a considered design choice to begin with (nothing in `UserInterfaceWindows.md` explains *why* Component was the one exception).

<a id="stage-5-new-features"></a>
## 7. Stage 5's Actual New Drag-and-Drop Features

Each mapped against §6's vocabulary and built on §5's shared primitives from the start.

<a id="reparent-a-project"></a>
### 7.1 Reparent a Project (Move)

Drag a Project row (`Project.tsx`, in the tree — same rows §4/§5.1 already made single-click-to-open) onto a different Project row to reparent it there. Drag handle: a small icon (`DRAG_HANDLE_SX`, §5.3) beside the row's own name — not the whole row, since the whole row is already a single-click-to-open target (§4) and a `draggable` attribute on the same element a plain click opens would make every ordinary click-to-open attempt also register as a (near-instant, sub-pixel) drag start, which browsers handle inconsistently. Drop target: the row being dropped onto gets `DROP_TARGET_ACTIVE_SX` (§5.2) while a compatible drag hovers it. Mutation: the existing `useReparentTask()`-shaped hook this project's own convention already establishes (`D1.4-7`) — here, `useReparentProject()`, calling the same PATCH the explicit tree-picker (`D-Win-9`) already uses. A circular-reparent guard (dropping a Project onto its own descendant) blocks with an inline message, mirroring V1.2's own circular-dependency guard shape (`UserInterfaceWindows.md` §3.2's Drag and Drop Behaviour).

<a id="reparent-a-component"></a>
### 7.2 Reparent a Component (Move)

Identical shape to 7.1, on `Component.tsx`'s own rows — deliberately not special-cased (§6's own call).

<a id="move-a-task"></a>
### 7.3 Move a Task into a different Project or Component (Move)

Drag a Task row (in `TaskGrid`, embedded within Project/Component Detail) onto a Project or Component row. Since `TaskGrid` rows already need double-click to open (§4's one exception) and single-click to start editing a governed cell, the drag handle can't be a whole-row `draggable` either, for the same reason as 7.1 — a small handle in the row (e.g. beside the existing Delete icon in the Actions column) is the source. Drop target styling and reparent-guard shape mirror 7.1/7.2. Mutation: `useMoveTask()` (or reuse a suitably generalised `useReparentTask()`, if the existing Task-move mutation from `D1.4-7`'s own foundational work already fits — confirm against `api/hooks.ts` before adding a new one).

<a id="dependency-creation-extended"></a>
### 7.4 Dependency creation, extended to Project/Component Dependencies tabs (Link)

Extends `D1.4-10`'s own already-shipped mechanism (§2.2) — same `TASK_DRAG_MIME_TYPE`/Ctrl+drag/auto-tab-switch/dashed-outline shape, same `useCreateDependency()` mutation, now also reachable by dragging onto a Project's or Component's own Dependencies tab (`DependenciesPanel.tsx` is already shared across Task/Project/Component per `D1.4-46`), not just another Task Detail window. No new vocabulary, no new visual language — this is the proof that §5/§6's shared primitives actually generalise, since it's the same interaction landing on a target type it wasn't originally built for.

<a id="os-file-drag-attachments"></a>
### 7.5 OS-file drag onto an Attachments grid (Attach)

A real drop target (`AttachmentsPanel.tsx`), but the *source* is the operating system, not another part of this app — no modifier-key vocabulary applies (§6's stated exception). Gets the same `DROP_TARGET_ACTIVE_SX` hover treatment as every other drop target in §5.2, for the same reason: a user should always get the same visual confirmation that a drop is about to land somewhere valid, regardless of what's on the other end of the drag.

<a id="already-built-unaffected"></a>
### 7.6 Already-built, unaffected by this stage

- **Ctrl+drag Task→Task Dependency creation** (`D1.4-10`) — unchanged; already conforms to §6's own vocabulary (it's what §6 was grounded in).
- **Gantt row reorder** (`D1.4-27`) — a plain, in-list drag, but reordering *within* one list rather than moving *between* containers, so it doesn't fit neatly into Move/Link/Adjust at all; flagged here as a genuine open question (§8) rather than silently forced into one of the three.

<a id="gantt-bar-reschedule"></a>
### 7.7 Port the Gantt view's Shift+drag bar reschedule from V1.2 (Adjust) — corrected into this section 2026-09-26

**Not already built, despite what an earlier version of §2.2/§6/§7.6 claimed** — see §2.2's own correction. This is genuinely new V2 work, moved here from the "already-built" list it was wrongly recorded in.

V1.2's own behaviour (`UserInterfaceWindows.md` §3.7, `Libs/PlanDisplay/Task.cs`/`Project.cs`): Shift-dragging a Task or Project bar horizontally is **not** a Windows drag-drop operation — a local mouse-move "rubber-band" gesture (`StartMove`/`FinishMove`) that, on release, shifts the bar's own Start Date by the number of business days dragged (permission-gated). For a Task, the new date is floored at its dependency predecessors' own latest end date, unless the shift is a delay. This is the one directly-editable interaction V1.2's own Plan Display offers at all — everything else there is read-only.

Porting this to `PlanPage.tsx` needs: a `mousedown`+`shiftKey` gesture on a bar (distinct from the *label's* own plain-mousedown row-reorder drag, §7.6, and from the bar's own plain click/double-click, §4), a live preview of the bar's shifted position while dragging (matching the horizontal-zoom-anchor precision already built for this view, `D1.4-111`), the same dependency-predecessor floor V1.2 enforces, and the same permission gate `canEditTaskField`/`canEditOwnedRecord` (`lib/permissions.ts`) already governs every other Task-field edit with. Not designed further here — flagged as real, scoped Stage 5 work, not assumed trivial just because V1.2 already has a working reference implementation to port from.

<a id="open-questions"></a>
## 8. Open Questions for Confirmation

1. **Gantt row reorder's own category.** §7.6 flags it as not cleanly Move/Link/Adjust. Recommendation: treat "reorder within the same list" as its own, fourth, narrowly-scoped exception (like OS-file-drag/Attach) rather than stretching Move to cover it — a reorder never removes an item from a *different* container the way every other Move in §7 does.
2. **Whether 7.1-7.3's drag handle is a new icon, or reuses an existing one already in that row** (e.g. `TeamsManagementPage.tsx`'s own row icons already sit at `rgba(0,0,0,0.87)`/`0.18` per `D1.4-98`-`D1.4-100`'s own established convention) — a genuine visual-design call best confirmed against a real rendered screen rather than decided from a text description alone.
3. **Sequencing**: recommend landing §4/§5/§6 (the consistency retrofit) as one self-contained pass first, verified and committed on its own, before starting any of §7's new interactions — so the new work is built on the corrected foundation from day one rather than needing its own follow-up retrofit.

<a id="status-tracker"></a>
## 9. Status Tracker (added 2026-09-26)

One row per window/screen this document actually touches, consolidating what's scattered across §4-§8 above into a single at-a-glance checklist. Several rows below depend on §5's shared primitives (`clickableCellSx`, `DROP_TARGET_ACTIVE_SX`, `DRAG_HANDLE_SX`, `draggableRowHighlightSx`) existing first — per §8's own recommended sequencing (item 3), those land once, as their own pass, before any row below points at them, rather than each being built independently and reconciled afterward.

| Window / screen | What needs to be done | Status |
|---|---|---|
| `TaskGrid.tsx` (All Tasks; embedded in Project/Component Detail) | §4.1: decide and build a fix for double-click-on-an-editable-cell also starting (then abandoning) an inline edit. §4.2: build the editable-cell visual affordance (persistent dimming on non-editable cells, hover cursor swap on editable ones). §7.3: add drag-to-move a Task onto a different Project/Component. | Not started building — §4.1/§4.2 designed in this document; §7.3 outlined in §7.3 only |
| `SearchPage.tsx` | ~~§4: `onRowDoubleClick` → `onCellClick` (single-click to open). §5.1: apply the shared clickable-cell affordance once opening is single-click.~~ | **Done (D1.4-123)** |
| `AdminPage.tsx` (both integrity-check grids) | ~~Same as Search: §4 single-click retrofit, §5.1 affordance.~~ | **Done (D1.4-123)** |
| `DashboardPage.tsx` | ~~Same as Search: §4 single-click retrofit, §5.1 affordance.~~ | **Done (D1.4-123)** |
| `TeamsManagementPage.tsx` | ~~§5.1: refactor its own inline cursor/underline styling to the shared `clickableCellSx` primitive once it exists — no behaviour change, it's already correct.~~ | **Done (D1.4-123)** |
| `Project.tsx` (sub-Project rows, in Project Detail) | §5.1 affordance **done (D1.4-123)** — turned out to already be correct, refactored onto the shared primitive regardless. §5.3/§7.1: add a drag handle and build "reparent a Project" (Move) — remaining. | §5.1 done; drag feature not started |
| `Component.tsx` (sub-Component rows, in Component Detail) | Same shape as `Project.tsx`: §5.1 **done (D1.4-123)**; §5.3/§7.2 "reparent a Component" (Move) remaining. | Same as `Project.tsx` |
| Task Detail (`TaskDetailPage.tsx`) | §5.3: refactor its own drag-handle badge styling to the shared `DRAG_HANDLE_SX` constant — no behaviour change, already correct (`D1.4-10`) — remaining. Project/Component fields' own double-click **converted to single-click + underline (D1.4-123)**. | Drag-handle refactor pending; navigation fields done |
| Project/Component Detail's own "Parent" pickers | ~~Had no click action at all — add single-click-to-open-parent, matching Task Detail's own Project/Component fields.~~ | **Done (D1.4-123)** — new, wasn't in this tracker before |
| Plan/Gantt view (`PlanPage.tsx`) | §5.3: refactor its own dotted hover-highlight styling to the shared `draggableRowHighlightSx` — no behaviour change — remaining. Bar/label double-click **confirmed staying double-click (D1.4-123)** — no longer an open question, §4.1-style exception now stated in §5.1. §8 item 1: decide how row-reorder-within-a-list fits the Move/Link/Adjust vocabulary (or confirm it's its own fourth exception) — still open. §7.7: Shift+drag bar reschedule (V1.2 port) — genuinely not built, corrected out of a wrong "already built" claim (`D1.4-124`) — still to build. | Refactor pending; double-click exception settled; row-reorder categorisation still open; bar reschedule not built (corrected) |
| `DependenciesPanel.tsx` | §5.2: refactor its own drop-target outline styling to the shared `DROP_TARGET_ACTIVE_SX` constant — no behaviour change on Task Detail, already correct there (`D1.4-10`). §7.4: confirm/extend Ctrl+drag Dependency creation actually reaches this panel's own Project/Component-embedded instances, not just Task Detail's. | Correct on Task Detail; refactor pending; Project/Component reach unconfirmed |
| `AttachmentsPanel.tsx` | §7.5: build the new OS-file-drag drop target, styled with the shared `DROP_TARGET_ACTIVE_SX`. | Not started |

<a id="hints"></a>
## 10. Hints — a fourth affordance, on demand (`D1.4-124`, added 2026-09-26)

§5's own three affordances (clickable/drop-target/draggable) are always-on visual cues. This is a different kind of thing entirely: an explicit, opt-out **tooltip** naming an element's own available gesture(s) and what each does — e.g. "Double-click: Open its Task Detail window." — gated on a new Settings-window preference, "Hints" (`On`/`Off`, default `On`, `lib/settings.ts`). Requested directly, after §4.1/§4.2's own investigation made it obvious just how much of this app's own gesture vocabulary has no discoverability at all beyond already knowing it's there. (Hint-text phrasing and styling were refined once in use — see §10.4.)

### 10.1 The setting

`lib/settings.ts`'s `getHintMode`/`setHintMode`/`HINT_MODES` (`"On"`/`"Off"`), alongside the existing `NewWindowMode` (`D1.4-122`) in the same Settings window. **Different from "New Window" in one important way**: "New Window" only matters at the moment a *new* window opens, so reading it fresh, once, at that moment is enough. "Hints" affects tooltips *continuously rendered by whatever window is already open* — a change made in the Settings window has to reach those windows immediately, not just the next one opened. `useHintsEnabled()` (a hook, not a plain getter) is what every hint-rendering call site actually uses, and it listens for the browser's own native `storage` event — which fires in every *other* open window the moment `localStorage` changes in one of them — rather than needing any new pub/sub mechanism of this app's own. The Settings window itself never renders hint tooltips, so it never needs to listen to its own writes.

### 10.2 Three implementation shapes, one per UI paradigm

A hint can't be attached the same way everywhere — what a "tooltip" even *is* differs by what's being wrapped:

1. **A plain element** (a `Box`, `IconButton`, anything that can hold a ref) — `components/HintTooltip.tsx`, wrapping `children` in a real MUI `Tooltip` when hints are on, or rendering `children` completely unwrapped (no stray wrapper, no mounted Tooltip machinery at all) when they're off. Used by `Project.tsx`/`Component.tsx`'s own name spans and collapse chevrons, `FieldTreePicker`'s breadcrumb (a new `breadcrumbHint` prop, combined with the existing "show full path when truncated" native `title` into one tooltip rather than two competing ones on the same element), Task Detail's own Ctrl-drag badge, and `DependenciesPanel.tsx`'s two drop-target lists.
2. **A MUI DataGrid** — a cell has no `sx`/ref of its own to wrap the way a plain element does, so `components/DenseDataGrid.tsx` gained a `hint?: string` prop instead, applied to the grid's own *outer container*: a plain native `title` at first (D1.4-124); briefly a `HintTooltip` wrap of that container (D1.4-127); now a cursor-following custom tooltip (`gridHintTooltip`, D1.4-128, §10.6) — the `HintTooltip` wrap anchored to the *whole grid's own bounding box*, which rendered off-screen on a full-page-height grid (`AllTaskPage.tsx`'s `fillHeight`), the same failure mode already known to rule out a plain wrap for the Gantt's own whole-canvas hint (§10.5) but not recognised as applying here too until it actually shipped broken. Shown while hovering anywhere in the grid either way (a more specific tooltip already on a child, e.g. `GridActionsCellItem`'s own `label`, is a real MUI Tooltip and takes visual precedence regardless). Used by `TaskGrid.tsx`, `SearchPage.tsx`, both of `AdminPage.tsx`'s grids, `DashboardPage.tsx`, and `TeamsManagementPage.tsx` — one hint string per grid, worded to name *where* to click when only one column is actually the target (`TeamsManagementPage.tsx`'s own Name-only affordance) rather than implying the whole row.
3. **The Gantt view's own bars/labels** (`PlanPage.tsx`) — already had custom, cursor-following floating tooltips (`BarTooltip`/the label tooltip) built well before this setting existed, specifically because a native tooltip's position can't be offset from the cursor (§2.3's own history). Rather than add a *second*, competing tooltip mechanism on top, the gesture hint is appended as an extra line to the tooltip text these already build, gated on `useHintsEnabled()` read once per render (not per-element, since there's no per-element wrapper to attach here). The collapse/expand chevron (a small, already-unambiguous icon) originally used a plain SVG `<title>`, now a `HintTooltip` instead (D1.4-127, §10.5) — its `<g>` wrapper forwards a ref the same way an HTML element does. The drawing area's own zoom/pan gestures (Ctrl/Shift/Ctrl+Shift+scroll, right-click-drag pan) apply to the whole canvas, not one row; originally one whole-canvas native `title`, now a third instance of the same cursor-following floating-tooltip mechanism as `BarTooltip`/the label tooltip (`canvasTooltip`, D1.4-127, §10.5) — a plain `HintTooltip` wrap wasn't viable here, since it anchors to a fixed placement on the wrapped element's bounding box rather than the cursor, which reads badly on an area this large.

### 10.3 Coverage so far (built alongside this section, `D1.4-124`)

Every interaction identified as *currently implemented* while writing this section:

| Interaction | Hint text | Mechanism |
|---|---|---|
| `TaskGrid.tsx` — double-click row / single-click editable field | "Double-click: Open Task Detail window." always, plus "Click: Edit \<column\>." only for the specific cell under the cursor when it's actually editable (§10.7) | Grid `hint` + `getCellEditHint` |
| `SearchPage.tsx` — click a result | "Click: Open the result." | Grid `hint` |
| `AdminPage.tsx` — leaderless-Teams / stale-resource grids | Two separate hints, one per grid | Grid `hint` |
| `DashboardPage.tsx` — click a Resource row | "Click: See the Resource's own filtered Tasks." | Grid `hint` |
| `TeamsManagementPage.tsx` — click a Team's name | "Click: Open the Team's own Team Management window." | Grid `hint` |
| `Project.tsx`/`Component.tsx` — name (open), chevron (expand/collapse) | Per-element, e.g. "Click: Open this Project's own window." | `HintTooltip` |
| `FieldTreePicker` breadcrumb — Task Detail's Project/Component fields, Project/Component Detail's own "Parent" pickers | Per-caller wording, e.g. "Click: Open this Project's own window." (no longer repeats the Project/Component's own name — see §10.4) | `HintTooltip` (`breadcrumbHint`) |
| Task Detail's own Ctrl-drag badge | "Ctrl+drag onto another Task's own Dependencies tab: Create a Dependency on this Task." | `HintTooltip` |
| `DependenciesPanel.tsx` — "Depends upon"/"Dependants" drop targets | Two separate hints | `HintTooltip` |
| Gantt bar/label — double-click, drag-to-reorder | Appended to the existing custom tooltip | Custom tooltip extension |
| Gantt collapse chevron | "Click: Expand."/"Click: Collapse." | `HintTooltip` (§10.5; was SVG `<title>`) |
| Gantt drawing area — zoom/pan | One combined hint, suppressed while a bar/label tooltip is showing (§10.4) | Custom cursor-following tooltip (`canvasTooltip`, §10.5; was whole-canvas `title`) |
| Gantt controls row — "Memorise order"/"Zoom Reset"/"Today," Show Names/Boxed/Weekends checkboxes (§10.10) | Per-control, e.g. "Click: Reset zoom to 100%." | `DenseButton`'s own `hint` prop / `HintTooltip` |
| "View Gantt"/"View Tasks"/"View Project"/"View Component" toggle (§10.10) | Both states now hinted, not just "View Gantt" | `DenseButton`'s own `hint` prop |
| `TaskGrid.tsx`'s own filterable sort header — click to sort, double-click to auto-size (§10.8) | "Click: Sort." and/or "Double-click: Auto size column.", combined into one when both apply | `HintTooltip` |
| `TaskGrid.tsx`'s own filter row — text box, checklist icon (§10.8) | "Click: Enter text to filter." / "Click: Select filter values." | `HintTooltip` (replacing the icon's old native `title`) |
| "View Gantt" button — `AllTaskPage.tsx`/`ProjectDetailPage.tsx`/`ComponentDetailPage.tsx` (§10.8) | "Click: View Gantt for these tasks." (only while showing "View Gantt," not its own toggled-back state) | `HintTooltip` (`DenseButton`'s new `hint` prop) |
| `TaskGrid.tsx`'s own Delete (bin) icon — enabled only (§10.8) | "Click: Delete this task." | `HintTooltip` (correcting an earlier assumption that `GridActionsCellItem`'s own `label` was already a visible tooltip — it's `aria-label` only) |

**Not yet covered — genuinely new features from §7/§9, not oversights**: none of §7's *new* drag-and-drop features (reparent a Project/Component, move a Task, extended Dependency creation, OS-file-drag Attachments, the corrected §7.7 bar reschedule) have hints yet, since none of them are built yet either — per the user's own instruction, a hint is added *as* each interaction is actually implemented, not speculatively ahead of it. `§4.2`'s own editable-cell visual affordance (dimming) is similarly unbuilt, so `TaskGrid.tsx`'s own hint text above already describes the *intended* editable-field behaviour without yet having the dimming cue to point at.

`tsc -b`/`vitest` (94, unchanged)/`build`/`lint` all clean. Not verified interactively in a browser this session.

### 10.4 Refinements after first use (`D1.4-125`, added 2026-09-26)

Reported after actually using the §10.3 tooltips:

1. **`FieldTreePicker`'s breadcrumb hint dropped the Project/Component's own name.** It previously read `` `${breadcrumb} — ${breadcrumbHint}` `` — redundant, since that name is already the field's own visible content; the tooltip now shows `breadcrumbHint` alone. Its native-`title` fallback (the full path, shown once ellipsis truncates it) had a related logic bug: it was suppressed whenever a click handler/hint existed at all, regardless of whether "Hints" was actually `On` — fixed to also check the live `useHintsEnabled()` value, so the fallback title still works with Hints `Off`.
2. **One fixed phrasing, everywhere**: `"<Gesture>: <Effect>."`, capitalised effect, one gesture per line (`\n`-joined) when an element has more than one. Every hint string in the §10.3 table was reworded to this shape.
3. **Styling**: MUI `Tooltip`'s own default look (dark background, bold-reading white text) didn't read as a tooltip. `HintTooltip.tsx` now sets a pale-yellow background (`HINT_TOOLTIP_BG = "#fff9c4"`, shared by name so every mechanism agrees), `fontWeight: 400`, and `whiteSpace: "pre-line"` (so a `\n` in the hint string renders as a real line break) via MUI's own `slotProps`. `PlanPage.tsx`'s own custom `barTooltip`/label tooltip Boxes were changed to the same colour and weight directly. **At the time, a native `title` attribute couldn't be styled at all** (it's OS-rendered), so the Gantt's collapse chevron and whole-canvas zoom/pan hint, the DataGrid `hint` prop, and `FieldTreePicker`'s truncation-fallback title all kept the browser's own default tooltip appearance — mostly converted away from native `title` since, see §10.5.
4. **Gantt dual-tooltip**: hovering a bar/label showed its own custom tooltip *and* the whole-canvas native `title` (zoom/pan) at the same time — the native title bubbles from the `ganttAreaRef` ancestor independently of the custom tooltip's own state. Fixed by making that ancestor's `title` conditionally `undefined` whenever `barTooltip`/`labelTooltip` state is currently non-null, so only one tooltip can ever be showing over the Gantt at a time.

`tsc -b`/`vitest` (94, unchanged)/`build`/`lint` all clean. Not verified interactively in a browser this session.

### 10.5 Retiring native `title` where possible (`D1.4-127`, added 2026-09-26)

Asked directly, after §10.4: could the tooltips still using a native `title` (§10.4 point 3's own limitation) move to the same pale-yellow mechanism as everything else? Two of the three could, one couldn't (for a real UX reason, not a technical shortcut):

- **`components/DenseDataGrid.tsx`'s whole-grid `hint`** — the outer `Box` already wrapping the whole grid forwards a ref like any element, so this originally wrapped it in `HintTooltip` instead of setting a `title` on itself. **This was wrong, corrected in `D1.4-128`, §10.6** — turned out to have exactly the same "anchor is too large" problem the whole-canvas hint below was already known to have, just not recognised as applying here too at the time.
- **The Gantt's collapse/expand chevron** — its `<g>` wrapper forwards a ref exactly like an HTML element does, so it wraps in `HintTooltip` too, the same way `Project.tsx`'s own chevron already did, replacing its plain SVG `<title>`.
- **The Gantt's whole-canvas zoom/pan hint could *not* just wrap in `HintTooltip`.** `ganttAreaRef`'s own `Box` is the whole (often much taller/wider than the viewport, scrollable) Gantt area — a MUI `Tooltip` anchors to a fixed placement on its wrapped element's *bounding box*, not to the cursor. For an area this large, that would pin the hint in one static spot rather than following the mouse, which is strictly worse than the native `title` it would replace (browser-positioned at the actual hover point) and inconsistent with the bar/label tooltips already in this view (custom, cursor-following, built for exactly this reason — §2.3/D1.4-28). It instead gained a third instance of that same cursor-following floating-`Box` mechanism (`canvasTooltip`, styled identically to `barTooltip`/`labelTooltip`) in place of the native `title`.

  Keeping this mutually exclusive with `barTooltip`/`labelTooltip` (so hovering a bar/label never also shows the canvas hint) needed a ref, not those two pieces of state read directly, for the same reason `D1.4-125`'s dual-tooltip fix needed to check state at all: a bar/label's own `onMouseMove` and `ganttAreaRef`'s own `onMouseMove` fire from the *same bubbled native event*, child before ancestor, but a `setState` call only takes effect on the *next* render — reading `barTooltip`/`labelTooltip` state in the ganttAreaRef handler would still see the stale (`null`) value on the very event that's first setting it, showing both tooltips for one frame. `overInteractiveTooltipRef.current` (set `true` on entering a bar/label/chevron, `false` on leaving) is updated synchronously instead, so the ancestor handler always reads the current answer within that same event, not last render's.

- **`FieldTreePicker`'s truncation-fallback `title` was deliberately left alone** — it isn't a gesture hint at all (it just shows the full path once ellipsis truncates it), applies regardless of the "Hints" setting, and is already mutually exclusive with the styled `HintTooltip` on the same element (§10.4 point 1) — converting it would mean showing *some* tooltip on that element even with Hints "Off", which the rest of this feature deliberately never does.

`tsc -b`/`vitest` (94, unchanged)/`build`/`lint` all clean. Not verified interactively in a browser this session.

### 10.6 The All Tasks grid "had no tooltip" (`D1.4-128`, added 2026-09-26)

Reported directly. Two independent bugs, both found by scripting a real headless-browser hover against the running app rather than guessing further from source — the first is exactly the failure mode §10.5's own whole-canvas-hint reasoning already identified and avoided, just not recognised as applying to the DataGrid `hint` conversion too:

1. **The `D1.4-127` `HintTooltip` wrap of `DenseDataGrid`'s whole grid container was rendering off-screen.** `AllTaskPage.tsx`'s `fillHeight` grid is nearly the full window, starting right under the page header; a MUI `Tooltip`'s default "top" placement anchors above the *whole wrapped element*, so for an anchor this tall the popup lands above the browser window entirely — a scripted hover's `getBoundingClientRect()` came back with a negative `y`. Same root cause as §10.5's "why the whole-canvas hint couldn't just wrap in `HintTooltip`" — just not caught as the same problem until it actually shipped this way and someone tried to use it. Fixed identically: a cursor-following custom tooltip (`gridHintTooltip`), not an anchor-based `Tooltip`.
2. **`TaskGrid.tsx`'s own hint string had a literal `\n`**, not a real line break — written as a plain JSX attribute (`hint="...\n..."`), and JSX doesn't run attribute strings through JS escape processing the way a `{}`-wrapped string does. Fixed by moving the string inside `{}`.

**Rule of thumb for any future hint, written down so this doesn't get rediscovered a third time**: a plain `HintTooltip` wrap is only safe when the wrapped element is small relative to the viewport (an icon, a name span, a single cell) — anything that can plausibly fill most of the window needs the cursor-following custom-Box pattern instead (now four instances of it: `gridHintTooltip`, `canvasTooltip`, `barTooltip`, `labelTooltip`).

`tsc -b`/`vitest` (94, unchanged)/`build`/`lint` all clean. **Verified interactively this time** — a scripted Playwright hover against the real running dev server, screenshotted, confirming the tooltip renders on-screen, pale-yellow, on two real lines.

### 10.7 A second, competing white tooltip, and a fixed hint list (`D1.4-129`, added 2026-09-26)

Reported directly, alongside a fourth, unrelated request folded into the same pass: Detailed Description should never be inline-editable from the All Tasks grid, only from Task Detail.

1. **A plain white tooltip was showing for every cell, truncated or not, and colliding with the pale-yellow one.** Not this app's own code — MUI's `DataGrid` sets a native `title` to a cell's full text *unconditionally* whenever its column has no `renderCell` of its own (confirmed by reading `GridCell.js`), which is every plain column in every grid built on `DenseDataGrid` (none of them had ever needed a custom `renderCell`). Fixed by giving every such column a trivial fallback `renderCell` (`components/DenseDataGrid.tsx`'s new `effectiveColumns`, generic to every grid this component serves, not just TaskGrid) that renders the identical text MUI would have shown anyway — since MUI only sets that `title` in the `children === undefined` branch, this alone stops it, with no visible change to the cell itself. A column whose `type` already supplies its own default `renderCell` (`"actions"`, `"boolean"`) is left alone, since that merge happens later, inside `DataGrid` itself.
2. **The cell's own full text, restored, but only when actually needed, and inside the one existing tooltip.** `gridHintTooltip`'s own `onMouseMove` (from `D1.4-128`) now also checks whether the specific hovered cell's rendered text is clipped (`scrollWidth > clientWidth`) and, only then, adds that text as the tooltip's own first line — so an already-fully-visible, auto-fit cell (most of them) adds nothing, and a genuinely truncated one shows its full value in the same pale-yellow box as everything else, never a second tooltip.
3. **The fixed "Single-click an editable field (Status, Priority, …)" list is gone**, replaced by a new `getCellEditHint?: (params) => string | null` prop on `DenseDataGrid`: called for whichever cell is currently under the cursor, it returns `` `Click: Edit <column>.` `` only when *that specific cell* is editable right now (`TaskGrid.tsx`'s own `GOVERNED_FIELDS`/`canEditCell`), nothing otherwise. `hint` itself shrank back to just the row-level gesture that's always true regardless of column: `"Double-click: Open Task Detail window."`.
4. **Detailed Description is no longer inline-editable from the grid at all** — removed from `TaskGrid.tsx`'s own `GOVERNED_FIELDS` (added there at `D1.4-52`), so it's now a plain read-only column there, with no grey read-only-cell styling either (governed by the same set). Task Detail's own field is completely unaffected — `lib/permissions.ts`'s `canEditTaskField` and its tier-3 `RESOURCE_EDITABLE_FIELDS` carve-out aren't touched, so who can edit it there hasn't changed; only *this window's* inline single-line cell editor for it is gone, replaced by "open Task Detail to edit the full text."

Verified interactively (scripted Playwright hover, as `D1.4-128`'s own check did): an editable cell shows only its own `Click: Edit <Name>.` line; an untruncated, non-editable cell shows only the row's hint with nothing extra; a genuinely truncated cell shows its own full text plus the row hint; Detailed Description shows no edit line and a single click no longer starts editing it (confirmed via the cell's own class list, which never gained `cell--editing`).

`tsc -b`/`vitest` (94, unchanged)/`build`/`lint` all clean.

### 10.8 Filter row, sort header, "View Gantt," and Delete icon (`D1.4-133`/`D1.4-134`, added 2026-09-26)

Reported directly, working through §11 group C: four more controls with a hand cursor where the arrow was expected, three of them with no hint at all, and one (the filter row) showing the *wrong* hint entirely.

- **The filter row was showing this grid's own row-level hint** ("Double-click: Open Task Detail window.") instead of anything about filtering. Root cause: `DenseDataGrid.tsx`'s `onMouseMove` pushed that `hint` string whenever "Hints" was on, with no check that the mouse was actually over a data cell — so it bubbled from the column header/filter row too. Fixed grid-wide (every `DenseDataGrid` consumer had this same latent bug) by gating it on an actual `.MuiDataGrid-cell` match.
- **The hand cursor on the filter row and the sortable header shared one root cause**: MUI's own `GridRootStyles.js` sets `cursor: pointer` directly on `.MuiDataGrid-columnHeader--sortable`, and — `cursor` being inherited — that cascades into everything a column's `renderHeader` draws inside it. Overridden once, grid-wide in `TaskGrid.tsx`'s `urgencyRowSx`, which fixes the label by inheritance alone; the filter text input and its checklist-icon button each still needed their own explicit override on top, since a browser's UA stylesheet sets `cursor: text` directly on a text `<input>`, and the icon button already had its own explicit `cursor: "pointer"` — a direct declaration always beats an inherited one.
- **New hints, one per control, wired up alongside its own cursor fix**: `GridColumnFilter.tsx`'s `FilterableHeader` gained a `sortable` prop and now shows `"Click: Sort."` and/or `"Double-click: Auto size column."` on its label (combined into one tooltip when both apply, neither for a `flex` column, which has no manual width to auto-size to); its filter text box and checklist icon each gained their own `HintTooltip` (`"Click: Enter text to filter."` / `"Click: Select filter values."`), the icon's replacing an old *native* `title` that never matched the rest of the app's own pale-yellow look. `components/DenseField.tsx`'s `DenseButton` gained opt-in `sx`/`hint` props (every other `DenseButton` in the app is unaffected), used by all three "View Gantt" buttons (`AllTaskPage.tsx`/`ProjectDetailPage.tsx`/`ComponentDetailPage.tsx`) for `cursor: "default"` and `"Click: View Gantt for these tasks."` — only while actually showing "View Gantt," not the toggled-back state.
- **The Delete (bin) icon** needed the same cursor fix (via a `& .task-grid-delete-cell button` selector, since `GridActionsCellItem`'s own TS types don't expose an `sx` prop) and a `HintTooltip` (`"Click: Delete this task."`) shown only while enabled — correcting an assumption recorded back at `D1.4-124` that its own `label` prop was already a visible MUI Tooltip; reading MUI's source confirmed it's `aria-label` only, never rendered. **Its disable/grey-out behaviour for a user without delete permission was checked live and confirmed already correct** (a non-Team-Lead viewing a colleague's Task: `disabled: true`, `pointer-events: none`, icon colour `rgba(0,0,0,0.18)`) — not a bug, contrary to what was suspected when this was raised.
- **A genuine dual-tooltip collision, found while verifying the Delete icon's own new hint**: it showed alongside the grid's own row-level hint, stacked — the actions cell is still a real `.MuiDataGrid-cell`, so the filter-row fix above didn't exclude it. Fixed by excluding any cell whose column `type === "actions"` from the grid-wide `hint` line specifically, leaving only that column's own more specific tooltip.
- **The non-editable cell background wash (`D1.4-132`) was halved again**, to `rgba(120,120,120,0.11)` — still too strong even at `0.22`.

`tsc -b`/`vitest` (94, unchanged)/`build`/`lint` all clean. **Verified interactively** (scripted Playwright hover against the real running dev server): filter input/icon, a sortable column's label, and the "View Gantt" button all come back `cursor: default` with their own respective hint text; the Delete icon shows exactly one tooltip when enabled and none when correctly disabled for a non-owning, non-Team-Lead user.

### 10.9 A directional bug and an inconsistent delay on the same three controls (`D1.4-136`, added 2026-09-26)

Reported directly, immediately after §10.8: moving the mouse *upward* from the filter row into the sortable label directly above it never showed the label's own tooltip at all — the filter's own just closed instead — while moving *downward* worked correctly; separately, the filter row, sort header, and "View Gantt" button's own tooltips all showed a noticeable, inconsistent delay the main grid's own `gridHintTooltip` doesn't have.

- **The directional bug**: `HintTooltip.tsx`'s `<Tooltip>` was left at MUI's own default `disableInteractive={false}` (meant for a tooltip whose *own content* needs to be hovered, e.g. a link inside it) — which keeps its popper `pointer-events: auto`. The filter row sits directly under its own sortable label, and a `placement="top"` tooltip renders *above* its own anchor — so the filter input's own (still-open) tooltip popup physically overlapped the label's own screen position above it. Moving upward, the cursor entered that interactive popup first, intercepting the pointer, so the label's own `onMouseEnter` never fired — moving downward never had this problem, since the label's own tooltip renders even further away, never near the filter row beneath it. Fixed by adding `disableInteractive` to every `HintTooltip` app-wide (`pointer-events: none` on the popper) — none of this app's hints have interactive content, so nothing is lost.
- **The inconsistent delay**: `HintTooltip` gained an `enterDelay` prop (default `400`, unchanged everywhere else — a deliberate hover-intent delay, avoiding a "tooltip storm" while the mouse crosses several hintable elements quickly), passed as `0` at the three call sites sitting right beside the grid's own delay-free mechanism: `GridColumnFilter.tsx`'s label/input/icon, and `DenseButton`'s own internal wrap (today, only the "View Gantt" buttons).
- The slightly different visual appearance between a MUI `Tooltip` and the grid's own plain custom `Box` was flagged but not asked to be fixed, and wasn't touched.

`tsc -b`/`vitest` (94, unchanged)/`build`/`lint` all clean. **Verified interactively** (scripted Playwright mouse movement + screenshots at each step, both directions, on the real running dev server): upward movement now correctly shows the label's own tooltip; downward movement still shows the filter's own, as before. A first pass checking DOM presence of `role="tooltip"` elements gave a false positive (an element mid-exit-transition can still be in the DOM) — the actual verification is the screenshots, not that check.

### 10.10 The Gantt view's own controls row and row-reorder drag (`D1.4-138`/`D1.4-139`, added 2026-09-26)

Reported directly, working through the Gantt/Plan view specifically: "Memorise order," "View Tasks"/"Zoom Reset"/"Today," and the three checkboxes all needed hints; the H/V zoom fields only applied on blur; the manual row-reorder drag's own dashed drop-highlight followed the cursor even over illegitimate targets; and, separately, the Dependency arrows needed a different colour and to draw on top of everything else.

- **Hints added**: "Memorise order," "Zoom Reset," "Today," and all three checkboxes (Show Names/Boxed/Weekends) — plus "View Tasks"/"View Project"/"View Component," the toggled-back state of the existing "View Gantt" button (`D1.4-133`), deliberately left unhinted there as "not asked for" and now asked for.
- **A real bug found wiring these up**: wrapping a `<DenseButton>` *externally* with `<HintTooltip>` (as "Zoom Reset"/"Today"/"Memorise order" originally were) silently never opens a tooltip at all — `DenseButton` is a plain function component, not `React.forwardRef`, so the `ref` a `HintTooltip`'s underlying MUI `Tooltip` needs never reaches the real `<button>` inside it. `DenseButton` already avoids this internally (it applies its own `HintTooltip` to its own inner `Box` directly, which is how "View Gantt"'s own hint, `D1.4-133`, always worked correctly) — routing these three through `DenseButton`'s own `hint` prop instead of wrapping it from outside fixed it immediately.
- **Live H/V zoom**: `ZoomPercentInput` now commits on every `onChange`, not just blur/Enter. Committing naively broke mid-typing, though — the zoom change feeds back into this same input's own `value` prop on the very next render, and the existing `useEffect` syncing `text` from it would stomp on whatever the user was still typing (e.g. a trailing "." for "150.5") with a rounded value from the *previous* keystroke. Fixed with a `focusedRef`: while the input is focused, that effect never overwrites `text` at all, leaving the user's own keystrokes as the one source of truth until blur.
- **The row-reorder drag's own dashed highlight**: driven by a plain `onMouseEnter` on every row's own hit-rect, unconditionally — during a drag, moving over a *different* Project's own rows still fired it, with nothing checking whether that row was actually a legitimate drop target. Fixed with a new `isValidDropTarget(candidate)` (renamed and generalised at `D1.4-140`, see §10.11 — it now answers *which row* to highlight, not just whether the one physically hovered qualifies), checked before setting the highlight: `true` when nothing's being dragged; otherwise the same sibling-group scoping (`candidate.parentKey` matches the dragged bar's own) the actual drop-position math was already restricted to, excluding the dragged bar itself.
- **A design question settled by testing, not assumption, before extending that fix to Project-dragging**: the report described a stricter rule ("a Project can't be dropped onto a Task") than the existing, deliberate `D1.4-27` design, which lets a Task and a sibling Project interleave freely. Verified live first — dragging a Task down past a sibling Project on the real running dev server *did* interleave them — confirming the existing design's behaviour is real. Given that evidence, the decision was to keep interleaving as-is and fix only the highlight to match it, which `isValidDropTarget` already does correctly (it was never kind-restricted); verified for all three cases (different-parent Task: no highlight; same-parent sibling Task while dragging a Project: highlight shows, correctly allowing the interleave; different-parent Project: no highlight).
- **Dependency arrows**: a new `DEPENDENCY_ARROW_COLOUR = "#00acc1"` (cyan), replacing the old near-black on both the line and its arrowhead marker. Moved from right after the grid lines (*before* every bar) to the very last thing drawn in the chart's own `<svg>` (*after* every bar) — SVG has no `z-index`, only paint order, so an arrow could previously be cut off by a bar or a Boxed-mode box painted over it.

`tsc -b`/`vitest` (94, unchanged)/`build`/`lint` all clean. **Verified interactively** throughout (scripted Playwright against the real running dev server): every new hint's own text confirmed by hovering; live H/V zoom confirmed by reading the chart's own SVG width mid-typing, before any blur; all three drop-highlight cases confirmed by dragging for real; the arrow's own colour and paint-order position confirmed by reading its `stroke` and DOM position, plus a screenshot showing one crossing visibly over a bar.

### 10.11 The drop highlight over a sibling's own children (`D1.4-140`, added 2026-09-26)

Reported directly, repeating `D1.4-138`'s own drag test: dragging a Task past a sibling Project *onto one of that Project's own child Tasks* showed no highlight at all — the request being that the highlight should stay on the sibling Project the whole time the cursor is over any of its own children, then jump to the *next* sibling once the cursor reaches that one's own subtree, since either way is genuinely where the drop would land.

1. **The core fix**: `isValidDropTarget` (renamed `resolveDropHighlightTarget`) now walks the hovered bar's own `parentKey` chain *upward* — via a new `barsByKey` lookup (every bar keyed by its own `${kind}:${id}` identity) — until it reaches whichever bar's *own* `parentKey` matches the dragged item's own (an actual sibling), and highlights *that* bar, not whatever was literally under the cursor. Hovering the dragged row's own descendants (dragging a Project, hovering one of its own children) walks up to the dragged bar itself, which stays excluded, same as before.
2. **A second, genuine bug found verifying (1) directly against the real app, not assumed from the first fix alone**: hovering a sibling Project's *own* row — not a descendant, its own literal row — also showed no highlight, confirmed via a scripted hover reading the actual DOM (every leaf-Task row highlighted correctly; every container row didn't). Root cause: a container row's own collapse/expand chevron is a *separate* SVG element painted on top of part of the row's own hit-rect, and the browser correctly treats it as the topmost element there — so the row's own `onMouseLeave` fires the instant the cursor lands on the chevron, unconditionally clearing the highlight, with nothing on the chevron side to restore it.
3. **Fixed by sharing state, not just intent**: `handleRowHoverEnter(bar)`/`handleRowHoverLeave()`, extracted from the row's own `onMouseEnter`/`onMouseLeave`, are now called by the chevron's own handlers too — previously the chevron only ever touched `overInteractiveTooltipRef` (added at `D1.4-126` for the unrelated whole-canvas-hint suppression). Hovering the chevron now re-asserts the same row-level state the row's own hover already establishes, reading as "still hovering this row," not "left it."

`tsc -b`/`vitest` (94, unchanged)/`build`/`lint` all clean. **Verified interactively** (scripted Playwright drag against the real running dev server, reading the actual highlighted `<rect>`'s own `y`): hovering a sibling Project directly, or either of its own two child Tasks, all highlight *the same* row; moving on to the next sibling correctly jumps the highlight there instead.

<a id="implementation-plan"></a>
## 11. Implementation Plan (added 2026-09-26)

A checklist of everything left to finish this document's work, broken into small items that can each be built and tested on their own. Similar changes are grouped together; groups are in a sensible build order (shared pieces first, then the features that use them), but groups E–J don't depend on each other and can be done in any order. Every item that adds a new gesture also adds its hint tooltip (§10) at the same time, per the standing rule that hints are added as each interaction is built.

Items marked **Decide** need your call before the build items after them can start.

**Two separate marks, not one**: `[x]` means the item has been *built*; a trailing **✅ Verified** means *you've* actually tested it yourself and confirmed it works — the two are tracked separately because "built" is a claim, "verified" is your own check on that claim. An item can be `[x]` without being verified yet; it should never be verified without also being `[x]`.

### A. Already done (for the record) — verified 2026-09-26

- [x] A1. Single-click to open on Search, Admin Tools (both grids), Dashboard — `D1.4-123` **✅ Verified**
- [x] A2. Shared clickable affordance (`clickableCellSx`, `CLICKABLE_SX`) on every navigation target — `D1.4-123` **✅ Verified**
- [x] A3. Task Detail Project/Component fields and Project/Component Detail "Parent" pickers open on single click — `D1.4-123` **✅ Verified**
- [x] A4. "Hints" setting and hint tooltips on every existing interaction — `D1.4-124` to `D1.4-129` **✅ Verified**
- [x] A5. Detailed Description no longer editable from `TaskGrid` — `D1.4-129` **✅ Verified**

### B. Shared drag-and-drop styling (§5.2, §5.3) — refactor only, nothing should look or behave differently — **done (`D1.4-130`), verified 2026-09-26**

- [x] B1. Add `DROP_TARGET_ACTIVE_SX` to `lib/dnd.ts`; switch `DependenciesPanel.tsx`'s own copy over to it. **✅ Verified**
  *Test:* Ctrl-drag a Task's badge onto another Task Detail's Dependencies tab — the dashed outline looks exactly as before.
- [x] B2. Add `DRAG_HANDLE_SX` to `lib/dnd.ts`; switch Task Detail's drag badge over to it. **✅ Verified**
  *Test:* hovering the badge still shows the grab cursor.
- [x] B3. Add `draggableRowHighlightSx(isHovered)` to `lib/dnd.ts`; switch the Gantt label column's dotted row highlight over to it. **✅ Verified**
  *Test:* hovering a Gantt label row still shows the dotted outline; drag-to-reorder still works.

### C. `TaskGrid` — show which cells are editable (§4.2) — **done (`D1.4-131`/`D1.4-132`), verified 2026-09-26**

- [x] C1. Add one shared "not editable" cell class to `DenseDataGrid.tsx` (dimmed text colour plus a translucent grey background wash, `D1.4-132` — originally a `filter`, revised after report, §4.2). **✅ Verified**
- [x] C2. Apply it to every `TaskGrid` cell that isn't editable for this user and row — both columns that are never editable (Description, Project, …) and editable columns this user can't edit on this row. This replaces today's grey `task-grid-readonly-cell` background. **✅ Verified**
  *Test:* as a Task's owner, the 8 editable columns look normal and everything else is dimmed; on a colleague's Task, nearly everything is dimmed; the urgency tint is dimmed along with the text.
- [x] C3. Cursor: the plain arrow over every cell, switching to a text cursor over editable cells (`D1.4-132` — originally `pointer`, a hand with a pointing finger, revised after report). **✅ Verified**
  *Test:* hover across a row — cursor stays the plain arrow except over cells you can edit, where it's a text cursor.
- [x] C4. Check the same behaviour in the Task lists embedded in Project Detail and Component Detail. **✅ Verified**
- [x] C5. Update §4.2's own text, which still says 9 editable columns and that Resources can edit Detailed Description in the grid (both out of date since `D1.4-129`). **✅ Verified**

### D. `TaskGrid` — double-clicking an editable cell also starts an edit (§4.1)

- [ ] D1. **Decide**: "let it start, then undo it" (recommended — no delay on normal clicks, brief flicker on double-click) or "delay, then execute" (no flicker, but every single click feels slower).
- [ ] D2. Build the chosen fix.
  *Test:* double-click a Status cell — Task Detail opens and no editor is left open in the grid; a single click on Status still starts editing straight away.

### E. Open design questions (§8) — settle before starting the matching feature group

- [ ] E1. **Decide**: Gantt row reorder — confirm it's its own "reorder within a list" exception (recommended), then add it to §6's gesture table.
- [ ] E2. **Decide**: what the drag handle looks like on Project, Component and Task rows (a new icon, or reuse an existing one) — best decided against a quick mock-up on a real screen. Needed before groups F, G and H.
- [ ] E3. **Decide**: can a Project or Component be dragged to the top level (no parent)? If so, where do you drop it?
- [ ] E4. **Decide**: when a Task is dropped onto a Component, must the Component be in the Task's current Project, or does the drop also move the Task to that Component's Project?
- [ ] E5. **Decide**: should a plain (no Ctrl) drag of Task Detail's badge also move the Task (onto a Project/Component row), to match `TaskGrid`'s new drag handle? Today a plain drag from the badge is cancelled.

### F. Reparent a Project by dragging (§7.1)

- [ ] F1. Confirm the existing Project update call can change a Project's parent; add a `useReparentProject()` hook if a dedicated one is cleaner.
- [ ] F2. Add a Project drag type to `lib/dnd.ts`.
- [ ] F3. Add the drag handle (from E2, styled with `DRAG_HANDLE_SX`) to each row in `Project.tsx`; only shown if the user may edit that Project.
  *Test:* handle appears on your own Projects, not on ones you can't edit; clicking the name still opens the Project.
- [ ] F4. Make each Project row a drop target, highlighted with `DROP_TARGET_ACTIVE_SX` while a Project is dragged over it.
- [ ] F5. Block dropping a Project onto itself or one of its own descendants, with an inline message.
- [ ] F6. On drop, save the new parent and refresh the tree.
  *Test:* move a Project under another one and check it in the tree and on the Project's own Detail window; try an illegal drop and check it's refused.
- [ ] F7. Hints: on the handle ("Drag: Move this Project under another Project.") and on rows while a drag is in progress.
- [ ] F8. Top-level drop, if E3 says yes.

### G. Reparent a Component by dragging (§7.2) — same as F, on `Component.tsx`

- [ ] G1. Confirm the Component update call can change a Component's parent; add `useReparentComponent()` if needed.
- [ ] G2. Add a Component drag type to `lib/dnd.ts`.
- [ ] G3. Drag handle on each `Component.tsx` row, permission-gated.
- [ ] G4. Component rows as drop targets with `DROP_TARGET_ACTIVE_SX`.
- [ ] G5. Block dropping onto itself or a descendant.
- [ ] G6. Save on drop and refresh.
  *Test:* as F6, for Components.
- [ ] G7. Hints on handle and rows.
- [ ] G8. Top-level drop, if E3 says yes.

### H. Move a Task into a different Project or Component by dragging (§7.3)

- [ ] H1. Confirm the existing `useReparentTask()` covers both Project and Component moves (per E4).
- [ ] H2. Add a drag handle beside the Delete icon in `TaskGrid`'s actions column; only shown if the user may move that Task.
  *Test:* handle appears only on Tasks you can edit; the Delete icon, single-click edit and double-click open all still work.
- [ ] H3. Project rows (`Project.tsx`) accept a dropped Task — plain drag only, never Ctrl (Ctrl means Link).
- [ ] H4. Component rows (`Component.tsx`) accept a dropped Task, following the E4 rule.
- [ ] H5. Dropping onto the Task's current Project/Component does nothing.
- [ ] H6. On drop, save and refresh every affected Task list.
  *Test:* move a Task from one Project to another and check it leaves the old list and appears in the new one; same for a Component.
- [ ] H7. Hints on the handle and on rows while a Task is dragged over them.
- [ ] H8. Plain-drag Move from Task Detail's badge, if E5 says yes.

### I. Ctrl-drag to create a Dependency on a Project (§7.4)

- [ ] I1. Check whether Ctrl-dragging a Task's badge onto Project Detail's Dependencies tab already works — the drop code in `DependenciesPanel.tsx` is already written to handle a Project owner, but it's never been tried.
  *Test:* Ctrl-drag onto "Depends upon" and "Dependants" on a Project and check each creates the right Dependency.
- [ ] I2. Add the automatic switch to the Dependencies tab when a Ctrl-drag enters a Project Detail window — Task Detail already does this; Project Detail doesn't.
  *Test:* start a Ctrl-drag from a Task, move over a Project Detail window showing a different tab — it switches to Dependencies.
- [ ] I3. Fix anything I1 turns up.
- [ ] I4. Correct §7.4, which says "Project/Component": Component Detail has no Dependencies tab at all, so this only applies to Projects.

### J. Drag files from Windows onto an Attachments list (§7.5)

- [ ] J1. Check how `AttachmentsPanel.tsx` adds an attachment today and reuse that same save call.
- [ ] J2. Make the Attachments list accept dropped files, highlighted with `DROP_TARGET_ACTIVE_SX` while files are dragged over it.
- [ ] J3. Handle several files in one drop, one attachment each.
- [ ] J4. Show a clear error for a file that fails (too large, not allowed, network error) without losing the others.
- [ ] J5. Only accept drops if the user may add attachments here.
- [ ] J6. Hint: "Drag files here: Attach them."
  *Test:* drag one file, then several, from File Explorer onto Task Detail's Attachments tab and onto each other window that shows attachments; check they appear and open; try a file that should fail.

### K. Gantt: Shift-drag a bar to reschedule it (§7.7, port from V1.2)

- [ ] K1. **Decide**: Task bars only, or Project bars too? And confirm the V1.2 rules still wanted — business days only, can't move a Task earlier than its predecessors finish (except when delaying).
- [ ] K2. Shift + mouse-down on a bar starts the gesture; plain click, double-click, and label drag-to-reorder are unaffected.
- [ ] K3. Show the bar's new position live while dragging, correct at any zoom level and with weekends shown or hidden.
- [ ] K4. Work out the shift in business days from the distance dragged.
- [ ] K5. Apply the predecessor limit.
- [ ] K6. On release, save the new Start Date; the Gantt recalculates.
- [ ] K7. Only allowed if the user may edit that Task's dates; otherwise Shift-drag does nothing.
- [ ] K8. Add "Shift+drag: Reschedule." to the bar's tooltip.
  *Test:* Shift-drag a Task bar forward and back at a few zoom levels; check the saved date on Task Detail; try to drag before a predecessor's end; try on a Task you can't edit.

### L. Finishing up

- [ ] L1. Update §9's Status Tracker and §10.3's hint list to match what's been built.
- [ ] L2. Full check: `tsc -b`, tests, lint, build; click through every window touched above.
- [ ] L3. Add each group's decisions to `Plan.md` as it's done.

See `4_GuiClient/Plan.md` §6.5 for how this fits the overall Level 1 build order.
