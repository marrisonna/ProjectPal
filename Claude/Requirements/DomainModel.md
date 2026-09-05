# ProjectPal — Domain Model

*Open questions in this document use the prefix `Q-DM-`; decisions use `D-DM-`.*

## Contents

1. [Tenancy Scope Note](#tenancy-scope-note)
2. [Core Entities](#core-entities)
   - 2.1 [Organisation](#organisation)
   - 2.2 [Team](#team)
   - 2.3 [Person](#person)
   - 2.4 [PersonRole](#person-role)
   - 2.5 [Project](#project)
   - 2.6 [Task](#task)
   - 2.7 [Component](#component)
   - 2.8 [Resource assignment (Task ↔ Person)](#resource-assignment)
   - 2.9 [Dependency (Task/Project ordering)](#dependency)
   - 2.10 [Attachment](#attachment)
   - 2.11 [Remark](#remark)
3. [Entity Relationships](#entity-relationships)
4. [Cross-Cutting Concerns](#cross-cutting-concerns)
5. [Open Questions](#open-questions)
6. [Decisions](#decisions)
7. [Future Extensions (Beyond Level 1)](#future-extensions)

This describes the domain model for the **new** implementation (`V2`). It's grounded in the entities and relationships that exist in the old prototype (`V1.2`), since that's proven-out real usage, but it is a design for the new system — not a description of the old one. Where the old model has quirks, legacy artefacts, or decisions that don't fit the new multi-tenant/API-first direction (see `Goals.md`), that's called out explicitly as **a decision needed**, not silently carried forward.

<a id="tenancy-scope-note"></a>
## 1. Tenancy Scope Note

Per `Goals.md`, a tenant is an **Organisation**, and the model is database-per-tenant. Everything below (Project, Task, Person, etc.) lives *inside* one organisation's database — there is no `OrganisationId` on these tables because the database itself is the organisation boundary. **Team**, however, is a grouping *within* an organisation and does need to be a first-class concept in this model (the old app has no equivalent — it only ever supported one team per install). See Open Questions below.

<a id="core-entities"></a>
## 2. Core Entities

<a id="organisation"></a>
### 2.1 Organisation
See `KeyConcepts.md`'s Organisation entry for what this is and why it matters, and `Goals.md` §Multi-tenancy for the tenancy decision. Not present as a concept in the old app at all — the old app *is* a single organisation's worth of data. Organisation's own shape (name, subscription/plan info, admin contacts) belongs in the control-plane/metadata database, not in the tenant database itself.

<a id="team"></a>
### 2.2 Team
See `KeyConcepts.md`'s Team entry for what this is and why it matters. Not present in the old app — old app has a single implicit "team" (everyone in the database). **Needs design** — see Decisions (`D-DM-1`).

This needs more defintion, but a team will need a **TeamId** and a name, possibly other attributes to.

A Person is not confined to one Team: they may belong to multiple Teams at once, and their role can differ from one Team to the next — see PersonRole below, which is where that membership and per-Team role actually live.

<a id="person"></a>
### 2.3 Person
See `KeyConcepts.md`'s Person and Resource entries for what this dual-purpose modeling is and why it matters.

Key attributes are:
- `PersonId` - a primary key, unique id for a real person in the real world.  Each person in an organisation has a unique PersonId.
- `IsActive` — a Person is never hard-deleted, full stop, regardless of what they own/request/are-assigned-to. When someone leaves, they're marked `IsActive = false`; the record and its history stay intact.
- `IsOrganisationAdmin` — an organisation-wide administrator flag. Lives on the Person, not on PersonRole, because organisation administration isn't scoped to any one Team; more than one Person in an Organisation can hold it.
- Name and logon fields/attributes — to be defined in detail. Needs to become a proper external-identity reference (see `Goals.md` Level 1/2 identity direction).

Sentinel/placeholder resources ("Other", "Unassigned") were useful in the old app for representing non-human or not-yet-known resources — worth keeping the *concept* (a work item can have an unresolved or non-Person resource) but worth deciding whether it's better modeled as a nullable assignment plus a status, rather than magic sentinel rows.

<a id="person-role"></a>
### 2.4 PersonRole

A Person's membership in, and role within, one specific Team. A Person has one PersonRole per Team they belong to, so multi-Team membership falls out naturally: add another PersonRole row rather than modeling membership separately from role. Role is therefore per-Team, not global to the Person — the same Person can be a TeamLeadUser on one Team and a NormalUser on another.

Key attributes are:
- `PersonId` - a reference to the **Person**
- `TeamId` - a reference to the **Team**
- `IsResource` — whether this Person can be assigned to work items (independent of whether they can log in).
- `UserType`/role — TeamLeadUser / LeadUser / NormalUser / ReadOnlyUser for V2 (renamed from the old app's SuperUser / PowerUser / NormalUser / ReadOnlyUser — see `Claude/Requirements/UseCases.md`'s Annex A for the V1.2 behavior these were based on), scoped per-Team via this table (see `KeyConcepts.md`'s Role / Permission Level entry, and Decisions item `D-DM-4` for how this combines with the organisation-level admin role on Person).
- `Nickname` — a shorter name this Person is known by on this Team (e.g. "Alice" rather than "Alice Chen"), nullable, shown instead of Person's own name wherever a screen displays a name in this Team's context. Has no equivalent in the old app. See Decisions (`D-DM-11`).

The Gantt bar colour assigned to a Person should be a generic per-Person, per-Organisation setting.

<a id="project"></a>
### 2.5 Project
See `KeyConcepts.md`'s Project entry for what this is and why it matters.

Attributes worth carrying forward: name, priority, description, owner, due date. New attribute: `TeamId` — for Level 1 (see `Goals.md`), every Project belongs to exactly one Team (see Decisions (`D-DM-1`) and Future Extensions below for how this may broaden later). Reparenting is confined to that Team: a Project's `parent_project_id` may only point to another Project belonging to the same Team (`D-DM-9`).

The old model's **derived scheduling** is its most distinctive and most debatable feature: a Project's start date is constrained by its dependencies, and its *end date is never stored* — it's computed as the max end date across all child tasks and sub-projects, recursively. This makes the schedule always internally consistent but means nothing about timing is a simple stored fact. **Decision:** the new system keeps this derived-computation approach rather than moving to stored dates (see Decisions (`D-DM-2`) below).

<a id="task"></a>
### 2.6 Task
See `KeyConcepts.md`'s Task entry for what this is and why it matters.

Attributes worth carrying forward: description (short + detailed), priority, status, task type (enhancement/maintenance/support/etc.), owner, requestor (a Person), effort (amount + Effort-vs-Duration type — see `KeyConcepts.md`), percentage allocation, a tentative-assignment flag, an external reference URL, and relates to a Component.

A Task's relationship to a Component is optional, not mandatory: in the old schema the field (`AffectedComponentId`) is nullable, and application code treats an unset Component as a valid state rather than an error. It's a many-Tasks-to-one-Component relationship — a Task relates to at most one Component, a Component can have many Tasks relating to it — kept separate from the Project relationship (see `KeyConcepts.md`'s Component entry for why the two axes are independent).

The old model stores a Task's start as a **relative business-day offset from its Project's start date**, not an absolute date, and derives its actual start/end/duration from effort, assigned-resource count, and dependency constraints at read time. Same decision as Project above: the new system keeps this derived-computation approach.

A Task has exactly one Project. The old schema's earlier many-to-many Task↔Project link table was simplified away in favour of this direct relationship, which is a useful precedent: prefer the simpler relationship unless a real need for many-to-many resurfaces.

**Decision (Level 1, `D-DM-10`):** a Task may only be moved to a different Project belonging to the *same* Team as its current Project — moving a Task across Teams is deferred to Level 2 (see Future Extensions below).

<a id="component"></a>
### 2.7 Component
See `KeyConcepts.md`'s Component entry for what this is and why it matters. Structurally: same self-referencing tree shape as Project, but independent of it — plays no role in scheduling or dependencies. New attribute: `TeamId` (`D-DM-6`) — for Level 1, every Component belongs to exactly one Team, mirroring Project's Team-scoping (see Project entry above), needed so Team-scoped CRUD authorization (`Requirements/UseCases.md`'s `D-UC-4`) has a Team to check for Component the same way it already does for Project. This governs who may create/edit/delete the Component, not which Team's Tasks may *reference* it — a Task belonging to any Team can still tag a Component belonging to a different Team, since Component remains a cross-Team classification tag for reporting (`KeyConcepts.md`'s Component entry). Reparenting, unlike tagging, is confined to that Team: a Component's `parent_component_id` may only point to another Component belonging to the same Team (`D-DM-9`).

<a id="resource-assignment"></a>
### 2.8 Resource assignment (Task ↔ Person)
See `KeyConcepts.md`'s Resource entry for why this matters. Structurally: a many-to-many relationship between Task and Person, scoped to actual Person resources. This is the Level 1 scope specifically — see Future Extensions below for broadening "resource" beyond Person.

**Decision (`D-DM-8`):** Team-scoped — a Person is assignable to a Task only if they hold `is_resource = true` on the *same Team* as the Task's own Project (via that Person's PersonRole for that Team), not merely `is_resource = true` on some other Team. This matches PersonRole's per-Team shape and keeps resource assignment consistent with every other Team-scoped authorization check (`Requirements/UseCases.md`'s `D-UC-4`).

<a id="dependency"></a>
### 2.9 Dependency (Task/Project ordering)
See `KeyConcepts.md`'s Dependency entry for what this is and why it matters — including that either side of the relationship can be a Task or a Project. Structurally: the old model detects cycles before allowing a new link, and supports only finish-to-start ordering with no lag/lead time — worth deciding whether the new system needs richer dependency types.

<a id="attachment"></a>
### 2.10 Attachment
See `KeyConcepts.md`'s Attachment entry for what this is and why it matters. Structurally: exactly one owner among Task/Project/Component (mutually exclusive). Content is one of three kinds — an uploaded file, a captured email, or a hyperlink (a URL, stored with no binary payload of its own) — with a deduplication check on the first two (don't re-attach the identical file/email twice). Ingestion should happen through the API — e.g. direct upload from the client, pasting/entering a URL for the hyperlink kind, and, if capturing emails remains a requirement, inbound email processing via the API (see `Goals.md`) — rather than through a desktop integration.

<a id="remark"></a>
### 2.11 Remark
See `KeyConcepts.md`'s Remark entry for why this matters. Structurally: same "exactly one owner" shape as Attachment. **Decision (`D-DM-7`):** a Remark's own owner (the Person who created it, including a ReadOnlyUser — who can create Remarks in the first place) may edit or delete their own Remark; nobody else may, except a Team's TeamLeadUser, who may additionally delete (but not edit) a Remark they don't own (`Requirements/UseCases.md` §12). This reverses an earlier immutable/append-only design for this entity; what that original design actually protected against — and what's preserved here — is authorship never being reassigned: only a Remark's original owner can ever change or remove it, correcting the old model's quirk of reassigning authorship to whoever last edited a remark.

<a id="entity-relationships"></a>
## 3. Entity Relationships

This section pulls the relationships scattered through the Core Entities above into one place, so the shape of the model can be seen at a glance.

- **Organisation → Team**: one Organisation has many Teams. Since the database itself is the Organisation boundary (see Tenancy Scope Note above), this containment is implicit rather than a foreign key.
- **Organisation → Person, via `IsOrganisationAdmin`**: not a relationship table — a flag directly on Person, orthogonal to Team membership. Any number of People in an Organisation can hold it.
- **Team ↔ Person, via PersonRole**: many-to-many. A Person holds one PersonRole per Team they belong to, and each PersonRole carries its own `IsResource` flag and `UserType`/role — so the same Person can be a resource on one Team and not another, or hold a different role on each.
- **Team → Project**: one-to-many for Level 1 — each Project belongs to exactly one Team (see Decisions (`D-DM-1`) and Future Extensions below).
- **Team → Component**: one-to-many for Level 1 — each Component belongs to exactly one Team (see Decisions (`D-DM-6`)), independent of the Team → Project relationship above. This governs management of the Component, not which Team's Tasks may reference it — a Task can still tag a Component belonging to a different Team than its own Project's.
- **Project → Project**: self-referencing tree (sub-projects). A Project's owner is a Person.
- **Project → Task**: one-to-many, and mandatory on the Task side — a Task belongs to exactly one Project (see Task entry above for why the old many-to-many link table was simplified away).
- **Task → Person**: two distinct single-valued relationships — owner and requestor — plus a separate many-to-many relationship for resourcing (below). A Task's owner and requestor need not be the same Person, and neither need be one of the assigned resources.
- **Task ↔ Person, via Resource assignment**: many-to-many, scoped to People marked as a resource (`IsResource` on their PersonRole). This is separate from ownership/requesting above.
- **Task → Component**: many-to-one and optional — a Task relates to at most one Component, a Component can have many Tasks relating to it (see Task entry above for the optionality check against the old schema).
- **Component → Component**: self-referencing tree, structurally like Project's tree but otherwise independent of it — a Component's position in its tree has no bearing on Project/Task scheduling.
- **Dependency, between Task/Project**: a self-referencing many-to-many ordering relationship where either side can be a Task or a Project (see Dependency entry above), cycle-checked on creation.
- **Attachment and Remark, owned by Task/Project/Component**: each Attachment or Remark has exactly one owner, chosen from Task, Project, or Component (mutually exclusive — not a shared join table across all three).

<a id="cross-cutting-concerns"></a>
## 4. Cross-Cutting Concerns

- **Encryption at rest** on free-text fields (descriptions, remarks, names) — worth keeping the principle, expressed as a platform-level (database/column) encryption concern rather than application code manually encrypting/decrypting every field.
- **Concurrency model** — see `KeyConcepts.md`'s Merge/Conflict entry for the old app's approach. **Decision:** Level 1 has multiple named users accessing and modifying data concurrently — it does not assume a single user at a time. What Level 1 doesn't need is handling for two users editing the *same* record at the same time: real usage during the Demonstrator will avoid that scenario, so no conflict-detection/resolution mechanism is built. Level 2 needs real conflict handling, once that assumption can no longer be relied on — see Future Extensions below.
- **Cascade delete** — the old app removes related resource links, dependencies, attachments, and remarks in application code when a Task is deleted. Database-enforced cascades or soft-deletes are worth using instead now that this isn't constrained by the old ORM's shape.

<a id="open-questions"></a>
## 5. Open Questions

None currently open — every question originally raised for this document has already been answered; see Decisions below.

<a id="decisions"></a>
## 6. Decisions

- **D-DM-1**<br>
  **Question:** Team scoping — how does Team fit into the model, given the old app has no equivalent (see Team entry above)?<br>
  **Decision (Level 1):** every Project belongs to exactly one Team (`TeamId` on Project — see Project entry above). A Person's Team membership was already settled via PersonRole (a Person can belong to multiple Teams, with an independently-set role per Team). The one-Team-per-Project rule may be relaxed in later levels — see Future Extensions.
- **D-DM-2**<br>
  **Question:** Derived vs. stored scheduling — does the new system keep the old model's fully-computed schedule, or move to a simpler stored-date model (see Project and Task entries above)?<br>
  **Decision:** keep the old model's fully-computed schedule (dates derived from effort + dependencies + resourcing, nothing stored as an absolute date except where unavoidable). This is a Foundational Decision (see `KeyConcepts.md`) and holds from Level 1 onward — it isn't level-gated the way the items below are.
- **D-DM-3**<br>
  **Question:** Concurrency model — see `KeyConcepts.md`'s Merge/Conflict entry for the old app's approach; what does each Level need?<br>
  **Decision:** level-dependent — see Cross-Cutting Concerns above for the Level 1 answer, and Future Extensions for what later levels need.
- **D-DM-4**<br>
  **Question:** Role/permission model shape — how do a per-Team role and organisation-wide administration combine?<br>
  **Decision:** an organisation-level administrator role, modeled as `IsOrganisationAdmin` directly on Person (see Person entry above), independent of Team/PersonRole — any number of People in an Organisation may hold it, since organisation administration isn't scoped to any one Team. The two tiers have a clean boundary at the Person/PersonRole split: a Team's TeamLeadUser (the top of that Team's per-Team role — see PersonRole entry above) can manage their own Team's membership and roles — add an existing Person to the Team, remove one, change a member's role within that Team — but cannot create, edit, or delete a Person record itself. That remains exclusively `IsOrganisationAdmin`'s job. In practice this means a brand-new Person has to be created by an organisation admin before any Team's TeamLeadUser can add them to a Team.
- **D-DM-5**<br>
  **Question:** Non-Person resources — is there a real need to extend Resource assignment beyond Person?<br>
  **Decision:** yes — in this domain a Resource is something *responsible* for getting work done, so an external vendor qualifies as a Resource but a piece of equipment does not (equipment isn't responsible for anything). Deferred out of Level 1 scope — see Future Extensions.
- **D-DM-6**<br>
  **Question:** Should Component carry a `TeamId` like Project does, or stay Team-agnostic?<br>
  **Decision:** yes — every Component belongs to exactly one Team, mirroring Project's Level 1 Team-scoping (`D-DM-1`). This settles who may create/edit/delete a Component (checked against the caller's role on that Component's own Team, per `Requirements/UseCases.md`'s `D-UC-4`); it does not restrict which Team's Tasks may reference the Component, which stays a cross-Team classification tag as before.
- **D-DM-7**<br>
  **Question:** Should Remark stay immutable/append-only as originally specified, or let its owner edit/delete it?<br>
  **Decision:** the owner of a Remark (including a ReadOnlyUser owner) may edit or delete their own Remark; a Team's TeamLeadUser may additionally delete (but not edit) a Remark they don't own; nobody else may do either. Authorship is still never reassigned — only the original owner can ever change or remove their own Remark — which is the actual thing the original immutable/append-only design was protecting against. Requires the database's blanket update/delete rejection to be replaced with a narrower rule rejecting only a change to `created_by_person_id`; the owner/TeamLeadUser check itself is enforced by the API, not the database.
- **D-DM-8**<br>
  **Question:** Must a Person be a resource (`is_resource = true`) on the specific Team that owns a Task to be assignable to it, or can a resource on any Team be assigned to any Task?<br>
  **Decision:** must be a resource on that Task's own Team — assigning a Person to a Task whose Project belongs to Team T requires that Person to hold a PersonRole on Team T with `is_resource = true`.
- **D-DM-9**<br>
  **Question:** Can a Project's or Component's parent belong to a different Team than the child, or must reparenting stay within one Team?<br>
  **Decision:** must stay within the same Team — changing a Project's `parent_project_id`, or a Component's `parent_component_id`, to a node belonging to a different Team is rejected. Keeps each tree cleanly within one Team's management, rather than a TeamLeadUser's tree silently including nodes from a Team they have no standing in.
- **D-DM-10**<br>
  **Question:** Can a Task's Project (and therefore its Team) be changed to a Project in a different Team?<br>
  **Decision (Level 1):** no — a Task may only be reassigned to a Project belonging to the *same* Team as its current Project. Moving a Task across Teams is deferred to Level 2, where it needs a real answer (e.g. requiring write access on both the source and destination Team) — see Future Extensions.
- **D-DM-11**<br>
  **Question:** Should PersonRole gain a `Nickname` attribute, so a Person can be known by a shorter name on a given Team than their recorded `Name` (e.g. "Alice" rather than "Alice Chen")?<br>
  **Decision:** yes — nullable, on PersonRole (not Person), since it's inherently per-Team: the same Person could have a different nickname on each Team they belong to, or none at all. Wherever a screen's data belongs to a Team, display falls back to the plain `Name` when no nickname is set for that Team, or when the Person shown isn't scoped to any one Team (e.g. Owner/Requestor pickers list active People org-wide — `D1.4-15`). Level 1 is read-only: `Nickname` is only ever set via seed data for now — editing it is a team-lead-facing capability (mirroring the existing `person_role` write-permission rule) that almost certainly belongs on a dedicated Team Management screen once one exists, not scattered across other screens; deferred to Level 2, recorded in `Claude/Level2_Implementation/Scope.md` (`4_GuiClient/Plan.md` `D1.4-21`).

<a id="future-extensions"></a>
## 7. Future Extensions (Beyond Level 1)

Per `Goals.md`'s Delivery Levels, Level 1 is deliberately narrow in scope. The items below are changes to the domain model above that are anticipated or plausible once later levels are in scope — not designed in detail yet, but flagged so the Level 1 model doesn't quietly foreclose them.

- **Project may relate to more than one Team.** Level 1 fixes one Team per Project (see Team → Project in Entity Relationships above); cross-team or Team-spanning Projects, or Projects that move between Teams, may be needed once multi-team collaboration is a real scenario.
- **Component may relate to more than one Team.** Level 1 fixes one Team per Component (`D-DM-6`), same rationale as Project's entry above.
- **Moving a Task across Teams.** Level 1 restricts a Task's Project changes to Projects on the same Team (`D-DM-10`); letting a Task move to a different Team's Project needs a real authorization answer once cross-team collaboration is a real scenario (e.g. requiring write access on both the source and destination Team), not the blanket rejection Level 1 uses.
- **Conflict detection/resolution for concurrent edits.** Level 1 already has multiple users accessing and modifying data at the same time; what it doesn't handle is two of them editing the *same* record at once, since real usage during the Demonstrator avoids that. Level 2 can no longer rely on that avoidance and needs a real answer — either users editing the same Organisation's data seeing each other's changes live (a materially bigger architectural commitment: live push/sync between server and clients) or the old app's load-time conflict-detection dialog (see `KeyConcepts.md`'s Merge/Conflict entry for that older, lower-bar model), or both. Not to be confused with `UserInterfaceWindows.md`'s `D-Win-5`, which is Level 1 and already built: that's one user's own multiple open browser windows staying in sync with each other (a same-browser-session `BroadcastChannel`), not different users' clients seeing each other's changes — a genuinely different, larger mechanism this bullet is about.
- **Non-Person resources.** Extend Resource assignment (see Core Entities above) beyond Person to include things that are *responsible* for getting work done but aren't a Person in the system — e.g. an external vendor — while explicitly excluding things that aren't responsible for anything, like equipment. Deferred entirely out of Level 1; the Resource assignment relationship stays Person-only until this is designed.
