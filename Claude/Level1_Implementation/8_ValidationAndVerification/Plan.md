# ProjectPal V2 — Validation and Verification Phase

## Contents

1. [Status and Purpose](#status-and-purpose)
2. [Scope](#scope)
   - 2.1 [In Scope for Level 1](#in-scope)
   - 2.2 [Out of Scope](#out-of-scope)
3. [Use Cases In Scope for Level 1](#use-cases-in-scope)
4. [Material Differences: V1.2 → V2 Database Schema](#schema-differences)
   - 4.1 [Task Scheduling Model — Not Actually a Difference](#task-scheduling)
   - 4.2 [Role / Permission Model](#role-model)
   - 4.3 [Ownership Fields: String → Foreign Key](#ownership-fields)
   - 4.4 [Attachment Model](#attachment-model)
   - 4.5 [Remark Authorship and Audit Trail](#remark-audit)
   - 4.6 [Dependency Table](#dependency-table)
   - 4.7 [Resource Assignment](#resource-assignment)
   - 4.8 [Cascade Delete Behaviour — Needs Verification](#cascade-delete)
   - 4.9 [Concurrency / Optimistic Locking](#concurrency)
   - 4.10 [Structural Additions (Team, Organisation) — Not Differences](#structural-additions)
5. [Review Process](#review-process)
6. [Definition of Success](#definition-of-success)
7. [Open Questions (Phase-Specific)](#open-questions)
8. [Decisions (Phase-Specific)](#decisions)

<a id="status-and-purpose"></a>
## 1. Status and Purpose

**Status:** Not started.

Once the other Level 1 phases are functionally complete, this phase is a comprehensive, joint (user + Claude) review of what's actually been built against what was intended — the features and functionality, not just "does it build and pass its own tests." It exists because every other phase has been built and verified somewhat in isolation (each phase's own manual/automated testing, e.g. `2_RestApi/Plan.md` §6.5, `4_GuiClient/Plan.md` §7.2); this phase is the pass that looks at the whole system together, and specifically at where `V2` has knowingly or unknowingly diverged from `V1.2`'s proven-out behaviour.

<a id="scope"></a>
## 2. Scope

<a id="in-scope"></a>
### 2.1 In Scope for Level 1

- A joint walkthrough of every in-scope Use Case (§3) against the running system.
- A review of every material database-schema difference between `V1.2` and `V2` catalogued in §4 — confirming each one is a deliberate, understood choice, not an oversight.
- Surfacing anything found during that review as a new, numbered open question/decision in this document, or as a defect to fix in the phase that owns it.

<a id="out-of-scope"></a>
### 2.2 Out of Scope

- Fixing anything found — this phase is the review; fixes land in whichever phase owns the affected area (most likely `2_RestApi`, `3_Authentication`, or `4_GuiClient`), tracked from there.
- A line-by-line audit of every V1.2 SQL migration ever written (`V1.2/Apps/ProjectPal/SQL/V1/*`) — §4 is grounded in the current, consolidated V1.2 schema (`V1.2/Apps/ProjectPal/SQL/V2/CreateDB.sql`) compared against V2's actual schema (`V2/database/migrations/001_initial_schema.sql`), not the incremental history of how V1.2 got there.
- New features not already planned elsewhere — this phase validates what Level 1 already committed to, it doesn't expand scope.

<a id="use-cases-in-scope"></a>
## 3. Use Cases In Scope for Level 1

From `Requirements/UseCases.md`, cross-referenced against `D1-4` (`../ImplementationPlan.md`):

| # | Use Case | Level 1? | Notes |
|---|---|---|---|
| 1 | Manage Projects and Tasks (core CRUD + browsing) | Yes | Core workflow; `D1-4` |
| 2 | Assign Resources to a Task | Yes | `D1-4`; checklist model kept, `D1.4-4` |
| 3 | View the Plan (Gantt / Resource-Loading View) | Yes | `D1-4` — a key selling point, not a deferral candidate |
| 4 | Set Dependencies Between Work Items | Yes | `D1-4`; explicit "Add Dependency" dialog, `D1.4-4` |
| 5 | Attach Files and Captured Emails | Partial | `D1-4` — File and Link kinds only; Mail (captured email) is **not** required for Level 1 |
| 6 | Search / Find | Yes | `D1-4` |
| 7 | Add Comments (Remarks) to a Work Item | Yes | `D1-4`; modernised to an inline comment thread, `4_GuiClient/Plan.md` §5 |
| 8 | Resolve Concurrent Edit Conflicts | **No** | Explicitly deferred — `Requirements/DomainModel.md` `D-DM-3`, `Requirements/KeyConcepts.md` §14 |
| 9 | Manage People (Admin) | Yes | Admin-only screen, `4_GuiClient/Plan.md` §5 |
| 10 | Administer the System (Operational/Support Tooling) | Yes | Re-derived from first principles per `D1-4`, not the old `AdminWindow` 1:1 — see `2_RestApi/Plan.md` `Q1.2-4` |

Use Case 11 ("Use Cases Not Carried Forward") and the permission-model sections (§12 of `UseCases.md`, already settled as `D-UC-4`) aren't separate review items here — §12's matrix is exactly what §4.2 below re-examines from the schema side.

<a id="schema-differences"></a>
## 4. Material Differences: V1.2 → V2 Database Schema

Compares `V1.2/Apps/ProjectPal/SQL/V2/CreateDB.sql` (V1.2's current, consolidated schema) against `V2/database/migrations/001_initial_schema.sql`. Focused on **changes** to concepts that existed in both versions — not V2 additions that are new features with no V1.2 equivalent (Team and Organisation, `D-DM-1`, are the main example of the latter; noted at §4.10 for completeness but not treated as a "difference" to re-litigate).

<a id="task-scheduling"></a>
### 4.1 Task Scheduling Model — Not Actually a Difference

**Correction to a claim made earlier in this engagement.** While building `4_GuiClient` Stage 2, it was stated that "Tasks have no absolute dates anymore, just a relative offset from the Project" — implying this was a `V2` design change. Checking `V1.2/Apps/ProjectPal/SQL/V2/CreateDB.sql`'s `Task` table directly shows this is **wrong**: V1.2's `Task` table already has `StartRelativeDaysToProject` and, like V2, has **no** `StartDate`/`EndDate`/`RequestedStartDate` columns at all. The date pickers visible in `V1.2/Apps/ProjectPal/ProjectPal/Tasks/TaskDetail.Designer.cs` (`dateTimePickerEndDate`, `dateTimePickerStartDate`, `dateTimePickerRequestedStartDate`) must be computed/derived display values, not stored columns — the same relative-offset model V2 already uses (`4_GuiClient/Plan.md` §2.1's schedule-derivation work, Stage 3).

This is carried forward unchanged, not a difference — kept here, rather than deleted outright, specifically because it's exactly the kind of claim this phase exists to catch: confirm during the review (§5) that V2's Gantt schedule-derivation (Stage 3) actually reproduces the same offset-to-date computation V1.2's UI displayed, since "the underlying model is the same" doesn't guarantee "the derivation logic that reads it is."

<a id="role-model"></a>
### 4.2 Role / Permission Model

**Changed, and already decided** (`Requirements/DomainModel.md` line 69, `D-UC-4` in `UseCases.md`) — included here for completeness since it's exactly the shape of change this phase reviews:
- V1.2: a single global `UserType` string on `Person` (`SuperUser` / `PowerUser` / `NormalUser` / `ReadOnlyUser`) — one role for the whole application.
- V2: `person_role(person_id, team_id, role, is_resource)` — a role **per Team**, renamed `TeamLeadUser` / `LeadUser` / `NormalUser` / `ReadOnlyUser`, plus a separate `is_organisation_admin` flag on `Person` for what V1.2's global `SuperUser` meant.
- V1.2's `IsResource` was a global flag on `Person`; V2 moved it into `person_role`, so resource-eligibility is now per-Team too (`4_GuiClient/Plan.md` §6.2's Resources-checklist limitation is a direct consequence of this — the GUI doesn't yet pre-filter by it).

<a id="ownership-fields"></a>
### 4.3 Ownership Fields: String → Foreign Key

V1.2's `Project.Owner`, `Task.Owner`, `Component.Owner`, `Remark.Owner`, and every table's `ModifiedBy` are `nvarchar(50)` — a loose string (a login name), not a real reference, with nothing in the database stopping it from referencing a Person who doesn't exist (or existed once and was renamed). V2 makes every one of these a real foreign key (`owner_person_id`, `modified_by` → `person(person_id)`) — referential integrity that V1.2 never had at the database level. Worth confirming during the review that nothing in the migrated/seeded data relied on that old string being freeform (e.g. a value that was never a real login).

<a id="attachment-model"></a>
### 4.4 Attachment Model

- V1.2: `DataType` (free-text) and `From` (nvarchar(20), presumably the sender for a captured email) with no formal kind enum and no dedup mechanism.
- V2: a real `attachment_kind` enum (`File`/`Mail`/`Link`, though only `File`/`Link` are in scope for Level 1, §3), a dedicated `url` column for links, `mail_from` (the renamed equivalent of V1.2's `From`), and a new `content_hash` column plus a unique index enforcing "don't attach the identical file/email twice" — a rule V1.2 did not enforce at the database level at all.
- `Owner` (string) → `owner_person_id` (FK) — same pattern as §4.3.

<a id="remark-audit"></a>
### 4.5 Remark Authorship and Audit Trail

- V1.2: `Remark.Owner` (string, `NOT NULL`) plus `ModifiedBy`/`ModifiedTime` — meaning V1.2 tracked both who created a Remark *and* who last edited it and when, since presumably any of those columns could be updated on edit.
- V2: `created_by_person_id` (FK, `NOT NULL`) and `created_time` only — **no** modified-by/modified-time equivalent for Remarks, and a database trigger (`prevent_remark_reassignment`) makes `created_by_person_id` immutable after insert, which V1.2 never enforced (its `Owner` string could presumably be changed like any other column).
- **Net effect worth confirming is intentional:** V2 gained authorship-immutability but lost edit-history tracking (who last changed a Remark's text, and when) that V1.2 had. If a Remark's text is edited after creation, V2 currently has no record of that having happened.

<a id="dependency-table"></a>
### 4.6 Dependency Table

- V1.2: `TimeDependency` table, with `ModifiedBy`/`ModifiedTime` columns.
- V2: renamed to `dependency`, and **drops** `modified_by`/`modified_time` entirely — consistent with `2_RestApi`'s `dependencies.py` docstring ("create/delete only," no `PATCH` endpoint), but this also means V2 has no record of who created a given Dependency or when, which V1.2 did capture (via `ModifiedTime` being set on insert).
- V2 adds `check_dependency_no_cycle`, a database trigger rejecting a Dependency that would create a cycle. Whether V1.2 enforced this at all, and if so where (database vs. application code), hasn't been confirmed — worth checking against `V1.2/Apps/ProjectPal/ProjectPal/Tasks/TaskDetail.cs`'s dependency-handling code during the review if it matters.

<a id="resource-assignment"></a>
### 4.7 Resource Assignment

- V1.2: `Task2Resource` has a surrogate `Task2ResourceId`, `PersonId`, **and** `OtherResourceId`, plus `ModifiedBy`/`ModifiedTime`. `OtherResourceId` is very likely the sentinel/placeholder-resource mechanism `Requirements/DomainModel.md` (line 58) already flags — representing a non-Person or not-yet-known resource; V1.2's own seed data includes a literal `'Unassigned'` Person row (`CreateDB.sql`), consistent with that pattern, though the exact relationship between `OtherResourceId` and that row hasn't been confirmed from the application code.
- V2: `task_resource(task_id, person_id)` — a plain composite-key join table. No `OtherResourceId` equivalent, and no per-assignment audit trail (who assigned this Resource, and when).
- `Requirements/DomainModel.md` already flags the sentinel-resource question as needing a decision (a nullable assignment plus a status, rather than a magic sentinel Person) — this phase's review should confirm Level 1 doesn't actually need that capability, rather than silently having dropped it.

<a id="cascade-delete"></a>
### 4.8 Cascade Delete Behaviour — Needs Verification

**Flagged as a likely gap, not just a documented difference.** `Requirements/DomainModel.md` (line 141) already notes that V1.2 deletes related resource links, dependencies, attachments, and remarks *in application code* when a Task is deleted, and that V2 should use database-enforced cascades or soft-deletes instead. Checking `V2/database/migrations/001_initial_schema.sql`, none of the foreign keys referencing `task`/`project`/`component` (from `task_resource`, `dependency`, `attachment`, `remark`) specify `ON DELETE CASCADE`, and `rest-api/app/routes/tasks.py`'s `delete_task` does a plain `DELETE FROM task` with no manual cleanup of dependent rows either. **This needs to be actually exercised during the review** (delete a Task that has an assigned Resource, a Dependency, an Attachment, and a Remark) to confirm whether it fails with an unhandled foreign-key-violation error, rather than assumed to work from reading the code alone.

<a id="concurrency"></a>
### 4.9 Concurrency / Optimistic Locking

V1.2's near-universal `ModifiedTime` columns, together with `MainWindow`'s 10-second refresh timer (`Requirements/UserInterfaceWindows.md` §4) and the merge dialogs (`UseCases.md` #8), strongly suggest V1.2 used `ModifiedTime` for optimistic-concurrency detection (has this record changed since I loaded it?). V2 keeps `modified_time` columns (auto-maintained by trigger, §4.5) but has no conflict-detection mechanism reading them — a deliberate Level 1 decision (`D-DM-3`, `KeyConcepts.md` §14), not an oversight. Included here as context for the review, not as something to fix.

<a id="structural-additions"></a>
### 4.10 Structural Additions (Team, Organisation) — Not Differences

`Team` (and `person_role`'s per-Team scoping generally) and `Organisation` have no V1.2 equivalent at all — V1.2 has a single implicit "team" (everyone in the database). These are new-for-V2 concepts already decided and documented (`Requirements/DomainModel.md`'s Team/Organisation entries, `D-DM-1`), not something this phase re-examines as a "difference" — noted here only so the schema comparison in this section reads as complete, per the user's request to focus on changes rather than new-feature additions.

<a id="review-process"></a>
## 5. Review Process

Not yet designed in detail — to be worked out once the other Level 1 phases are far enough along for this phase to actually start. Expected shape: a joint (user + Claude) session-by-session walkthrough, one Use Case (§3) at a time, exercising it in the running system and checking its result against both the Use Case's stated intent and the relevant items in §4; findings become new `Q1.8-<N>` entries below, resolved into `D1.8-<N>` decisions as they're settled.

<a id="definition-of-success"></a>
## 6. Definition of Success

- Every in-scope Use Case (§3) has been exercised in the running system and confirmed to behave as intended.
- Every item in §4 has an explicit resolution recorded (confirmed-intentional, or a defect logged against the owning phase) — none left as an unexamined assumption.
- `§4.8`'s cascade-delete behaviour has been actually tested, not just reasoned about from the code.

<a id="open-questions"></a>
## 7. Open Questions (Phase-Specific)

- **Q1.8-1: Does deleting a Task/Project/Component with dependent rows (resources, dependencies, attachments, remarks) actually work?** See §4.8 — needs to be exercised directly against the running API, not just inferred from reading `tasks.py`/`projects.py`/`components.py` and the migration file.
- **Q1.8-2: Is V1.2's `OtherResourceId` sentinel-resource capability (§4.7) actually needed for Level 1?** `Requirements/DomainModel.md` already flags this as needing a decision; this phase is a natural point to actually settle it against real usage rather than leave it open indefinitely.
- **Q1.8-3: Does losing Remark/Dependency edit-audit trail (§4.5, §4.6) matter for Level 1?** V1.2 tracked who last modified a Remark and when, and who created a Dependency and when; V2 currently tracks neither. Worth a deliberate yes/no rather than an unexamined gap.
- **Q1.8-4: Does V1.2's `Priority` scale (`VHigh`/`High`/`Med`/`Low`/`VLow`) actually correspond 1:1, in that order, to V2's `priority_level` enum's five active values (`High`/`MedHigh`/`Med`/`MedLow`/`Low`)?** `4_GuiClient/Plan.md` `D1.4-15` ordered V2's Priority dropdown most-urgent-first on that assumption (same count, same relative shape) to fix its option order, but the exact historical correspondence — not just the ordering — hasn't been confirmed against `V1.2/Apps/ProjectPal/ProjectPal/Tasks/GUITaskColumns.cs`'s actual `PriorityValue` enum mapping or any migration note explaining the rename.

<a id="decisions"></a>
## 8. Decisions (Phase-Specific)

None yet — when an open question above is answered, its entry moves here as `D1.8-<N>` (same number, `D` prefix), recording the original question, the decision, and the date.

See `../ImplementationPlan.md` for how this phase fits into the Level 1 plan, and for open questions that span this phase and others.
