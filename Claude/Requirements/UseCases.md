# ProjectPal — Use Cases

*Open questions in this document use the prefix `Q-UC-`; decisions use `D-UC-`.*

## Contents

1. [Manage Projects and Tasks (core CRUD + browsing)](#manage-projects-and-tasks)
2. [Assign Resources to a Task](#assign-resources-to-a-task)
3. [View the Plan (Gantt / Resource-Loading View)](#view-the-plan)
4. [Set Dependencies Between Work Items](#set-dependencies)
5. [Attach Files and Captured Emails](#attach-files-and-captured-emails)
6. [Search / Find](#search-find)
7. [Add Comments (Remarks) to a Work Item](#add-comments)
8. [Resolve Concurrent Edit Conflicts](#resolve-concurrent-edit-conflicts)
9. [Manage People (Admin)](#manage-people)
10. [Administer the System (Operational/Support Tooling)](#administer-the-system)
11. [Use Cases Not Carried Forward](#not-carried-forward)
12. [Permission Model in V2 (Requirements)](#v2-permission-model)
13. [Open Questions](#open-questions)
14. [Decisions](#decisions)
- Annex A: [Permission Model in V1.2 (Research Findings)](#v1-2-permission-model)
  - A.1 [How Permissions Were Enforced](#permission-enforcement)
  - A.2 [Permission Matrix](#permission-matrix)
  - A.3 [Surprises and Anti-Patterns](#permission-surprises)
  - A.4 [Admin Screen Access](#permission-admin-screens)

These are the workflows the new system needs to support, grounded in what the old prototype (`V1.2`) actually does today — real, exercised functionality, not a wishlist. Each one notes what should carry forward as-is, what needs to change because of the new architecture (API-first, multi-tenant, cross-platform client), and open questions. See `Goals.md` for which level (Demonstrator / MVP / later) each is likely to matter for, and `KeyConcepts.md` / `DomainModel.md` for terminology and entities.

<a id="manage-projects-and-tasks"></a>
## 1. Manage Projects and Tasks (core CRUD + browsing)

**Actor:** any user with edit rights (owner, LeadUser, TeamLeadUser, per role).

The bread-and-butter workflow: browse a filterable/sortable list of Projects or Tasks, open one for detail editing (description, priority, status, effort, owner, requestor, component, dependencies, attachments, remarks), create new ones, reparent them in their respective trees (Project under Project, Component under Component).

This is the core of the product and carries forward directly as a use case, delivered through a web/cross-platform client talking to an API rather than a WinForms grid talking directly to SQL Server. The specific rich interactions in the old UI (drag-and-drop reparenting between tree nodes, drag-and-drop dependency lists) are UX choices to redesign for the new client technology (see `Goals.md` Level 1 "client technology" framing question).

<a id="assign-resources-to-a-task"></a>
## 2. Assign Resources to a Task

**Actor:** anyone with edit rights on the Task.

Assign one or more People (marked as resources) to a Task — see `KeyConcepts.md`'s Effort vs. Duration entry for how this affects scheduling. In the old app this is a checklist in the Task detail screen. Worth confirming that's still the intended model, rather than drag-and-drop directly onto the Gantt view, before designing the new UI.

<a id="view-the-plan"></a>
## 3. View the Plan (Gantt / Resource-Loading View)

**Actor:** anyone who can view the relevant Projects/Tasks.

A visual, hierarchical timeline of Projects/Tasks with a resource-workload graph underneath, colour-codable by Person/Priority/Status, supporting drag-to-reschedule (subject to edit permission) and click-through to the underlying Task/Project detail.

This is one of the app's most distinctive and most-valued features (per `Goals.md`'s framing of the GUI as something to actively trial with users), and also one of the most technically demanding to reproduce outside WinForms — the old implementation is a fully custom-drawn WPF control. For the new system this needs to be evaluated as its own significant piece of work (an existing charting/Gantt library vs. a custom build). Worth flagging as a Level 1 "feature scope" candidate for either inclusion (it's likely core to what's worth trialling) or deliberate, explicit deferral.

<a id="set-dependencies"></a>
## 4. Set Dependencies Between Work Items

**Actor:** anyone with edit rights.

See `KeyConcepts.md`'s Dependency entry and `DomainModel.md`'s Dependency section for what this is and its rules (e.g. cycle prevention). Feeds directly into the plan view's scheduling. Carries forward as a use case; the old drag-between-two-listboxes interaction is a UX detail to redesign.

<a id="attach-files-and-captured-emails"></a>
## 5. Attach Files and Captured Emails

**Actor:** anyone with create rights on Attachments (broadly open in the old model).

See `KeyConcepts.md`'s Attachment entry and `DomainModel.md`'s Attachment section for what this is and its rules (e.g. deduplication).

The use case ("keep supporting evidence attached to a work item") carries forward as file upload from the browser/client, and — if capturing emails is still wanted — a server-side mechanism (e.g. forward-to-an-address / inbound email processing via the API) rather than a desktop integration (see `Goals.md` Non-Goals). **Needs a decision**: is "capture an email as an attachment" still an important use case for the Demonstrator, or can it be deferred entirely to a later level?

<a id="search-find"></a>
## 6. Search / Find

**Actor:** any user.

Search across Tasks, Projects, Components, Remarks, and (optionally) Attachment contents, scoped to what the user can see, with a results list that opens the matching item. Carries forward as a core use case. Full-text search inside attachment *contents* (as opposed to just metadata) is reasonable to treat as a later enhancement, with metadata/field search as the Level 1 baseline.

<a id="add-comments"></a>
## 7. Add Comments (Remarks) to a Work Item

**Actor:** anyone with create rights on Remarks (broadly open — even ReadOnly users could add Remarks in the old model).

See `KeyConcepts.md`'s Remark entry for why this matters and `DomainModel.md`'s Remark section for its structure.

<a id="resolve-concurrent-edit-conflicts"></a>
## 8. Resolve Concurrent Edit Conflicts

**Actor:** any user editing a record someone else also changed.

See `KeyConcepts.md`'s Merge/Conflict entry for the concept, and `DomainModel.md`'s Decisions (`D-DM-3`) for the concurrency-model decision. In the old app, this triggers as an automatic dialog whenever a background refresh detects the record changed elsewhere since it was loaded — likely a lower-stakes problem in a web app where data is fetched fresh per view rather than held in a long-lived in-memory client cache.

This use case is specifically about two different *users* editing the same record — settled as out of scope for Level 1 (`D-DM-3`/`D-UC-2` above). A related but distinct question, *one* user's own multiple open windows staying in sync with each other after a save (motivated by the same V1.2 in-memory-cache behaviour, but not a conflict-resolution problem at all), is answered by `UserInterfaceWindows.md`'s `D-Win-5`.

<a id="manage-people"></a>
## 9. Manage People (Admin)

**Actor:** organisation-admin (Person/Team management is organisation-wide, not scoped to any one Team — see `DomainModel.md`'s Role/permission model decision).

Add a Person, mark them active/inactive, set whether they're a resource, set their role. People are never hard-deleted — marking someone `IsActive = false` when they leave is the only "removal" this use case does; the record and its history stay intact. Carries forward directly as a use case; the *identity* side needs rework — see `DomainModel.md`'s Person entry and `Goals.md`'s identity direction.

At the Organisation/Team level, this use case also needs to grow: inviting people into an Organisation, assigning them to Teams, and (per Level 3 in `Goals.md`) eventually self-service tenant onboarding — all of which is new design work.

<a id="administer-the-system"></a>
## 10. Administer the System (Operational/Support Tooling)

**Actor:** `IsOrganisationAdmin` / vendor operator — not any one Team's `TeamLeadUser`, since administering the system isn't a per-Team concern (`DomainModel.md`'s Role/permission model decision). Consistent with impersonation (`D1-2`) already being gated the same way.

The old app's `AdminWindow` bundles several things: impersonate another user for support purposes, switch storage backend, toggle encryption, force a full re-sync, bulk export attachments. Most of these are specific to the old app's particular architecture (a fat client with a local in-memory cache and a filesystem-backed alternative storage mode) and won't map 1:1 onto a server-hosted, API-first system. The underlying needs — a support/impersonation path, a way to verify data integrity, a way to bulk-export a customer's data — are legitimate and should be re-derived from first principles for the new architecture, likely as Level 2/3 vendor tooling (per `Goals.md`'s "cross-tenant tooling" cost note).

<a id="v2-permission-model"></a>
## 12. Permission Model in V2 (Requirements)

Primed from Annex A.2's V1.2 findings as a starting point (see Annex A at the end of this document), and now settled (`D-UC-4`, §14) — V1.2's matrix was a reference point to weigh against, not something carried forward verbatim (e.g. Project deletion staying TeamLeadUser-only even for the owner reflects a deliberate re-decision, not an unexamined carry-over).

Renamed for V2: `SuperUser` → **TeamLeadUser**, `PowerUser` → **LeadUser** (Annex A keeps the old names, since it's a historical record of V1.2).

| Entity | Create | Edit | Delete |
|---|---|---|---|
| Task | LeadUser or TeamLeadUser | Owner (any role above ReadOnly), or TeamLeadUser | Owner (any role above ReadOnly), or TeamLeadUser |
| Component | LeadUser or TeamLeadUser | Owner (any role above ReadOnly), or TeamLeadUser | Owner (any role above ReadOnly), or TeamLeadUser |
| Project | LeadUser or TeamLeadUser | Owner (any role above ReadOnly), or TeamLeadUser | **TeamLeadUser only — not even the owner** |
| Remark | Anyone, including ReadOnlyUser | **Owner**, including a ReadOnlyUser owner (`D-DM-7`) — reverses the immutable/append-only design originally specified for this entity (`Requirements/DomainModel.md`'s Remark entity). Doesn't reintroduce the old app's actual quirk (reassigning authorship to whoever edits), since only the original owner can ever edit or delete their own Remark. | **Owner**, including a ReadOnlyUser owner, or TeamLeadUser |
| Attachment | Anyone above ReadOnlyUser | *(no edit path)* | Owner (any role above ReadOnly), or TeamLeadUser |
| Dependency | Governed by the *owning Task/Project's* Edit permission — Dependency has no permission concept of its own. | | |
| Person | `IsOrganisationAdmin` only | `IsOrganisationAdmin` only | Never — People are never hard-deleted; `is_active = false` is the only "removal" (§9) |
| Team | `IsOrganisationAdmin` only | `IsOrganisationAdmin` only | Not yet decided |
| PersonRole (Team membership & role) | TeamLeadUser (their own Team, any member/role) — or `IsOrganisationAdmin`, only to bootstrap the initial TeamLeadUser entry when a Team is first created (see note below) | TeamLeadUser (their own Team, any member/role) — or `IsOrganisationAdmin`, only to set/change who holds the TeamLeadUser role for a Team | TeamLeadUser (their own Team, any member/role) — `IsOrganisationAdmin` not included |
| TeamLeadUser | Can do everything, unconditionally, regardless of ownership. | | |
| ReadOnlyUser | Can do nothing except create Remarks — every other check short-circuits to denied. | | |

**Team creation must bootstrap a TeamLeadUser.** A Team is useless with nobody able to lead it, so whenever an `IsOrganisationAdmin` creates a Team, they must also create the PersonRole entry giving some existing Person the `TeamLeadUser` role for that Team, as part of the same workflow — not a separate, optional follow-up step.

<a id="not-carried-forward"></a>
## 11. Use Cases Not Carried Forward

- Switching between a SQL Server backend and a local filesystem-based pseudo-database — an old-app implementation detail with no equivalent need in the new architecture.
- The one-off batch mail-import tool (`AttachmentLoader`, hardcoded to a local folder) — a migration utility, not a repeatable end-user workflow.
- Any Word-document generation/automation via COM — per `Goals.md` Non-Goals, revisit later via a non-COM mechanism if still needed.

<a id="open-questions"></a>
## 13. Open Questions

None currently open — every question originally raised for this document has already been answered; see Decisions below.

<a id="decisions"></a>
## 14. Decisions

- **D-UC-1**<br>
  **Question:** Which of these use cases are actually essential to a meaningful Demonstrator trial (per `Goals.md` Level 1's "feature scope" framing question)?<br>
  **Decision:** see `D1-4` in `Level1_Implementation/ImplementationPlan.md`.
- **D-UC-2**<br>
  **Question:** Does the Demonstrator need multi-user concurrent editing at all, or can it reasonably assume low contention and defer conflict-handling design to MVP?<br>
  **Decision:** see `D-DM-3` in `Requirements/DomainModel.md`, and `D1-4` in `Level1_Implementation/ImplementationPlan.md` (concurrent access by multiple users is required for Level 1; handling two users editing the *same* record at once is not).
- **D-UC-3**<br>
  **Question:** For attachments, is "capture an email" a Level 1 requirement, or can Level 1 ship with plain file upload only?<br>
  **Decision:** see `D1-4` in `Level1_Implementation/ImplementationPlan.md` (file and hyperlink attachments only; captured emails are not required for Level 1).
- **D-UC-4**<br>
  **Question:** What should each of the four per-Team roles actually permit in V2, per entity — and is that check scoped to a specific Team?<br>
  **Decision:** yes, Team-scoped — since a Person's role lives on PersonRole per-Team, every role check in §12's table is against the role that Person holds on the *specific Team that owns the resource being acted on*, not any role they hold anywhere else in the Organisation. Project and Component (`D-DM-6`) each carry their own `TeamId` directly; Task's Team is its own Project's Team (Task has no independent `TeamId`). With that scoping rule settled, §12's table (as amended for Remark by `D-DM-7`) is the settled V2 permission matrix.

<a id="v1-2-permission-model"></a>
## Annex A: Permission Model in V1.2 (Research Findings)

The old app's four roles (ReadOnlyUser/NormalUser/PowerUser/SuperUser) were never fully documented — `KeyConcepts.md` §13 and `DomainModel.md`'s Role/permission model decision record *that* the old model was flat and system-wide, but not exactly what each role could do. This section records what a direct code investigation of `V1.2` actually found, as input to designing the new per-Team role + `IsOrganisationAdmin` model — it's a factual record of old behavior, not a decision about what V2 should do (see the Open Questions, §13, above).

<a id="permission-enforcement"></a>
### A.1 How Permissions Were Enforced

Every permission check in the old app funnels through one function: `Utils.Permissions.IsAllowed(objectOwner, EntityType, ChangeType)` (`V1.2\Libs\Utils\Utils\Permissions.cs`), called from roughly 60 sites across the GUI (`Apps\ProjectPal\ProjectPal\*`, `Libs\PlanDisplay\*`) to enable/disable buttons, grid columns, and menu items. The four-role enum's declared order is never used numerically — every check is an explicit equality/membership test, not a "≥ this level" comparison.

**Critically, `DBAccess` (the data-access layer) never calls `IsAllowed` at all.** Enforcement is entirely GUI-side; nothing in the old app stops a client that bypasses the UI from writing directly. This is a real weakness the new system's API-first, API-layer-authorization design (`D1-2`, `D1.2-3`) already avoids by construction — worth naming explicitly as a lesson, not just an old quirk.

<a id="permission-matrix"></a>
### A.2 Permission Matrix

| Entity | Create | Edit | Delete |
|---|---|---|---|
| Task | PowerUser or SuperUser | Owner (any role above ReadOnly), or SuperUser | Owner (any role above ReadOnly), or SuperUser |
| Component | PowerUser or SuperUser | Owner (any role above ReadOnly), or SuperUser | Owner (any role above ReadOnly), or SuperUser |
| Project | PowerUser or SuperUser | Owner (any role above ReadOnly), or SuperUser | **SuperUser only — not even the owner** |
| Remark | Anyone, including ReadOnlyUser | *(no edit path in the old app — V2 reverses this, see `D-DM-7`)* | Owner (any role above ReadOnly), or SuperUser |
| Attachment | Anyone above ReadOnlyUser | *(no edit path)* | Owner (any role above ReadOnly), or SuperUser |
| Dependency | Governed by the *owning Task/Project's* Edit permission — Dependency has no permission concept of its own. | | |
| SuperUser | Can do everything, unconditionally, regardless of ownership. | | |
| ReadOnlyUser | Can do nothing except create Remarks — every other check short-circuits to denied. | | |

The practical NormalUser/PowerUser distinction is narrower than the names suggest: they're identical except PowerUser can *create* new Tasks/Components/Projects, while NormalUser is limited to editing/deleting things they already own plus creating Remarks/Attachments. PowerUser does not get broader *edit* rights over other people's work — only broader *create* rights.

<a id="permission-surprises"></a>
### A.3 Surprises and Anti-Patterns

- **ReadOnlyUser creating Remarks is confirmed real**, not a documentation error — it's the one and only thing that role can do beyond reading.
- **Project deletion is SuperUser-only, full stop** — unlike Task/Component, even a Project's own owner cannot delete it.
- **A shared hardcoded password bypasses the role system entirely.** `MainWindow.cs` gates the Admin/Manage Users menu items on being SuperUser *or* knowing a fixed, weakly-obfuscated password ("namnam", checked case-insensitively on its first three characters, in `Functions.AdminPassword`) — so anyone who knows that string gets full Admin/Manage-People access regardless of their actual role. **This is a security anti-pattern from the old app and must not be replicated in V2.**
- **A dead hardcoded-username backdoor exists but is never actually called.** `DBAccess`'s `DBObjectBase.ThisUserType` auto-grants SuperUser if the database username is `"Neil"` or contains `"marrison"` — but nothing in the codebase calls this property; it's orphaned, not live behavior. Noted for completeness, not as a live concern.
- **`Permissions.EntityType.Link` doesn't exist**, even though `Links\GUILink.cs`/`Links\LinkWindow.cs` reference it — consistent with those files being dead/orphaned code excluded from every `.csproj` (as already found during the earlier UI sweep for `UserInterfaceWindows.md`).

<a id="permission-admin-screens"></a>
### A.4 Admin Screen Access

- **Admin window / Manage Users**: SuperUser, or anyone who enters the "namnam" password (Annex A.3). Nothing inside the Admin window is further role-gated once you're in.
- **Manage People grid**: every column is read-only unless SuperUser, with one carve-out — the Gantt bar colour swatch is editable by anyone regardless of role, matching `DomainModel.md`'s framing of colour as a per-Person setting.
- **Config window** (general settings): open to everyone, except the "View Private Items" checkbox, which is SuperUser-only — tied to the old Private/Visibility flag V2 has already dropped (see `DomainModel.md`).
- **MainWindow's default task list is role-scoped by default, not hard-gated**: non-SuperUser users default to seeing only Tasks where they're a resource; SuperUser defaults to seeing everyone's. This is a starting filter a user can presumably change, not a permission boundary.
