# Project Detail — Design and Implementation Plan

## Contents

1. [Purpose and Scope](#purpose-and-scope)
2. [Requirements](#requirements)
   - 2.1 [What V1.2 Did](#what-v1-2-did)
   - 2.2 [What's Different in V2](#whats-different-in-v2)
   - 2.3 [Permissions](#permissions)
3. [What Already Exists to Build On](#what-already-exists)
   - 3.1 [Server API](#server-api)
   - 3.2 [Reusable Client-Side Pieces](#reusable-client-side-pieces)
   - 3.3 [Gaps to Fill](#gaps-to-fill)
4. [Design](#design)
   - 4.1 [Screen Structure](#screen-structure)
   - 4.2 [Header](#header)
   - 4.3 [Main Fields](#main-fields)
   - 4.4 [Sub-Projects and Tasks Tree](#sub-projects-and-tasks-tree)
   - 4.5 [Dependencies / Attachments / Remarks Tabs](#tabs)
   - 4.6 [Create / Rename Project Dialog](#create-rename-dialog)
   - 4.7 [Adding a Task](#adding-a-task)
   - 4.8 [Deleting a Project](#deleting-a-project)
   - 4.9 [Drag and Drop](#drag-and-drop)
5. [Implementation Plan](#implementation-plan)
   - 5.1 [New API Client Hooks](#new-api-client-hooks)
   - 5.2 [Generalising the Dependencies Panel](#generalising-dependencies-panel)
   - 5.3 [New Components](#new-components)
   - 5.4 [Build Steps](#build-steps)
   - 5.5 [A Pre-Existing Bug This Work Found — Already Fixed](#pre-existing-bug)
6. [Testing Approach](#testing-approach)
7. [Open Items for Review](#open-items-for-review)

<a id="purpose-and-scope"></a>
## 1. Purpose and Scope

This document designs and plans **Project Detail**, the first screen built in Stage 4 (`Plan.md` §6.4). It covers Project Detail only — not Project List (`ProjectWindow`, `UserInterfaceWindows.md` §3.4), which is a simple grid expected to reuse `TaskListPage.tsx`'s own `@mui/x-data-grid` pattern directly (the same "minimal grid as the pilot's navigation entry point" role Task List played for Task Detail, §6.2) and doesn't need its own design pass here. Written before any code, per the user's request, so the design can be reviewed first.

Per `D1.4-40`'s Stage 5 split, this design's own drag-and-drop interactions (reparenting, dragging a Task onto a Project row, cross-window dependency creation) are deferred — §4.9 records what's deferred and why, so Stage 5 has a concrete starting list rather than needing to rediscover it from V1.2 again.

<a id="requirements"></a>
## 2. Requirements

<a id="what-v1-2-did"></a>
### 2.1 What V1.2 Did

From `Requirements/UserInterfaceWindows.md` §3.4–§3.6 (the authoritative source-swept description of the old app; see that document for full detail):

- **ProjectDetail** is V1.2's hierarchical Project editor: one Project's own fields, plus a nested, expandable embedded tree of its sub-projects and their Tasks. Opened with no Project at all, it doubles as the "Top Level Projects" browser.
- Fields shown: Title (also the reparent drag-source), a clickable Parent link (navigates up), Priority and Owner (editable dropdowns), Due Date and Start Date (editable), a computed read-only End Date, Detailed Description (editable), a Private flag, a view-only "only active tasks" filter, and a Task-visibility radio group (None/Open/All — a display filter, not a stored field).
- The embedded tree: each sub-project row shows its name, a task count/priority summary, an "Add Task" button, expand/collapse (lazily loading nested content), and a right-click menu (Delete — blocked while it has dependants; Rename — opens `NewProject`). A toolbar offers "Add New Project" and "Gantt Display".
- An Attachments grid and the same Dependencies panel shape `TaskDetail` (§3.3) uses.
- **ProjectWindow** is the flat, read-only Project list feeding into ProjectDetail — Name/Priority/Parent visible, more columns available, default-filtered to active Projects, default-sorted Priority descending, no add/edit/delete of its own.
- **NewProject** is a small modal used both to create a new Project and to rename an existing one — parent name (read-only) plus an editable Name field.

<a id="whats-different-in-v2"></a>
### 2.2 What's Different in V2

- **No Private flag.** Dropped from the domain model entirely (`Plan.md` §5's Screen Inventory table, ConfigWindow row) — not carried into `ProjectRecord` and not shown here.
- **No stored "is active" flag — and none needed.** `ProjectRecord` has no such column, but neither does V1.2's own schema: checking `V1.2/Libs/DBProjectPal/DBProjectPal/Project.cs` directly, `Project.IsActive` is a **computed property**, not a persisted column (unlike `Person.IsActive`, which really is a stored `bit` in V1.2's own SQL schema) — recomputed from the live Task/sub-Project tree on every access, never stored. V2 should port the same computed definition rather than the Priority-only stand-in used so far (`isVisibleProjectPriority`, `lib/ganttLayout.ts`) — see below.

  **V1.2's exact definition**, read directly from source:
  ```csharp
  public bool IsActive
  {
      get
      {
          if (this.Priority == PriorityValue._0_Cancelled ||
              this.Priority == PriorityValue._0_Closed)
              return false;
          foreach (Task currentTask in Tasks)
          {
              if (currentTask.Status.HasValue &&
                  currentTask.Status != StatusValue.Closed &&
                  currentTask.Status != StatusValue.Cancelled)
                  return true;
          }
          foreach (Project currentProject in SubProjects)
          {
              if (currentProject.IsActive)
                  return true;
          }
          return false;
      }
  }
  ```
  In words: a Project is active if its own Priority isn't Cancelled/Closed, **and** it has at least one direct Task that isn't Closed/Cancelled, **or** at least one sub-Project that is itself active (recursively — an active grandchild makes every ancestor above it active too, not just its immediate parent). A Project with Priority Cancelled/Closed is never active regardless of its Tasks/sub-Projects; a Project with an acceptable Priority but only Closed/Cancelled Tasks and no active sub-Project is not active either — Priority alone (today's stand-in) only captures the first half of this.

  **Porting this to V2:** a new `isProjectActive(graph: ScheduleGraph, projectId: number): boolean` in `lib/schedule.ts`, alongside `getProjectSchedule` — it needs the same `childTasksByProject`/`childProjectsByParent` maps `ScheduleGraph` already builds, so it belongs with that infrastructure rather than duplicating a second Task/Project tree walk in `lib/ganttLayout.ts`. Two deliberate deltas from the V1.2 source, both consistent with this codebase's own established practice elsewhere for a ported recursive walk:
  - No `Status.HasValue` check — V2's `TaskRecord.status` is always set (`TASK_STATUSES`, no null variant), so every Task is considered, matching `isVisibleTaskStatus`'s own existing status check exactly (same two excluded values, Closed/Cancelled).
  - A `visited: Set<number>` cycle guard on the sub-Project recursion, which V1.2's own version doesn't have — the same guard already added when porting `buildProjectChain`/`ancestorPriorityChain` elsewhere in this codebase (`D1.5-3`'s reasoning), protecting against a malformed `parent_project_id` chain looping forever, a case V1.2's own code has no defence against either.

  **This should eventually replace, not sit alongside, the existing Priority-only `isVisibleProjectPriority`** used by the already-shipped Gantt view (`lib/ganttLayout.ts`) — per `document-guidelines.md` rule 2, "is this Project active" should have one definition in this codebase, not a fuller one here and a simplified stand-in there. **Decision (`D1.4-41`):** that switch-over is deferred to Stage 6 (`Plan.md` §6.6), not done as part of this screen's own build — it's a real behaviour change to already-shipped, tested code (changing which Projects appear in `/plan`'s default view), kept separate from Project Detail's own, unrelated build. `isProjectActive` itself is still written now, as part of this screen (§5.4 step 4) — only the Gantt view's own switch to it is deferred.
- **End Date is still always computed, never stored** — same derived-scheduling approach as Task (`DomainModel.md` §2.5), already available via `getProjectSchedule` (`lib/schedule.ts`).
- **The Task-visibility radio group (None/Open/All) is a display filter, not a Project field** — carries forward as pure client-side state (which of a Project's own Tasks the embedded tree shows), never sent to the server.
- **Drag-and-drop is deferred to Stage 5** (`D1.4-40`) — see §4.9. Every interaction V1.2 built as a drag (reparenting, moving a Task into a Project, cross-window Dependency creation) gets an explicit-control equivalent here instead, for Level 1's own first cut — the same substitution Stage 2 already made for Task Detail (`D1.4-4`, `D1.4-7`).
- **No right-click context menu.** V1.2's per-row Delete/Rename live behind a right-click menu; this design uses small inline icon buttons per row instead, consistent with this codebase's established preference for explicit, discoverable controls over a hidden gesture (the same reasoning behind `D1.4-4`/`D1.4-7`).

<a id="permissions"></a>
### 2.3 Permissions

Read directly from `V2/rest-api/app/routes/projects.py` and `tasks.py` (not assumed from Task Detail's own rules, which turn out to differ in one important way):

- **Viewing** a Project has no server-side Team restriction at all (`GET /project` returns every Team's Projects to any authenticated caller) — exactly like `GET /task`. Client-side team-scoping is therefore this screen's own responsibility, the same pattern already applied in `TaskListPage.tsx` (`myTeamIds`) and `PlanPage.tsx` (`memberTeamIds`): only show Projects on a Team the signed-in Person belongs to.
- **Editing** an existing Project's fields (`PATCH /project/{id}`) uses the exact same rule as Task — `require_owner_or_team_lead` server-side, `canEditOwnedRecord()` (`lib/permissions.ts`) client-side, unchanged. No new permission helper needed here.
- **Creating** a Project (`POST /project`) or a Task (`POST /task`) both require **at least LeadUser** on the target Team (`require_role_at_least(caller, team_id, "LeadUser")`) — stricter than editing, which only needs ownership plus Normal User. This is a real, easy-to-miss distinction: a Task's own owner (a plain Normal User with no other standing) can edit that Task, but the same person cannot create a new Project or a new Task at all unless they're also a LeadUser or TeamLeadUser on that Team. The existing `hasRoleAtLeast(person, teamId, "LeadUser")` (`lib/permissions.ts`) already expresses this check exactly — used to gate "Add New Project" and "Add Task" (§4.4/§4.6/§4.7), not `canEditOwnedRecord`.
- **Deleting** a Project (`DELETE /project/{id}`) requires **TeamLeadUser specifically** — not even the Project's own owner may delete it (`projects.py`'s own comment: "Only TeamLeadUser may delete a Project, not even its owner"). This is stricter again than editing, and stricter than Task deletion would presumably be (Task has no delete endpoint yet at all — see §3.3). Gate the Delete control on `isTeamLead(person, teamId)` (`lib/permissions.ts`), not `canEditOwnedRecord`.

<a id="what-already-exists"></a>
## 3. What Already Exists to Build On

<a id="server-api"></a>
### 3.1 Server API

Checked directly against `V2/rest-api/app/routes/projects.py` and the generated `api/schema.d.ts` — full CRUD already exists and is ready to use as-is:

- `GET /project` (list, with optional `team_id`/`parent_project_id` filters), `GET /project/{id}`, `POST /project`, `PATCH /project/{id}`, `DELETE /project/{id}`.
- `POST /task` already exists too (`CreateTaskRequest` needs only `project_id` and `description`; every other field is optional), needed for §4.7's "Add Task."
- `/dependency`'s `GET`/`POST` already accept `project_id`/`pre_project_id`/`post_project_id` symmetrically alongside their Task equivalents (`dependencies.py`'s `list_dependencies`/`create_dependency`) — a Project's Dependencies work identically to a Task's on the server today. Nothing needs to change server-side for §4.5's Dependencies tab.
- `/remark` and `/attachment` already accept a `project_id`-shaped owner (`RemarkOwner`'s existing `{ project_id: number }` variant, `api/hooks.ts`) — confirmed this is not new API surface, just an unused-so-far option on an already-generic endpoint.

<a id="reusable-client-side-pieces"></a>
### 3.2 Reusable Client-Side Pieces

Reused **unchanged**:

- `RemarksPanel` (`features/remarks/RemarksPanel.tsx`) and `AttachmentsPanel` (`features/attachments/AttachmentsPanel.tsx`) — both already take a generic `RemarkOwner`, so passing `{ project_id }` instead of `{ task_id }` needs no code change at all.
- `DenseField.tsx`'s shared field controls (`FieldLabel`, `FieldSelect`, `FieldInput`, `FieldStatic`, `FieldTextArea`, `DateField`, `DenseButton`, `FieldTreePicker`) and `TreePicker.tsx` (the dropdown tree picker already used for Task Detail's own Project/Component fields) — this design's whole visual language (§4) is built from these, matching the user's own instruction to follow Task Detail's layout and style.
- `lib/permissions.ts` (`canEditOwnedRecord`, `hasRoleAtLeast`, `isTeamLead`) and `lib/people.ts` (`personDisplayName`) — no changes needed, per §2.3.
- `lib/schedule.ts`'s `getProjectSchedule`/`buildScheduleGraph` and `computeUrgencyColour` — Project Detail's own computed End Date (§4.3) reads from the same schedule graph Task Detail and the Gantt view already build.
- `lib/windowNav.ts`'s `openItemWindow`/`useSingletonWindowIdentity` — Project Detail is a singleton-per-Project window exactly like Task Detail (`openItemWindow("projects", id)`), already generic over entity type.

Reused **with a needed change** — see §5.2:

- `DependenciesPanel.tsx` is currently hard-coded to a `taskId: number` prop and to `{ pre_task_id, post_task_id }` request bodies. It needs generalising to accept either a Task or a Project as its own owner, and its "Add Dependency" search dialog needs to search across both Tasks and Projects, not Tasks only — since either side of a Dependency can be either kind (`KeyConcepts.md`'s Dependency entry).

<a id="gaps-to-fill"></a>
### 3.3 Gaps to Fill

- No `useProject(id)`, `useCreateProject`, `useUpdateProject`, or `useDeleteProject` hooks exist yet in `api/hooks.ts` (only the bulk `useProjects()` reference-data list, used today purely for lookups). All four need adding — see §5.1.
- No `useCreateTask` hook exists yet either (needed for §4.7's "Add Task"). There's also no Task delete endpoint at all yet (`schema.d.ts`'s `/task/{task_id}` has no `delete` operation) — not needed by this design, noted only so it isn't assumed to exist elsewhere.
- No route for `/projects/:projectId` exists yet in `App.tsx` — needs adding as a `BareAuthenticatedLayout` route (matching Task Detail and Plan View, both standalone popouts with no app bar) plus a `/projects` route for the future Project List.
- `openItemWindow`/`openListWindow` (`lib/windowNav.ts`) are already generic over entity-type strings, so `openItemWindow("projects", id)` needs no change there — only a new route to point at.

<a id="design"></a>
## 4. Design

<a id="screen-structure"></a>
### 4.1 Screen Structure

Follows Task Detail's own established shape (`TaskDetailPage.tsx`) as closely as the different domain allows, per the user's own instruction:

- Its own small standalone window (`BareAuthenticatedLayout`, no app bar), singleton per Project (`useSingletonWindowIdentity(\`projects-${id}\`)`), opened via `openItemWindow("projects", id)` — same mechanism, same reasoning (`D1.4-8`) as Task Detail.
- Opened with no Project at all (`/projects`, no id), it shows the "Top Level Projects" browser — the same duality V1.2's own ProjectDetail has, and the same duality the Gantt view already implements for its own unscoped `/plan` route. This reuses the same `rootProjectId != null ? scoped : top-level` shape `PlanPage.tsx` already establishes, not a new pattern.
- A single card (white background, subtle border/shadow, rounded corners) matching Task Detail's own outer `Box` styling exactly, not a new visual treatment.
- Unlike Task Detail (a fixed 656px width, since it edits one flat set of fields), Project Detail's embedded tree (§4.4) needs more vertical room to be useful — width can stay similar, but the window's own default height (`TASK_DETAIL_WINDOW_FEATURES`-equivalent `openNamedWindow` features string) should be taller. Exact sizing is a detail for implementation time, not this design.

<a id="header"></a>
### 4.2 Header

Mirrors Task Detail's own compact identity header:

- A small icon badge (a "P" badge, matching Task Detail's "T"), the Project's own ID and Team name, and its Name shown as an editable title input (same pattern as Task's Description field) — editable only if `canEditOwnedRecord` allows it (§2.3).
- **Parent**: where Task Detail shows a plain Owner dropdown next to the title, Project Detail shows a clickable **Parent** breadcrumb-style link instead (V1.2's own "clickable Parent link, navigates up") — clicking it opens Project Detail for the parent Project (or does nothing / is hidden for a top-level Project). This is a `FieldStatic`-styled clickable link, not a dropdown — reparenting itself is a separate control (§4.3), not this header shortcut.
- **Owner** dropdown, same shape and same candidate set (`resourceCandidates`-equivalent, i.e. active People holding a role on this Project's own Team) as Task Detail's Owner field.
- No Urgency badge — Urgency (`KeyConcepts.md` §12) is a per-Task score; a Project has no Urgency of its own. Nothing replaces this slot in the header.
- An **"All Projects"** button (matches Task Detail's "All Tasks" button, opens the future Project List window) and, if `canEditOwnedRecord` allows it, a **Save** button, active once the form is dirty — identical mechanics to Task Detail's own.

<a id="main-fields"></a>
### 4.3 Main Fields

Two rows, `DenseField.tsx` components throughout, matching Task Detail's own row-of-fixed-width-fields layout:

- **Row 1:** Priority (`FieldSelect`, same `PRIORITY_LEVELS` list Task uses — a Project's Priority uses the identical vocabulary, `KeyConcepts.md` §11), Start Date (`DateField`, editable — a Project's own stored `start_date`, unlike a Task's derived one), Due Date (`DateField`, editable — `ProjectRecord.due_date`), End Date (`FieldStatic`, computed via `getProjectSchedule`, read-only, matching Task's own Planned Start/End Date treatment).
- **Row 2:** A **Reparent** control — a `FieldTreePicker` labelled "Parent Project", populated from this Project's own Team's Projects (same `teamProjects`-shaped list Task Detail already builds for its own Project field), with an "(none)" option meaning top-level. This is the explicit-control replacement for V1.2's drag-based reparenting (§2.2/§4.9) — reusing the exact same picker component Task Detail already uses for its own Project/Component fields, not a new one.

No Effort/Duration/%Allocation/Resources row — none of those are Project fields (they're Task-specific, `KeyConcepts.md` §10). No Requestor/Task Type either, for the same reason.

Below the two rows: **Detailed Description** (`FieldTextArea`), identical treatment to Task Detail's own.

<a id="sub-projects-and-tasks-tree"></a>
### 4.4 Sub-Projects and Tasks Tree

The screen's own distinguishing feature — replaces Task Detail's fixed field layout with a browsable tree below the main fields, taking up most of the window's remaining height. Built with `@mui/x-tree-view` (already a dependency, chosen specifically for this, `Plan.md` §3.3's `D1.4-1`), not the existing bespoke `TreePicker.tsx` (that component is shaped for a dropdown single-selection picker, not an always-visible browsing/management tree with per-row actions).

Each row is either a sub-Project or a Task directly under this Project, in that order (mirroring the Gantt view's own default ordering, `ganttLayout.ts`'s `collectRows`) — sub-Projects sorted alphabetically, Tasks by computed start date:

- **A sub-Project row** shows its name (click opens that sub-Project's own Project Detail window, replacing V1.2's drag-source/click duality — click alone is enough since dragging is deferred), an expand/collapse arrow (children loaded from the already-fetched `useProjects()`/`useTasks()` reference data — no separate lazy-load request needed at Level 1's data scale, unlike V1.2's lazy DB fetch), and, if `hasRoleAtLeast(person, teamId, "LeadUser")`, a small trash icon to delete it (§4.8) and a small pencil icon to rename it (§4.6).
- **A Task row** shows its own description (click opens that Task's own Task Detail window, `openItemWindow("tasks", id)`) and its computed Urgency, colour-coded exactly as in All Tasks/the Gantt view (`computeTaskRowColour`) — giving an at-a-glance read of this Project's own Task health without needing to open All Tasks separately.
- A **Task-visibility filter** (None/Open/All radio group, or an equivalent MUI toggle) above the tree, controlling which of this Project's own Tasks the tree shows at every level — a pure client-side display filter (§2.2), defaulting to "Open" (hides Closed/Cancelled Tasks, matching All Tasks' own default Status filter, `D-Win-16`).
- A small toolbar above the tree: **"Add New Project"** (§4.6) and **"Add Task"** (§4.7), both gated on `hasRoleAtLeast(person, teamId, "LeadUser")` (§2.3) — hidden, not merely disabled, for anyone below that, matching Task Detail's own Save-button-hidden-not-disabled treatment for a non-editor.

**Deliberately not built now** (left for Stage 6, `Plan.md` §6.6, alongside that stage's other polish items, since it's hard to judge exactly how much detail is worth showing per row until this tree exists and gets real use): a per-row task-count/priority summary badge, and a per-row "Gantt Display" shortcut button. The tree itself, and clicking through it, is fully functional without either.

<a id="tabs"></a>
### 4.5 Dependencies / Attachments / Remarks Tabs

Identical tab strip to Task Detail's own (`TABS`, sub-tab state, count-labelled tabs), placed below the tree:

- **Dependencies** — `DependenciesPanel` generalised to a Project owner (§5.2); otherwise identical UI (Depends upon / Dependants lists, "Add Dependency" dialog, trash-icon removal).
- **Attachments** — `AttachmentsPanel` with `owner={{ project_id: id }}`, unchanged.
- **Remarks** — `RemarksPanel` with `owner={{ project_id: id }}` and `teamId={project.team_id}`, unchanged.

<a id="create-rename-dialog"></a>
### 4.6 Create / Rename Project Dialog

A small modal, matching V1.2's own dual-purpose `NewProject` (§2.1) and this codebase's own existing dialog pattern (`DependenciesPanel`'s "Add Dependency" `Dialog`/`DialogTitle`/`DialogContent`/`DialogActions`):

- **Fields:** Parent Project name (read-only text, pre-filled from context — the Project whose "Add New Project" or rename action opened this), Name (editable text field, pre-filled with the current name when renaming, empty when creating).
- **Actions:** Create/Rename and Cancel; Enter in the Name field confirms, matching V1.2.
- **Required field:** Name — `V1.2/Apps/ProjectPal/ProjectPal/Projects/ProjectDetail.cs`'s `toolStripButton1_Click` refuses to create a Project with a blank name ("A name for a project must be specified"), checked before anything is written; nothing else about a Project is required at creation. This dialog shows the same message inline (not a separate popup, matching this codebase's own existing dialog conventions) if Create is clicked with Name empty.
- **Creating** calls the new `useCreateProject` mutation (§5.1) with `{ team_id, name, parent_project_id }` (`team_id`/`parent_project_id` taken from context, never chosen by the user in this dialog — matching V1.2's own read-only Parent display); **renaming** calls `useUpdateProject(id)` with just `{ name }`.

<a id="adding-a-task"></a>
### 4.7 Adding a Task

**Revised from an earlier draft of this design** (`D1.4-43`), which assumed V1.2 has no minimum-required-fields rule for a new Task, based on there being no separate "New Task" *window* in the catalogue (`UserInterfaceWindows.md` §3). Checking `V1.2/Apps/ProjectPal/ProjectPal/Tasks/TaskDetail.cs` directly found that's the wrong read: `TaskDetail` itself doubles as the "New Task" form (a `WindowMode.New` constructor overload, `TaskDetail(Project ownerProject)`), and its own `buttonOK_Click` refuses to save a Task created this way until several fields are filled in — there just isn't a *separate* window for it. An instant, no-input create (this design's original proposal) isn't a faithful port of that; a small dialog is needed instead.

**V1.2's exact requirement**, from `buttonOK_Click`'s own `WindowMode.New` branch, checked in this order, each with its own message ("Task detail not complete"):

1. A Component must be specified.
2. A Project must be specified.
3. The task must have a Description.
4. The task must have a Priority.
5. The task must have a Task Type.
6. The task must have a Requestor.

Opened from a Project's own context (the `TaskDetail(Project ownerProject)` overload — the same situation as Project Detail's own "Add Task" button), some of these already arrive pre-filled with a sensible default and so pass without the user doing anything: **Project** (the owning Project itself), **Priority** (defaults to Med), and **Requestor** (defaults to the Task's Owner, itself defaulted to the person creating it). Only **Description**, **Component**, and **Task Type** are left genuinely blank and must be actively supplied before V1.2 lets the create through.

**Component being required at creation only, unlike `DomainModel.md`'s own general "optional" stance, is a deliberate choice here (`D1.4-45`), not an oversight.** `DomainModel.md` §2.6 documents a Task's Component relationship as optional — nullable, with an unset Component treated as a normal, valid state — and Task Detail's own existing Component field already reflects that (`allowNone` on its `FieldTreePicker`). V1.2's own creation-time check is narrower: a *new* Task can't be saved without one, even though an *existing* Task's Component can freely be cleared back to none afterward (nothing re-checks this on a later edit — `buttonOK_Click`'s validation block only runs in `WindowMode.New`). `D1.4-45` decided to match V1.2 exactly for this one dialog — required at creation, still freely clearable afterward via Task Detail's own unchanged Component field — rather than relax it to match the domain model's more general default.

**Revised design:** a small **Add Task** dialog (same modal pattern as §4.6's Create/Rename dialog), opened by the "Add Task" button (§4.4), collecting:

- **Description** (required, text field, empty by default).
- **Component** (required — `D1.4-45` — a `FieldTreePicker` scoped to this Project's own Team, without the `allowNone` option Task Detail's own, already-existing Component field has).
- **Task Type** (required, `FieldSelect`, `TASK_TYPES`, empty/unselected by default).
- **Priority** (`FieldSelect`, `PRIORITY_LEVELS`, pre-filled to `Med`, editable).
- **Requestor** (`FieldSelect`, the Project's own Team-scoped candidate list Task Detail already builds, pre-filled to the current signed-in Person, editable).

**Project** is fixed to this Project (not shown as an editable field in this dialog — it's already established by context, matching V1.2's own pre-filled, effectively-uneditable value in this exact flow). Clicking **Add** validates Description/Component/Task Type are non-blank (Priority/Requestor already have a value from their own defaults) — showing an inline message if not, matching V1.2's own message text where there's a direct equivalent — then calls the new `useCreateTask` mutation (§5.1) with all five fields plus `project_id: id`, and finally calls `openItemWindow("tasks", newTaskId)` to open the newly-created Task's own Task Detail window for anything not covered by this quick dialog (dates, effort, Resources, and so on).

<a id="deleting-a-project"></a>
### 4.8 Deleting a Project

A **Delete** button in the header (visible only to a `TeamLeadUser` on this Project's own Team, §2.3), asking for confirmation (a plain confirm dialog is sufficient at Level 1 — nothing here needs a custom modal), then calling the new `useDeleteProject` mutation (§5.1).

V1.2 blocks this while the Project "has dependants" (`UserInterfaceWindows.md` §3.5). The current server route has no such explicit check — deleting a Project with sub-Projects, Tasks, Dependencies, Attachments, or Remarks still pointing at it fails on a foreign-key constraint instead, which today would surface as a generic, unhelpful message (§5.5 covers why, and what to do about it). This design relies on that constraint failing loudly and being reported clearly (§5.5), rather than re-implementing V1.2's own pre-check client-side — simpler, and correct by construction rather than by a second, separately-maintained rule that could drift from what the database actually enforces.

<a id="drag-and-drop"></a>
### 4.9 Drag and Drop

Per `D1.4-40`, this design deliberately builds none of V1.2's own drag-and-drop for Level 1's first cut of this screen — each is replaced by an explicit control above, and each is picked up again in Stage 5 (`Plan.md` §6.5) once Component Detail (and possibly other windows) exist alongside this one to drag between:

- Reparenting a Project by dragging its own title, or dragging a sub-project row, onto another Project — replaced by the "Parent Project" `FieldTreePicker` (§4.3).
- Dragging a Task onto a Project row to move it there — a Task's own Project field (already an explicit `FieldTreePicker` in Task Detail, `D1.4-4`) already covers this; nothing new needed here, and nothing to defer either, since Task Detail already made this choice.
- OS-file drag-and-drop onto the Attachments grid — replaced by `AttachmentsPanel`'s existing **Add File** picker button, unchanged from Task Detail.
- Cross-window Ctrl-drag Dependency creation from another window's own title badge (V1.2's `ProjectDetail.cs`/`ProjectControl.xaml.cs`; V2's own existing Task-to-Task equivalent, `D1.4-10`) — deferred to Stage 5, extended from Task Detail's own existing drag source/target once Project Detail windows are real to drag between.

<a id="implementation-plan"></a>
## 5. Implementation Plan

<a id="new-api-client-hooks"></a>
### 5.1 New API Client Hooks

All added to `api/hooks.ts`, following the exact shape of the equivalent Task hooks already there:

- `useProject(id)` — `GET /project/{id}`, mirroring `useTask(id)`.
- `useCreateProject()` — `POST /project`, mirroring the shape `useCreateDependency` already establishes for a mutation (invalidate the bulk `["projects"]` key on success, same as `useCreateDependency` invalidates `["dependencies"]`).
- `useUpdateProject(id)` — `PATCH /project/{id}`, mirroring `useUpdateTask(id)`.
- `useDeleteProject(id)` — `DELETE /project/{id}`, a new shape (no existing delete hook to mirror) but a simple one.
- `useCreateTask()` — `POST /task`, needed for §4.7; no existing equivalent to mirror since Task creation doesn't exist in the GUI yet either.

<a id="generalising-dependencies-panel"></a>
### 5.2 Generalising the Dependencies Panel

`DependenciesPanel.tsx` changes from a `{ taskId: number }` prop to an owner shape matching `RemarkOwner`'s own pattern (`{ task_id: number } | { project_id: number }`), so Task Detail's own call site changes from `<DependenciesPanel taskId={id} .../>` to `<DependenciesPanel owner={{ task_id: id }} .../>` — a one-line change at that call site, no behaviour change for Task Detail itself. Internally:

- `useDependencies`/`useCreateDependency`/`useDeleteDependency` (`api/hooks.ts`) generalise the same way — `useDependencies(owner: RemarkOwner)`, querying `task_id` or `project_id` depending on which key is present (the server's own `list_dependencies` already accepts either, §3.1), and `useCreateDependency`'s mutation body accepts all four of `pre_task_id`/`pre_project_id`/`post_task_id`/`post_project_id` (already true today — only the panel's own call sites need widening, not the hook's request shape).
- The "Add Dependency" dialog's `Autocomplete` currently searches `tasks` only (`options={tasks?.filter(...)}`). It needs to search across both Tasks and Projects — e.g. a combined, labelled option list (`"Task #123 — description"` / `"Project — name"`), each carrying enough information (`{ kind: "task" | "project", id }`) to build the right `pre_*_id`/`post_*_id` field when submitting.

<a id="new-components"></a>
### 5.3 New Components

- `ProjectDetailPage.tsx` (`features/projects/`), the main screen, built the same way `TaskDetailPage.tsx` is — one component, `DenseField.tsx` controls, no new field-level components needed.
- A small `ProjectTaskTree` component (or similar), wrapping `@mui/x-tree-view`, rendering the sub-Project/Task rows described in §4.4 — new, since nothing in this codebase uses `@mui/x-tree-view` yet.
- A `CreateOrRenameProjectDialog` component (§4.6), following `DependenciesPanel`'s own `Dialog` usage as a direct template.
- An `AddTaskDialog` component (§4.7), the same `Dialog` pattern, collecting Description/Component/Task Type/Priority/Requestor and running the required-field check `D1.4-43` found before calling `useCreateTask`.

<a id="build-steps"></a>
### 5.4 Build Steps

In dependency order:

1. `api/hooks.ts`: add the five new hooks (§5.1).
2. `App.tsx`: add `/projects/:projectId` and `/projects` routes under `BareAuthenticatedLayout`.
3. Generalise `DependenciesPanel.tsx` and its backing hooks (§5.2); update `TaskDetailPage.tsx`'s own call site to match. Verify Task Detail's Dependencies tab still behaves identically before moving on — this step touches shared, already-shipped code.
4. `lib/schedule.ts`: add `isProjectActive` (§2.2), with unit tests (a Cancelled/Closed-priority Project; one with only Closed/Cancelled direct Tasks and no active sub-Project; one made active only by a deeply-nested active grandchild; a cyclic `parent_project_id` chain, confirming the new guard stops it looping). **Not** wired into `lib/ganttLayout.ts` as part of this step — that switch is deferred to Stage 6 (`D1.4-41`).
5. Build `ProjectDetailPage.tsx`'s header and main fields (§4.2/§4.3) against real data, without the tree or tabs yet — get the field-level editing/Save flow (permissions, dirty-tracking) working first, the same incremental order Task Detail's own build likely followed.
6. Build `ProjectTaskTree` (§4.4) and wire it in, including the Task-visibility filter and `isProjectActive` (step 4) for whether a sub-Project's own row is shown at all.
7. Wire in the (now-generalised) Dependencies tab, plus Attachments/Remarks with a `project_id` owner (§4.5) — expected to be the fastest step, since all three panels are already generic or made generic in step 3.
8. Build `CreateOrRenameProjectDialog` (§4.6) and wire up "Add New Project"/rename.
9. Wire up "Add Task" (§4.7) and Delete (§4.8), including the error-handling work in §5.5.

<a id="pre-existing-bug"></a>
### 5.5 A Pre-Existing Bug This Work Found — Already Fixed

Found while researching this design, not previously recorded: `V2/rest-api/app/errors.py`'s exception handlers (which translate a Postgres constraint violation — including the foreign-key violation a blocked Delete would raise, §4.8 — into a JSON error body) returned `{"error": "<message>"}`, but `lib/apiErrors.ts`'s `formatApiError` (the function every mutation's own catch block already uses to show a real server message instead of a generic guess) only reads a `{"detail": "<message>"}` shape — the shape FastAPI's own `HTTPException` produces, which is what every *other* error path in this codebase raises. A Delete blocked by a real foreign-key constraint (the realistic case for this screen, since nothing here pre-checks for dependants client-side, §4.8) would have fallen through to `formatApiError`'s generic fallback text instead of the actual, more useful "Refers to a record that doesn't exist" message `errors.py` already computed for it.

**Resolved (`D1.4-44`):** `errors.py` now emits `{"detail": ...}` everywhere, matching every `HTTPException` already in this codebase — one response shape for every error, not two. `formatApiError` needed no change (it already only read `.detail`). One existing test asserted the old shape directly and was updated; the `rest-api` Docker image was rebuilt and the full test suite re-run (51 passed) to confirm. Not a Project-Detail-specific bug — fixed ahead of this screen's own build, rather than left for step 8 (§5.4) to rediscover via a confusing error message during testing.

<a id="testing-approach"></a>
## 6. Testing Approach

Same approach already established for Task Detail and the Gantt view (`Plan.md` §7): manual testing against the real running `rest-api` and its seeded data, no mocked API layer. Once built, add a numbered Manual Testing entry to `Plan.md` §7.2 covering: opening Project Detail from a real seeded Project, editing and saving each field, reparenting via the Parent Project picker, expanding/collapsing the tree and opening a sub-Project/Task from it, the Task-visibility filter, adding/renaming/deleting a sub-Project, adding a Task, and Dependencies/Attachments/Remarks round-tripping with a `project_id` owner. Also add the corresponding end-user document, `V2/UserDocumentation/ProjectDetailView.md`, once the screen is real, per `UserDocumentationGuidelines.md` and the standing note in `Plan.md` §7.3.

<a id="open-items-for-review"></a>
## 7. Open Items for Review

Everything above is a considered design proposal, not a settled decision. The handful of judgment calls most worth a second look are tracked as proper open questions/decisions in `Plan.md` §9/§10, not restated here (`document-guidelines.md` rule 2):

- **`D1.4-41`** — resolved: `isProjectActive` is written as part of this screen's own build (§5.4 step 4), but switching the already-shipped Gantt view over to it is deferred to Stage 6 (`Plan.md` §6.6), kept separate from this screen's own, unrelated build.
- **`D1.4-42`** — resolved: §4.4's per-row task-count/priority summary and "Gantt Display" shortcut are deferred to Stage 6, confirmed.
- **`D1.4-43`** — resolved: V1.2 does enforce required fields at creation time (Task: Component/Project/Description/Priority/Task Type/Requestor; Project: Name only) — §4.6/§4.7 above are the revised design reflecting this (a real "Add Task" dialog replaces the earlier instant-create proposal; the Create/Rename Project dialog's own Name-required check is now stated explicitly).
- **`D1.4-44`** — resolved and **implemented**: `errors.py` now emits `{"detail": ...}` everywhere (§5.5) — the one code change made ahead of this screen's own build, rather than deferred to it.
- **`D1.4-45`** — resolved: Component is required in the "Add Task" dialog (§4.7), matching V1.2 exactly — no sentinel "Not Set" Component exists anywhere to have offered a third option (checked and ruled out — full findings in `Plan.md` §10).

Every open item raised while writing this document is now resolved — nothing currently blocks moving from this design into implementation (`Plan.md` §9 is empty).
