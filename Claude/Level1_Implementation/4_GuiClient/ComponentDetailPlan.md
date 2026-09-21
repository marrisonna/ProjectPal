# Component List + Component Detail — Design and Implementation Plan

**Status: design confirmed (`D1.4-66`–`D1.4-72`), no code written yet.** Written before any code, per the user's own request, following the same before-code-review pattern `ProjectDetailPlan.md`/`TaskGridPlan.md`/`ProjectsGUIComponent.md` (this folder) already established. All three open questions (§7) are answered: Option A (a new, parallel `Component`/`Components` pair, `D1.4-70`), generalise `AddTaskDialog.tsx` (`D1.4-71`), and defer the Component-rooted Gantt Display as its own follow-up rather than building it in this same slice (`D1.4-72`).

## Contents

1. [Purpose and Scope](#purpose-and-scope)
2. [Requirements](#requirements)
   - 2.1 [What V1.2 Did](#what-v12-did)
   - 2.2 [What's Different in V2](#whats-different-in-v2)
   - 2.3 [Permissions](#permissions)
3. [What Already Exists to Build On](#what-already-exists)
   - 3.1 [Server API](#server-api)
   - 3.2 [Reusable Client-Side Pieces](#reusable-pieces)
   - 3.3 [Gaps to Fill](#gaps-to-fill)
4. [Design](#design)
   - 4.1 [Screen Structure](#screen-structure)
   - 4.2 [Header](#header)
   - 4.3 [The Component/Components Tree](#the-tree)
   - 4.4 [Attachments / Remarks Tabs](#sub-tabs)
   - 4.5 [Create / Rename Component Dialog](#create-rename-dialog)
   - 4.6 [Adding a Task From a Component](#adding-a-task)
   - 4.7 [Deleting a Component](#deleting-a-component)
   - 4.8 [Gantt Display](#gantt-display)
5. [Implementation Plan](#implementation-plan)
   - 5.1 [New API Client Hooks](#new-hooks)
   - 5.2 [Reuse vs. a New Component/Components Pair](#reuse-vs-new)
   - 5.3 [New Files](#new-files)
   - 5.4 [Build Steps](#build-steps)
6. [Testing Approach](#testing-approach)
7. [Open Items for Review](#open-items)

<a id="purpose-and-scope"></a>
## 1. Purpose and Scope

`4_GuiClient/Plan.md` §6.4 (Stage 4 — Remaining Screens) lists Component List/Component Detail as the next unbuilt screen, explicitly noting it "reuses Project Detail's tree pattern." This document designs it, having read V1.2's real implementation directly (`V1.2/Apps/ProjectPal/ProjectPal/Components/`) rather than assuming it's a Project reskin, and checked what's genuinely reusable unchanged from the Project/Task work already shipped this phase.

Out of scope, matching the Screen Inventory (`Plan.md` §5): Component merging (`FormComponentMerge.cs` — `ConfigWindow`/`Merge Dialogs` row, "No", deferred per `D-DM-3`) and anything drag-and-drop-based (Stage 5, `D1.4-40`).

<a id="requirements"></a>
## 2. Requirements

<a id="what-v12-did"></a>
### 2.1 What V1.2 Did

Read directly: `ComponentWindow.cs`/`.Designer.cs`, `ComponentControl.xaml.cs`, `ComponentStackControl.xaml.cs`, `GUIComponent.cs`, `NewComponent.cs`, and `DBProjectPal/Component.cs`.

Component's V1.2 architecture is a structural clone of Project's (`ComponentWindow`/`ComponentControl`/`ComponentStackControl` vs. `ProjectDetail`/`ProjectControl`/`ProjectStackControl` — the same pattern `ProjectsGUIComponent.md` §2 already documented in detail), but the **data model is much smaller**: `DBProjectPal.Component` has only `Name`, `Parent` (self-referencing), and `Owner` — no Priority, no dates, no detailed description, and (per `Component.cs`'s own comment) it "plays no role in scheduling or dependencies." `ComponentWindow.Designer.cs`'s own field list confirms this: `labelComponentTitle` (name), `labelOwner`, `labelParent` — nothing else. Two tabs: "Components" (the tree + an Attachments grid split) and "Gantt Display".

Confirmed, reading the code directly rather than assuming symmetry with Project:

- **Sibling ordering is alphabetical only** — `SortComponents`/`SortComponentName` are both a plain `string.Compare(a.Name, b.Name)`, never priority-weighted, because Component has no Priority field to weight by at all.
- **No "Add Sub-Component" affordance inside the tree.** `ComponentControl.xaml.cs` has `imageAddTask` (per-row "Add Task", opening `TaskDetail(componentItemOnDisplay.DBComponent)`) and a rename/delete context menu — same as `ProjectControl`'s own confirmed shape (`ProjectsGUIComponent.md` §2.1/`D1.4-61`). Adding a **sub**-Component is `ComponentWindow`'s own single `buttonNewComponent`, which creates a child of whichever Component that window is currently rooted at — exactly the "Add Subproject" button precedent already built for Project Detail, not a per-row tree affordance.
- **No Dependencies tab.** Only Attachments is wired into `ComponentWindow`'s own tab (`gridControlAttachments`); no Remarks panel either in this V1.2 window specifically (Remarks was `RemarkWindow`, a separate popup in V1.2 — V2 already inlines it everywhere per `Plan.md` §5's row for it, so V2 gets a Remarks tab here `V1.2` itself never had, for consistency with Task/Project Detail rather than V1.2 fidelity).
- **A dead checkbox.** `checkBoxOnlyOpenTasks_CheckedChanged` exists but its handler body is empty — a non-functional leftover, not a real "active Components only" filter (Component has no notion of "active" to filter by in the first place, unlike Project's computed `IsActive`).
- **Delete is blocked client-side if the Component has dependants** (`HasDependants`: any Task or sub-Component) — checked before deleting, with a message box. V2's schema (below) doesn't replicate this check server-side; see §4.7.

<a id="whats-different-in-v2"></a>
### 2.2 What's Different in V2

`Requirements/DomainModel.md` §2.7 (`D-DM-6`): Component gained a `team_id` for Level 1 — "mirroring Project's Team-scoping... governs who may create/edit/delete the Component, not which Team's Tasks may *reference* it." The actual V2 schema and REST API already exist and were verified directly (`V2/database/migrations/001_initial_schema.sql`, `V2/rest-api/app/routes/components.py`):

```sql
CREATE TABLE component (
    component_id        serial PRIMARY KEY,
    parent_component_id integer REFERENCES component(component_id),
    team_id              integer NOT NULL REFERENCES team(team_id),
    name                 text NOT NULL,
    owner_person_id      integer REFERENCES person(person_id),
    ...
);
```

Matches the client's own already-shipped `ComponentRecord` type (`api/types.ts`) exactly: `component_id, parent_component_id, team_id, name, owner_person_id`. No Priority, no dates, no detailed description — confirming V1.2's own simpler shape carried through unchanged, just with `team_id` added.

Every foreign key into `component` (`task.component_id`, `component.parent_component_id`, `attachment.component_id`, `remark.component_id`) is a plain `REFERENCES` with no `ON DELETE` clause — Postgres default is `NO ACTION`, so deleting a Component that still has Tasks, sub-Components, Attachments, or Remarks pointing at it will fail at the database level with a foreign-key-violation error, not a friendly message. See §4.7.

<a id="permissions"></a>
### 2.3 Permissions

Read `components.py` directly rather than assuming it mirrors Project's rules — it doesn't, in one place:

| Action | Server check | Same as Project? |
|---|---|---|
| Create | `require_role_at_least(caller, team_id, "LeadUser")` | Yes, identical |
| Update (incl. rename/reparent) | `require_owner_or_team_lead(...)` | Yes, identical |
| **Delete** | `require_owner_or_team_lead(...)` | **No** — Project's `delete_project` is TeamLeadUser-only, no owner exception. Component's `delete_component` allows the record's own owner too. |

This is exactly the kind of mismatch `D1.4-64` was written to prevent recurring: the permission rule must be read from the server route itself, not assumed-by-analogy from a sibling entity, and computed inside the reusable component, never injected as a window-specific callback. Recorded as **`D1.4-66`**: Component's own three permission checks, computed the same way `Project.tsx` computes its own —

```ts
const canRename = canEditOwnedRecord(person, component.team_id, component.owner_person_id);
const canDelete = canEditOwnedRecord(person, component.team_id, component.owner_person_id); // NOT isTeamLead — Component's delete allows the owner too
const canAddTaskHere = hasRoleAtLeast(person, component.team_id, "LeadUser");
```

`lib/permissions.ts` needs no changes at all — `canEditOwnedRecord`/`isTeamLead`/`hasRoleAtLeast` are already fully generic over `(person, teamId, ownerPersonId)`, not Project-specific; confirmed by reading the file.

<a id="what-already-exists"></a>
## 3. What Already Exists to Build On

<a id="server-api"></a>
### 3.1 Server API

Full CRUD already implemented and tested (`components.py`, `tests/test_crud_component.py`): `GET /component` (optional `team_id`/`parent_component_id` filters, unused by the client so far — see §3.3), `GET /component/{id}`, `POST /component`, `PATCH /component/{id}`, `DELETE /component/{id}`. Reparenting is rejected across a Team boundary server-side (`D-DM-9`), same as Project.

<a id="reusable-pieces"></a>
### 3.2 Reusable Client-Side Pieces

Verified by reading each directly — all usable **unchanged**:

- **`api/hooks.ts`'s `useComponents()`** — already fetches every Component (no params), matching `useProjects()`'s own "fetch all, filter client-side to member Teams" convention used throughout `ProjectDetailPage.tsx`/`AllTaskPage.tsx`/`PlanPage.tsx`. No mutations exist yet (§3.3).
- **`RemarkOwner`** (`api/hooks.ts`) already includes `{ component_id: number }`, and `AttachmentsPanel`/`RemarksPanel` are already typed against it (`owner: RemarkOwner`) — both panels work for a Component today with zero changes, confirmed by reading their prop signatures directly.
- **`DependencyOwner`** deliberately excludes `component_id` (`{ task_id } | { project_id }` only) — confirms §2.1's "plays no role in... dependencies" is already correctly reflected server- and client-side. No Dependencies tab for Component (§4.4).
- **`components/TreePicker.tsx`/`DenseField.tsx`'s `FieldTreePicker`/`buildBreadcrumb`** — already used for a Component *picker* in `AddTaskDialog.tsx` and `TaskDetailPage.tsx`; the same `TreeItem[]`-building shape (`{ id: component_id, name, parentId: parent_component_id }`) is exactly what a "Parent Component" field needs too.
- **`TaskGrid`** — the whole component, unchanged; only a new column-set constant is needed (§3.3).
- **`Project.tsx`/`Projects.tsx`** — the conventions `Component.tsx`/`Components.tsx` mirror as a new, parallel pair, not a generic reuse of these (§5.2, `D1.4-70`).
- **`windowNav.ts`'s `openItemWindow`/`openListWindow`** — the singleton-window mechanism itself needs no changes, just a new `entityType` ("components") wired into `windowFeaturesFor`.

<a id="gaps-to-fill"></a>
### 3.3 Gaps to Fill

- `useCreateComponent`/`useUpdateComponent`/`useDeleteComponent` mutations (§5.1) — don't exist yet.
- A `COMPONENT_DETAIL_WINDOW_FEATURES` preset in `windowNav.ts`, and a "COMPONENTS" nav button in `AppShell.tsx` (`openListWindow("components")`), matching Projects' own.
- A new embedded `TaskGrid` column-set constant (§4.3) — Component's own embedded grid needs `project_id` shown (unlike Project's, where it's redundant), since a Component's Tasks can span many different Projects.
- Everything in §4/§5 below.

<a id="design"></a>
## 4. Design

<a id="screen-structure"></a>
### 4.1 Screen Structure

Mirrors `ProjectDetailPage.tsx`'s own dual-purpose shape exactly: one `ComponentDetailPage` component, routed at both `/components` ("Top Level Components", no id) and `/components/:componentId` (a specific Component), under the same `BareAuthenticatedLayout` route group Project/Task/Plan already use (no app shell — it's a singleton popout window, opened via `openItemWindow("components", id)` / `openListWindow("components")`).

Given §2.1/§2.2's findings, Component Detail's header has **no separate "Main Fields" section at all** — unlike Project Detail's Priority/dates/parent/description block, a Component has only three editable things (name, owner, parent), all of which fit in the same compact header row `ProjectDetailPage.tsx` already uses for Project's own name/owner/parent. Below that: the Tasks-visibility toggle + Add Subcomponent/Add Task buttons, the Component/Components tree, then the Attachments/Remarks sub-tabs.

<a id="header"></a>
### 4.2 Header

Same layout convention as Project Detail's own header (`ProjectDetailPage.tsx`'s top `Box`): a "C" badge (matching Project's "P"), editable name input (readOnly unless `canRename`), a Parent breadcrumb link (when a parent exists), an Owner `<select>` (candidates: active People with `is_resource` on this Component's own Team — same restriction Project's owner field and TaskGrid's own Owner column already use), "All Components"/Save/Delete buttons — **Delete before Save**, matching the button-order fix already applied to Project Detail. Saving is staged behind one "Save" button (dirty-tracked the same way Project Detail's `form`/`dirty` state already works) — same session save model (`D-Win-5`), not immediate-write.

No Priority/Start Date/Due Date/End Date/Detailed Description row — Component has none of these fields.

<a id="the-tree"></a>
### 4.3 The Component/Components Tree

Functionally identical to `ProjectsGUIComponent.md`'s own design (§4), with two real differences, both already established as their own decisions above:

- **No "Only Active Components Only" checkbox** — Component has no Priority/activity concept to filter by (§2.1); omit it entirely rather than inventing one V1.2 never had.
- **Sibling ordering is alphabetical only** (`D1.4-67`) — no priority-weighted sort exists for Component; `childComponentsOf` sorts purely by `a.name.localeCompare(b.name)`, matching V1.2's own `SortComponentName` directly.

Everything else carries over unchanged in shape (whichever implementation approach §5.2 settles on): a chevron-collapsible row per Component (name, rename/delete/add-task icons — permissions per `D1.4-66`), an embedded `TaskGrid` for that Component's own Tasks (filtered by the same None/Open/All toggle), recursing into sub-Components.

**New embedded column set (`D1.4-68`).** Project's own `EMBEDDED_TASK_GRID_COLUMNS` excludes `project_id` because it's redundant inside that Project's own section — the inverse applies here: a Component's Tasks can belong to *any* Project (Task→Component is independent of Task→Project, `DomainModel.md` §2.7), so knowing which Project a given Task belongs to is actually useful, while `component_id` is what's now redundant. `TaskGrid.tsx` needs a second constant:

```ts
export const COMPONENT_EMBEDDED_TASK_GRID_COLUMNS: TaskGridColumnKey[] = [
  "urgency", "tentative_resource_assignment", "description", "status", "resources",
  "end_date", "effort_in_days", "effort_type", "percentage_allocation", "task_type",
  "project_id", "priority", "start_date", "attachments", "remarks",
  "owner_person_id", "requestor_person_id", "date_added", "status_date",
  "external_reference_url",
];
```
(The same front-loaded ordering already chosen for `EMBEDDED_TASK_GRID_COLUMNS`, with `component_id` dropped and `project_id` swapped in where `component_id` used to sit in the tail.)

<a id="sub-tabs"></a>
### 4.4 Attachments / Remarks Tabs

Two tabs, not three — no Dependencies (§3.2). Otherwise identical to `ProjectSubTabs`: `<AttachmentsPanel owner={{ component_id: id }} hideHeading />`, `<RemarksPanel owner={{ component_id: id }} hideHeading teamId={teamId} />`.

<a id="create-rename-dialog"></a>
### 4.5 Create / Rename Component Dialog

A direct copy of `CreateOrRenameProjectDialog.tsx`'s shape (same dual-purpose modal, same required-name-only validation, same "Parent" shown read-only) — `CreateOrRenameComponentDialog.tsx`, calling the new `useCreateComponent`/`useUpdateComponent` mutations (§5.1) instead of Project's.

<a id="adding-a-task"></a>
### 4.6 Adding a Task From a Component

**A real design decision, not a copy of Project's.** `AddTaskDialog.tsx` today hard-codes `project_id` (a prop) and requires picking a Component via `FieldTreePicker` — because a Task's Project is mandatory and its Component is optional, so opening the dialog *from* a Project already supplies the one mandatory field. Opening it *from* a Component is the inverse: the Component is now the one already known, but a Task's Project is still mandatory and **must** be picked — V1.2 has no real precedent to follow here either (`GUIComponent`'s own `imageAddTask` opens the full `TaskDetail` window, not a lightweight dialog — V2 already made its own "small dialog instead of the full window" modernisation decision, `D1.4-43`/`D1.4-45`, independent of this).

**Decided (`D1.4-71`): generalise the existing dialog**, rather than build a separate one — the validation, error handling, and most fields (Description/Task Type/Priority/Requestor) are identical either way, and keeping one dialog avoids the two copies drifting apart the way `D1.4-64`'s permission bug happened from having two independent notions of the same rule. `AddTaskDialog.tsx` takes an `openedFrom: { kind: "project"; projectId; teamId } | { kind: "component"; componentId; teamId }` — existing Project-opened behaviour unchanged; the Component-opened case swaps which field is implicit vs. picked, needing a Project `FieldTreePicker` (the exact tree-building shape already used for Component pickers, just fed Project data instead).

<a id="deleting-a-component"></a>
### 4.7 Deleting a Component

Same confirm-then-delete flow as Project Detail's `handleDeleteProject`, using `D1.4-66`'s owner-or-team-lead check to decide whether the Delete button shows at all. Unlike V1.2, there's no client-side "has dependants" pre-check — `delete_component` doesn't have V1.2's own guard, and neither does `delete_project`, so this isn't a new inconsistency to fix here, just an existing, already-accepted gap (a delete-with-dependants attempt fails server-side with a raw FK-violation error, surfaced through the same `formatApiError` helper already used everywhere else). Worth a one-line note in `formatApiError` only if this turns out to read badly in practice — not designing a bespoke message for it speculatively.

<a id="gantt-display"></a>
### 4.8 Gantt Display

`Plan.md` §6.3 already commits to this: "the Component-scoped variant... stays assigned to Stage 4... a straightforward reuse of the already-built rendering code once Component Detail exists." Concretely, `lib/ganttLayout.ts`'s `collectRows`/`buildGanttLayout` walk `Project → sub-Projects (+ own Tasks)`; a Component-rooted equivalent needs a second, parallel walk over `Component → sub-Components (+ own Tasks)` — the same shape, a different tree, and (per §2.1) no Priority-based filtering or ordering to carry over. `PlanPage.tsx` would need a `rootKind: "project" | "component"` (or a second route, `/component-plan/:id`) to pick which walk to run.

This is real, separable work, not a one-line addition — **decided (`D1.4-72`): deferred as its own explicit follow-up**, not built as part of this same slice of work. Component List/Detail itself should land and be verified first.

<a id="implementation-plan"></a>
## 5. Implementation Plan

<a id="new-hooks"></a>
### 5.1 New API Client Hooks

In `api/hooks.ts`, immediately after `useComponents()`, matching `useCreateProject`/`useUpdateProject`/`useDeleteProject`'s exact shape (mutation + cache invalidation of `["components"]`):

```ts
export function useCreateComponent() { /* POST /component, invalidate ["components"] */ }
export function useUpdateComponent(componentId: number) { /* PATCH /component/{id}, invalidate ["components"] */ }
export function useDeleteComponent() { /* DELETE /component/{id}, invalidate ["components"] */ }
```

<a id="reuse-vs-new"></a>
### 5.2 Reuse vs. a New Component/Components Pair

**The central decision (`D1.4-70`, decided: Option A).** `Project.tsx`/`Projects.tsx` and what Component Detail's own tree needs are structurally almost identical (name + embedded TaskGrid + recursive sub-list, chevron row, internally-computed permissions). Two ways to build it:

- **Option A — a new, parallel `Component.tsx`/`Components.tsx` pair**, deliberately mirroring `Project.tsx`/`Projects.tsx`'s own conventions closely (same chevron/icon treatment, same `getValues`-driven embedded-TaskGrid sizing, same D1.4-64-style internally-computed permissions) but each genuinely simpler and independently correct for Component's own (different) rules — no priority-based sort, no "active only" checkbox, the different delete permission (`D1.4-66`), the different embedded column set (`D1.4-68`). Some duplication of the recursion *shape* between the two pairs, in exchange for each file staying simple and never needing to branch on "which entity am I."
- **Option B — generify `Project`/`Projects` into one pair parameterised over Project vs. Component** (a `kind: "project" | "component"` discriminator, or an adapter object supplying `{ getChildren, getOwnTasks, computePermissions, sortSiblings }`). Genuinely less code overall, but every one of §2–§4's real differences (permission rule, sort order, the "only active" filter, the embedded column set, whether a Tasks-toggle-adjacent "also show own tasks" concept even applies) has to be threaded through as a parameter or branch, and `D1.4-64`'s own stated principle — permission logic lives *inside* the component, never injected — sits uneasily with a shared component that has to be told which entity's permission rule to run.

**Decided: Option A** — matching the precedent already set for `TaskGrid` vs. `Project`/`Projects` themselves: `TaskGrid` was centralised because every window shows the *same* Task shape and the *same* editability rules — genericising pays for itself. Project and Component are two genuinely different entities with two genuinely different rule sets that happen to share a tree shape; forcing them through one parameterised component risks exactly the kind of subtle cross-entity bug `D1.4-64` already found once (a permission rule silently borrowed from the wrong sibling). A future third tree-shaped entity would be the point to reconsider — not before.

<a id="new-files"></a>
### 5.3 New Files

- `features/components/ComponentDetailPage.tsx` — mirrors `ProjectDetailPage.tsx`.
- `features/components/Component.tsx` / `features/components/Components.tsx` — mirrors `Project.tsx`/`Projects.tsx` (`D1.4-70`).
- `features/components/CreateOrRenameComponentDialog.tsx` — mirrors `CreateOrRenameProjectDialog.tsx`.
- `AddTaskDialog.tsx` generalised in place (`D1.4-71`) — moved or kept in `features/projects/`? It stops being Project-specific either way; likely relocates to a shared location (e.g. `features/tasks/`) once two different owners open it. Worth deciding at implementation time, not a design blocker now.

<a id="build-steps"></a>
### 5.4 Build Steps

1. `api/hooks.ts`: the three new mutations (§5.1).
2. `TaskGrid.tsx`: `COMPONENT_EMBEDDED_TASK_GRID_COLUMNS` (§4.3).
3. `Component.tsx`/`Components.tsx` (`D1.4-70`).
4. `CreateOrRenameComponentDialog.tsx`.
5. `AddTaskDialog.tsx` generalisation (`D1.4-71`).
6. `ComponentDetailPage.tsx`, wiring all of the above together, per §4.
7. `windowNav.ts` (`COMPONENT_DETAIL_WINDOW_FEATURES`, `windowFeaturesFor`), `App.tsx` (routes), `AppShell.tsx` ("COMPONENTS" nav button).

Gantt Display (§4.8) is explicitly not a build step here — deferred (`D1.4-72`).

<a id="testing-approach"></a>
## 6. Testing Approach

Same as every other screen this phase: no automated GUI tests yet (`Plan.md` §7.1); manual verification against the real running `rest-api`/seeded data, extending `Plan.md` §7.2's checklist — create/rename/delete a Component (as owner, as TeamLeadUser, as neither), reparent within and across Teams (the cross-Team case should be rejected, per `D-DM-9`), add a Task from both a Project and a Component context, confirm the Owner-or-TeamLead delete rule specifically (a non-owning LeadUser should see Delete on a Component they don't own — unlike Project, where they wouldn't), and confirm the embedded TaskGrid's Project column actually appears (unlike Project Detail's own embedded grid).

<a id="open-items"></a>
## 7. Open Items for Review

None open — all three were answered by the user and are recorded as decisions where they arise in the design above (also cross-referenced in `Plan.md` §10):

- **`D1.4-70`** (§5.2): Option A — a new, parallel `Component`/`Components` pair, not a generic reuse of `Project`/`Projects`.
- **`D1.4-71`** (§4.6): generalise the existing `AddTaskDialog.tsx` (an `openedFrom` discriminator) rather than build a separate dialog.
- **`D1.4-72`** (§4.8): the Component-rooted Gantt Display is deferred as its own explicit follow-up, not built as part of this same slice of work.
