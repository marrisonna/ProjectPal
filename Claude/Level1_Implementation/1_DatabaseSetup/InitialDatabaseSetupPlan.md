# ProjectPal V2 — Level 1 Local Database: Plan

## Contents

1. [What This Covers](#what-this-covers)
2. [What to Download and Install](#downloads-and-installs)
3. [What Needs to Be Configured](#configuration)
4. [What Code Needs to Be Written, and by Whom](#code-and-ownership)
5. [Schema Design](#schema-design)
6. [Example Data](#example-data)
7. [Setup, Install, and Test](#setup-install-test)
8. [Out of Scope for This Pass](#out-of-scope)
9. [Implementation Outcome Summary](#implementation-outcome-summary)

<a id="what-this-covers"></a>
## 1. What This Covers

This is the plan for standing up the **database** half of a Level 1 Demonstrator (`Goals.md` §4.1) on this PC, following the locally-hosted architecture recommended in `Claude/Level1_Implementation/1_DatabaseSetup/DataBaseHostingOptions.md`: **PostgreSQL, run in Docker, with nothing else talking to it directly** — no REST API, GUI, or authentication layer is built in this pass (see §8). The resulting code and schema now live under [`V2/`](../../../V2) — this document stays here in `Claude/Level1_Implementation/1_DatabaseSetup/` as the historical planning record; it isn't needed by the application itself.

<a id="downloads-and-installs"></a>
## 2. What to Download and Install

| What | Where from | Why |
|---|---|---|
| **Docker Desktop for Windows** | <https://www.docker.com/products/docker-desktop/> | Runs PostgreSQL in a container rather than installing it directly into Windows — matches `DataBaseHostingOptions.md`'s recommended local architecture, and means uninstalling later is just deleting a container/volume. Requires the WSL2 backend (Docker Desktop's installer offers to set this up; this PC's Windows 10 22H2 build supports it). A reboot is typically needed after first install. |
| *(Optional)* **DBeaver Community** | <https://dbeaver.io/download/> | A free, generic database GUI for browsing/querying the database directly during development (per `DataBaseHostingOptions.md`'s "Interpretation B" — fine for your own admin/inspection use, but the eventual application GUI must go through an API, never connect to Postgres directly). |
| *(Optional)* **pgAdmin 4** | <https://www.pgadmin.org/download/pgadmin-4-windows/> | Alternative to DBeaver, Postgres-specific. Pick one, not both. |

Nothing else needs installing for the database itself — PostgreSQL runs from the official `postgres:16` Docker image, pulled automatically the first time you start the container (see §7). Git and this repo are already in place.

This machine already has SQL Server and `sqlcmd` installed (used to inspect the old `ProjectPalDB_1` database for §6) — that's unrelated to this plan and needs no action.

<a id="configuration"></a>
## 3. What Needs to Be Configured

1. **Docker Desktop**: use the WSL2 backend (default on a fresh install on this Windows version). No other settings need changing for a database this small.
2. **A local password**: copy `.env.example` to `.env` and set a real `POSTGRES_PASSWORD`. `.env` is git-ignored — never commit real credentials, even local-only ones.
3. **Network exposure**: already configured in `docker-compose.yml` — PostgreSQL's port 5432 is bound to `127.0.0.1` only, so nothing else on the LAN can reach it. Don't change this binding without a reason; see `DataBaseHostingOptions.md`'s "I'd keep PostgreSQL completely inaccessible from the network" principle.
4. **If you install DBeaver/pgAdmin**: point it at `localhost:5432`, database/user/password from your `.env`.

<a id="code-and-ownership"></a>
## 4. What Code Needs to Be Written, and by Whom

| Artifact | Who | Status |
|---|---|---|
| Schema migration (`database/migrations/001_initial_schema.sql`) | Claude, this session, from `DomainModel.md` | Done |
| Example/seed data (`database/seed/001_example_data.sql`) | Claude, this session | Done |
| Team 2 seed data (`database/seed/002_team2_from_v1.sql`), migrated from real `ProjectPalDB_1` content | Claude, a later session, per §6's correction | Done |
| `docker-compose.yml`, `.env.example` | Claude, this session | Done |
| Setup/verify/reset scripts (`scripts/*.ps1`) | Claude, this session | Done |
| **Future schema changes** — new migration files as `DomainModel.md` evolves | You + Claude Code together, same pattern as this one | Ongoing, as needed |
| REST API (e.g. PostgREST config, or a hand-written service) | Not started | Next phase — see §8 |
| GUI / web client | Not started | Next phase — see §8 |
| Authentication | Not started | Next phase — see §8 |

Nothing here requires you to hand-write SQL yourself unless you want to — the pattern going forward is the same one used today: describe the change to `DomainModel.md`, then have Claude Code turn it into a new numbered migration file.

<a id="schema-design"></a>
## 5. Schema Design

Full DDL is in [`database/migrations/001_initial_schema.sql`](../../../V2/database/migrations/001_initial_schema.sql) — this section is a summary, not a restatement (per `Claude/Guidelines/document-guidelines.md` rule 2, `DomainModel.md` remains the canonical description of *why* each entity/relationship exists).

- One PostgreSQL schema, `projectpal`, containing every Level 1 entity from `DomainModel.md` §2: `team`, `person`, `person_role`, `component`, `project`, `task`, `task_resource`, `dependency`, `attachment`, `remark`. No `organisation` table — per `DomainModel.md`'s Tenancy Scope Note, this whole database *is* the one Organisation for Level 1, so there's nothing to store.
- Enumerated types for Priority, Task Status, Task Type, Effort Type, Team Role, and Attachment Kind, matching the value sets confirmed against both `DomainModel.md`/`KeyConcepts.md` and the old `ProjectPalDB_1` data (§6).
- Three business rules from `DomainModel.md` are enforced at the database layer, not left to be re-implemented later in application code:
  - **Dependency cycle prevention** (`DomainModel.md`'s Dependency entity) — a trigger walks the existing dependency graph before allowing a new edge, and rejects it if it would close a loop.
  - **A Remark's authorship can never be reassigned** (`DomainModel.md`'s Remark entity, `D-DM-7`) — a trigger rejects any attempt to change `created_by_person_id`. Editing the remark text, or deleting it, is otherwise allowed at the database layer; restricting that to the Remark's own owner (or a TeamLeadUser, for delete) is the REST API's job, not the database's.
  - **Attachment deduplication** (`DomainModel.md`'s Attachment entity) — a `content_hash` column plus a unique index reject re-attaching identical file/email content to the same owner. Computing the hash (SHA-256 of the uploaded bytes) is an API-layer responsibility once that exists; the constraint is ready for it.
- A few structural decisions made while turning the domain model into an actual schema (not mandated by `DomainModel.md`, but consistent with it — worth reviewing):
  - `Component.Owner` and `Task`/`Project`'s various "Owner"/"Requestor" fields are proper foreign keys to `person`, not the old schema's free-text username strings — a natural consequence of `Person` now being a first-class table.
  - Column names use `snake_case`, idiomatic for PostgreSQL, rather than the old schema's `PascalCase`.
  - `Task.EndDate` is not a column at all, matching `DomainModel.md`'s decision to keep derived scheduling — only `start_relative_days_to_project` is stored (a business-day offset from the owning Project's `start_date`), and actual start/end dates are computed at read time by application logic that doesn't exist yet (§8).
  - Urgency (`KeyConcepts.md` §12) is **not** a column or a view here — it's explicitly a presentation-layer calculation over other stored data, recomputed on demand, so there's nothing to store.
  - `Component` carries a `team_id` (`DomainModel.md` `D-DM-6`, added while designing the REST API phase's authorization model), mirroring `Project`. This governs who may create/edit/delete the Component, not which Team's Tasks may reference it — Component stays usable across Teams as a classification tag.

<a id="example-data"></a>
## 6. Example Data

[`database/seed/001_example_data.sql`](../../../V2/database/seed/001_example_data.sql) loads a small, entirely fictional dataset: 1 Team ("Platform"), 7 People, a 4-Component tree, 4 Projects (one with two sub-projects), 11 Tasks covering every Priority/Status/Task Type/Effort Type combination worth exercising, a few resource assignments, four Dependencies (including a Project-to-Project one), three Attachments (one of each kind — File/Mail/Link), and four Remarks. (Originally 2 Teams — collapsed into one when `002_team2_from_v1.sql`, below, needed a free `team_id`.)

No real names, descriptions, or content were copied from anywhere in 001. Its *shape* — status/priority mix, how resourcing and dependencies tend to be used — was informed by looking at the old `V1.2` SQL Server database (`ProjectPalDB_1`, on this machine): its table/column layout (confirmed via `INFORMATION_SCHEMA.COLUMNS`) and its distinct enum-like values (e.g. `Priority`, `Status`, `TaskType`, `UserType`) directly informed the enum types in the schema.

**Correction to a claim made earlier in this document.** The paragraph above used to say actual V1.2 row content (real people's names, real task descriptions) was deliberately *not* copied, "since that's real historical work data, not example data." That was a considered decision at the time, but it's since been revisited and reversed: [`database/seed/002_team2_from_v1.sql`](../../../V2/database/seed/002_team2_from_v1.sql) imports the real content of that same `ProjectPalDB_1` database — all 43 People, 182 Projects, 201 Components, 1,542 Tasks, 1,630 resource assignments, 87 Remarks, and 1,813 Attachments (a handful of rows were dropped where the source data itself was malformed — e.g. a few Remarks/Attachments with no owning Task/Project/Component at all) — as **Team 2**, deliberately, to get realistic-volume test data for exercising the multi-Team model at scale. This was a real decision, made with the user, not a silent reversal:

- Real first names are kept (low risk on their own); real Windows domain logins (`DBLogin`) are replaced with anonymised `firstname@example.com` logins, and only for the 10 People who had a `DBLogin` in the first place (everyone else gets no login capability, matching their original NULL).
- Attachment file content was never carried over (V1.2's own `Data` column was already NULL for 1,812 of 1,818 rows, and the remaining 6 were trivial/corrupted-looking test artifacts, not real files worth preserving) — every imported Attachment gets a small placeholder naming the original file instead, satisfying V2's "non-Link attachments need data" CHECK constraint without any real content.
- `002_team2_from_v1.sql` is a **static, one-off transform's output**, not a live script — it was generated once by a throwaway Python tool reading a JSON export of `ProjectPalDB_1`, and carries no dependency on SQL Server at rebuild time. If the schema changes, it gets hand-edited the same way `001_example_data.sql` does (`Claude/Guidelines/ImplementationApproach.md` §5's edit-and-rebuild convention) — there's no regeneration pipeline to re-run.
- Enum values turned out to map almost for free: V1.2's `Status`/`TaskType` values are name-for-name identical to V2's enums, and `Priority` needed only its numeric prefix stripped (`"4_MedHigh"` → `"MedHigh"`) — evidence that `4_GuiClient/Plan.md`'s `D1.4-15`/`Q1.8-4` had this partly wrong (it assumed V1.2's Priority names didn't correspond to V2's, based on a UI display-string constant rather than the actual underlying enum).

<a id="setup-install-test"></a>
## 7. Setup, Install, and Test

See [`README.md`](../../../V2/README.md) for the step-by-step instructions (first-time setup, everyday use, verifying the install, connecting a GUI tool, resetting, and adding a future migration). In short: install Docker Desktop (§2), copy `.env.example` to `.env` (§3), then run `.\scripts\setup.ps1` and `.\scripts\verify.ps1` from the `V2` folder.

I wasn't able to run this myself in this session — this machine has neither Docker nor a PostgreSQL client installed (confirmed by trying), so the SQL and scripts here have been carefully reviewed but not executed end-to-end. Please run `.\scripts\setup.ps1` and `.\scripts\verify.ps1` after installing Docker Desktop and report back anything that doesn't work as described.

<a id="out-of-scope"></a>
## 8. Out of Scope for This Pass

Deliberately not built yet, per the user's request to focus on "a database" and per `DataBaseHostingOptions.md`'s own phased approach:

- **REST API** — `DataBaseHostingOptions.md` suggests PostgREST (auto-generates CRUD endpoints from the schema) as the fastest starting point, with hand-written endpoints (e.g. FastAPI) added later for real business operations. Next step once you're ready.
- **GUI / web client** — depends on `Goals.md` Level 1's still-open "client technology" question.
- **Authentication** — `DataBaseHostingOptions.md` suggests starting simple (e.g. PostgREST + JWT) rather than building this into the database layer.
- **HTTPS / reverse proxy** — only relevant once there's an API in front of the database to secure.
- **Automated backups** — worth a manual `docker compose exec db pg_dump ...` habit even at this stage, but a scheduled/automated backup wasn't part of this request.
- **Urgency calculation** — deliberately not implemented as a database view/function; `KeyConcepts.md` treats it as a presentation-layer calculation, so it belongs in the future API layer, not the schema.

<a id="implementation-outcome-summary"></a>
## 9. Implementation Outcome Summary

**What was implemented:** everything listed as "Done" in §4 — the full Level 1 schema (`database/migrations/001_initial_schema.sql`, every entity from `DomainModel.md` §2, enumerated types matching `DomainModel.md`/`KeyConcepts.md`), the three database-enforced business rules from §5 (dependency-cycle prevention, append-only Remarks, Attachment deduplication), the example dataset (`database/seed/001_example_data.sql`), the `docker-compose.yml`/`.env.example` local-hosting setup, the `setup.ps1`/`verify.ps1`/`reset.ps1` scripts, and the `V2/README.md` how-to. This matches what §1–§6 originally planned; nothing in that scope was dropped or substituted.

**Testing:** `verify.ps1` runs `database/verify/smoke_test.sql` — a read-only check that reports row counts for every table plus a few readable summary reports — and is intended to be re-run after every `setup.ps1`. As §7 notes, this session couldn't run Docker/`psql` itself to execute that end-to-end, so the SQL and scripts were reviewed but not exercised here; the phase's "Done" status (and the presence of a real, git-ignored `.env` in `V2/`) indicates the user carried out that setup and verification locally, but a first-hand account of that run's specific output isn't captured in this document — worth adding here if anything notable came up.

**Issues that arose:** `V2/README.md` §6 ("Trying Out the Business Rules") refers to re-running an insert "from the `schema-draft.sql`" when the actual file is `database/seed/001_example_data.sql` — a leftover/incorrect filename reference worth fixing next time that section is touched. No issues arose with the schema or scripts themselves as far as this document's record shows.

**Further consideration:**
- The derived-scheduling read-time computation (Task/Project start/end dates from `start_relative_days_to_project`, effort, and dependencies — §5's third bullet) has no implementation yet; it's application logic for the REST API / GUI phases, not something this phase was scoped to build.
- Attachment content-hash computation (§5's dedup rule) is explicitly an API-layer responsibility not yet built — the database constraint is ready, but nothing computes the hash yet.
- The versioned, repeatable multi-tenant migration tooling `Requirements/Goals.md` §3.2 calls out (applying migrations across many tenant databases, not just this one local one) is out of scope here by design, and remains a Level 2/3 concern.
