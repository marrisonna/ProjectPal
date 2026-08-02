# ProjectPal — Domain Model

## Contents

1. [Tenancy Scope Note](#tenancy-scope-note)
2. [Core Entities](#core-entities)
   - [Organisation](#organisation)
   - [Team](#team)
   - [Person](#person)
   - [Project](#project)
   - [Task](#task)
   - [Component](#component)
   - [Resource assignment (Task ↔ Person)](#resource-assignment)
   - [Dependency (Task/Project ordering)](#dependency)
   - [Attachment](#attachment)
   - [Remark](#remark)
3. [Cross-Cutting Concerns](#cross-cutting-concerns)
4. [Open Questions / Decisions Needed](#open-questions)

This describes the domain model for the **new** implementation (`V2`). It's grounded in the entities and relationships that exist in the old prototype (`V1.2`), since that's proven-out real usage, but it is a design for the new system — not a description of the old one. Where the old model has quirks, legacy artefacts, or decisions that don't fit the new multi-tenant/API-first direction (see `Goals.md`), that's called out explicitly as **a decision needed**, not silently carried forward.

<a id="tenancy-scope-note"></a>
## 1. Tenancy Scope Note

Per `Goals.md`, a tenant is an **Organisation**, and the model is database-per-tenant. Everything below (Project, Task, Person, etc.) lives *inside* one organisation's database — there is no `OrganisationId` on these tables because the database itself is the organisation boundary. **Team**, however, is a grouping *within* an organisation and does need to be a first-class concept in this model (the old app has no equivalent — it only ever supported one team per install). See Open Questions below.

<a id="core-entities"></a>
## 2. Core Entities

<a id="organisation"></a>
### Organisation
See `KeyConcepts.md`'s Organisation entry for what this is and why it matters, and `Goals.md` §Multi-tenancy for the tenancy decision. Not present as a concept in the old app at all — the old app *is* a single organisation's worth of data. Organisation's own shape (name, subscription/plan info, admin contacts) belongs in the control-plane/metadata database, not in the tenant database itself.

<a id="team"></a>
### Team
See `KeyConcepts.md`'s Team entry for what this is and why it matters. Not present in the old app — old app has a single implicit "team" (everyone in the database). **Needs design** — see Open Questions.

This needs more defintion, but a team will need a **TeamId** and a name, possibly other attributes to.

<a id="person"></a>
### Person
See `KeyConcepts.md`'s Person and Resource entries for what this dual-purpose modeling is and why it matters.

Key attributes are:
- `PersonId` - a primary key, unique id for a real person in the real world.  Each person in an organisation has a unique PersonId.
- `IsActive` — soft-disable, not a hard delete (deleting a Person is blocked while they still own/request/are-assigned-to anything).
- Name and logon fields/attributes — to be defined in detail. Needs to become a proper external-identity reference (see `Goals.md` Stage 1/2 identity direction).

Sentinel/placeholder resources ("Other", "Unassigned") were useful in the old app for representing non-human or not-yet-known resources — worth keeping the *concept* (a work item can have an unresolved or non-Person resource) but worth deciding whether it's better modeled as a nullable assignment plus a status, rather than magic sentinel rows.

### PersonRole

This describes a person's role within a specific team

Key attributes are:
- `PersonId` - a reference to the **Person**
- `TeamId` - a reference to the **Team**
- `IsResource` — whether this Person can be assigned to work items (independent of whether they can log in).
- `UserType`/role — SuperUser / PowerUser / NormalUser / ReadOnlyUser in the old app; the role model needs revisiting once Team-scoped permissions exist (see `KeyConcepts.md` and Open Questions).

The Gantt bar colour assigned to a Person should be a generic per-Person, per-Organisation setting.

Sentinel/placeholder resources ("Other", "Unassigned") were useful in the old app for representing non-human or not-yet-known resources — worth keeping the *concept* (a work item can have an unresolved or non-Person resource) but worth deciding whether it's better modeled as a nullable assignment plus a status, rather than magic sentinel rows.

<a id="project"></a>
### Project
See `KeyConcepts.md`'s Project entry for what this is and why it matters.

Attributes worth carrying forward: name, priority, description, owner, a private/visibility flag, due date.

The old model's **derived scheduling** is its most distinctive and most debatable feature: a Project's start date is constrained by its dependencies, and its *end date is never stored* — it's computed as the max end date across all child tasks and sub-projects, recursively. This makes the schedule always internally consistent but means nothing about timing is a simple stored fact. **This needs a deliberate decision for the new system** (see Open Questions) rather than automatic carry-over.

<a id="task"></a>
### Task
See `KeyConcepts.md`'s Task entry for what this is and why it matters.

Attributes worth carrying forward: description (short + detailed), priority, status, task type (enhancement/maintenance/support/etc.), owner, requestor (a Person), effort (amount + Effort-vs-Duration type — see `KeyConcepts.md`), percentage allocation, a tentative-assignment flag, a private/visibility flag, an external reference URL.

The old model stores a Task's start as a **relative business-day offset from its Project's start date**, not an absolute date, and derives its actual start/end/duration from effort, assigned-resource count, and dependency constraints at read time. Same note as Project: powerful and consistent, but a real design choice to make deliberately rather than inherit by default.

A Task has exactly one Project. The old schema's earlier many-to-many Task↔Project link table was simplified away in favour of this direct relationship, which is a useful precedent: prefer the simpler relationship unless a real need for many-to-many resurfaces.

<a id="component"></a>
### Component
See `KeyConcepts.md`'s Component entry for what this is and why it matters. Structurally: same self-referencing tree shape as Project, but independent of it — plays no role in scheduling or dependencies.

<a id="resource-assignment"></a>
### Resource assignment (Task ↔ Person)
See `KeyConcepts.md`'s Resource entry for why this matters. Structurally: a many-to-many relationship between Task and Person, scoped to actual Person resources rather than reintroducing the old schema's unused stub for non-Person resource types.

<a id="dependency"></a>
### Dependency (Task/Project ordering)
See `KeyConcepts.md`'s Dependency entry for what this is and why it matters — including that either side of the relationship can be a Task or a Project. Structurally: the old model detects cycles before allowing a new link, and supports only finish-to-start ordering with no lag/lead time — worth deciding whether the new system needs richer dependency types.

<a id="attachment"></a>
### Attachment
See `KeyConcepts.md`'s Attachment entry for what this is and why it matters. Structurally: exactly one owner among Task/Project/Component (mutually exclusive), with a deduplication check (don't re-attach the identical file/email twice). Ingestion should happen through the API — e.g. direct upload from the client, and, if capturing emails remains a requirement, inbound email processing via the API (see `Goals.md`) — rather than through a desktop integration.

<a id="remark"></a>
### Remark
See `KeyConcepts.md`'s Remark entry for why this matters. Structurally: same "exactly one owner" shape as Attachment; each Remark is immutable once created, forming an append-only comment thread with a preserved author and timestamp per entry — correcting the old model's quirk of reassigning authorship to whoever last edited a remark.

<a id="cross-cutting-concerns"></a>
## 3. Cross-Cutting Concerns

- **Encryption at rest** on free-text fields (descriptions, remarks, names) — worth keeping the principle, expressed as a platform-level (database/column) encryption concern rather than application code manually encrypting/decrypting every field.
- **Private/visibility flag** — see `KeyConcepts.md`'s Private (Visibility) entry.
- **Concurrency model** — see `KeyConcepts.md`'s Merge/Conflict entry. **Needs a decision** — see Open Questions.
- **Cascade delete** — the old app removes related resource links, dependencies, attachments, and remarks in application code when a Task is deleted. Database-enforced cascades or soft-deletes are worth using instead now that this isn't constrained by the old ORM's shape.

<a id="open-questions"></a>
## 4. Open Questions / Decisions Needed

1. **Team scoping** — does every Project (and therefore its Tasks) belong to exactly one Team? Can a Person belong to multiple Teams? This has no precedent in the old model and needs first-principles design.
2. **Derived vs. stored scheduling** — keep the old model's fully-computed schedule (dates derived from effort + dependencies + resourcing, nothing stored as an absolute date except where unavoidable), or move to a simpler stored-date model with recalculation as an explicit action? This is a Foundational Decision (see `KeyConcepts.md`) worth resolving with a direction of travel even at Demonstrator stage, since it shapes the whole Task/Project schema.
3. **Concurrency model** — see `KeyConcepts.md`'s Merge/Conflict entry for the options; affects both API design and client UX.
4. **Role/permission model shape** — see `KeyConcepts.md`'s Role / Permission Level entry. Once Organisations and Teams are real, does this become per-organisation roles, per-team roles, or both?
5. **Non-Person resources** — is there a real need to assign work to something other than a Person (equipment, external vendor placeholder)? If yes, design it properly as a first-class concept.
