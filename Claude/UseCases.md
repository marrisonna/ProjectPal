# ProjectPal — Use Cases

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
- [Use Cases Not Carried Forward](#not-carried-forward)
- [Open Questions](#open-questions)

These are the workflows the new system needs to support, grounded in what the old prototype (`V1.2`) actually does today — real, exercised functionality, not a wishlist. Each one notes what should carry forward as-is, what needs to change because of the new architecture (API-first, multi-tenant, cross-platform client), and open questions. See `Goals.md` for which stage (Demonstrator / MVP / later) each is likely to matter for, and `KeyConcepts.md` / `DomainModel.md` for terminology and entities.

<a id="manage-projects-and-tasks"></a>
## 1. Manage Projects and Tasks (core CRUD + browsing)

**Actor:** any user with edit rights (owner, PowerUser, SuperUser, per role).

The bread-and-butter workflow: browse a filterable/sortable list of Projects or Tasks, open one for detail editing (description, priority, status, effort, owner, requestor, component, dependencies, attachments, remarks), create new ones, reparent them in their respective trees (Project under Project, Component under Component).

This is the core of the product and carries forward directly as a use case, delivered through a web/cross-platform client talking to an API rather than a WinForms grid talking directly to SQL Server. The specific rich interactions in the old UI (drag-and-drop reparenting between tree nodes, drag-and-drop dependency lists) are UX choices to redesign for the new client technology (see `Goals.md` Stage 1 "client technology" framing question).

<a id="assign-resources-to-a-task"></a>
## 2. Assign Resources to a Task

**Actor:** anyone with edit rights on the Task.

Assign one or more People (marked as resources) to a Task; effort is then split across the assigned people (see `DomainModel.md` Effort/Duration). In the old app this is a checklist in the Task detail screen. Worth confirming that's still the intended model, rather than drag-and-drop directly onto the Gantt view, before designing the new UI.

<a id="view-the-plan"></a>
## 3. View the Plan (Gantt / Resource-Loading View)

**Actor:** anyone who can view the relevant Projects/Tasks.

A visual, hierarchical timeline of Projects/Tasks with a resource-workload graph underneath, colour-codable by Person/Priority/Status, supporting drag-to-reschedule (subject to edit permission) and click-through to the underlying Task/Project detail.

This is one of the app's most distinctive and most-valued features (per `Goals.md`'s framing of the GUI as something to actively trial with users), and also one of the most technically demanding to reproduce outside WinForms — the old implementation is a fully custom-drawn WPF control. For the new system this needs to be evaluated as its own significant piece of work (an existing charting/Gantt library vs. a custom build). Worth flagging as a Stage 1 "feature scope" candidate for either inclusion (it's likely core to what's worth trialling) or deliberate, explicit deferral.

<a id="set-dependencies"></a>
## 4. Set Dependencies Between Work Items

**Actor:** anyone with edit rights.

See `KeyConcepts.md`'s Dependency entry and `DomainModel.md`'s Dependency section for what this is and its rules (e.g. cycle prevention). Feeds directly into the plan view's scheduling. Carries forward as a use case; the old drag-between-two-listboxes interaction is a UX detail to redesign.

<a id="attach-files-and-captured-emails"></a>
## 5. Attach Files and Captured Emails

**Actor:** anyone with create rights on Attachments (broadly open in the old model).

See `KeyConcepts.md`'s Attachment entry and `DomainModel.md`'s Attachment section for what this is and its rules (e.g. deduplication).

The use case ("keep supporting evidence attached to a work item") carries forward as file upload from the browser/client, and — if capturing emails is still wanted — a server-side mechanism (e.g. forward-to-an-address / inbound email processing via the API) rather than a desktop integration (see `Goals.md` Non-Goals). **Needs a decision**: is "capture an email as an attachment" still an important use case for the Demonstrator, or can it be deferred entirely to a later stage?

<a id="search-find"></a>
## 6. Search / Find

**Actor:** any user.

Search across Tasks, Projects, Components, Remarks, and (optionally) Attachment contents, scoped to what the user can see, with a results list that opens the matching item. Carries forward as a core use case. Full-text search inside attachment *contents* (as opposed to just metadata) is reasonable to treat as a later enhancement, with metadata/field search as the Stage 1 baseline.

<a id="add-comments"></a>
## 7. Add Comments (Remarks) to a Work Item

**Actor:** anyone with create rights on Remarks (broadly open — even ReadOnly users could add Remarks in the old model).

See `KeyConcepts.md`'s Remark entry for why this matters and `DomainModel.md`'s Remark section for its structure.

<a id="resolve-concurrent-edit-conflicts"></a>
## 8. Resolve Concurrent Edit Conflicts

**Actor:** any user editing a record someone else also changed.

See `KeyConcepts.md`'s Merge/Conflict entry for the concept, and `DomainModel.md`'s Open Questions for the concurrency-model decision. In the old app, this triggers as an automatic dialog whenever a background refresh detects the record changed elsewhere since it was loaded — likely a lower-stakes problem in a web app where data is fetched fresh per view rather than held in a long-lived in-memory client cache.

<a id="manage-people"></a>
## 9. Manage People (Admin)

**Actor:** SuperUser/organisation-admin.

Add a Person, mark them active/inactive, set whether they're a resource, set their role, delete them (blocked while they still own or are assigned to anything). Carries forward directly as a use case, but the *identity* side (how a Person's login maps to an actual authenticated account) needs to move from a raw DB/Windows login string to real external identity integration (see `Goals.md` identity direction, `DomainModel.md`).

