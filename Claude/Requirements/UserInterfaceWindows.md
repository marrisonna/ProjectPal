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

**Screenshots supplement the source sweep.** `Claude/Requirements/V1.2_ScreenShots/` holds real screenshots of the running `V1.2` app, and `Claude/Requirements/V2_ScreenShots/` holds screenshots of the corresponding `V2` screen as it's built — both added to and grown over time as more screens are built and compared, not a one-off snapshot. Source code is authoritative for what a screen *does* (exact field behaviour, computed values, save logic), but a screenshot is often the faster and more reliable way to judge what a screen *looks like* — visual density, field sizing, grouping — since that's easy to misjudge from designer-file coordinates alone (`4_GuiClient/Plan.md`'s Screen Layout Principles section was written this way, comparing a `V1.2` and a `V2` screenshot side by side rather than from source alone).

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

**None of Requested Start Date, Planned Start Date, or End Date are stored** — confirmed from `V1.2/Libs/DBProjectPal/DBProjectPal/Task.cs`: `Task` has never had a `StartDate`/`EndDate` column in either version's database (`Claude/Level1_Implementation/8_ValidationAndVerification/Plan.md` §4.1), only a business-day offset from the owning Project's start date. All three are computed by this window every time it's shown — see §4's "computed display values" pattern below.

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
- **Several displayed values are computed at render time, not read from a stored column.** `TaskDetail`'s Requested Start Date/Planned Start Date/End Date (§3.3) and Urgency are the clearest examples — none of them exist as database columns in either version; the GUI (`V1.2/Libs/DBProjectPal/DBProjectPal/Task.cs`'s `EarliestStartDate`/`StartDate`/`Duration`/`EndDate`) computes them from stored fields (a relative day-offset, Effort, Effort Type, assigned Resources, Dependencies) every time the window opens. Worth checking for any V2 screen showing a value that looks like it should be a simple field read: confirm whether V1.2 actually stored it, or computed it the same way — `Claude/Level1_Implementation/8_ValidationAndVerification/Plan.md` §4.1 and `4_GuiClient/Plan.md` `D1.4-14` are where this was caught for Task dates specifically.
- **Dropdown/combo-box option order is a deliberate choice, not incidental.** `GUITaskColumns.cs`'s `GetComboValues_static` (feeding `TaskDetail`'s Priority/Status/Task Type dropdowns, §3.3) lists options in a specific order — most-urgent-first for Priority, a particular non-alphabetical order for Status and Task Type — not the order any enum happens to be declared in. Worth matching this order in `V2`'s equivalent dropdowns rather than defaulting to schema-declaration order, which is exactly what happened before this was caught — see `4_GuiClient/Plan.md` `D1.4-15`.

<a id="open-questions"></a>
## 5. Open Questions

This document is intentionally descriptive, not prescriptive — actual `V2` UI decisions belong in `Goals.md` (Level 1's "client technology" and "feature scope" framing questions) and `UseCases.md` (which already flags several of these interactions — drag-and-drop reparenting, drag-and-drop dependency lists — as UX choices to redesign rather than port). A few things surfaced during this sweep worth feeding into that design work:

- **Q-Win-1: Which windows map to "essential" for the Demonstrator** (`Goals.md` Level 1 feature-scope question) — the core browsing/editing loop is `TaskWindow`/`ProjectWindow`/`ComponentWindow` → their respective detail windows, plus Find. Plan Display, the admin/config windows, and the merge dialogs are all separable.
- **Q-Win-2: The old app's "Private Items" config setting and grid column** (`ConfigWindow`, `TaskWindow`'s hidden Private column) reference the visibility flag `DomainModel.md` has already decided not to carry forward — these UI elements have no `V2` equivalent to map onto.

<a id="decisions"></a>
## 6. Decisions

