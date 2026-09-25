# Stage 5 — Drag and Drop, Click Semantics, and Interaction Affordances

## 1. Purpose and Scope

Stage 4 (`4_GuiClient/Plan.md` §6.4) is now complete. Its own windows were each built with Stage 2's explicit-control pattern (`D1.4-4`/`D1.4-7`) rather than V1.2's drag-and-drop, deliberately deferring that surface to this stage (`D1.4-40`) once the windows to drag between actually existed.

This document was requested with three explicit requirements beyond "build the drag-and-drop interactions themselves," raised because the app's own history — audited below — already shows the failure mode they're worried about actually happening in already-shipped screens, not just a hypothetical risk:

1. **Gesture consistency** — plain drag, Ctrl+drag, and Shift+drag should each mean a *comparable* thing everywhere they appear, not a different thing per window.
2. **Click consistency** — the same operation shouldn't be a single click in one window and a double click in another.
3. **A consistent visual language** for "this is draggable," "this is a valid drop target," and "this is clickable" — hover feedback and/or a persistent cue, applied the same way everywhere.

All three are treated as prerequisites for Stage 5's own new work, not an afterthought: §4/§5/§6 below establish the vocabulary and shared primitives first, and §7 (the actual new drag-and-drop features) is built on top of them, not built ad hoc again and reconciled later.

## 2. What Already Exists (the audit)

Three real interaction patterns already ship today, built independently, at different points in this project, without a shared convention to check against. Reading them side by side is what actually surfaces the inconsistency the request is worried about — it isn't hypothetical.

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

### 2.2 The two modifier-key conventions already in play