At the Organisation/Team level, this use case also needs to grow: inviting people into an Organisation, assigning them to Teams, and (per Stage 3 in `Goals.md`) eventually self-service tenant onboarding — all of which is new design work.

<a id="administer-the-system"></a>
## 10. Administer the System (Operational/Support Tooling)

**Actor:** SuperUser / vendor operator.

The old app's `AdminWindow` bundles several things: impersonate another user for support purposes, switch storage backend, toggle encryption, force a full re-sync, bulk export attachments. Most of these are specific to the old app's particular architecture (a fat client with a local in-memory cache and a filesystem-backed alternative storage mode) and won't map 1:1 onto a server-hosted, API-first system. The underlying needs — a support/impersonation path, a way to verify data integrity, a way to bulk-export a customer's data — are legitimate and should be re-derived from first principles for the new architecture, likely as Stage 2/3 vendor tooling (per `Goals.md`'s "cross-tenant tooling" cost note).

<a id="not-carried-forward"></a>
## Use Cases Not Carried Forward

- Switching between a SQL Server backend and a local filesystem-based pseudo-database — an old-app implementation detail with no equivalent need in the new architecture.
- The one-off batch mail-import tool (`AttachmentLoader`, hardcoded to a local folder) — a migration utility, not a repeatable end-user workflow.
- Any Word-document generation/automation via COM — per `Goals.md` Non-Goals, revisit later via a non-COM mechanism if still needed.

<a id="open-questions"></a>
## Open Questions

1. Which of these use cases are actually essential to a meaningful Demonstrator trial (per `Goals.md` Stage 1's "feature scope" framing question)? Candidates for the essential set: Manage Projects/Tasks, Assign resources, Set dependencies, Search, Remarks. Candidates for deliberate deferral or cutting: the Gantt/plan view (valuable but heavy to build), email-capture attachments, admin/support tooling, concurrent-edit resolution (with only one org's worth of users during the Demonstrator, conflicts may be rare enough to defer handling gracefully).
2. Does the Demonstrator need multi-user concurrent editing at all, or can it reasonably assume low contention and defer conflict-handling design to MVP?
3. For attachments, is "capture an email" a Stage 1 requirement, or can Stage 1 ship with plain file upload only?
