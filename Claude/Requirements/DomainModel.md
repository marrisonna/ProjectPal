# ProjectPal — Domain Model

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
5. [Open Questions / Decisions Needed](#open-questions)
6. [Future Extensions (Beyond Stage 1)](#future-extensions)

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
See `KeyConcepts.md`'s Team entry for what this is and why it matters. Not present in the old app — old app has a single implicit "team" (everyone in the database). **Needs design** — see Open Questions.

This needs more defintion, but a team will need a **TeamId** and a name, possibly other attributes to.

A Person is not confined to one Team: they may belong to multiple Teams at once, and their role can differ from one Team to the next — see PersonRole below, which is where that membership and per-Team role actually live.

<a id="person"></a>
### 2.3 Person
See `KeyConcepts.md`'s Person and Resource entries for what this dual-purpose modeling is and why it matters.

Key attributes are:
- `PersonId` - a primary key, unique id for a real person in the real world.  Each person in an organisation has a unique PersonId.
- `IsActive` — soft-disable, not a hard delete (deleting a Person is blocked while they still own/request/are-assigned-to anything).
- `IsOrganisationAdmin` — an organisation-wide administrator flag. Lives on the Person, not on PersonRole, because organisation administration isn't scoped to any one Team; more than one Person in an Organisation can hold it.
- Name and logon fields/attributes — to be defined in detail. Needs to become a proper external-identity reference (see `Goals.md` Stage 1/2 identity direction).

Sentinel/placeholder resources ("Other", "Unassigned") were useful in the old app for representing non-human or not-yet-known resources — worth keeping the *concept* (a work item can have an unresolved or non-Person resource) but worth deciding whether it's better modeled as a nullable assignment plus a status, rather than magic sentinel rows.

<a id="person-role"></a>
### 2.4 PersonRole

A Person's membership in, and role within, one specific Team. A Person has one PersonRole per Team they belong to, so multi-Team membership falls out naturally: add another PersonRole row rather than modeling membership separately from role. Role is therefore per-Team, not global to the Person — the same Person can be a SuperUser on one Team and a NormalUser on another.

Key attributes are:
- `PersonId` - a reference to the **Person**
- `TeamId` - a reference to the **Team**
- `IsResource` — whether this Person can be assigned to work items (independent of whether they can log in).
- `UserType`/role — SuperUser / PowerUser / NormalUser / ReadOnlyUser in the old app, now scoped per-Team via this table (see `KeyConcepts.md`'s Role / Permission Level entry, and Open Questions/Decisions item 4 for how this combines with the organisation-level admin role on Person).

The Gantt bar colour assigned to a Person should be a generic per-Person, per-Organisation setting.

<a id="project"></a>
### 2.5 Project
See `KeyConcepts.md`'s Project entry for what this is and why it matters.

Attributes worth carrying forward: name, priority, description, owner, due date. New attribute: `TeamId` — for Stage 1 (see `Goals.md`), every Project belongs to exactly one Team (see Open Questions/Decisions and Future Extensions below for how this may broaden later).

The old model's **derived scheduling** is its most distinctive and most debatable feature: a Project's start date is constrained by its dependencies, and its *end date is never stored* — it's computed as the max end date across all child tasks and sub-projects, recursively. This makes the schedule always internally consistent but means nothing about timing is a simple stored fact. **Decision:** the new system keeps this derived-computation approach rather than moving to stored dates (see Open Questions/Decisions below).

<a id="task"></a>
### 2.6 Task
See `KeyConcepts.md`'s Task entry for what this is and why it matters.

Attributes worth carrying forward: description (short + detailed), priority, status, task type (enhancement/maintenance/support/etc.), owner, requestor (a Person), effort (amount + Effort-vs-Duration type — see `KeyConcepts.md`), percentage allocation, a tentative-assignment flag, an external reference URL, and relates to a Component.

A Task's relationship to a Component is optional, not mandatory: in the old schema the field (`AffectedComponentId`) is nullable, and application code treats an unset Component as a valid state rather than an error. It's a many-Tasks-to-one-Component relationship — a Task relates to at most one Component, a Component can have many Tasks relating to it — kept separate from the Project relationship (see `KeyConcepts.md`'s Component entry for why the two axes are independent).

The old model stores a Task's start as a **relative business-day offset from its Project's start date**, not an absolute date, and derives its actual start/end/duration from effort, assigned-resource count, and dependency constraints at read time. Same decision as Project above: the new system keeps this derived-computation approach.

A Task has exactly one Project. The old schema's earlier many-to-many Task↔Project link table was simplified away in favour of this direct relationship, which is a useful precedent: prefer the simpler relationship unless a real need for many-to-many resurfaces.

<a id="component"></a>
### 2.7 Component
See `KeyConcepts.md`'s Component entry for what this is and why it matters. Structurally: same self-referencing tree shape as Project, but independent of it — plays no role in scheduling or dependencies.

<a id="resource-assignment"></a>
### 2.8 Resource assignment (Task ↔ Person)
See `KeyConcepts.md`'s Resource entry for why this matters. Structurally: a many-to-many relationship between Task and Person, scoped to actual Person resources. This is the Stage 1 scope specifically — see Future Extensions below for broadening "resource" beyond Person.

<a id="dependency"></a>
### 2.9 Dependency (Task/Project ordering)
See `KeyConcepts.md`'s Dependency entry for what this is and why it matters — including that either side of the relationship can be a Task or a Project. Structurally: the old model detects cycles before allowing a new link, and supports only finish-to-start ordering with no lag/lead time — worth deciding whether the new system needs richer dependency types.

<a id="attachment"></a>
### 2.10 Attachment
See `KeyConcepts.md`'s Attachment entry for what this is and why it matters. Structurally: exactly one owner among Task/Project/Component (mutually exclusive). Content is one of three kinds — an uploaded file, a captured email, or a hyperlink (a URL, stored with no binary payload of its own) — with a deduplication check on the first two (don't re-attach the identical file/email twice). Ingestion should happen through the API — e.g. direct upload from the client, pasting/entering a URL for the hyperlink kind, and, if capturing emails remains a requirement, inbound email processing via the API (see `Goals.md`) — rather than through a desktop integration.

<a id="remark"></a>
### 2.11 Remark
See `KeyConcepts.md`'s Remark entry for why this matters. Structurally: same "exactly one owner" shape as Attachment; each Remark is immutable once created, forming an append-only comment thread with a preserved author and timestamp per entry — correcting the old model's quirk of reassigning authorship to whoever last edited a remark.

<a id="entity-relationships"></a>
## 3. Entity Relationships

This section pulls the relationships scattered through the Core Entities above into one place, so the shape of the model can be seen at a glance.

- **Organisation → Team**: one Organisation has many Teams. Since the database itself is the Organisation boundary (see Tenancy Scope Note above), this containment is implicit rather than a foreign key.
- **Organisation → Person, via `IsOrganisationAdmin`**: not a relationship table — a flag directly on Person, orthogonal to Team membership. Any number of People in an Organisation can hold it.
- **Team ↔ Person, via PersonRole**: many-to-many. A Person holds one PersonRole per Team they belong to, and each PersonRole carries its own `IsResource` flag and `UserType`/role — so the same Person can be a resource on one Team and not another, or hold a different role on each.
- **Team → Project**: one-to-many for Stage 1 — each Project belongs to exactly one Team (see Open Questions/Decisions and Future Extensions below).
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
- **Concurrency model** — see `KeyConcepts.md`'s Merge/Conflict entry for the old app's approach. **Decision:** Stage 1 assumes a single user at a time, so no conflict-handling is built. Later stages need real-time multi-user editing, where users see each other's changes live — see Future Extensions below.
- **Cascade delete** — the old app removes related resource links, dependencies, attachments, and remarks in application code when a Task is deleted. Database-enforced cascades or soft-deletes are worth using instead now that this isn't constrained by the old ORM's shape.

<a id="open-questions"></a>
## 5. Open Questions / Decisions Needed

1. **Team scoping** — **Decision (Stage 1):** every Project belongs to exactly one Team (`TeamId` on Project — see Project entry above). A Person's Team membership was already settled via PersonRole (a Person can belong to multiple Teams, with an independently-set role per Team). The one-Team-per-Project rule may be relaxed in later stages — see Future Extensions.
2. **Derived vs. stored scheduling** — **Decision:** keep the old model's fully-computed schedule (dates derived from effort + dependencies + resourcing, nothing stored as an absolute date except where unavoidable) rather than moving to a simpler stored-date model (see Project and Task entries above). This is a Foundational Decision (see `KeyConcepts.md`) and holds from Stage 1 onward — it isn't stage-gated the way the items below are.
3. **Concurrency model** — **Decision:** staged — see Cross-Cutting Concerns above for the Stage 1 answer, and Future Extensions for what later stages need.
4. **Role/permission model shape** — **Decision:** an organisation-level administrator role, modeled as `IsOrganisationAdmin` directly on Person (see Person entry above), independent of Team/PersonRole — any number of People in an Organisation may hold it, since organisation administration isn't scoped to any one Team.
5. **Non-Person resources** — **Decision:** yes, there's a real need — in this domain a Resource is something *responsible* for getting work done, so an external vendor qualifies as a Resource but a piece of equipment does not (equipment isn't responsible for anything). Deferred out of Stage 1 scope — see Future Extensions.

<a id="future-extensions"></a>
## 6. Future Extensions (Beyond Stage 1)

Per `Goals.md`'s Delivery Stages, Stage 1 is deliberately narrow in scope. The items below are changes to the domain model above that are anticipated or plausible once later stages are in scope — not designed in detail yet, but flagged so the Stage 1 model doesn't quietly foreclose them.

- **Project may relate to more than one Team.** Stage 1 fixes one Team per Project (see Team → Project in Entity Relationships above); cross-team or Team-spanning Projects, or Projects that move between Teams, may be needed once multi-team collaboration is a real scenario.
- **Real-time multi-user concurrency.** Stage 1 assumes a single user at a time. Later stages need users editing the same Organisation's data at the same time to see each other's changes live — a materially bigger architectural commitment (live push/sync between server and clients) than the old app's load-time conflict-detection dialog (see `KeyConcepts.md`'s Merge/Conflict entry for that older, lower-bar model, which may still be worth keeping as a fallback for cases live sync doesn't cover).
- **Non-Person resources.** Extend Resource assignment (see Core Entities above) beyond Person to include things that are *responsible* for getting work done but aren't a Person in the system — e.g. an external vendor — while explicitly excluding things that aren't responsible for anything, like equipment. Deferred entirely out of Stage 1; the Resource assignment relationship stays Person-only until this is designed.