- **D-Win-3** (decided 2026-08-30)<br>
  **Question:** Multiple simultaneous windows per item type is a deliberate old-app affordance (§4's singleton-per-object pattern still allows many *different* items open at once) — does `V2`'s client technology support an equivalent multi-window/multi-tab workflow, since several interactions above (dragging a Task from one open window onto a Project in another) depend on it?<br>
  **Decision:** yes, but via the browser's own native tab/window handling rather than app-managed pop-out windows — every item gets a real, stable, deep-linkable URL, so any tab can already be dragged to its own window on any monitor, plus an explicit "open in new tab" action on every detail screen as a direct equivalent to the old double-click-to-pop-out habit. Automatic singleton-per-object re-focusing is knowingly not reproduced. Full reasoning and options considered: `Claude/Level1_Implementation/4_GuiClient/Plan.md` §4, `D1.4-2`.<br>
  **Superseded by:** `D-Win-4`
- **D-Win-4** (decided 2026-08-30)<br>
  **Question:** Revisiting `D-Win-3`: does that answer also need to reproduce true singleton-per-object re-focusing (never two windows for the same item), given how important that specific behaviour was to real V1.2 usage?<br>
  **Decision:** yes — every item still gets a real, stable, deep-linkable URL, but "open in new window" (and Ctrl/middle-click) now opens it via `window.open(url, name)` with a deterministic name per item, which the browser itself uses to refocus an already-open window for that item rather than opening a duplicate, and each such window is a chrome-less installed-PWA window rather than an ordinary browser tab. Full reasoning and options considered: `Claude/Level1_Implementation/4_GuiClient/Plan.md` §3.7 and §4, `D1.4-5`/`D1.4-8`. The drag-between-two-open-windows interactions `D-Win-3`'s question flagged are themselves redesigned away from drag-and-drop for Level 1 (`UseCases.md`'s own framing, resolved as `4_GuiClient/Plan.md`'s `D1.4-4`); cross-window drag-and-drop specifically is decided by a dedicated spike (`4_GuiClient/Plan.md`'s `D1.4-10`), so this decision governs general multi-window navigation, not that specific old interaction.<br>
  **Refined by:** `D-Win-5` — refocusing via a bare `window.open(url, name)` call turned out to visibly reload the target window every time (see `D-Win-5` part 2 for why, and the actual mechanism that replaced it).
