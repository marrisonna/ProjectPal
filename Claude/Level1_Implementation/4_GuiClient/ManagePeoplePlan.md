# Manage People & Team Management — Design and Implementation Plan

**Status: design confirmed (`D1.4-83`–`D1.4-87`, `D-DM-12`), no code written yet.** Written before any code, per the user's own request, following the same before-code-review pattern `ProjectDetailPlan.md`/`TaskGridPlan.md`/`ComponentDetailPlan.md`/`SearchPlan.md` (this folder) already established. This document grew from a single-screen "Manage People" plan into two related screens once the user confirmed Team Management (originally deferred to Level 2 by `D-DM-11`) is needed for Level 1 too, and asked for it to be folded into this same piece of work (§5). All open questions (§9) are answered — see that section for the final list, and `Plan.md` §10/`Requirements/DomainModel.md`'s own `D-DM-12` for the full decision text.

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
4. [Manage People (Org-Admin) Design](#manage-people-design)
   - 4.1 [Screen Structure and Navigation](#screen-structure)
   - 4.2 [The People Grid](#the-grid)
   - 4.3 [Create Person Dialog](#create-dialog)
   - 4.4 [Set / Reset Password](#set-password)
   - 4.5 [Deactivation and Deletion](#deactivation-and-deletion)
   - 4.6 [Admin Self-Demotion Guard](#self-demotion-guard)
5. [Team Management (Team Lead) Design](#team-management-design)
   - 5.1 [Screen Structure, Navigation, and the Team Picker](#team-screen-structure)
   - 5.2 [The Team Members Grid](#team-members-grid)
   - 5.3 [Add an Existing Person to the Team](#add-member)
   - 5.4 [Remove a Member](#remove-member)
   - 5.5 [Edit Role / Is Resource / Nickname](#edit-role)
   - 5.6 [Never-Leaderless Guard](#never-leaderless)
6. [Server Changes Required](#server-changes)
7. [Implementation Plan](#implementation-plan)
   - 7.1 [New API Client Hooks](#new-hooks)
   - 7.2 [New Files](#new-files)
   - 7.3 [Build Steps](#build-steps)
8. [Testing Approach](#testing-approach)
9. [Open Items for Review](#open-items)

<a id="purpose-and-scope"></a>
## 1. Purpose and Scope

`4_GuiClient/Plan.md` §6.4 (Stage 4 — Remaining Screens) lists Manage People as one of the remaining unbuilt screens. This plan designs it having read V1.2's real `FormManagePeople`/`FormNewUser`/`GUIPerson` source directly (`V1.2/Apps/ProjectPal/ProjectPal/People/`), and found that **most of the server-side surface both screens need is already built and tested** (`rest-api/app/routes/teams.py`) — this started as mostly GUI-only work, though the review round below adds some genuine, deliberate server-side changes (§6).

Two screens, two audiences, sharing the same underlying Person/PersonRole data:

- **Manage People** — organisation-admin-only, org-wide: create a Person, edit their Person-level fields, set/reset their password, deactivate or (in a narrow case) delete them.
- **Team Management** — Team-Lead-facing (or organisation-admin, for any Team), scoped to one Team at a time: who belongs to this Team, their role and `is_resource` flag, their nickname.

<a id="requirements"></a>
## 2. Requirements

<a id="what-v12-did"></a>
### 2.1 What V1.2 Did

`FormManagePeople.cs`: a modeless singleton window (opened from `MainWindow`'s "Manage Users" status-strip item) showing every Person in one grid — **Name, Is Active, Is Resource, DB Login, User Type, Colour** — editable in-place, gated on a single, screen-wide check: `GUIPerson.IsReadOnly` returns `true` for every column (except the colour swatch, always read-only) unless the current user `Permissions.IsSuperUser`. Two buttons:

- **New Person**: opens `FormNewUser` (just a Name field plus a "this name already exists" live-validated warning that also disables OK); on confirm, `Person.AddNewInstance(name)` then `newPerson.IsActive = true`.
- **Delete Person**: walks every Task (owned/requested/resourced) and every Project (owned) looking for any reference to the selected Person; if none exist, reuses `FormNewUser` a second way (pre-filled, read-only Name, relabelled "Delete this person?") as a confirmation dialog, then genuinely deletes the row; if any reference exists, blocks the delete entirely with a message listing exactly what's still attached.

V1.2 has no Team Management equivalent at all — it predates Team entirely.

<a id="whats-different-in-v2"></a>
### 2.2 What's Different in V2

- **`IsResource` and `UserType` (role) are no longer Person-level fields.** V1.2 predates Team; V2's `PersonRole` (`Requirements/DomainModel.md` §2.4) moved both to per-(Person, Team) — the same Person can be a resource on one Team and not another, or hold a different role on each. Neither has one value to show on a Person-level grid any more; both move to Team Management instead (§5).
- **The gating role for Person records is `is_organisation_admin`, not `TeamLeadUser` — a settled decision, confirmed against real server code.** `Requirements/DomainModel.md`'s `D-DM-4`: "a Team's TeamLeadUser ... cannot create, edit, or delete a Person record itself. That remains exclusively `IsOrganisationAdmin`'s job." Every Person-mutating route in `teams.py` calls `require_org_admin`, never anything TeamLeadUser-related. PersonRole writes, by contrast, already accept *either* `is_organisation_admin` *or* that Team's own TeamLeadUser (`_require_admin_or_team_lead`) — which is exactly the split between the two screens this document now covers.
- **Person deletion, revisited.** `teams.py`'s own docstring currently states "no delete route ever — `is_active=false` is the only 'removal' (`D-DM-4`)." The user's own review (§9) asks for a narrow delete capability back — see §4.5/§6, and note this reopens/amends `D-DM-4` rather than just adding a feature alongside it.
- **A real password now exists, admin-managed only.** V1.2 predates real authentication entirely. `3_Authentication/Plan.md` (`D1.3-4`) settled Level 1 as admin-set-password only, and confirmed again in this review round (§9): no self-service change, no forced first-login change either — an admin sets or resets a Person's password and tells them what it is, out of band. Level 2 revisits how a Person is first onboarded, how they reset a forgotten password, and self-service change generally.
- **`is_active` already has real, wired-up downstream effect.** `TaskGrid.tsx` already filters every Owner/Requestor/Resource picker to `p.is_active`. Toggling this flag from Manage People has an immediate, visible effect on every other screen's own pickers.
- **Team Management is a genuinely new V2-only screen** — V1.2 has nothing to port here at all, only the underlying `PersonRole` write endpoints already built for a different reason (`D-DM-6`/`D-DM-8`'s own Team-scoping work, not originally built with a dedicated management UI in mind).

<a id="permissions"></a>
### 2.3 Permissions

**Manage People**: single, screen-wide gate, `is_organisation_admin` — confirmed by reading every Person-mutating route: `create_person`, `update_person`, `set_person_password` (and the new `delete_person`, §6) all call the same `require_org_admin(caller)`, no row- or field-specific exception anywhere.

**Team Management**: `is_organisation_admin` **or** `is_team_lead(team_id)` for the specific Team being managed — exactly `_require_admin_or_team_lead`, already implemented server-side for every `PersonRole` write. An organisation admin can manage any Team's membership; a Team Lead can only manage Teams they personally lead.

<a id="what-already-exists"></a>
## 3. What Already Exists to Build On

<a id="server-api"></a>
### 3.1 Server API

From `rest-api/app/routes/teams.py`, already built and tested:

- `GET /person`, `POST /person`, `PATCH /person/{id}`, `POST /person/{id}/password` — Manage People's entire data surface except delete (new, §6).
- `GET /team` — list every Team (id + name) — not yet consumed by any GUI hook (`useTeams()` doesn't exist yet, §3.3).
- `GET /person-role` (optionally `?team_id=`), `POST /person-role`, `PATCH /person-role/{person_id}/{team_id}`, `DELETE /person-role/{person_id}/{team_id}` — Team Management's entire data surface except the nickname field and the never-leaderless guard (both new, §6).

<a id="reusable-pieces"></a>
### 3.2 Reusable Client-Side Pieces

- **`usePeople()`/`usePersonRoles()`** (`api/hooks.ts`) — already fetch the full lists; both screens' own data sources, no new *read* hooks needed for these two.
- **`components/DenseDataGrid.tsx`** (`D1.4-73`) — the shared dense-grid chrome and `useDenseGridColumns` (per-column filtering/auto-sizing, double-click-to-fit). Both screens' own grids build on this, matching every other grid in this app.
- **The `Dialog`/`TextField`/`Button` MUI pattern** (`CreateOrRenameProjectDialog.tsx`) — Create Person, Set/Reset Password, and Add Member (§5.3) all follow this small-focused-modal shape.
- **`formatApiError`** (`lib/apiErrors.ts`) — the same shallow, inline error-display convention every other mutation in this app uses; this is how a blocked delete's own explanation (§4.5) and the never-leaderless guard's own rejection (§5.6) both surface to the user.
- **`useAuth()`'s `person.is_organisation_admin`/`person.team_roles`, and `lib/permissions.ts`'s `isTeamLead`/`isTeamLeadOfAnyTeam`** — already exist; reused directly for both screens' own gates and the Team picker's own "which Teams do I lead" list (`person.team_roles.filter(tr => tr.role === "TeamLeadUser")`).
- **`ProjectDetailPage.tsx`'s "Top Level Projects" (no-id) vs. "one specific Project" (with id) duality** — the exact shape Team Management's own team-picker vs. one-Team-at-a-time view reuses (§5.1).

<a id="gaps-to-fill"></a>
### 3.3 Gaps to Fill

- No API client hooks wrap any Person or PersonRole *write* endpoint yet, nor `GET /team` at all — all new (§7.1).
- No route/nav-gating pattern exists yet for an admin-only or Team-Lead-only screen — every other screen this phase is reachable by any authenticated Person.
- No native colour-input convention exists yet anywhere for the `colour` column, which nothing currently renders or edits.
- Server-side (§6): no `DELETE /person` route, no `nickname` field on `WritePersonRoleRequest`/`UpdatePersonRoleRequest`, no guard anywhere preventing a Team being edited down to zero `TeamLeadUser`s, no guard preventing an admin removing their own `is_organisation_admin` flag.

<a id="manage-people-design"></a>
## 4. Manage People (Org-Admin) Design

<a id="screen-structure"></a>
### 4.1 Screen Structure and Navigation

A new popped-out singleton window (`D1.4-8`): `AppShell.tsx` gets a "Manage People" nav button, rendered only when `person?.is_organisation_admin` is true (mirroring the existing "Admin" chip's own conditional). Direct navigation by a non-admin (bookmarked URL, typed by hand) shows a plain "You don't have access to this page" message instead of the grid — no redirect, no heavier access-control framework, matching this app's consistently lightweight approach to permission UI elsewhere. `windowNav.ts` gets a `PEOPLE_WINDOW_FEATURES` entry (similar to Search's own, `900×600`).

<a id="the-grid"></a>
### 4.2 The People Grid

Built on `DenseDataGrid`/`useDenseGridColumns` — real per-column filtering, **hidden by default** (matching Search's own choice, confirmed in review), auto-sized columns, double-click-to-fit.

Columns, replacing V1.2's own `Is Resource`/`User Type` (no longer Person-level, §2.2) with the fields that actually live on `PersonRecord`:

- **Name** — governed (editable when `is_organisation_admin`).
- **Is Active** — governed, checkbox column.
- **Login** — `external_login`, governed.
- **Is Organisation Admin** — governed, checkbox column, disabled specifically on the current admin's own row (§4.6).
- **Colour** — governed, a small colour swatch plus a native `<input type="color">` for editing (this app's own "native controls" convention, and the one HTML control that naturally constrains input to a valid colour). Nothing else in this app renders `colour` yet (it exists for the deferred Gantt colour-mode toggle); this screen becomes its first editor.
- **Set/Reset Password** and **Delete** — not data columns; per-row actions (§4.4/§4.5), matching `TaskGrid`'s own leading action-column shape.

<a id="create-dialog"></a>
### 4.3 Create Person Dialog

A `Dialog`/`TextField` modal matching `CreateOrRenameProjectDialog.tsx`'s own shape — **Name** and **Login** (`external_login`) up front (both needed before the new Person is usable for anything — Login specifically because it's the `POST /auth/login` identifier), calling `POST /person`. `is_organisation_admin` defaults to `false`, `colour` left unset — both editable afterward in the grid, matching V1.2's own "create with what's essential, edit the rest in place" shape.

<a id="set-password"></a>
### 4.4 Set / Reset Password

One dialog, one action, used both to give a brand-new Person their first password and to reset an existing one later (confirmed in review: admin-driven only, no self-service, no forced first-login change — Level 2 revisits onboarding/reset/forgot-password properly). New password (min length 8, matching the server's own `SetPasswordRequest` validation) plus a confirm field to catch typos, calling `POST /person/{id}/password`. The admin is expected to communicate the new password to the Person out of band — nothing here emails or displays it anywhere else.

<a id="deactivation-and-deletion"></a>
### 4.5 Deactivation and Deletion

**Deactivation (`is_active = false`) remains the primary, always-safe removal mechanism** — no reference-checking needed, since nothing is actually removed.

**Deletion is added back, narrowly (§6, `DELETE /person/{id}`, reopening `D-DM-4`).** Per the user's own framing: the main purpose is undoing an accidental creation, soon after it happens — apart from that, People should never actually be deleted. Rather than enforce that intent with a time-based rule ("only within N minutes of creation"), the design leans entirely on **reference-checking**: the delete button is always present for an admin, but the server rejects it — with a message listing exactly what's still attached, matching V1.2's own UX — unless the Person is referenced *nowhere at all*. In practice this only ever succeeds for a Person who hasn't been used for anything yet, which is precisely "soon after creation" without needing to track how soon. Checked tables (more than V1.2 ever had to check, since V2's own domain model is richer): `task.owner_person_id`, `task.requestor_person_id`, `task_resource.person_id`, `project.owner_person_id`, `component.owner_person_id`, `remark.created_by_person_id`, `attachment.owner_person_id`, and — new relative to V1.2, since V1.2 has no such concept — **`person_role.person_id`**: a Person who's been added to even one Team, with nothing else attached yet, still blocks the delete, matching "not mentioned on any other tasks/project/remarks/attachments **etc.**" read as exhaustive, not just the four V1.2 happened to check.

A confirmation dialog precedes the actual delete call, matching this app's existing `window.confirm(...)` pattern for Task delete — this is the one genuinely irreversible action on this whole screen.

<a id="self-demotion-guard"></a>
### 4.6 Admin Self-Demotion Guard

Confirmed in review: an admin may not remove their own `is_organisation_admin` flag. Enforced in two places, not one — client-side alone is never sufficient for something this sensitive:

- **Server (authoritative):** `update_person` rejects (400) a request where `person_id == caller.person_id` and `is_organisation_admin` is being set to `false`.
- **Client (UX):** the checkbox on the admin's own row in the grid is disabled, so the rejection is never actually hit in the normal case.

Deliberately narrow — this guards *self*-demotion only. It doesn't stop a *different* admin from demoting someone else (allowed, matches the user's own wording), and doesn't stop the *last remaining* admin from being deactivated by someone else, which would have a similar locking-out effect — flagged as a related but distinct gap, not fixed here (§9).

<a id="team-management-design"></a>
## 5. Team Management (Team Lead) Design

<a id="team-screen-structure"></a>
### 5.1 Screen Structure, Navigation, and the Team Picker

A new popped-out singleton window. `AppShell.tsx` gets a "Team Management" nav button, rendered when `person?.is_organisation_admin || isTeamLeadOfAnyTeam(person)` is true.

Routes mirror `ProjectDetailPage.tsx`'s own no-id/with-id duality, but the "no id" mode resolves differently depending on who's looking, since — per the user's own note — a Team Lead may lead more than one Team, and an admin can manage any Team at all:

- **`/team-management` (no id):**
  - An organisation admin sees a picker listing **every** Team (`GET /team`) — they can manage any.
  - A Team Lead who leads **exactly one** Team is redirected straight to `/team-management/:teamId` for it — no extra click for the common case.
  - A Team Lead who leads **more than one** sees a picker listing only the Teams they lead.
  - Anyone who leads none and isn't an admin never sees the nav button; direct navigation shows the same access-denied message as Manage People.
- **`/team-management/:teamId`:** the main view (§5.2). A Team switcher (a small dropdown, not a full picker page) appears at the top **only when the viewer has more than one Team available to switch to** (multiple Teams led, or any Team at all for an admin) — a single-Team Lead sees no switcher at all, matching this app's own "don't show UI with nothing to do" pattern (e.g. Component has no "Only Active" checkbox at all, since it has no Priority concept to filter by).
  - Access guard, evaluated against *this specific* `teamId`: `is_organisation_admin || isTeamLead(person, teamId)` — exactly `_require_admin_or_team_lead`'s own shape, not just "leads *some* Team."

<a id="team-members-grid"></a>
### 5.2 The Team Members Grid

Built on `DenseDataGrid`/`useDenseGridColumns`, scoped to `usePersonRoles()`'s own rows filtered to this `teamId` (or a new `?team_id=` query, matching `GET /person-role`'s own support for it), joined against `usePeople()` for display name:

- **Name** (read-only here — editing the Person's own name is Manage People's job, not this screen's).
- **Nickname** — governed, the per-Team display name `D-DM-11` already added the column for but left read-only pending "a dedicated Team Management screen" — this is that screen.
- **Role** — governed, single-select (`NormalUser`/`LeadUser`/`TeamLeadUser`/`ReadOnlyUser`).
- **Is Resource** — governed, checkbox.
- **Remove from Team** — a per-row action (§5.4), not a data column.

<a id="add-member"></a>
### 5.3 Add an Existing Person to the Team

A small `Dialog` — a picker of active People **not already on this Team** (`usePeople()` filtered against this Team's own current `person_role` rows), plus initial Role and Is Resource, calling `POST /person-role`. Deliberately not a "create a new Person" flow — per `D-DM-4`, a Team Lead cannot create Person records at all; only an organisation admin can (Manage People, §4.3), so this dialog only ever picks among People who already exist.

<a id="remove-member"></a>
### 5.4 Remove a Member

`DELETE /person-role/{person_id}/{team_id}`. Unlike Person deletion (§4.5), this is **not** a hard, reference-blocked action — removing a `PersonRole` row doesn't risk an orphaned foreign key the way deleting a Person would (nothing else references `person_role` itself), so the person being removed can't leave a broken reference behind. It can, however, leave a *confusing* one: someone still shown as a Task's Owner or a Resource assignment on this Team after they've been removed from it. Recommendation (§9): a **soft warning**, not a hard block — the confirmation dialog names how many open Tasks/Projects on this Team still reference them (owner, requestor, or resource) if any, but lets the removal proceed regardless once confirmed.

<a id="edit-role"></a>
### 5.5 Edit Role / Is Resource / Nickname

In-place grid editing (matching `TaskGrid`'s own governed-cell pattern), calling `PATCH /person-role/{person_id}/{team_id}`. Nickname requires a server-side field addition (§6) — the write route currently only accepts `is_resource`/`role`.

<a id="never-leaderless"></a>
### 5.6 Never-Leaderless Guard

`teams.py`'s own module docstring already states the intended invariant — "a Team is never left leaderless" — but today only enforces it at Team *creation* (the atomic first-`TeamLeadUser` bootstrap in `create_team`). Neither `update_person_role` nor `remove_person_role` checks it at all: a Team Lead can currently demote themselves, or remove themselves/another Team Lead, right down to zero `TeamLeadUser`s on a Team, with nothing stopping it. New in this plan (§6): both routes reject (400) a change that would leave the target Team with no remaining `TeamLeadUser`. This also naturally covers self-demotion here (unlike Manage People's own admin flag, which needed an explicit self-check, §4.6) — a lone Team Lead trying to step down is blocked by the general "don't leave this Team leaderless" rule, while a Team with two or more Team Leads may freely demote any one of them, including themselves.

<a id="server-changes"></a>
## 6. Server Changes Required

All in `rest-api/app/routes/teams.py`, none built yet — this is the one part of this plan that isn't GUI-only, and each reopens or extends a previously-settled decision rather than being net-new territory:

1. **`DELETE /person/{id}`** (§4.5) — `require_org_admin`; checks all eight tables listed there for any reference, rejecting (409) with a message naming what's still attached if any exist; deletes the row otherwise. Reopens `D-DM-4`'s current "no delete route ever" — needs that decision explicitly amended, not just a feature bolted on beside a stated rule that contradicts it.
2. **Admin self-demotion guard** (§4.6) — `update_person` rejects (400) `person_id == caller.person_id` with `is_organisation_admin: false` in the request body.
3. **`nickname` on `WritePersonRoleRequest`/`UpdatePersonRoleRequest`** (§5.5) — the field `D-DM-11`/`Claude/Level2_Implementation/Scope.md` both already flagged as "currently deliberately omitted," now needed since its own stated precondition (a dedicated Team Management screen) is what this document builds.
4. **Never-leaderless guard** (§5.6) — `update_person_role` and `remove_person_role` both reject (400) a change that would leave the target Team with zero remaining `TeamLeadUser` rows.

Each of these needs its own test in `tests/test_crud_reference_data.py` (or a new `test_person_deletion.py`/similar) alongside the route change — matching this repo's own established practice of shipping server logic and its test coverage together, not the GUI phase's "manual verification only" testing model (§8).

<a id="implementation-plan"></a>
## 7. Implementation Plan

<a id="new-hooks"></a>
### 7.1 New API Client Hooks

`api/hooks.ts`:

```ts
export function useTeams() {
  return useQuery({
    queryKey: ["teams"],
    queryFn: async () => unwrap<TeamRecord[]>(await apiClient.GET("/team")),
  });
}

export function useCreatePerson() { /* POST /person, invalidates ["people"] */ }
export function useUpdatePerson(personId: number) { /* PATCH /person/{id}, invalidates ["people"] */ }
export function useSetPersonPassword(personId: number) { /* POST /person/{id}/password */ }
export function useDeletePerson() { /* DELETE /person/{id}, invalidates ["people"] */ }

export function useCreatePersonRole() { /* POST /person-role, invalidates ["personRoles"] */ }
export function useUpdatePersonRole(personId: number, teamId: number) { /* PATCH /person-role/{personId}/{teamId} */ }
export function useDeletePersonRole() { /* DELETE /person-role/{personId}/{teamId} */ }
```

(`TeamRecord` — `{ team_id: number; name: string }` — is itself new in `api/types.ts`; nothing today models a Team as its own record client-side.)

<a id="new-files"></a>
### 7.2 New Files

- `features/people/ManagePeoplePage.tsx`, `features/people/SetPasswordDialog.tsx` (or inlined — a judgement call at implementation time).
- `features/teams/TeamManagementPage.tsx`, `features/teams/AddTeamMemberDialog.tsx`.

<a id="build-steps"></a>
### 7.3 Build Steps

1. Server changes (§6) and their own tests, first — both GUI screens depend on capabilities that don't exist yet otherwise.
2. `api/hooks.ts`/`api/types.ts`: all of §7.1.
3. `features/people/ManagePeoplePage.tsx` (§4).
4. `features/teams/TeamManagementPage.tsx` (§5).
5. `windowNav.ts`, `App.tsx` (`/people`, `/team-management`, `/team-management/:teamId` routes), `AppShell.tsx` (both conditional nav buttons).
6. `Requirements/DomainModel.md` (`D-DM-4` amendment), `Claude/Level2_Implementation/Scope.md` (remove the now-Level-1 nickname-editing item), `4_GuiClient/Plan.md` (Screen Inventory gains a Team Management row) — once the design itself is confirmed, not before.

<a id="testing-approach"></a>
## 8. Testing Approach

Server changes (§6) get real automated test coverage, per this repo's existing `rest-api` convention (unlike the GUI phase's own manual-only model). GUI: manual verification against the real running `rest-api`/seeded data, per this phase's established pattern. Confirm: both nav buttons are correctly present/absent per role; direct navigation as an unauthorised Person shows the access message; the self-demotion checkbox is genuinely disabled on your own row and the server also rejects it directly; deleting a fresh, unused Person succeeds while deleting one referenced anywhere is rejected with an accurate explanation; the Team switcher appears only when there's genuinely more than one Team to switch to; demoting the sole `TeamLeadUser` on a Team is rejected, while a Team with two is unaffected; toggling Is Active/removing a Team member is reflected immediately in other screens' own pickers without a manual refresh.

<a id="open-items"></a>
## 9. Open Items for Review

None open — every question was answered by the user and is recorded as a decision where it arises in the design above (also cross-referenced in `Plan.md` §10 and, for the one domain-model-level reversal, `Requirements/DomainModel.md`):

- **`D1.4-83`** (§4.2/§4.4/§4.6): password stays fully admin-driven (no self-service, no forced first-login change — a genuine Level 2 question); grid filtering included on both screens, hidden by default; admin self-demotion guarded both server- and client-side.
- **`D1.4-84`** / **`D-DM-12`** (§4.5/§6): Person deletion added back, narrowly — reference-checked (including `PersonRole` itself) rather than time-limited, reopening `2_RestApi/Plan.md`'s original "no delete route, ever" position.
- **`D1.4-85`** (§5): Team Management pulled into Level 1, a separate screen from Manage People (different audience and data scope), with a Team picker/switcher for a Team Lead who leads more than one Team, and for an organisation admin managing any Team.
- **`D1.4-86`** (§4.6/§5.4): removing a Team member is a soft warning, not a hard block (unlike Person deletion, nothing risks an orphaned reference); the last-remaining-admin gap is left unguarded for Level 1, noted rather than silently accepted; nav button placement for the growing set of admin-only screens is deliberately deferred until Admin tooling is designed too.
- **`D1.4-87`** (§5.5/§5.6/§6): `nickname` added to `PersonRole` writes (amending `D-DM-11`'s own "Level 1 read-only" framing), and a new never-leaderless guard on `PersonRole` changes, covering both self-demotion by a lone Team Lead and removal/demotion of the last of several.
