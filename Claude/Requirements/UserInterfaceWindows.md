# ProjectPal — User Interface Windows

*Open questions in this document use the prefix `Q-Win-`; decisions use `D-Win-`.*

## Contents

1. [Purpose and Scope](#purpose-and-scope)
2. [Navigation Overview](#navigation-overview)
3. [Window Catalogue](#window-catalogue)
   - 3.1 [MainWindow](#main-window)
   - 3.2 [TaskWindow](#task-window)
   - 3.3 [TaskDetail](#task-detail)
   - 3.4 [ProjectWindow](#project-window)
   - 3.5 [ProjectDetail](#project-detail)
   - 3.6 [NewProject](#new-project)
   - 3.7 [Plan Display (Gantt View)](#plan-display)
   - 3.8 [ComponentWindow](#component-window)
   - 3.9 [NewComponent](#new-component)
   - 3.10 [RemarkWindow](#remark-window)
   - 3.11 [Find](#find-window)
   - 3.12 [Manage People](#manage-people)
   - 3.13 [New Person](#new-user)
   - 3.14 [AdminWindow](#admin-window)
   - 3.15 [ConfigWindow](#config-window)
   - 3.16 [PasswordWindow](#password-window)
   - 3.17 [Progress Indicator](#progress-window)
   - 3.18 [Merge Dialogs (Task / Project / Component)](#merge-dialogs)
   - 3.19 [GridFilterSelect](#grid-filter-select)
4. [Cross-Cutting UI Patterns](#cross-cutting-ui-patterns)
5. [Open Questions](#open-questions)
6. [Decisions](#decisions)

<a id="purpose-and-scope"></a>
## 1. Purpose and Scope

This document catalogues every window/dialog in the old `V1.2` prototype, gathered by sweeping the C# source directly: what each one shows, which parts of that are editable versus read-only, how a user interacts with it, and how a user navigates to and from it. Unlike `Goals.md`, `DomainModel.md`, `KeyConcepts.md`, and `UseCases.md`, this document **describes the old app as it is, not a design for the new one** — it's raw material to prime UI/UX design work for `V2`, in the same spirit that `DomainModel.md` and `UseCases.md` were grounded in the old codebase. Field-level *meaning* (what a Priority or an Urgency or a Dependency actually is) is defined once in `KeyConcepts.md`/`DomainModel.md` and not repeated here; this document is concerned with the UI surface — which control shows a field, whether the user can change it, and what clicking/dragging/double-clicking it does. Windows are referred to here by a design-level name rather than their old implementation class name where the two differ (e.g. the old `FormFind` class is called "Find" below) — this is a description of the UI design, not a map of the old code.

<a id="navigation-overview"></a>
## 2. Navigation Overview

The app is **not** MDI — `MainWindow` is a plain `Form` and every other window is a free-floating top-level `Show()`/`ShowDialog()` window, not an MDI child. `MainWindow` is the hub every menu-driven window traces back to:

```
MainWindow (app shell — per-person workload report + toolbar)
 ├─ TaskWindow  ──▶ TaskDetail ──▶ ComponentWindow, ProjectDetail, RemarkWindow
 ├─ ProjectWindow ──▶ ProjectDetail ──▶ NewProject, Plan Display, TaskDetail, ProjectDetail (parent/child)
 ├─ ComponentWindow ──▶ NewComponent, TaskDetail, ComponentWindow (parent/child)
 ├─ Find ──▶ Progress Indicator; double-click a result ──▶ TaskDetail, ComponentWindow, ProjectDetail, RemarkWindow
 ├─ PasswordWindow ──▶ AdminWindow (password-gated, unless already SuperUser)
 ├─ Manage People ──▶ New Person
 └─ ConfigWindow
```

Two navigation patterns recur everywhere and are described once in §4 rather than per-window: **singleton-per-object windows** (double-clicking the same Task/Project/Component/Remark twice re-focuses the existing window instead of opening a second copy) and **grid double-click opens the detail window** for whatever the row represents.

A second, independent trigger path exists outside the menu structure entirely: the app's periodic background database sync can pop a **merge dialog** (Task / Project / Component, §3.18) whenever it detects a record was edited both in memory and in the database since it was last loaded — this is not reachable from any button or menu, only from that runtime condition.

<a id="window-catalogue"></a>
## 3. Window Catalogue

<a id="main-window"></a>
### 3.1 MainWindow

**Purpose:** Application shell and navigation hub. Shows a per-Resource workload summary and is the launch point for almost everything else, including the Admin and Manage People windows.

**Displayed:**
- A read-only report grid, one row per resource: Person, open/ready/in-progress task counts, total effort, average and max Urgency (plus a synthesized "Total" row and an "Other" bucket for non-resource people).
- Status strip: an "About" dropdown (version/copyright, read-only), the current user's role (read-only), and a "Config" split button.

**Interactions:** Buttons for Task List, Show Projects, Show Components, Project List, Find, and Save (turns red when there are unsaved changes). Double-clicking a report row opens `TaskWindow` filtered to that person. A hidden manual-refresh button and a 10-second auto-refresh timer keep the report and unsaved-changes indicator current, and can force-close the app if its version drifts from the database's recorded version. Closing the window prompts to save unsaved changes.

**Navigation:** The only entry point (`Program.Main` creates it). Opens `TaskWindow`, `ComponentWindow`, `ProjectDetail`, `ProjectWindow`, Find (all modeless), and — behind a password prompt (`PasswordWindow`) — `AdminWindow`; also opens Manage People (modeless singleton) and `ConfigWindow` (modal), both from the status-strip "Config" split button's dropdown.

**Status:** Live — the primary shell, and the only place Admin and Manage People are reachable from.

<a id="task-window"></a>
### 3.2 TaskWindow

**Purpose:** The main Task list, with an embedded Gantt tab.

**Displayed:** A filterable/sortable grid (read-only cells; editing happens in `TaskDetail`) with columns ID, Urgency, Resources, Status, Tentatively-Assigned Resources, Description, Affected Component, Project, Priority, End Date, Start Date, Attachments, Remarks, Owner (plus hidden columns for Effort, % Allocation, Requested By, the old app's Private flag, Ref URL, Detailed Description). Default filter narrows to open-ish statuses and, unless the user is a SuperUser, to their own tasks; default sort is Urgency descending. A second tab lazily renders the same (filtered) tasks as a read-only Gantt chart.

**Interactions:** Double-click a row or a Gantt bar opens `TaskDetail`. Rows can be deleted directly from the grid (with confirmation). Drag-drop onto grid cells is enabled (e.g. reassigning a resource). Column headers sort/filter.

**Navigation:** Opened from `MainWindow`'s "Task List" button — modeless, multiple instances can be open at once and are redisplayed together when data changes. Opens `TaskDetail`.

**Status:** Live.

<a id="task-detail"></a>
### 3.3 TaskDetail

**Purpose:** The full single-Task editor — the primary place a Task's fields, attachments, remarks, and dependencies actually get changed.

**Displayed / editable:** Description and Detailed Description (editable text), Priority/Status/Task Type/Requested By (editable dropdowns, some options gated by whether the user is a resource on the task or has full edit rights), Resources (editable checklist), Effort amount, % Allocation, and Effort-vs-Duration type (editable — see `KeyConcepts.md`'s Effort vs. Duration entry), End Date and Requested Start Date (editable, each with a clear button), Planned Start Date (read-only, shown in red if it disagrees with the requested date), Component and Project (editable via a tree-picker; the text field itself is read-only, double-clicking it opens `ComponentWindow`/`ProjectDetail`), Urgency (read-only, computed — see `KeyConcepts.md`'s Urgency entry), Task ID / date added / status date / owner (read-only), New Owner (editable, reassign — visible only with full edit rights), Tentative and Private flags (editable, visibility permission-gated), Ref URL (editable, click to open, right-click to edit/copy). A tabbed sub-panel holds Remarks (list + "Add" opens `RemarkWindow`) and Attachments (drag-drop upload, double-click to open, delete from grid). A collapsible dependency panel shows "Depends upon"/"Dependants" lists — drag-drop to add a dependency, double-click to open the linked item, Delete key to remove it.

**Interactions:** OK saves (validates required fields for a brand-new task); Cancel discards a not-yet-saved new task and restores any dependencies removed during the edit; Delete Task (permission-gated); drag files onto the form to attach them; closing with unsaved changes prompts to save.

**Navigation:** Reached via a singleton-per-task pattern (re-focuses an existing window rather than opening a duplicate) from `TaskWindow`, `ComponentWindow`, `ProjectDetail`, Find, and the Gantt view — modeless (`Show`) in all of these. Two special constructors create a **brand-new** Task from `ProjectDetail`'s or `ComponentWindow`'s embedded controls, opened **modally** (`ShowDialog`) in that case. Opens `ComponentWindow`, `ProjectDetail`, `RemarkWindow`.

**Status:** Live — the most heavily used window in the app.

<a id="project-window"></a>
### 3.4 ProjectWindow

**Purpose:** A flat Project list — the Project counterpart to `TaskWindow`, without a Gantt tab.

**Displayed:** A read-only, filterable/sortable grid: Name, Priority, Parent (visible columns), plus Is Active, Total Active Task Count, Total Active Task Effort, Owner, Start Date, Due Date (available columns). Default filter is active projects only; default sort is Priority descending. No add/edit/delete on this window itself.

**Interactions:** Double-click a row opens `ProjectDetail`. Column sort/filter.

**Navigation:** Opened from `MainWindow`'s "Project List" button — modeless. Opens `ProjectDetail`.

**Status:** Live.

<a id="project-detail"></a>
### 3.5 ProjectDetail

**Purpose:** The hierarchical Project editor — one Project's own fields plus a nested, expandable tree of its sub-projects and their tasks. Opened with no Project, it doubles as the "Top Level Projects" browser.

**Displayed / editable:** Title (drag-source/drop-target for reparenting), clickable Parent link (navigates up), Priority and New Owner (editable dropdowns), Due Date and Start Date (editable, with clear/reset), computed End (read-only), Detailed Description (editable), Private flag (editable, visible only to the owner), a view-only "only active tasks" filter and a Task-visibility radio group (None/Open/All — a display filter, not a Project field), an Attachments grid (drag-drop upload), and the same drag-drop Dependency panel as `TaskDetail`. Project ID is read-only. Each sub-project row (rendered by an embedded WPF tree) shows its name (drag-source/drop-target, click opens that project's own `ProjectDetail`), a task count/priority summary, an "Add Task" button, an expand/collapse toggle that lazily loads nested tasks/sub-projects, and a right-click menu (Delete — blocked while it has dependants; Rename — opens `NewProject`). A toolbar offers "Add New Project" and "Gantt Display".

**Interactions:** Add/rename/delete a sub-project; drag-drop to reparent projects or move a Task into a project; drag-drop dependency links; expand/collapse rows; open the Gantt view for this project's subtree; click the parent link to navigate up.

**Navigation:** Reached via a singleton-per-project pattern from `ProjectWindow`, `TaskDetail` (Project field / dependency double-click), the Gantt view, the embedded project-row control (click to open a sub-project), and `MainWindow`'s "Show Projects" button (opens the top-level/no-project view) — modeless in all cases. Opens `NewProject` (modal), Plan Display, `TaskDetail` (new task), and itself (parent/child navigation).

**Status:** Live — the core window for browsing/editing the project hierarchy.

<a id="new-project"></a>
### 3.6 NewProject

**Purpose:** Small dual-purpose dialog for creating a new Project or renaming an existing one.

**Displayed:** Parent project name (read-only), Project Name (editable, pre-filled with the current name when renaming).

**Interactions:** Create/Rename or Cancel; Enter confirms.

**Navigation:** Opened modally from `ProjectDetail`'s toolbar ("Add New Project") and from the embedded project-row's right-click "Rename" menu. Opens nothing; the caller performs the actual create/rename after a successful result.

**Status:** Live.

<a id="plan-display"></a>
### 3.7 Plan Display (Gantt View)

**Purpose:** The standalone, full-window version of the Gantt/resource-loading chart — the same rendering `TaskWindow`'s Gantt tab uses embedded, built here as its own window scoped to one Project (or "Top Level Projects" if opened with none).

**Displayed:** A read-only visualization: Task/Project/Component bars recursively laid out from the given Project (or from all top-level active projects and orphan tasks), colour-coded by resource and by priority/status, with a "today" marker line. Hovering a bar shows a tooltip; nothing here is a form field to edit directly.

**Interactions:** Double-click a Task bar opens `TaskDetail`; double-click a Project bar opens `ProjectDetail`; double-click a Component bar opens `ComponentWindow`; Ctrl+double-click a Project bar opens another Plan Display scoped to that sub-project. Dragging a bar horizontally shifts its Start Date by the dragged number of business days (permission-gated, respects dependency constraints) — the one directly-editable interaction this view offers.

**Navigation:** Reached via a singleton-per-project pattern from `ProjectDetail`'s "Gantt Display" toolbar button and via Ctrl+double-click drill-down from itself — modeless. Opens `TaskDetail`, `ProjectDetail`, `ComponentWindow`, and itself.

**Status:** Live.

<a id="component-window"></a>
### 3.8 ComponentWindow

**Purpose:** One Component's detail — its sub-component tree, attachments, and a Gantt view of its tasks. Opened with no Component, it's the top-level Component browser.

**Displayed / editable:** Title (drag-drop target for reparenting), clickable Parent link, Owner (read-only, hidden at top level), a Task-visibility filter (None/Open/All, view-only), an embedded WPF tree of sub-components and their tasks (double-click a task opens `TaskDetail`), an Attachments grid (drag-drop upload, double-click to open, delete from grid), and an "add sub-component" button (permission-gated). A second tab holds a read-only Gantt chart of the component's tasks, built the same way as Plan Display.

**Interactions:** Drag a component onto the title to reparent it; click the parent link to navigate up; toggle the task-visibility filter; add a child component; drag-drop files onto the attachments grid.

**Navigation:** Reached via a singleton-per-component pattern from `MainWindow`'s "Show Components" button (top level), Find, the Gantt view, `TaskDetail` (Component field), the embedded sub-component tree, and itself (parent/child navigation) — modeless. Opens `NewComponent` (modal), `TaskDetail`, and itself.

**Status:** Live.

<a id="new-component"></a>
### 3.9 NewComponent

**Purpose:** Small dual-purpose dialog for creating or renaming a Component — structurally identical to `NewProject`.

**Displayed:** Parent component name (read-only), Component Name (editable).

**Interactions:** Create/Rename or Cancel; Enter confirms.

**Navigation:** Opened modally from `ComponentWindow`'s "add" button and from the embedded component tree's rename action. Opens nothing.

**Status:** Live.

<a id="remark-window"></a>
### 3.10 RemarkWindow

**Purpose:** Add a new Remark on a Task, or view/edit an existing one — see `KeyConcepts.md`'s Remark entry and `DomainModel.md`'s Remark entry for why edits here are being redesigned as immutable/append-only in `V2`.

**Displayed:** Made By and On (read-only, author and timestamp), Remark text (editable — becomes read-only if the user lacks edit rights on an existing remark).

**Interactions:** OK saves and closes (disabled if not permitted); Cancel discards.

**Navigation:** Reached via a singleton-per-remark pattern — created fresh from `TaskDetail`'s "Add" button, or reopened for viewing from a remark elsewhere in the app. Modeless. Opens nothing.

**Status:** Live.

<a id="find-window"></a>
### 3.11 Find

**Purpose:** Global text search across Tasks, Components, Projects, and Remarks, optionally including Attachment contents.

**Displayed:** Search text box (editable), checkboxes for which types to search and whether to include closed items and attachment contents (all editable), a read-only results grid (type, description, and related metadata).

**Interactions:** Find scans the selected sources (showing the Progress Indicator while it runs) and populates the results grid, sorted with highlighted items first. Double-clicking a result routes to the matching detail window by type.

**Navigation:** Opened from `MainWindow`'s "Find" button — modeless. Opens Progress Indicator, and via double-click: `TaskDetail`, `ComponentWindow`, `ProjectDetail`, `RemarkWindow` (via `TaskDetail` for a Remark result).

**Status:** Live.

<a id="manage-people"></a>
### 3.12 Manage People

**Purpose:** Admin list of all People — add or delete a Person.

**Displayed:** A grid (Name, Is Active, Is Resource, DB Login, User Type, Colour) — editable in-place, but **only for SuperUsers**; read-only for everyone else.

**Interactions:** A "New Person" button opens the New Person dialog; "Delete Person" checks whether the selected Person still owns, requests, or is resourced on anything and, if so, blocks the delete with an explanation; otherwise confirms via the same New Person dialog before deleting.

**Navigation:** Opened from `MainWindow`'s "Manage Users" status-strip item — modeless singleton (re-focuses if already open). Opens the New Person dialog (modal, for both add and delete-confirm).

**Status:** Live.

<a id="new-user"></a>
### 3.13 New Person

**Purpose:** Dual-purpose dialog — add a new Person, or confirm deleting one — reusing the same layout.

**Displayed:** Name (editable when adding, read-only/pre-filled when confirming a delete), a "name already exists" warning that also disables OK when triggered.

**Interactions:** OK/Cancel; Enter confirms.

**Navigation:** Opened modally, only from Manage People. Opens nothing.

**Status:** Live.

<a id="admin-window"></a>
### 3.14 AdminWindow

**Purpose:** Support/admin tooling — impersonate another user, switch storage backend, toggle encryption, force-mark records modified, bulk export/reload attachments. Password-gated.

**Displayed:** A storage-backend toggle button (label reflects current backend), a "mark all as modified" action, an encryption on/off checkbox (edits a live global flag), a "restart as a different user" section (editable user picker + read-only role display), a "restart as SuperUser" button, and attachment bulk load/export buttons.

**Interactions:** Every control here performs an immediate, consequential action (relaunching the app under a different identity, migrating storage backend, toggling encryption, dumping all attachments to a local temp folder) rather than editing a record.

**Navigation:** Opened only from `MainWindow`'s "Admin" menu item, behind `PasswordWindow` (skipped if already a SuperUser) — modal. Opens nothing.

**Status:** Live — these operations are largely specific to `V1.2`'s particular architecture (local storage-backend switching, in-process re-launch-as-another-user) and are unlikely to carry forward as designed; see `UseCases.md`'s "Administer the System" use case.

<a id="config-window"></a>
### 3.15 ConfigWindow

**Purpose:** User-facing app settings.

**Displayed:** "Hide Closed Projects" (editable), "View Private Items" (editable, visible/persisted only for SuperUsers — tied to the old app's Private/Visibility flag that `DomainModel.md` has since dropped), "Save settings and restart" button.

**Interactions:** OK persists settings; Restart persists then relaunches the app.

**Navigation:** Opened only from `MainWindow`'s "Config" split button — modal. Opens nothing.

**Status:** Live.

<a id="password-window"></a>
### 3.16 PasswordWindow

**Purpose:** Admin-password gate shown before `AdminWindow`.

**Displayed:** A single masked password field (editable).

**Interactions:** OK/Cancel.

**Navigation:** Opened only from `MainWindow`, immediately before `AdminWindow` — modal, skipped entirely if the user is already a SuperUser. Opens nothing.

**Status:** Live.

<a id="progress-window"></a>
### 3.17 Progress Indicator

**Purpose:** A minimal progress-bar indicator shown during Find's search.

**Displayed:** A single progress bar (read-only, driven by the caller).

**Interactions:** None — purely passive, closed programmatically when the search finishes.

**Navigation:** Opened only from Find, positioned over it, closed by it. Opens nothing.

**Status:** Live, but narrowly used (this one caller only).

<a id="merge-dialogs"></a>
### 3.18 Merge Dialogs (Task / Project / Component)

**Purpose:** Conflict resolution when the background database sync detects that a record was edited both in memory and in the database since it was last loaded — see `KeyConcepts.md`'s Merge/Conflict entry and `DomainModel.md`'s decision to handle this differently in `V2` (Level 1: single user, no conflict handling; later levels: real-time multi-user editing rather than this dialog). A shared, non-instantiable base dialog provides the common merge UI; the Task, Project, and Component variants each just declare which fields are eligible for merging on their entity type (Task: description, details, requester, component, priority, due date, effort, effort type, % allocation, task type, status; Project: name, details, priority, due date; Component: name only).

**Displayed:** One row per conflicting field, each showing "your" value and "their" value (both read-only) plus a live-computed "merged" value; fields that don't actually conflict are auto-resolved and don't need a user decision at all.

**Interactions:** Per-field "Yours"/"Theirs" radio choice; OK applies the merge; Cancel discards it. The dialog is only shown at all if a real conflict exists — otherwise the merge happens silently.

**Navigation:** Instantiated only by the app's periodic database-sync logic, one dialog per conflicting Task/Project/Component — modal, and not reachable from any menu or button. Opens nothing.

**Status:** Live, but condition-triggered only — a user may go an entire session without ever seeing one of these.

<a id="grid-filter-select"></a>
### 3.19 GridFilterSelect

**Purpose:** Not a top-level navigable window — a small borderless popup attached to a filterable grid column's header ("funnel" filter), letting the user check/uncheck which distinct values of that column to show. One instance exists per filterable column and is shown/hidden repeatedly rather than recreated. Appears on every grid that turns filtering on: the Task list, the Project list, the People admin grid, and the Find results grid.

**Displayed:** A checklist of the column's distinct values (all editable/checkable).

**Interactions:** Check All / None / OK (applies the filter) / Cancel (discards); losing focus also just hides it.

**Navigation:** Not opened via `Show`/`ShowDialog` from application code — it's embedded per-column by the grid control itself whenever a grid sets its filter row visible.

**Status:** Live, widely used — worth documenting as a reusable grid-filtering UI pattern for `V2` (§4) rather than as its own screen.

<a id="cross-cutting-ui-patterns"></a>
## 4. Cross-Cutting UI Patterns

Patterns that recur across many windows above, described once here rather than repeated per window:

- **Singleton-per-object windows.** `TaskDetail`, `ProjectDetail`, `ComponentWindow`, `RemarkWindow`, and Plan Display all cache one window instance per underlying database object and re-focus the existing window instead of opening a duplicate when the same item is opened twice.
- **Grid double-click opens detail.** Every list/grid window (`TaskWindow`, `ProjectWindow`, Find's results, the Gantt views) uses double-click on a row/bar as the way into that item's detail window, consistently across the app.
- **Drag-and-drop is the primary way to restructure the hierarchy.** Reparenting a Project or Component, moving a Task into a Project, and creating a Dependency are all done by dragging one item onto another, rather than through a picker dialog or menu command (the Component/Project text field in `TaskDetail` is the one exception, offering an explicit tree-picker).
- **Modal vs. modeless is used deliberately, not arbitrarily.** Small, single-purpose data-entry dialogs (`NewProject`, `NewComponent`, New Person, `PasswordWindow`, the merge dialogs) are modal. Anything that's a "place to work" — a list, a detail editor, the Gantt view — is modeless, so a user can have several open side by side (e.g. a Task open next to its Project).
- **Permission-gated editability, not permission-gated visibility.** Read-only vs. editable is usually decided per-field at render time (e.g. a Remark becomes read-only if the user lacks edit rights on it, admin-only grid columns are read-only for non-SuperUsers) rather than by hiding the field entirely — see `KeyConcepts.md`'s Role / Permission Level entry.
- **A background timer drives both refresh and conflict detection.** `MainWindow`'s 10-second timer both refreshes displayed data and is what surfaces the merge dialogs in §3.18 — the two concerns (auto-refresh, conflict handling) are implemented as one mechanism in the old app, which won't hold once `V2`'s later levels target real-time multi-user editing (`DomainModel.md`'s Future Extensions).
- **The reusable grid filter popup** (`GridFilterSelect`, §3.19) is a shared, generic building block rather than a per-window feature — worth keeping as one reusable filter component in `V2` rather than something to design fresh per screen.

<a id="open-questions"></a>
## 5. Open Questions

This document is intentionally descriptive, not prescriptive — actual `V2` UI decisions belong in `Goals.md` (Level 1's "client technology" and "feature scope" framing questions) and `UseCases.md` (which already flags several of these interactions — drag-and-drop reparenting, drag-and-drop dependency lists — as UX choices to redesign rather than port). A few things surfaced during this sweep worth feeding into that design work:

- **Q-Win-1: Which windows map to "essential" for the Demonstrator** (`Goals.md` Level 1 feature-scope question) — the core browsing/editing loop is `TaskWindow`/`ProjectWindow`/`ComponentWindow` → their respective detail windows, plus Find. Plan Display, the admin/config windows, and the merge dialogs are all separable.
- **Q-Win-2: The old app's "Private Items" config setting and grid column** (`ConfigWindow`, `TaskWindow`'s hidden Private column) reference the visibility flag `DomainModel.md` has already decided not to carry forward — these UI elements have no `V2` equivalent to map onto.
- **Q-Win-3: Multiple simultaneous windows per item type is a deliberate old-app affordance** (§4's singleton-per-object pattern still allows many *different* items open at once) — worth deciding early whether `V2`'s client technology (per `Goals.md`) supports an equivalent multi-window/multi-tab workflow, since several interactions above (dragging a Task from one open window onto a Project in another) depend on it.

<a id="decisions"></a>
## 6. Decisions

None yet — when an open question above is answered, its entry moves here as `D-Win-<N>`, in the three-line format described in `Claude/Guidelines/ImplementationApproach.md` §3.2.