- **D-Win-5** (decided 2026-09-05)<br>
  **Question:** V1.2's Save model is a deferred batch commit — every open window edits one shared in-memory object graph, every other open window sees the change immediately (same process, same objects), and nothing reaches the database until `MainWindow`'s "Save" button, with a per-field conflict-resolution dialog (§3.18) if another *user* had also changed the same record in the meantime. `D-DM-3`/`D-UC-2` already settled that Level 1 doesn't need to resolve two *different users'* concurrent edits to the same record — but separately, with `D-Win-4`'s multiple singleton windows now real (e.g. a Task's `TaskDetail` open right beside `TaskWindow`'s grid, exactly the side-by-side arrangement the multi-window model exists for), what should happen *within one user's own session* when a change made in one open window needs to show up in another?<br>
  **Decision:** four separate mechanisms, easy to conflate, together covering every future V2 screen — not just Task Detail/All Tasks, which happen to be the only two built so far:<br>
  1. **Save model (Level 1): per-screen Save, straight to the database, no client-side batch cache.** Each screen keeps its own Save button (`TaskDetailPage.tsx`'s Save), and pressing it writes immediately via the REST API — there is no V1.2-style deferred "Save All" and no client-side staging cache of unsaved edits. V1.2's instant-everywhere-before-save behaviour was never a deliberate save-model design; it fell out for free from being a single OS process where every window held a reference to the *same* in-memory objects. V2's windows are separate browser processes talking to a shared remote database, so there is no equivalent shared object graph to piggyback on — building one (a real client-side pending-edit store, merge logic between two windows editing the same Task before either saves, a story for a window closed/crashed with unsaved edits still pending) would be a substantial standalone feature, not a small extension of anything already built, and would also pressure `D1.4-6`'s "business rules live server-side" decision, since useful UX would want to validate pending edits client-side before an eventual commit. Not ruled out forever — reconsider alongside real multi-user conflict handling (`DomainModel.md`'s Future Extensions), since a deferred-write model needs conflict resolution anyway once it exists — but not Level 1.
  2. **Cross-window live data refresh — `gui-client/src/lib/liveSync.ts`.** One shared `BroadcastChannel` (`"pp-data-changed"`), held as a single module-level instance rather than one per call, so a window's own broadcast doesn't loop back into its own listener (a `BroadcastChannel` never delivers to the exact object that posted, but *does* deliver to a second, different object for the same channel name — including one in the same window). `invalidateEverywhere(queryClient, queryKey)` is the one rule for every future mutation: call it from a mutation's `onSuccess` in place of a bare `queryClient.invalidateQueries({ queryKey })` — every mutation in `api/hooks.ts` does already. It invalidates locally exactly as before, and also posts `queryKey` on the channel; `startLiveSync(queryClient)`, called once at startup in `main.tsx`, is what makes every *other* open window invalidate that same key on receipt, so it refetches and re-renders with the fresh value. Deliberately not React Query's own default `refetchOnWindowFocus` (already on): that only refreshes a window once it regains actual OS focus, which doesn't cover two windows genuinely visible side by side where neither is being clicked into — the whole point of this app's multi-window model — while the broadcast covers exactly that case, live, without focus needing to change at all.<br>
  **No flash, and nothing to build to get that:** invalidating a query only triggers a background refetch — React Query keeps the *old* data on screen the whole time the new request is in flight, and once it arrives, React's own rendering diffs the new output against the old and patches only the DOM nodes that actually changed (a grid cell's text, a select's chosen value), not the page. A screen with local edit state fed by a query needs one extra guard, though — see part 3.
  3. **Guard against a live refresh clobbering an unsaved edit.** A screen that copies fetched data into local editable state (`TaskDetailPage.tsx`'s `form`, copied from the fetched `task`) must only re-copy when the record's own *id* changes (first load, or navigating to a different record) — not every time the query's data object changes, which now also happens on every background refresh a live-sync broadcast triggers. Getting this wrong means: type into a field, someone else's unrelated save fires a broadcast, this window's query refetches in the background, and the in-progress edit is silently overwritten by whatever the server has right now. The fix is a ref tracking the last-loaded id, guarding the reset effect (`TaskDetailPage.tsx`'s `loadedTaskIdRef`) — a one-line pattern, but easy to miss when a new detail screen is built, since the bug is invisible until two windows are open on the same record at once. The trade-off this accepts: a field someone *else* changed server-side won't appear in this window until it's saved or reloaded, if the local user has any unsaved edit pending — reasonable for Level 1, and revisit only if it's a real problem in practice.
  4. **Window focus without a reload — `gui-client/src/lib/windowNav.ts`.** `registerThisWindow()` (called once at startup, every window, `main.tsx`) is a no-op in the main app window (its `window.name` is empty) and, in every popped-out window, records that window as alive under its own name in `localStorage` (`"pp-window-alive:" + name`) — synchronously readable by any other same-origin window, unlike asking `window.open()` itself, which requires actually opening something just to find out whether a name is taken. `openNamedWindow()` checks that flag first: already alive — a single `window.open("", name)` call, which returns the existing window *without* navigating/reloading it (an empty URL means "don't navigate"), and brings it to the front as a direct, synchronous consequence of this window's own click — the same native behaviour a plain `target="name"` link has always had; not alive — a single `window.open(realUrl, name, features)` call, with the real URL from the very start (a version that opened a blank window first and navigated it afterward was tried and reverted — Chromium decides whether a new window opens as a standalone app window or an ordinary browser tab from the URL *at creation time*, not from a later script-driven navigation, so that version silently downgraded every new window to a plain browser tab). Deliberately never more than one `window.open()` call per click either way: an earlier version probed for an existing window with a blank-URL `window.open()` call and then opened a second, real one when needed — Chromium's popup blocker treats a *second* `window.open()` in the same click handler as an unrequested popup and silently blocks it, so that version could fail to open anything at all, in either direction. A different earlier version kept a single `window.open()` call but asked the *target* window to focus itself on receiving a `BroadcastChannel` message — that quietly did nothing, because `window.focus()` called from inside a message handler is an async, script-initiated call with no direct user gesture in that window's own context, and Chromium's anti-focus-stealing protection generally ignores exactly that (a background window can't just decide to bring itself forward on its own — that's not the same thing as a person having just clicked something, even though one did, in a *different* window).<br>
  **Known limitation:** the `localStorage` flag clears on `pagehide`, which fires reliably for every normal close, but not a hard crash/force-kill — in that rare case `window.open("", name)` finds nothing left to return (there's no window with that name anymore) and creates a fresh, blank, wrongly-named window instead. Not solved here — rare enough, for a Level 1 Demonstrator, not to justify the complexity of detecting and recovering from it.
  5. **Say what actually went wrong — `gui-client/src/lib/apiErrors.ts`.** A save/mutation failure's `catch` block should show the REST API's own error, not a fixed guessed string: `api/hooks.ts`'s `unwrap()` throws the response's own JSON body on a non-2xx response, which for a plain `HTTPException(status, detail)` is `{ detail: "some message" }` and for a Pydantic validation failure (422) is `{ detail: [{ loc, msg, type }, ...] }`, one entry per invalid field. `formatApiError(err, fallback)` reads whichever shape actually came back — a validation array becomes a `field: message` line per field, directly naming the problem, and any other shape falls back to the caller's own generic text. `TaskDetailPage.tsx`'s Save button and its Resources checklist both use it now; a bare `catch { setError("generic guess") }` (which is what both used to do) shows the *same* fixed text regardless of whether the real problem was a validation error, a permission check, or something else entirely — actively misleading when the guess is wrong, as it was here (a real 403 permission-check message was being shown to the user as "check required fields and try again").<br>
  Together with `D-Win-4`, this is the complete "how do V2's open windows and its data interact" story for Level 1 — and, since none of it is Task/TaskList-specific (`liveSync.ts`, `windowNav.ts`, and `apiErrors.ts` are all generic over any query key, window name, or error shape), the recipe every future screen (Project Detail, Component Detail, Manage People, the Gantt view, all still unbuilt) should reuse rather than reinvent: singleton windows that re-focus without reloading (`D-Win-4` + part 4 above), each screen owning and immediately committing its own edits (part 1), every other open window reflecting that commit within moments regardless of focus (part 2, flash-free per its own note), a record's local edit state protected from being overwritten mid-edit by exactly that mechanism (part 3), and a failed save or mutation saying what actually went wrong rather than guessing (part 5) — without ever needing V1.2's shared in-memory cache, deferred batch commit, or same-session merge dialog, none of which have a natural equivalent in a multi-process browser client talking to a remote API.
- **D-Win-6** (decided 2026-09-05)<br>
  **Question:** `D-Win-5` part 5 surfaced that a Save can legitimately fail on a permission check (`require_owner_or_team_lead` — the caller is neither the Task's owner nor a TeamLeadUser on its Team), not just a validation error — the error message now says so, but the screen still showed a live Save button and fully editable fields to a user who could never have saved. Two related gaps worth a real answer rather than living with a surprise 403 at Save time: should a screen editing a record ever let the user click Save when nothing has actually changed, and should it show editable-looking controls at all to a user who has no edit permission on this specific record?<br>
  **Decision:** three rules, all implemented in `TaskDetailPage.tsx` and the shared `components/DenseField.tsx` controls it's built from, meant to generalise to every future detail screen (Project Detail, Component Detail) the same way `D-Win-5`'s mechanisms do:<br>
  1. **Save is disabled until something has actually changed.** A `dirty` flag, set on every `setField` call and cleared both on loading a (possibly different) record and after a successful save, gates the Save button's `disabled` prop alongside the existing in-flight check. Pressing a disabled button is already a no-op in the browser — nothing to build for "and pressing it does nothing" beyond disabling it correctly.
  2. **A user with no edit permission on this record sees no Save button and cannot edit any field.** `gui-client/src/lib/permissions.ts`'s `canEditOwnedRecord(person, teamId, ownerPersonId)` mirrors `rest-api/app/security/deps.py`'s `require_owner_or_team_lead` exactly (same two conditions, same lack of an `is_organisation_admin` bypass — replicating one client-side that the server doesn't have would show controls a save would then reject), decided against the record's own *stored* owner (`task.owner_person_id`), not a pending unsaved reassignment in the form, since that's what the server actually checks against. When it's `false`: the Save button doesn't render at all, and every field that would otherwise be editable is passed `readOnly` (native `<select>`s: `disabled`, since there's no such thing as a read-only `<select>`; native number `<input>`s: `disabled` rather than `readOnly`, since a read-only number input's spinner arrows can still change its value in some browsers, a gap `disabled` doesn't have). Checking this up front, rather than only handling the 403 a save attempt would return, is what actually satisfies "should not be allowed to edit" — a field that's merely going to fail to save if touched is not the same thing as a field that can't be touched.
  3. **Any field that can't be edited right now gets a light grey background** (`components/DenseField.tsx`'s shared `READONLY_BG`), not just a functionally-disabled control with no visual cue — this applies uniformly whether the field is *situationally* read-only (rule 2, permission-gated) or *permanently* read-only by its own nature (a computed value with no `onChange` at all, e.g. Planned Start, which is always shown via `FieldStatic` — now always grey, unconditionally, rather than only when a broader read-only mode is active, since it was never editable in the first place). One shared visual rule for "you can't change this," regardless of *why*.<br>
  **Full detail:** `gui-client/src/lib/permissions.ts`, `components/DenseField.tsx`'s `readOnly` prop on `FieldSelect`/`FieldInput`/`FieldTextArea`/`DateField` and `FieldStatic`'s now-unconditional read-only styling, `features/tasks/TaskDetailPage.tsx`'s `dirty` state and `canEdit` check.
- **D-Win-7** (decided 2026-09-05)<br>
  **Question:** `TaskDetailPage.tsx`'s Save-failed error banner (the one `D-Win-5` part 5 taught to say what actually went wrong) had no way to dismiss it short of reloading the whole window — the adjacent Resources-error banner right next to it already had one (an `onClose` handler on the same MUI `Alert`), so this was a one-off omission on this specific banner, not a missing capability. What's the general rule, so it doesn't get missed again on the next screen that shows an error this way?<br>
  **Decision:** every error/status banner a screen shows as the *result of an action* (a failed save, a failed field-level mutation, a failed login) must be dismissible without reloading — an MUI `Alert`'s own `onClose` prop (which renders the small "x" in its corner for free) wired to clear that banner's `useState`, exactly the pattern the Resources-error banner already had and the Save-error banner and `LoginPage.tsx`'s login-failure banner now also have. Not every `Alert` needs this: a banner that reflects a *live, ongoing* condition rather than a one-off action's result — `Dashboard.tsx`'s "Could not reach the API" — isn't a notification to dismiss so much as a status that will simply reappear on its own next render while the condition holds, so forcing a dismiss control on it would be misleading, not helpful. The distinguishing question for any future screen's error display: does this banner represent something that just happened once (dismissible), or something that is still true right now (not)?
- **D-Win-8** (decided 2026-09-05)<br>
  **Question:** `D-Win-5` part 1 decided Level 1's save model is per-screen Save, straight to the database — but Task Detail's Resources checklist never actually followed that: it called `assignResource`/`unassignResource` directly from each checkbox's own `onChange`, writing to the database on every click regardless of whether Save was ever pressed. Ticking a Resource, then closing the window without pressing Save, left the change permanently applied — the opposite of every other field on the same screen, where an un-saved edit is genuinely discarded by closing the window (nothing was ever staged anywhere to write it from). Should Resources really be a standing exception to `D-Win-5`'s own save model, or was this simply built inconsistently with it?<br>
  **Decision:** an inconsistency, not a deliberate exception — fixed so Resources now follow exactly the same rule as every other field. `TaskDetailPage.tsx` stages the checklist's state in a local `resourceIds` set (reset from the query's own `assignedResources` only on first load or navigating to a different Task — the same guard `D-Win-5` part 3 already uses for `form`, and for the same reason: a live-refresh background refetch must not silently overwrite an in-progress, unsaved checklist change). Checking or unchecking a Resource now only updates that local set and marks the screen `dirty` (`D-Win-6` part 1) — no API call happens at all until Save is pressed, at which point `handleSave` diffs the staged set against the original assignment and fires exactly the `assignResource`/`unassignResource` calls needed for what actually changed, right alongside the Task's own field PATCH. Two knock-on effects of treating Resources as genuinely staged, not a side-channel: the checklist's checked-first sort order (`D1.4-18`) now re-sorts off the staged set live, same as it always visually appeared to; and Duration/the computed dates, which depend on resource *count*, now recompute from the staged count before Save too, matching the same "recompute from what's on screen, not last-saved" treatment `D1.4-19`'s Effort field already had — previously they silently used the last-*saved* count until the page was reloaded, a second, quieter symptom of the same underlying bug.<br>
  **General principle, for every future screen with a similar list-of-associations control** (e.g. a Project's own resource/team assignments, once built): nothing on a detail screen should reach the database before its own Save is pressed, and nothing already on screen should be lost by closing the window without saving — nothing here is Task-Resources-specific, so a control that writes immediately on click, no matter how natural that feels for a checkbox, needs a specific reason to be an exception to that rule, not the default.