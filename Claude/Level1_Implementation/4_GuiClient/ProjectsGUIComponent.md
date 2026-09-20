# Project / Projects GUI Components — Design

**Status: design confirmed (`D1.4-58`–`D1.4-62`, `D1.4-64`), no code written yet.** Written before any code, per the user's own request, following the same before-code-review pattern `TaskGridPlan.md`/`ProjectDetailPlan.md` (this folder) already established. `Q1.4-63` (§7) is deliberately left open — build the initial implementation against it, then revisit.

<a id="contents"></a>
## Contents

1. [Purpose and Scope](#purpose-and-scope)
2. [V1.2's Real Implementation](#v12-implementation)
   - 2.1 [ProjectControl — the "Project" Analogue](#project-control)
   - 2.2 [ProjectStackControl — the "Projects" Analogue](#project-stack-control)
   - 2.3 [ProjectDetail — the Window That Hosts Them](#project-detail-window)
3. [Requirements](#requirements)
4. [Design](#design)
   - 4.1 [Component API: `Project` (Singular)](#project-api)
   - 4.2 [Component API: `Projects` (Plural)](#projects-api)
   - 4.3 [The Recursion/Toggle-Duplication Problem](#recursion-problem)
   - 4.4 [State Ownership](#state-ownership)
   - 4.5 [TaskGrid Embedding](#taskgrid-embedding)
   - 4.6 [Permissions and Row Actions](#permissions-and-actions)
5. [What This Replaces](#what-this-replaces)
6. [Implementation Options](#implementation-options)
   - 6.1 [Option A — Keep `@mui/x-tree-view`, Embed TaskGrid Inside `TreeItem`](#option-a)
   - 6.2 [Option B — Bespoke Recursive Components, No Tree Widget](#option-b)
   - 6.3 [Decision](#recommendation)
7. [Open Items for Review](#open-items)

<a id="purpose-and-scope"></a>
## 1. Purpose and Scope

The user asked for two related things on the "Top Level Projects" window (`ProjectDetailPage.tsx`'s no-Project-open mode):

1. Where Tasks are shown on this window, use a real `TaskGrid` instance, not the current one-line-per-Task tree row.
2. Factor the whole Project/sub-Project/Task browsing structure into two new, independently reusable GUI components — `Project` (singular) and `Projects` (plural) — defined recursively, so the same pattern repeats to any depth, and matching V1.2's own real precedent for this (studied in §2).

This document covers the design of both, and the implementation options for building them in React — explicitly not the implementation itself, which is a separate, later step once the user has reviewed this.

<a id="v12-implementation"></a>
## 2. V1.2's Real Implementation

Read directly from `V1.2/Apps/ProjectPal/ProjectPal/Projects/ProjectControl.xaml(.cs)`, `ProjectStackControl.xaml(.cs)`, and `ProjectDetail.cs`, rather than inferred. Unlike `GridControl` (`UserInterfaceWindows.md` §3.20, WinForms), this part of V1.2 is WPF — `UserControl`s composed via XAML, not `DataGridView`.

<a id="project-control"></a>
### 2.1 ProjectControl — the "Project" Analogue

One `ProjectControl` instance represents exactly one Project. Its own layout is a header row — a "+/−" expand/collapse button, an "Add Task" icon, and the Project's own name as a draggable, droppable, rename/delete-context-menued label, followed by a `"(Priority - activeTasks / totalActiveTasks)"` count summary (deferred out of this design, `D1.4-62` — Stage 6 Polishing, `D1.4-42`) — plus, only while expanded (`m_detailVisible`, a plain instance-local boolean, not shared/global state), whatever `CreateSubPanel()` builds:

1. **A Task grid** (`AddTaskGrid()`) — one embedded `GridControl` (§3.20) instance for this Project's own Tasks, but with a *narrower* column set than the full Task Grid/All Tasks window uses: `m_columnOrder` omits the Project column (redundant — you're already looking at this Project's own section) and Detailed Description, both hidden explicitly (`m_hiddenTaskColumns`). Only built at all if there's at least one Task to show for the currently-selected visibility (see below) — an expanded Project with no visible Tasks gets no grid at all, not an empty one.
2. **A nested `ProjectStackControl`** holding one `ProjectControl` per sub-Project (§2.2) — this is the recursive step. Hidden Projects are skipped outright; an "active projects only" filter (a checkbox on the outer window, threaded down as a callback, §2.3) can additionally skip inactive ones.

Both are rebuilt from scratch on every genuine structural change (`DestroySubPanel`/`CreateSubPanel`) but *diffed* against the currently-displayed children on an ordinary `Redisplay()` (insert/remove/update by comparing sorted DB state against what's already on screen) — a WPF-specific optimisation with no React equivalent needed (React's own reconciliation already does this).

Only the *first* level of Projects placed directly into the outermost `ProjectStackControl` auto-expands (`openSubProjects: true`, passed once at construction) — every recursive call one level down always passes `false`, so a grandchild Project starts collapsed and needs an explicit click. Rename/delete (via the name label's context menu) and the "Add Task" icon are all gated on `Permissions.IsAllowed(project.Owner, Project, Edit)` — dimmed (`Opacity = 0.4`) or removed outright when not allowed, not just disabled-but-visible.

`GUIProject.Tasks(TasksDisplayValues)` itself only ever distinguishes `All` (every Task) from "not All" (open only, i.e. not Closed/Cancelled) — the third value, `None`, is never handled inside this method at all. Every caller checks for `None` itself, before ever calling `Tasks(...)`, and simply doesn't build a grid in that case. This is a deliberate separation, not an oversight: "should Tasks show at all" and "which Tasks, once showing" are two different questions, answered in two different places.

<a id="project-stack-control"></a>
### 2.2 ProjectStackControl — the "Projects" Analogue

A `ProjectStackControl` is a bare, scrollable, ordered list of `ProjectControl`s — `AddProject`/`InsertProject`/`RemoveProjectAt`/`ProjectAt`/`ChildCount`. It has **no** task-visibility toggle, no "active projects only" checkbox, and no sorting/filtering logic of its own — every one of those lives on the surrounding window (§2.3) instead, and is handed down as a plain delegate/callback reference through every `ProjectControl`/`ProjectStackControl` in the tree, however deep. Structurally, this is the "one thing holds a list of the other thing" half of the recursion — `ProjectControl`'s own §2.1's nested list is a `ProjectStackControl`, and `ProjectStackControl` never holds anything but `ProjectControl`s.

<a id="project-detail-window"></a>
### 2.3 ProjectDetail — the Window That Hosts Them

The WinForms `ProjectDetail` Form owns exactly **one** `ProjectStackControl` (`m_theProjectsStackControl`), reused unchanged for two different purposes depending on how the window was opened:

- **No root Project** (`GetAndShowDetailWindow(null)`) — the "Top Level Projects" browser. The stack control is seeded with every top-level Project the user can see.
- **A specific root Project** — the stack control is seeded with exactly *one* Project (the one this window is showing), with `openSubProjects: true` so it starts expanded, immediately showing its own Tasks/sub-Projects without an extra click.

The window itself owns the `None`/`Open`/`All` radio buttons (`TasksToDisplay()`) and the "active projects only" checkbox (`ActiveProjectsOnlyFn()`) — both read once per `Redisplay()` and passed down as the same delegate reference at every level, so the whole tree, however deep, always agrees on the current filter with nothing to keep in sync. Sibling Projects are sorted by Priority (`GUIProject.SortProjectsByPriority`) — the same rule V2 already implements (`priorityWeight`, D1.4-47).

<a id="requirements"></a>
## 3. Requirements

Restating the user's own two asks precisely, since everything in §4 onward is scoped to them:

1. Wherever the "Top Level Projects" window shows Tasks, it should use a real `TaskGrid` instance (`TaskGridPlan.md`) — sortable, filterable, inline-editable, right-click menu and all — not the current one-line-per-Task `TreeItem` (`ProjectTaskTree.tsx`'s `TaskRowLabel`).
2. Two new, genuinely reusable components, defined recursively:
   - **`Project`** (singular): a Project's own name, followed by a `TaskGrid` for that Project's own Tasks, followed by a list of further `Project` components — one per sub-Project — "this pattern repeats to any depth."
   - **`Projects`** (plural): a Task-visibility toggle (None/Open/All, as already exists at the top of "Top Level Projects" today), followed by a list of `Project` components.
   - The "Top Level Projects" window becomes, in essence, a single `Projects` instance, seeded with the list of top-level Projects.
   - `Projects` is explicitly meant to be reused "on a number of different windows," not just this one.

<a id="design"></a>
## 4. Design

<a id="project-api"></a>
### 4.1 Component API: `Project` (Singular)

Illustrative shape, not final code:

```ts
interface ProjectProps {
  project: ProjectRecord;

  // Shared reference data, threaded down unchanged at every level — the
  // same "controlled, data-in" pattern TaskGrid itself already uses
  // (TaskGridPlan.md §4.1), so however deep the tree goes, there's still
  // exactly one fetch per window, not one per node.
  projects: ProjectRecord[];
  tasks: TaskRecord[];
  components: ComponentRecord[];
  people: PersonRecord[];
  personRoles: PersonRoleRecord[];
  scheduleGraph: ScheduleGraph;
  resourceIdsByTask: Map<number, number[]>;
  attachmentsCountByTask: Map<number, number>;
  remarksCountByTask: Map<number, number>;

  // A read-only signal from whichever `Projects` sits at the top of this
  // tree (§4.4) — a `Project` never owns or changes this itself.
  taskVisibility: TaskVisibility;

  // Actions — opened/handled by whichever window embeds this, exactly
  // like today's `TreeCallbacks` (`ProjectTaskTree.tsx`). No
  // `onAddSubProject` (`D1.4-61`) — V1.2 itself never offers that per-row,
  // only "Add Task" (§2.1); creating a new Project stays a page-level
  // action, unchanged from how `ProjectDetailPage.tsx` already does it.
  // No permission-check props at all (`D1.4-64`, §4.6) — `Project` decides
  // for itself whether each action is currently allowed.
  onOpenProject: (project: ProjectRecord) => void;
  onRenameProject: (project: ProjectRecord) => void;
  onDeleteProject: (project: ProjectRecord) => void;
  onAddTask: (parent: ProjectRecord) => void;

  // Only the direct children of the outermost `Projects` start expanded
  // (§2.1's own one-level auto-expand) — every deeper `Project` defaults
  // to `false`.
  initiallyExpanded?: boolean;
}
```

Renders: a header row (name — clickable to open this Project's own detail — rename/delete/"Add Task" icons, each shown only when `canManageTeam` allows, matching V1.2's own per-row affordances exactly, §2.1) with its own local `expanded` state (`useState`, defaulting from `initiallyExpanded`); when expanded, a `TaskGrid` scoped to this Project's own Tasks (only rendered at all if there's at least one visible Task, matching §2.1) followed by a plain list of `Project` elements, one per sub-Project (via the same `childProjectsOf` sort already in `ProjectTaskTree.tsx`, unchanged), each with `initiallyExpanded: false`.

<a id="projects-api"></a>
### 4.2 Component API: `Projects` (Plural)

```ts
interface ProjectsProps {
  // The sibling set to render — every top-level Project, or one Project's
  // own direct children, depending on where this instance sits.
  projects: ProjectRecord[];

  // ...the same shared reference data and action callbacks as ProjectProps...

  // See §4.3 — whether this instance renders its own toggle at all.
  showToggle?: boolean; // default true
  defaultTaskVisibility?: TaskVisibility; // default "None", used only when showToggle
  taskVisibility?: TaskVisibility; // required when showToggle is false — see §4.4
}
```

Renders: the `None`/`Open`/`All` toggle (when `showToggle`), then a plain list of `Project` elements — the actual "list of Project" rendering (sort, map, `key`) lives in exactly one place regardless of which implementation option (§6) is chosen.

<a id="recursion-problem"></a>
### 4.3 The Recursion/Toggle-Duplication Problem

The user's own description of `Project` says its own third part is "a list for **further `Project` GUI components**," not "a nested `Projects` component" — and for good reason: if `Project`'s own children were rendered by instantiating another `Projects`, and `Projects` always renders its own toggle, every level of the tree would grow its own toggle, which is wrong — V1.2 shows exactly one toggle, on the outermost window, full stop (§2.3).

**Decided (`D1.4-58`):** give `Projects` a `showToggle` prop (default `true`), and have `Project`'s own sub-Project section literally instantiate `Projects` again with `showToggle={false}` and the inherited `taskVisibility` passed straight through — truest to V1.2's own literal structure (`ProjectStackControl` nested inside `ProjectControl`), and keeps "sort siblings, render a `Project` per one" in exactly one place rather than duplicated between `Projects` and `Project`.

<a id="state-ownership"></a>
### 4.4 State Ownership

**Task visibility.** Whichever `Projects` sits at the very top of a tree owns the actual `taskVisibility` value — self-contained (uncontrolled), defaulting to `"None"` (D1.4-47), so an embedding window gets a fully working toggle for free, with nothing of its own to wire up beyond rendering `<Projects projects={...} />`. This is a deliberate difference from `TaskGrid`'s own filter state (window-supplied `initialFilterState`, TaskGridPlan.md §4.6) — there's no evidence any embedding window needs a different starting value here, and self-containment matches the "reused on a number of different windows with minimal per-window wiring" goal more directly than a controlled prop would. Every `Project`, and every *nested* `Projects` (§4.3, `D1.4-58`), only ever receives this value as a plain prop, read-only, exactly mirroring how V1.2's own `TasksToDisplayFn` delegate is threaded down unchanged to any depth.

**Expand/collapse.** Purely local to each `Project` instance (`useState`, §4.1) — never lifted, never shared — matching V1.2's own per-`ProjectControl` `m_detailVisible` exactly. Collapsing a `Project` un-mounts its own `TaskGrid` and child `Project`s entirely (matching `DestroySubPanel`) rather than merely hiding them, so a large hierarchy's actual rendering cost tracks how much is *currently expanded*, not how large the whole tree is.

<a id="taskgrid-embedding"></a>
### 4.5 TaskGrid Embedding

Requirement 1 (§3) — each `Project`'s own Tasks render through a real `TaskGrid` instance, scoped (via its own `tasks` prop) to just this Project's Tasks, already filtered by the inherited `taskVisibility`. Two gaps this surfaces in `TaskGrid` itself:

- **Sizing (`D1.4-59`).** `TaskGrid.tsx` currently hard-codes `<Box sx={{ height: 600 }}>` — appropriate for a full-page grid, absurd for a small embedded one (a Project with two Tasks doesn't need 600px). MUI DataGrid's own `autoHeight` prop (sizes to content, no internal scrollbar) is the natural fit — decided to apply **always**, in every `TaskGrid` instance including `AllTaskPage`, not only embedded ones: confirmed as a deliberate, welcome behaviour change to the already-shipped `AllTaskPage` too, not something needing a caller-set flag to opt into per context. The wrapping `<Box sx={{ height: 600 }}>` itself has to change too, not just the `DataGrid`'s own `autoHeight` prop — left as a fixed 600px box, it would either clip a taller grid or leave dead space under a shorter one, defeating the point; it needs to size to its content the same way (e.g. drop the fixed `height` entirely).
- **Footer (`D1.4-59`).** A grid whose own rows all fit on one page has no use for a "1–100 of 100" pagination footer — decided to hide it automatically whenever there are 100 or fewer Tasks to display (`filteredTasks.length <= 100`, DataGrid's own `hideFooter` prop), computed by `TaskGrid` itself from its own current row count rather than supplied by the caller, and — like `autoHeight` — applying equally to `AllTaskPage` and every embedded instance. The footer (and its pagination) only ever appears once there's genuinely more than one page's worth of Tasks to page through.
- **Column set.** Matching V1.2's own narrower embedded column set (§2.1) — a new, smaller constant (alongside the existing `DEFAULT_TASK_GRID_COLUMNS`) omitting `project_id` (redundant — you're already inside this Project's own section) and `detailed_description`, passed via `TaskGrid`'s already-existing `columns` prop. No new `TaskGrid` capability needed for this part, just a different call-site value.
- **Reference data `ProjectDetailPage.tsx` doesn't fetch today.** Every `TaskGrid` prop needs `components`, `attachmentsCountByTask`, and `remarksCountByTask` (§4.1) — `ProjectDetailPage.tsx` currently fetches none of `useComponents()`/`useAllRemarks()`/`useAllAttachments()` at all, and its own `resourceCountByTaskId` (built from the `useAllTaskResources()` it already fetches) is a per-Task *count* for schedule/duration math, not the `Map<number, number[]>` of person ids `TaskGrid`'s own Resources column and `isAssignedResource` check need. `AllTaskPage.tsx` already computes every one of these from the same underlying hooks — a direct port of that existing code, not a new design problem, but a genuinely required step before `TaskGrid` can render inside `ProjectDetailPage.tsx` at all.

Everything else — inline editing, `TaskGrid`'s own field-level permissions, the right-click menu (D1.4-56), delete — is reused completely unchanged; none of it is specific to the embedded case.

<a id="permissions-and-actions"></a>
### 4.6 Permissions and Row Actions

**Decided (`D1.4-61`):** `onAddTask` (§4.1) is new — a per-row "Add Task" icon at every depth, matching `imageAddTask` on every `ProjectControl` (§2.1) exactly, closing the gap where V2 today only offers "Add Task" once, for the currently-open Project, via a button sitting above the tree on `ProjectDetailPage.tsx` itself. Confirmed by re-checking `ProjectControl.xaml`/`.xaml.cs` directly: V1.2 has no per-row "Add (Sub-)Project" affordance anywhere at all — only `imageAddTask` and a Delete/Rename context menu. Creating a new Project is not part of `Project`'s own design at all, then — it stays exactly as it already is, a page-level action on whichever window is currently open (`ProjectDetailPage.tsx`'s existing "Add New Project" button), unaffected by this work.

**Decided (`D1.4-64`) — permission checks move inside `Project`, and a real pre-existing bug this surfaced gets fixed at the same time.** `ProjectTaskTree.tsx` today accepts a single injected `canManageTeam: (teamId) => boolean` callback (`ProjectDetailPage.tsx`'s own `hasRoleAtLeast(person, teamId, "LeadUser")`), used to gate *both* the rename and delete icons. Checked directly against what `rest-api/app/routes/projects.py` actually enforces, neither actually matches it:

| Action | Server actually requires | `canManageTeam` currently checks |
|---|---|---|
| Rename (a `name` PATCH) | `require_owner_or_team_lead` — i.e. `canEditOwnedRecord(person, project.team_id, project.owner_person_id)` | `hasRoleAtLeast(person, teamId, "LeadUser")` — wrong: ignores the Project's own owner entirely |
| Delete | `is_team_lead(team_id)` only, no owner exception | *(the same, too-permissive check as rename)* |

Concretely: on a Team with two LeadUsers (the seed data already has this), each currently *sees* a working-looking rename/delete icon on the *other's* Projects, which the server then rejects with a 403 on click — a real, pre-existing bug in already-shipped code, not something introduced by this design, but directly relevant to it since `Project`'s own callback API is being properly specified right now anyway.

Fixed by removing permission-check props from `Project`/`Projects` entirely, rather than just correcting `canManageTeam`'s own formula in place — the same centralising principle `D1.4-48` already established for `TaskGrid` ("never a per-window override... used identically by every window"), extended here: every one of `lib/permissions.ts`'s `canEditOwnedRecord`/`isTeamLead`/`hasRoleAtLeast` is already a plain, entity-agnostic function needing only `(person, teamId, ownerId)`-shaped arguments — nothing Task-specific — so `Project` can call `useAuth()` and these functions directly, the same way `TaskGrid` itself already does, rather than trusting each embedding window to correctly re-derive and inject the right check (which is exactly how this bug happened in the first place). Concretely, `Project` computes, per row, from its own `project` prop:

```ts
const { person } = useAuth();
const canRename = canEditOwnedRecord(person, project.team_id, project.owner_person_id);
const canDelete = isTeamLead(person, project.team_id);
const canAddTaskHere = hasRoleAtLeast(person, project.team_id, "LeadUser");
```

`onRenameProject`/`onDeleteProject`/`onAddTask` (§4.1) stay as injected callbacks — *which* dialog opens, and how, is still genuinely page-specific (`ProjectDetailPage.tsx`'s own `DialogState`) — only *whether the icon shows at all* moves out of the embedding window and into `Project` itself. `ProjectDetailPage.tsx`'s own header Delete button and "Add New Project"/"Add Task" buttons (`canDelete`/`canCreateHere`, unaffected by this — already independently verified to already match the server exactly) are untouched; this only fixes the tree's own per-row icons.

<a id="what-this-replaces"></a>
## 5. What This Replaces

`ProjectTaskTree.tsx` — today's single exported component, built on `@mui/x-tree-view`'s `SimpleTreeView`/`TreeItem` (chosen specifically for this browsing/management tree, D1.4-1) — is replaced entirely by `Project`/`Projects`. `ProjectDetailPage.tsx` changes in two ways: its own `taskVisibility` state and the `ToggleButtonGroup` it currently renders directly (lines ~106, ~411–420) move into `Projects` itself (§4.4); and its tree area (currently `<ProjectTaskTree rootProjectId={id} .../>`, `ProjectDetailPlan.md` §4.4) becomes a single `<Projects projects={childProjectsOf(projects, id)} .../>` call — the "Top Level Projects" mode (`id == null`) and the "one Project's own tree" mode both become the exact same call, differing only in which Projects are passed in — matching the user's own framing that "Top Level Projects" is just a `Projects` instance seeded with the top-level list.

<a id="implementation-options"></a>
## 6. Implementation Options

<a id="option-a"></a>
### 6.1 Option A — Keep `@mui/x-tree-view`, Embed TaskGrid Inside `TreeItem`

Keep today's `SimpleTreeView`/`TreeItem` structure, and render a `TaskGrid` as part of (or alongside) a Project `TreeItem`'s own children.

**Against this:** `@mui/x-tree-view` owns its own keyboard navigation, focus, and selection model across every `TreeItem` in the tree — arrow keys move a single "current" selection up/down through every visible node, Home/End jump to the first/last, and clicking anywhere selects that node. `TaskGrid` (via `@mui/x-data-grid`) owns an equally real, and different, internal keyboard/focus model of its own (arrow keys move the focused *cell*, Enter commits an edit, Tab moves across cells) — nesting one inside the other means two independent widgets both trying to own arrow-key behaviour in the same DOM subtree, which is the kind of interaction bug that's easy to trigger and awkward to fully fix (e.g. arrowing through grid rows unexpectedly also moving the tree's own selection, or vice versa). There's also no clean way to size a `TreeItem` around an arbitrarily-tall grid without fighting the tree's own virtualization/row-height assumptions, since `@mui/x-tree-view` expects each item to be roughly uniform, list-like content — not another fully interactive grid.

<a id="option-b"></a>
### 6.2 Option B — Bespoke Recursive Components, No Tree Widget

Drop `@mui/x-tree-view` for this specific feature entirely (`D1.4-1`'s own choice of it stays correct for *other* uses of tree-like browsing, if any arise — this isn't revisiting that decision generally, only for this one case). `Project`/`Projects` become plain function components built from ordinary `<Box>` layout and local `useState` for expand/collapse (§4.4) — structurally the direct React translation of V1.2's own `UserControl` composition (§2), not a tree-selection widget at all. This matches the user's own description precisely (a plain block layout: name, then a grid, then a list of more of the same), avoids Option A's two-different-keyboard-models collision outright, and gives `TaskGrid` exactly the same amount of room and behaviour it already has everywhere else it's used (`AllTaskPage`) — including, now, `autoHeight` and count-based footer-hiding (§4.5, `D1.4-59`), since those apply uniformly rather than being embedded-only special cases.

<a id="recommendation"></a>
### 6.3 Decision

**Decided (`D1.4-60`):** Option B. Option A's fundamental conflict (two widgets, each with their own real keyboard/focus model, nested inside each other) isn't a matter of more careful implementation — it's a structural mismatch between what `@mui/x-tree-view` is built to hold and what `TaskGrid` actually is, and would very likely surface as real, hard-to-fully-fix interaction bugs once built. Option B also happens to match the user's own stated design and V1.2's own real structure more directly, rather than less.

<a id="open-items"></a>
## 7. Open Items for Review

**`Q1.4-63`** — Sizing inside `ProjectDetailPage.tsx`'s own tree area: today's tree sits in a `maxHeight: 320, overflowY: "auto"` box (a single shared scroll region for the whole tree). With each expanded `Project` now potentially containing a full, `autoHeight` `TaskGrid` (§4.5, `D1.4-59`) — growing to fit its own rows rather than scrolling internally — does that fixed 320px outer box still make sense, or does it need to grow/change alongside this work? **Kept open, deliberately** — build the initial implementation with this box unchanged (a plain default, not a considered answer), and revisit once there's a real embedded-`TaskGrid` tree to actually judge it against, rather than guessing ahead of that.

(`Q1.4-61` and `Q1.4-62`, this section's other two original items, are now `D1.4-61` — §4.6 above — and `D1.4-62`, confirming `D1.4-42`'s existing Stage-6-Polishing deferral still stands, recorded in `Plan.md` §10.)