- **V1.2's own rule** (`Requirements/UserInterfaceWindows.md` §4, "Ctrl-modified drag effect"): no modifier → Move effect; Ctrl held → Link effect. Per-target, not per-source — a target only ever accepts one of the two. One documented exception: Component reparenting "ignores Ctrl and always offers every effect."
- **V2's own shipped precedent** (`D1.4-10`, Stage 2's cross-window spike, built out for real): Ctrl+drag a Task's title badge onto another Task Detail window's Dependencies tab creates a Dependency. A plain drag (no Ctrl) is cancelled outright at the source (`event.preventDefault()` unless `event.ctrlKey`) — there's no Move counterpart wired up here at all, since this particular source has no Move operation to offer (reparenting a Task already goes through the explicit tree-picker, `D-Win-9`).
- **Shift+drag has exactly one existing use**, and it isn't a Windows-drag-and-drop gesture at all: the Gantt view's own bar-reschedule interaction (`UserInterfaceWindows.md` §4's own aside, §3.7) — dragging a bar with Shift held adjusts *that bar's own* dates in place. Nothing else in the app uses Shift for anything.

So V2 already has two of the three gestures genuinely exercised (plain drag doesn't yet do anything anywhere in V2, only Ctrl+drag and — separately — Shift+drag), but no written-down rule tying what they mean together, and this document is the first place that rule is stated.

### 2.3 Two different "this is draggable" affordances, already shipped, already different

- **Task Detail's own drag source** (`D1.4-10`): a small 24×24 coloured badge next to the Task's title (its own live-editable `<input>` can't double as a drag source), styled `cursor: "grab"` — a persistent, discoverable-on-hover cue via cursor shape.
- **The Gantt view's own draggable rows** (`D1.4-27`/`D1.4-32`): a *dotted hover-highlight* on the row, with `cursor: "default"` — a code comment there explains this was a deliberate choice: "the dotted hover-highlight is what shows a row is draggable here, not the cursor shape."

Both are reasonable in isolation, but they're two different visual languages for the exact same underlying fact ("you can pick this up"), each invented independently for its own screen. A user who learns one doesn't get any benefit from that knowledge on the other screen.

### 2.4 The one existing drop-target affordance

`DependenciesPanel.tsx`'s "Depends upon"/"Dependants" lists (the `D1.4-10` drop targets): while a valid drag hovers over them, `outline: "2px dashed", outlineColor: "primary.main", outlineOffset: "-2px", borderRadius: "4px"`. This is the only drop-target affordance in the app today, and it's a good one — MUI-idiomatic, uses the theme's own primary colour, doesn't fight the element's own layout (a dashed *outline*, not a border, so it doesn't shift anything by taking up layout space). §5.2 adopts it as the app-wide standard rather than inventing a new one.

### 2.5 The one existing "this row can be clicked to navigate" affordance

`TeamsManagementPage.tsx`'s own Name column: `cursor: pointer` plus `text-decoration: underline` on hover, added specifically because "a plain grid cell otherwise gives no visual hint it opens something." This is the only click-affordance precedent in the app and, like the drag ones above, was invented once, locally, and never generalised.

## 3. Design Principle

**A gesture's meaning is fixed app-wide by what it *is*, not by which window it happens to run in.** Concretely: before adding any new click or drag interaction, ask "which of the fixed vocabulary items below does this belong to," not "what feels right for this screen." Where an existing screen's own behaviour doesn't match the vocabulary once it's fixed (§2.1's four double-click grids, §2.3's two different draggable cues), that's a defect to fix as part of this stage, not a difference to preserve for compatibility — none of them are load-bearing on any particular gesture; nothing else in the app depends on Search or Admin Tools specifically needing a double click.

## 4. Click Semantics Policy

**Rule: opening an item is a single click, everywhere, with exactly one standing exception — a grid whose single click is already claimed by inline cell editing uses double-click instead, and only because of that conflict.**

Today, `TaskGrid` is the only grid in the app with inline cell editing, so it's the only screen the exception actually applies to. If a future screen ever gains inline editing, the same exception applies to it too, automatically, by the same reasoning — this is a *rule*, not a hand-maintained list of screens.

**Changes required** (retrofitting the four grids from §2.1 that don't actually need double-click):

| Screen | Change |
|---|---|
| `SearchPage.tsx` | `onRowDoubleClick` → `onCellClick` (skip action-type cells, matching `TeamsManagementPage.tsx`'s own precedent) |
| `AdminPage.tsx` (both grids) | Same |
| `DashboardPage.tsx` | Same |
| `TaskGrid.tsx` | **No change** — keeps double-click, for the one reason that actually justifies it |
| `TeamsManagementPage.tsx`, `Project.tsx`, `Component.tsx` | **No change** — already correct |

This does *not* touch `TaskGrid`'s own single-click-starts-edit behaviour on a governed cell, which is unrelated and unaffected.

## 5. Interaction Affordances — a shared visual vocabulary

Three states, one consistent treatment for each, built as shared primitives so a new screen reuses them rather than inventing a third variant of something the app already has two of.

### 5.1 "This can be clicked to open something"

Adopt `TeamsManagementPage.tsx`'s own existing pattern as the app-wide standard: `cursor: pointer` plus `text-decoration: underline` on hover, scoped via `data-field` to just the cell(s) that actually open something (never the whole row indiscriminately, since an Actions cell alongside it isn't a "click to open" target).

**New shared primitive**: `clickableCellSx(field: string)` in `components/DenseDataGrid.tsx` (or a new small `lib/interactionSx.ts`, if it turns out other non-grid places need it too — e.g. `Project.tsx`/`Component.tsx`'s own tree rows, which are plain `Box`es, not DataGrid cells) — returns the `sx` object `TeamsManagementPage.tsx` currently hand-writes inline, so every future single-click-to-open cell/row looks identical without re-deriving the CSS.

**Retrofit**: apply it to `Project.tsx`/`Component.tsx`'s own sub-item rows (currently `onClick` with no hover cue at all — functionally already correct per §4, but with no visual affordance to tell a user that's true) and to every grid converted in §4.

### 5.2 "This is a valid drop target, right now"

Adopt `DependenciesPanel.tsx`'s own existing pattern (§2.4) as the app-wide standard: `outline: "2px dashed", outlineColor: "primary.main", outlineOffset: "-2px", borderRadius: "4px"`, applied only while a compatible drag is actually hovering (`onDragOver`/`onDragLeave` toggling local state), never as a persistent cue — a drop target only needs to announce itself once a drag that could actually land there is already in progress.

**New shared primitive**: `DROP_TARGET_ACTIVE_SX` exported from `lib/dnd.ts` (which already exists, currently holding just `TASK_DRAG_MIME_TYPE`) — a plain constant, not a hook, since the actual `onDragOver`/`onDragLeave`/`onDrop` wiring differs enough per target (what MIME type it accepts, what mutation it calls, whether it needs the auto-tab-switch behaviour) that a one-size hook would just be a thin wrapper fighting each call site's own real differences. `DependenciesPanel.tsx` itself is refactored to import this constant rather than keep its own private copy, so there's exactly one definition once this stage lands, not one original plus new callers that merely match it by eye.

### 5.3 "This is draggable"

**Resolve the §2.3 split.** Recommendation: keep both cursor treatments, but tie the *choice* between them to a stated rule rather than per-screen taste, so the next screen doesn't have to guess: a **discrete, small drag handle** (an icon/badge that is not also the row's own primary content — Task Detail's own title badge is exactly this shape) gets `cursor: "grab"`, since the cursor change is scoped to a small, deliberate target the user is already looking at; a **whole draggable row** in a dense list (the Gantt view's own bars) keeps `cursor: "default"` plus the dotted hover-highlight, since changing the cursor for an entire row the user might also be hovering for other reasons (reading its label, right-clicking it) is a heavier-handed signal than the row-highlight already gives for free. This isn't declaring one of the two existing choices wrong — it's naming *why* each one is right for its own shape, so future screens pick the same way rather than by feel.

**New shared primitives**, both in `lib/dnd.ts`:
- `DRAG_HANDLE_SX` — the small-badge treatment (`cursor: "grab"`), for any future discrete drag handle (e.g. a Project/Component row's own drag handle, §7.1).
- `draggableRowHighlightSx(isHovered: boolean)` — the Gantt's own dotted-highlight-on-hover treatment, extracted so a future dense-list drag source (e.g. reordering rows somewhere other than the Gantt, should that ever come up) doesn't reinvent it.

Neither existing call site (`TaskDetailPage.tsx`'s badge, `PlanPage.tsx`'s row highlight) is behaviourally changed — both are refactored to import the shared constant instead of keeping their own private, now-duplicate definition.

## 6. Drag Gesture Vocabulary

A fixed, small set of *intents* — not "what does dragging do on this screen," but "what does holding this key, while dragging, always mean, regardless of which screen it happens on."

| Gesture | Intent | What it does |
|---|---|---|
| **Plain drag** | **Move** | Relocates the dragged item into the drop target, removing it from wherever it was before (reparenting, moving a Task into a different Project/Component) |
| **Ctrl+drag** | **Link** | Creates an association/reference between the dragged item and the drop target, without moving the dragged item at all (Dependency creation) |
| **Shift+drag** | **Adjust** | Changes a property of the dragged item *itself*, in place, via the gesture's own motion — no hierarchy change, no link created (the Gantt's own bar reschedule, already built) |
| OS file drag (Attachments) | **Attach** | A deliberate, stated exception — dragging *from the operating system*, not from elsewhere in the app, so there's no app-internal modifier-key vocabulary to apply; a user dragging a file out of Explorer isn't thinking in this app's own Move/Link/Adjust terms at all |

**This table is the actual deliverable of the "comparable behaviour" request** — not "every drag interaction supports all three gestures," which isn't true and doesn't need to be (Dependency creation is Link-only; reparenting is Move-only; nothing currently needs both on the same drag source at once, unlike V1.2's own per-source "Ctrl picks Move vs. Link" branching, which V2 doesn't need to reproduce since none of the new interactions share a single drag source with two different valid targets the way V1.2's grid rows did). What matters is that **wherever a gesture *is* supported, it always means the same thing**: Ctrl always means "create a link, don't move anything"; plain drag always means "relocate this"; Shift always means "adjust this item's own properties in place." A user who learns Ctrl+drag creates a Dependency in Task Detail (already true today) can walk up to a Project's own Dependencies tab once Stage 5 extends the same gesture there (§7.4) and already know what it does, without re-learning anything.

**V1.2's own Component-reparenting exception (§2.2, "ignores Ctrl") is deliberately not carried forward.** Making Component reparenting behave identically to Project reparenting (plain drag only, since Ctrl is reserved for Link everywhere else) removes a special case rather than preserving one — keeping it would be exactly the kind of per-window inconsistency this whole document exists to close, for a V1.2 quirk that was never a considered design choice to begin with (nothing in `UserInterfaceWindows.md` explains *why* Component was the one exception).

## 7. Stage 5's Actual New Drag-and-Drop Features

Each mapped against §6's vocabulary and built on §5's shared primitives from the start.

### 7.1 Reparent a Project (Move)

Drag a Project row (`Project.tsx`, in the tree — same rows §4/§5.1 already made single-click-to-open) onto a different Project row to reparent it there. Drag handle: a small icon (`DRAG_HANDLE_SX`, §5.3) beside the row's own name — not the whole row, since the whole row is already a single-click-to-open target (§4) and a `draggable` attribute on the same element a plain click opens would make every ordinary click-to-open attempt also register as a (near-instant, sub-pixel) drag start, which browsers handle inconsistently. Drop target: the row being dropped onto gets `DROP_TARGET_ACTIVE_SX` (§5.2) while a compatible drag hovers it. Mutation: the existing `useReparentTask()`-shaped hook this project's own convention already establishes (`D1.4-7`) — here, `useReparentProject()`, calling the same PATCH the explicit tree-picker (`D-Win-9`) already uses. A circular-reparent guard (dropping a Project onto its own descendant) blocks with an inline message, mirroring V1.2's own circular-dependency guard shape (`UserInterfaceWindows.md` §3.2's Drag and Drop Behaviour).

### 7.2 Reparent a Component (Move)

Identical shape to 7.1, on `Component.tsx`'s own rows — deliberately not special-cased (§6's own call).

### 7.3 Move a Task into a different Project or Component (Move)

Drag a Task row (in `TaskGrid`, embedded within Project/Component Detail) onto a Project or Component row. Since `TaskGrid` rows already need double-click to open (§4's one exception) and single-click to start editing a governed cell, the drag handle can't be a whole-row `draggable` either, for the same reason as 7.1 — a small handle in the row (e.g. beside the existing Delete icon in the Actions column) is the source. Drop target styling and reparent-guard shape mirror 7.1/7.2. Mutation: `useMoveTask()` (or reuse a suitably generalised `useReparentTask()`, if the existing Task-move mutation from `D1.4-7`'s own foundational work already fits — confirm against `api/hooks.ts` before adding a new one).

### 7.4 Dependency creation, extended to Project/Component Dependencies tabs (Link)

Extends `D1.4-10`'s own already-shipped mechanism (§2.2) — same `TASK_DRAG_MIME_TYPE`/Ctrl+drag/auto-tab-switch/dashed-outline shape, same `useCreateDependency()` mutation, now also reachable by dragging onto a Project's or Component's own Dependencies tab (`DependenciesPanel.tsx` is already shared across Task/Project/Component per `D1.4-46`), not just another Task Detail window. No new vocabulary, no new visual language — this is the proof that §5/§6's shared primitives actually generalise, since it's the same interaction landing on a target type it wasn't originally built for.

### 7.5 OS-file drag onto an Attachments grid (Attach)

A real drop target (`AttachmentsPanel.tsx`), but the *source* is the operating system, not another part of this app — no modifier-key vocabulary applies (§6's stated exception). Gets the same `DROP_TARGET_ACTIVE_SX` hover treatment as every other drop target in §5.2, for the same reason: a user should always get the same visual confirmation that a drop is about to land somewhere valid, regardless of what's on the other end of the drag.

### 7.6 Already-built, unaffected by this stage

- **Ctrl+drag Task→Task Dependency creation** (`D1.4-10`) — unchanged; already conforms to §6's own vocabulary (it's what §6 was grounded in).
- **Gantt bar Shift+drag reschedule** (§3.7) — unchanged; already conforms (it's what defines the Adjust slot).
- **Gantt row reorder** (`D1.4-27`) — a plain, in-list drag, but reordering *within* one list rather than moving *between* containers, so it doesn't fit neatly into Move/Link/Adjust at all; flagged here as a genuine open question (§8) rather than silently forced into one of the three.

## 8. Open Questions for Confirmation

1. **Gantt row reorder's own category.** §7.6 flags it as not cleanly Move/Link/Adjust. Recommendation: treat "reorder within the same list" as its own, fourth, narrowly-scoped exception (like OS-file-drag/Attach) rather than stretching Move to cover it — a reorder never removes an item from a *different* container the way every other Move in §7 does.
2. **Whether 7.1-7.3's drag handle is a new icon, or reuses an existing one already in that row** (e.g. `TeamsManagementPage.tsx`'s own row icons already sit at `rgba(0,0,0,0.87)`/`0.18` per `D1.4-98`-`D1.4-100`'s own established convention) — a genuine visual-design call best confirmed against a real rendered screen rather than decided from a text description alone.
3. **Sequencing**: recommend landing §4/§5/§6 (the consistency retrofit) as one self-contained pass first, verified and committed on its own, before starting any of §7's new interactions — so the new work is built on the corrected foundation from day one rather than needing its own follow-up retrofit.

See `4_GuiClient/Plan.md` §6.5 for how this fits the overall Level 1 build order.
