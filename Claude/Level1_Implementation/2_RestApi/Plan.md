# ProjectPal V2 — REST API Phase Plan

## Contents

1. [Status and Purpose](#status-and-purpose)
2. [Scope for Level 1](#scope)
   - 2.1 [Endpoints In Scope](#in-scope)
   - 2.2 [Deferred Out of Level 1](#deferred)
3. [Architecture](#architecture)
   - 3.1 [Option A: PostgREST-Only](#option-a)
   - 3.2 [Option B: PostgREST + Hand-Written Service](#option-b)
   - 3.3 [Option C: Fully Hand-Written](#option-c)
   - 3.4 [Decision](#architecture-lean)
4. [API Design](#api-design)
   - 4.1 [Resource Endpoints](#resource-endpoints)
   - 4.2 [Auth & Search Endpoints](#custom-endpoints)
   - 4.3 [Authentication Seam](#auth-seam)
   - 4.4 [Error Conventions](#error-conventions)
5. [Implementation Plan](#implementation-plan)
   - 5.1 [Repository Layout](#repo-layout)
   - 5.2 [Build Order](#build-order)
6. [Testing](#testing)
   - 6.1 [Approach](#testing-approach)
   - 6.2 [Test Suite Layout](#test-suite-layout)
   - 6.3 [Test Categories](#test-categories)
   - 6.4 [Example Tests](#example-tests)
   - 6.5 [Manual Testing](#manual-testing)
7. [Definition of Success](#definition-of-success)
8. [Open Questions (Phase-Specific)](#open-questions)
9. [Decisions (Phase-Specific)](#decisions)
10. [Implementation Outcome Summary](#implementation-outcome-summary)

<a id="status-and-purpose"></a>
## 1. Status and Purpose

**Status:** Done.

Build the secured web API that fronts the Level 1 database (`Requirements/Goals.md` §3.1) — every client (the GUI, and later mobile) talks to this API, never to PostgreSQL directly, per the API-first foundational decision (`../Scope.md` §3). This document is the design, implementation plan, test plan, and definition of success for this phase; see `../ImplementationPlan.md` for how it fits into the Level 1 plan as a whole.

<a id="scope"></a>
## 2. Scope for Level 1

<a id="in-scope"></a>
### 2.1 Endpoints In Scope

Settled by `D1-4` (`../ImplementationPlan.md`), `D-UC-4`/`D-DM-6`/`D-DM-7` (`Requirements/UseCases.md`, `Requirements/DomainModel.md`), plus what's needed to make `D1-2`'s auth model actually work:

- **Team-scoped authorization, generally** (`D-UC-4`): every per-role check below (LeadUser/TeamLeadUser/Owner) is against the role the caller holds, via PersonRole, *on the specific Team that owns the resource being acted on* — not any role they hold anywhere else in the Organisation. Project and Component (`D-DM-6`) each carry their own `team_id` directly; a Task's Team is its own Project's Team (Task has no independent `team_id`); a Dependency's Team is whichever Task/Project side it touches. `is_organisation_admin` checks (below) are the one exception — they're Team-independent by design (`D-DM-4`).
- **Team, Person, PersonRole** — read for everyone (needed for assignment pickers and role checks). Writes split at the Person/PersonRole boundary (`Requirements/DomainModel.md` Decisions item `D-DM-4`): creating/updating a **Person** is `is_organisation_admin`-only (someone has to be able to onboard People at all), with no delete route, ever — People are never hard-deleted; `PATCH` to set `is_active = false` is how someone leaving is represented. Writing a **PersonRole** row is allowed for `is_organisation_admin` *or* the target Team's own `TeamLeadUser` (V2's renamed `SuperUser` — see `Requirements/UseCases.md` §12) — a Team's TeamLeadUser can add an existing Person to their Team, remove one, or change a member's role within that Team, but can't touch the Person record itself. **Team** row creation is `is_organisation_admin`-only, and must atomically create the new Team's first `PersonRole` row too, granting some existing Person the `TeamLeadUser` role for it — a Team is never left leaderless, even transiently (`Requirements/UseCases.md` §12's "Team creation must bootstrap a TeamLeadUser"). `POST /team`'s request body therefore includes the initial leader's `person_id` alongside the Team's `name`, and both inserts happen in one transaction — if either fails, both roll back. Renaming a Team (`PATCH /team/{id}`) is `is_organisation_admin`-only, same as creation. There's no `DELETE /team/{id}` route: `Requirements/UseCases.md` §12 leaves Team deletion "not yet decided," so it isn't built until that's settled.
- **Project** — full CRUD, including the self-referencing parent tree, checked against the caller's role on the Project's own `team_id`. A `PATCH` changing `parent_project_id` to a Project belonging to a different Team is rejected (`D-DM-9`).
- **Component** — full CRUD, including its self-referencing parent tree. Carries its own `team_id` (`D-DM-6`), governing who may create/edit/delete it (same role check as Project, against the Component's own Team) — this does **not** restrict which Team's Tasks may *reference* a given Component; Component stays a cross-Team classification tag for reporting (`Requirements/KeyConcepts.md`'s Component entry), only its management is Team-scoped. Reparenting is narrower than tagging: a `PATCH` changing `parent_component_id` to a Component belonging to a different Team is rejected, same as Project above (`D-DM-9`).
- **Task** — full CRUD, checked against the caller's role on the Task's own Project's `team_id`. Moving a Task is narrower than editing it otherwise: a `PATCH` changing `project_id` to a Project belonging to a different Team is rejected for Level 1 (`D-DM-10`) — a Task can only move between Projects on its own Team.
- **Task ↔ Person resource assignment** (`task_resource`) — assign/unassign, same Team scope as Task; the target Person must additionally hold `is_resource = true` via PersonRole *on the Task's own Team* (`D-DM-8`) — a resource on a different Team isn't assignable, even if they're a resource there.
- **Dependency** — create/delete. The cycle-rejection rule is already enforced by `1_DatabaseSetup`'s `check_dependency_no_cycle` trigger; this phase's job is surfacing that cleanly (§4.4), not re-implementing it.
- **Attachment** — upload/list/download, for `File` and `Link` kinds only (`D1-4`). The dedup rule is already enforced by `1_DatabaseSetup`'s `ux_attachment_dedup` index; again, this phase surfaces it, not re-implements it.
- **Remark** — create, list, and now edit/delete by the Remark's own owner (`D-DM-7`, reversing the immutable/append-only design `1_DatabaseSetup` originally built): any Person, including a ReadOnlyUser, may create a Remark and later edit or delete their own; nobody else may edit it, but a `TeamLeadUser` may additionally delete (not edit) a Remark they don't own, per `Requirements/UseCases.md` §12's table. The database only enforces that authorship (`created_by_person_id`) can never be reassigned (`prevent_remark_reassignment`); the owner/TeamLeadUser check itself is this API's job.
- **Search** — across Task/Project/Component/Remark, plus Attachment *metadata* (`name`, `url`, `mail_from` — not attachment content/full-text, which stays a later enhancement per `Requirements/UseCases.md`'s Search / Find use case), per `D1-4`.
- **Auth** — login only (Phase 3 builds the real implementation; this phase only needs to agree the seam — see §4.3). No impersonation endpoint: `D1.3-1` (`3_Authentication/Plan.md`) found Level 1 doesn't need one, since an admin can verify another Person's view by logging in as them directly with their own credentials.
- **Admin/support tooling** — required per `D1-4`, `is_organisation_admin`-gated (`Requirements/UseCases.md`'s Administer the System use case — not a per-Team `TeamLeadUser` concern). Exactly which capabilities (e.g. bulk data export, a data-integrity check) is still open — see `Q1.2-4`.

<a id="deferred"></a>
### 2.2 Deferred Out of Level 1

- **Merge/Conflict resolution endpoints** — Level 1 has multiple concurrent users but not two of them editing the same record at once; real usage avoids that, so no conflict handling is built (`D1-4`, `Requirements/DomainModel.md` Cross-Cutting Concerns). Nothing to build here.
- **Captured-email attachments** (`Mail` kind) — the schema supports it, but the capture *mechanism* (inbound email processing) is its own integration piece, not part of this phase (`D1-4`). Manual upload of `File`/`Link` attachments covers Level 1.
- **Old-architecture-specific admin operations** — storage-backend switching, forced re-sync, etc. — per `Requirements/UseCases.md`'s Administer the System use case, these are tied to the old app's particular architecture and aren't being carried forward, unlike admin/support tooling generally (see §2.1).

`Requirements/UseCases.md`'s View the Plan (Gantt) use case is required (`D1-4`) but deliberately **not** in scope for this API at all — the database needs nothing special for it, and it's handled entirely in the GUI layer by composing the Task/Project/Dependency reads this phase already provides. See `../5_GuiClient/Plan.md`.

<a id="architecture"></a>
## 3. Architecture

`1_DatabaseSetup/DataBaseHostingOptions.md` suggested PostgREST as the fastest starting point, with hand-written endpoints layered in later. Two ways to read that for Level 1:

<a id="option-a"></a>
### 3.1 Option A: PostgREST-Only

Everything — including login and impersonation — is PostgREST plus Postgres. PostgREST's own documented pattern for JWT auth is a SQL function (e.g. `login(email, password)`) using `pgcrypto` to verify a password hash and the `pgjwt` extension to sign a token, exposed as an RPC call (`POST /rpc/login`); impersonation is the same shape, gated by checking the caller's claims inside the function. Per-Team/role authorization is enforced by Postgres Row-Level Security policies reading the JWT's claims (PostgREST sets them as session variables automatically) — the same `person_id`/Team-roles/`is_organisation_admin` claims `D1-2` already specified. Search becomes another SQL function exposed as RPC.

- **Pros:** fewest possible moving parts — one more Docker Compose service (`postgrest`, alongside `db`) and nothing else; no new language/runtime to introduce; authorization lives right next to the data it protects (RLS), which is arguably easier to audit for correctness than re-deriving the same rules in application code.
- **Cons:** business logic (login, impersonation, search) is written in PL/pgSQL, which is less familiar to most developers and harder to unit-test with ordinary tooling (though HTTP-level tests, §6, still work fine against it). Level 2/3's move to a real external identity provider (`Requirements/Goals.md`'s identity direction) doesn't fit this shape naturally — federated sign-in wants a callback endpoint and token exchange logic that's awkward to write as a SQL function — so this simplicity likely doesn't survive past Level 1 unchanged.

<a id="option-b"></a>
### 3.2 Option B: PostgREST + Hand-Written Service (Hybrid)

PostgREST handles straightforward CRUD (Team, Person, Project, Task, Component, `task_resource`, `dependency`, `attachment` metadata, `remark`). A thin hand-written service (Python/FastAPI, per `DataBaseHostingOptions.md`'s own example) handles login, impersonation, and search — the things that aren't plain CRUD.

- **Pros:** auth code lives in a general-purpose language with mature libraries (password hashing, JWT signing) and ordinary unit-testing tools; this is the same shape Level 2/3 will need anyway when swapping in real OIDC/Google sign-in (`Requirements/Goals.md`'s identity direction), so the login endpoint's internals change later but the seam everything else depends on doesn't — echoing the same reasoning `D1-2` already used for named users over shared login.
- **Cons:** one more service to run and keep up to date (still just one more Docker Compose entry, matching the existing pattern), and a second codebase/language boundary from day one, for a Level whose whole point is minimal infrastructure.

<a id="option-c"></a>
### 3.3 Option C: Fully Hand-Written (No PostgREST)

One hand-written service (Python/FastAPI) implements every endpoint — the CRUD surface (§2.1) as well as login, impersonation, and search. No PostgREST anywhere in the stack.

- **Pros:** every request passes through the same code, so there's exactly one place to compute `Attachment.content_hash`/`size_bytes` before insert, and one place (`errors.py`) to catch and translate the three DB-enforced business-rule exceptions (and ordinary constraint violations) into clean HTTP errors — there's no longer a question of *which* system is actually in a given request's path. Team/role authorization is ordinary application code reading the JWT's claims, not a second authorization model (Postgres Row-Level Security) needing its own migration and its own correctness story. `4_HttpsReverseProxy` stays pure TLS termination in front of one service, rather than also needing to route between two.
- **Cons:** every resource's CRUD (Team, Person, PersonRole, Project, Component, Task, `task_resource`) has to be hand-written rather than generated by PostgREST — real, if mechanical, additional code, in exchange for removing the RLS migration, PostgREST's JWT-claims wiring, and Phase 4's would-be routing logic.

<a id="architecture-lean"></a>
### 3.4 Decision

Reopens `D1.2-1` (§9) — see `D1.2-3`: Option C, not Option B. Laying out the concrete shape in §5–§6 (originally under Option B) showed that roughly half the "plain CRUD" surface — Dependency, Attachment, Remark — already needed hand-written logic, and Team/Person/PersonRole needed a different, non-Team-scoped authorization model PostgREST's RLS approach didn't fit cleanly. That undercuts PostgREST's core value (skip writing the boring parts) enough that one hand-written service for everything is now simpler overall, not just architecturally cleaner. `D1.2-1` itself is left as it was decided — see its entry in §9 for why Option B looked right at the time. Where Urgency (`Requirements/KeyConcepts.md` §12) gets computed is unaffected by this and remains settled — `D1.2-2` (§9): in the GUI, not this API.

<a id="api-design"></a>
## 4. API Design

<a id="resource-endpoints"></a>
### 4.1 Resource Endpoints

The CRUD surface listed in §2.1, one route family per table (e.g. `GET/POST /task`, `GET/PATCH/DELETE /task/{id}`, filtering via query params such as `GET /task?project_id=5`), all served by the one hand-written service (`D1.2-3`) — the same service Level 2/3 will later extend with tenant-routing. Full route signatures are nailed down in the API contract (§5.2 step 8), not repeated here.

<a id="custom-endpoints"></a>
### 4.2 Auth & Search Endpoints

Also served by the same service — there's no longer a resource-vs-custom distinction to draw, since nothing is generated.

| Endpoint | Purpose |
|---|---|
| `POST /auth/login` | Verify credentials, issue a JWT (`person_id`, Team/role memberships, `is_organisation_admin`) — `D1-2`. |
| `GET /auth/whoami` | Return the calling token's own claims (`person_id`, Team/role memberships, `is_organisation_admin`) — lets a client (and, per §6.4, a test) confirm who it's currently authenticated as. |
| `GET /search?q=...` | Cross-table search over Task/Project/Component/Remark, plus Attachment `name`/`url`/`mail_from` — `D1-4`, `Requirements/UseCases.md` Search / Find. |

<a id="auth-seam"></a>
### 4.3 Authentication Seam

Every endpoint expects a `Bearer` JWT and authorizes off its claims (`D1-2`) — this doesn't wait for Phase 3. Phase 2 stands up the seam with a stub token issuer (e.g. a test-only endpoint or fixture that mints a validly-signed JWT for a chosen Person without checking a real password), so authorization logic is real and testable from the start; Phase 3 replaces the stub with real password verification without touching any other endpoint.

<a id="error-conventions"></a>
### 4.4 Error Conventions

The database already enforces three business rules as triggers/constraints (`1_DatabaseSetup`): dependency-cycle rejection, remark immutability, attachment deduplication. Raw Postgres errors are not an acceptable API response — each must be translated to a clean, consistent HTTP error (a stable error code/message shape, not a leaked exception string), so the GUI can show a sensible message rather than a stack trace. With every write going through this one service (`D1.2-3`), `errors.py` (§5.1) is unambiguously the one place this happens, for these three cases and for ordinary constraint violations alike (e.g. a foreign-key violation from referencing a Team or Person that doesn't exist).

<a id="implementation-plan"></a>
## 5. Implementation Plan

<a id="repo-layout"></a>
### 5.1 Repository Layout

One service, one codebase, no PostgREST (`D1.2-3`) — so there's no separate authorization model to stand up (no RLS, no new migration) and no second backend to route between. Team/role authorization is checked directly in each route handler, reading the same JWT claims `D1-2` already specified. No ORM: plain `psycopg` with hand-written SQL, consistent with `1_DatabaseSetup`'s raw-SQL migrations, so there's one schema-management story, not two.

The GUI talks to one base URL from day one, since there's only one service to talk to. `4_HttpsReverseProxy` (Phase 4) stays pure TLS termination in front of it.

```
V2/
├── docker-compose.yml            (existing — modified: adds a single `rest-api` service)
├── .env.example                  (existing — modified: adds JWT_SECRET)
├── rest-api/                     (NEW — everything this phase owns; not just "api" in case another kind of API comes along later)
│   ├── Dockerfile
│   ├── requirements.txt          — fastapi, uvicorn, pyjwt, psycopg[binary], bcrypt
│   ├── app/
│   │   ├── __init__.py
│   │   ├── main.py               — FastAPI app; mounts every router below; registers the §4.4 error handlers
│   │   ├── config.py             — JWT secret, DB connection string, read from environment
│   │   ├── db.py                 — the service's one shared DB connection/pool, used by every route
│   │   ├── security/
│   │   │   ├── __init__.py
│   │   │   ├── jwt.py            — encode/decode; claim shape (`person_id`, Team/role memberships, `is_organisation_admin`) per `D1-2`
│   │   │   └── deps.py           — FastAPI dependency that extracts + validates the Bearer token on every route; this **is** the authentication seam (§4.3), and where Team/role checks happen (replaces what RLS would have done under Option B)
│   │   ├── routes/
│   │   │   ├── __init__.py
│   │   │   ├── auth.py           — `POST /auth/login` (stub issuer this phase, §4.3), `GET /auth/whoami`
│   │   │   ├── search.py         — `GET /search`, including Attachment `name`/`url`/`mail_from` (`D1-4`)
│   │   │   ├── teams.py          — Team, Person, PersonRole (§2.1's write rules live here, in code: Person is `is_organisation_admin`-only with no delete route — `is_active` is the only "removal" — while PersonRole also accepts the target Team's own `TeamLeadUser`; creating a Team atomically inserts its bootstrap `TeamLeadUser` PersonRole row in the same transaction)
│   │   │   ├── projects.py       — Project
│   │   │   ├── components.py     — Component, Team-scoped by its own `team_id` (`D-DM-6`)
│   │   │   ├── tasks.py          — Task, `task_resource`
│   │   │   ├── dependencies.py   — Dependency
│   │   │   ├── attachments.py    — Attachment; computes `content_hash`/`size_bytes` before insert
│   │   │   ├── remarks.py        — Remark create/list/edit/delete; edit restricted to the Remark's own owner, delete to the owner or a TeamLeadUser on that Remark's Team (`D-DM-7`)
│   │   │   └── admin.py          — admin/support tooling required by `D1-4`; exact capabilities still open, see `Q1.2-4`
│   │   └── errors.py             — maps Postgres exceptions (the three business rules, plain constraint violations) to clean HTTP responses (§4.4)
│   └── tests/                    — see §6.2
└── scripts/
    └── test-api.ps1              (NEW — mirrors setup.ps1/verify.ps1: brings the stack up, runs the test suite)
```

<a id="build-order"></a>
### 5.2 Build Order

1. Stand up the hand-written service (`D1.2-3`) against the existing `1_DatabaseSetup` schema/data — no schema or migration changes needed for this phase.
2. Expose read-only routes for reference/lookup data first (Team, Person, PersonRole, Component) — lowest risk, no auth-sensitive writes yet.
3. Add the authentication seam (`security/deps.py`), with Team-scoped role authorization (§2.1) checked directly in each route as it's built: Person/PersonRole/Team writes first (including Team-creation's atomic TeamLeadUser bootstrap), then Project, Component, Task, `task_resource`, `dependency`, and Remark (including its owner-only edit and owner-or-TeamLeadUser delete).
4. Implement Attachment upload (`content_hash`/`size_bytes` computed before insert) and the error-mapping (`errors.py`, §4.4) for the three DB-enforced business rules plus ordinary constraint violations.
5. Add the Search and Auth routes (§4.2).
6. Resolve `Q1.2-4` and add the admin/support routes it settles on (`admin.py`).
7. Write and pass the test suite (§6).
8. Publish an API contract (OpenAPI, auto-generated by FastAPI for every route) so the GUI/Web Client phase has something concrete to build against.

No step here for Urgency (`D1.2-2`): it's computed client-side from fields this API already serves, so this phase needs no additional work for it.

<a id="testing"></a>
## 6. Testing

<a id="testing-approach"></a>
### 6.1 Approach

HTTP-level integration tests against a running instance (the same `docker compose` stack `1_DatabaseSetup` already established, with the hand-written service added to it), rather than unit-testing the service's internals directly — the thing that needs proving is the API's external behaviour, not its implementation. Being black-box, this test suite didn't need to change when `D1.2-1` was reopened in favour of `D1.2-3` — the same tests apply regardless of what's behind the HTTP boundary.

<a id="test-suite-layout"></a>
### 6.2 Test Suite Layout

```
V2/rest-api/tests/
├── conftest.py               — fixtures: an `api` HTTP client, `alice_token`/`bob_token`/`admin_token` (minted via the stub issuer against `1_DatabaseSetup`'s seeded People), seeded IDs (task_id, alice_person_id, ...) reused from the existing seed data
├── helpers.py                — the `auth()` header-builder used across test files
├── requirements-test.txt     — pytest, requests
├── test_auth.py              — no-token rejection; the stub login issues a usable token
├── test_authorization.py     — the authorization check in `security/deps.py` actually restricts by Team/role (`D-UC-4`) — a role held on one Team never grants access to another Team's resources, not just "any valid token"
├── test_crud_reference_data.py   — Team/Person/PersonRole/Component read; Person create/update is admin-only with no delete route at all; a Team's TeamLeadUser can write PersonRole for their own Team but is rejected writing Person or another Team's PersonRole; creating a Team atomically creates its bootstrap TeamLeadUser PersonRole row, and is rejected without one
├── test_crud_project.py      — includes rejecting a reparent onto a different Team's Project (`D-DM-9`)
├── test_crud_component.py    — includes Team-scoped create/edit/delete (`D-DM-6`), that a Task in a different Team can still reference the Component, and rejecting a reparent onto a different Team's Component (`D-DM-9`)
├── test_crud_task.py         — includes rejecting a `project_id` change that would move the Task onto a different Team's Project (`D-DM-10`, Level 1 only)
├── test_resource_assignment.py   — assigning a Person who isn't a resource on the Task's own Team is rejected, even if they're a resource elsewhere (`D-DM-8`)
├── test_dependency_rules.py  — includes the cycle-rejection case (§6.4)
├── test_attachment_rules.py  — includes the dedup-rejection case
├── test_remark_rules.py      — owner (including a ReadOnlyUser owner) can edit/delete their own Remark; a non-owner, non-TeamLeadUser cannot; a TeamLeadUser can delete but not edit a Remark they don't own; authorship (`created_by_person_id`) can never be changed by anyone, including the owner (`D-DM-7`)
├── test_search.py            — includes Attachment `name`/`url`/`mail_from` matches (`D1-4`)
├── test_admin.py             — whatever `Q1.2-4` settles on
└── test_journey.py           — the end-to-end scenario (§6.3)
```

`V2/scripts/test-api.ps1` (§5.1) is what actually runs this: bring the stack up if it isn't already, then `pytest V2/rest-api/tests`.

<a id="test-categories"></a>
### 6.3 Test Categories

- **CRUD happy paths** — one create/read/update/delete cycle per in-scope resource (§2.1).
- **Authorization, Team-scoped** (`D-UC-4`) — a request with no token, an expired token, or a token for a Person without the right role *on that resource's own Team* are all rejected; a valid token with the right role on that specific Team succeeds, and the same role held only on a *different* Team is still rejected.
- **Business-rule surfacing** (§4.4) — each of the three DB-enforced rules produces a clean 4xx, not a raw Postgres error.
- **Team creation bootstrap** — creating a Team without an initial TeamLeadUser `person_id` is rejected; creating one with it produces both the Team and its PersonRole row atomically.
- **Team-boundary integrity** — resource assignment requires the Person to be a resource on the Task's own Team, not merely a resource somewhere (`D-DM-8`); reparenting a Project or Component onto a different Team's parent is rejected (`D-DM-9`); moving a Task onto a different Team's Project is rejected for Level 1 (`D-DM-10`).
- **Remark ownership** (`D-DM-7`) — the owner can edit/delete their own Remark (even a ReadOnlyUser owner); a non-owner without TeamLeadUser standing on that Team cannot edit or delete it; a TeamLeadUser can delete but not edit a Remark they don't own; nobody, including the owner, can change a Remark's `created_by_person_id`.
- **End-to-end journey** — one test walking through a realistic sequence (create Project → create Task → assign a resource → add a Dependency → add a Remark) to prove the pieces work together, not just in isolation.

<a id="example-tests"></a>
### 6.4 Example Tests

Illustrative, using a Python/`pytest`/`requests`-style client against the endpoint shapes in §4.2.

```python
def test_dependency_cycle_is_rejected(api, alice_token):
    # Task 2 already depends on Task 1 (pre=1, post=2) in the seed data.
    resp = api.post(
        "/dependency",
        json={"pre_task_id": 2, "post_task_id": 1},
        headers=auth(alice_token),
    )
    assert resp.status_code == 409
    assert "cycle" in resp.json()["error"].lower()
    # Not a raw Postgres exception string leaking through.
    assert "psycopg" not in resp.text.lower()


def test_remark_owner_can_edit_their_own(api, alice_token, alice_owned_remark_id):
    resp = api.patch(
        f"/remark/{alice_owned_remark_id}",
        json={"remark_text": "edited by its own owner"},
        headers=auth(alice_token),
    )
    assert resp.status_code == 200


def test_remark_non_owner_cannot_edit(api, bob_token, alice_owned_remark_id):
    resp = api.patch(
        f"/remark/{alice_owned_remark_id}",
        json={"remark_text": "edited by someone else"},
        headers=auth(bob_token),
    )
    assert resp.status_code == 403


def test_remark_authorship_cannot_be_reassigned(api, alice_token, alice_owned_remark_id, bob_person_id):
    resp = api.patch(
        f"/remark/{alice_owned_remark_id}",
        json={"created_by_person_id": bob_person_id},
        headers=auth(alice_token),
    )
    assert resp.status_code in (400, 403)


def test_duplicate_attachment_is_rejected(api, alice_token, task_id, sample_file):
    first = api.post(f"/task/{task_id}/attachments", files=sample_file, headers=auth(alice_token))
    assert first.status_code == 201

    second = api.post(f"/task/{task_id}/attachments", files=sample_file, headers=auth(alice_token))
    assert second.status_code == 409


def test_request_without_token_is_rejected(api):
    resp = api.get("/task")
    assert resp.status_code == 401


def test_team_scoped_authorization_rejects_wrong_team_role(api, bob_token, other_team_project_id):
    # bob_token belongs to a LeadUser on Team 1; other_team_project_id belongs
    # to Team 2, where bob holds no role at all.
    resp = api.post(
        "/task",
        json={"project_id": other_team_project_id, "description": "Should be rejected"},
        headers=auth(bob_token),
    )
    assert resp.status_code == 403


def test_team_creation_bootstraps_team_lead_user(api, admin_token, alice_person_id):
    resp = api.post(
        "/team",
        json={"name": "New Team", "initial_team_lead_person_id": alice_person_id},
        headers=auth(admin_token),
    )
    assert resp.status_code == 201
    new_team_id = resp.json()["team_id"]

    roles = api.get(f"/person-role?team_id={new_team_id}", headers=auth(admin_token)).json()
    assert any(r["person_id"] == alice_person_id and r["role"] == "TeamLeadUser" for r in roles)
```

<a id="manual-testing"></a>
### 6.5 Manual Testing

The interactive Swagger UI at `http://127.0.0.1:8000/docs` (generated from `openapi.json`, §5.2 step 8) is the quickest way to exercise the API by hand, alongside the automated suite above.

**1. Make sure the stack is running.** The Swagger UI is served *by* the REST API itself, so both it and the database it depends on need to be up first. From `V2/`:
```powershell
docker compose up -d
```
This starts (and, the first time, builds) both the `db` and `rest-api` containers — `rest-api` won't start until `db` reports healthy (`docker-compose.yml`'s `depends_on` condition). Check `docker ps --filter name=projectpal` for `projectpal-db` (healthy) and `projectpal-rest-api` (up); the first request to `http://127.0.0.1:8000/docs` loading in a browser confirms it's actually ready. Nothing here loads or resets data — it's the same database and example dataset every other section of this document assumes. `.\scripts\test-api.ps1` (§6.2) does this same startup step automatically before running the automated suite, so it's also a one-command way to get the stack up if you'd rather not run `docker compose` directly.

**2. Get a token.** Find `POST /auth/login` under the **auth** section, click it → **Try it out**, and enter a body like:
```json
{"external_login": "alice.chen@example.com"}
```
Click **Execute**. Login is a stub for now (§4.3 — Phase 3 builds real password checking), so any seeded Person's `external_login` logs you in as them, no password needed. Copy the `token` string from the response (without the quotes).

**3. Authorize.** Click the green **Authorize** button (padlock icon, top-right of the page). Paste just the token value into the field — Swagger already knows it's a Bearer token (the OpenAPI spec declares `HTTPBearer`) and prepends `Bearer ` itself, so don't type that part. Click **Authorize**, then **Close**. Every endpoint's "Try it out" now sends that token automatically.

**4. Try endpoints.** Expand any route (e.g. `GET /team`, `GET /task`), click **Try it out** → **Execute**, and see the real response. The padlock icon on each route confirms it requires the token you just authorized with.

**5. Test different roles by logging in as different seeded People** — re-run step 2 with a different `external_login` and re-authorize with the new token:

| Person | `external_login` | Role |
|---|---|---|
| Alice Chen | `alice.chen@example.com` | org admin, TeamLeadUser (Team 1) |
| Ben Okafor | `ben.okafor@example.com` | LeadUser (Team 1), no role on Team 2 |
| Priya Sharma | `priya.sharma@example.com` | NormalUser (Team 1) |
| Tom Baxter | `tom.baxter@example.com` | TeamLeadUser (Team 2), NormalUser (Team 1) |
| Sam Patel | `sam.patel@example.com` | ReadOnlyUser (Team 2) |
| Nadia Fischer | `nadia.fischer@example.com` | org admin, NormalUser (both Teams) |

A few things worth trying to see the authorization rules in action:
- `POST /project` as Sam (ReadOnlyUser) → 403, since creating needs LeadUser+.
- `POST /task` with a Team-2 `project_id` while authorized as Ben → 403 (he has no role on Team 2, `D-UC-4`).
- `POST /team` as Ben → 403 (org-admin only); as Alice/Nadia → 201, and it requires an `initial_team_lead_person_id` in the body (`D1.2-4`'s bootstrap rule).
- `PATCH /remark/{id}` on someone else's remark → 403; on your own → 200, even as Sam (ReadOnlyUser owners can edit their own, `D-DM-7`).
- `POST /attachment` for `kind: "File"` needs multipart form fields (`name`, `task_id`, `kind`, `file`) — Swagger renders these as a form with a file picker automatically.

Tokens expire 8 hours after login (`security/jwt.py`'s `JWT_TTL_SECONDS`) — re-run step 2 if a previously-working request starts returning 401.

<a id="definition-of-success"></a>
## 7. Definition of Success

For Level 1, this phase is done when:

- Every endpoint in §2.1 exists, is covered by the test categories in §6.3, and the test suite passes.
- Every Team/role authorization check is genuinely Team-scoped (`D-UC-4`) — a role held on one Team never grants access to another Team's resources.
- Team creation always atomically bootstraps its TeamLeadUser PersonRole — there's no way to create a leaderless Team through this API.
- Team-boundary integrity holds end to end (`D-DM-8`/`D-DM-9`/`D-DM-10`): resource assignment, reparenting, and Task moves all stay within a single Team, verified by tests.
- Remark ownership rules (`D-DM-7`) hold: an owner (including a ReadOnlyUser) can edit/delete their own Remark; nobody else can edit it; a TeamLeadUser can delete but not edit one they don't own; authorship can never be reassigned.
- The three DB-enforced business rules are surfaced as clean errors (§4.4), verified by tests, not just manually checked once.
- Every endpoint requires and authorizes off a JWT (§4.3) — even before Phase 3 provides real login, the seam is real and tested via the stub issuer.
- Admin/support tooling (`D1-4`) is built, once `Q1.2-4` settles exactly which capabilities.
- The GUI/Web Client phase (Phase 5) can be built entirely against this API, with no direct database access from the client — proving the API-first foundational decision actually holds in practice, not just on paper.
- An API contract (§5.2 step 8) exists for the GUI phase to build against.

Explicitly **not** required for success: any Urgency computation (`D1.2-2` — this is the GUI's job, see `5_GuiClient/Plan.md`), a dedicated plan-view endpoint (§2.2), an impersonation mechanism (`D1.3-1` — not needed for Level 1), or anything on the deferred list.

<a id="open-questions"></a>
## 8. Open Questions (Phase-Specific)

None currently open — see Decisions below.

<a id="decisions"></a>
## 9. Decisions (Phase-Specific)

- **D1.2-1** (decided 2026-08-21)<br>
  **Question:** Architecture — PostgREST-only (§3.1) vs. PostgREST + hand-written service (§3.2)?<br>
  **Decision:** Option B, the hybrid. PostgREST serves the CRUD surface (§4.1) — a mature, production-grade tool in its own right, not a shortcut, so this half of the choice isn't in question. The hand-written service handles login, impersonation, and search (§4.2), and is deliberately designed as a *permanent* identity/auth/gateway layer from Level 1 onward, not Level-1-only scaffolding: Level 2/3's database-per-tenant architecture (`Requirements/Goals.md` §3.2) needs an application-layer request router that resolves "which tenant → which database" before a request reaches Postgres, and PostgREST has no way to do that — a given PostgREST instance is wired to one connection string. Option A (everything as Postgres/PL/pgSQL functions) would have no natural place to grow into that routing role, meaning it wouldn't just be login code getting rebuilt at Level 2/3, it would be an entirely new architectural layer bolted on afterward. Building the service now, even though at Level 1 it only has auth and search to do, means Level 2/3 extends an existing piece rather than introduces one. This also echoes `D1-2`'s reasoning: auth is the piece most likely to be rebuilt for Level 2/3 (real OIDC/Google sign-in), and belongs in ordinary application code with mature libraries, not PL/pgSQL, for the same reason `D1-2` chose named users over shared login.<br>
  **Superseded by:** `D1.2-3`.
- **D1.2-2** (decided 2026-08-22)<br>
  **Question:** Where is Urgency (`Requirements/KeyConcepts.md` §12) computed — client-side in the GUI from raw Task fields, or server-side in this API?<br>
  **Decision:** client-side in the GUI, computed from Task/Project fields this API's resource endpoints (§4.1) already expose. `Requirements/KeyConcepts.md` §12 already frames Urgency as a presentation-layer calculation; computing it in the GUI is the most literal reading of that — more so than in this API, which is a data/service layer, not presentation — and it keeps this API's job limited to serving the raw stored facts Urgency is derived from, not derived values themselves. It's also naturally suited to a likely later refinement: team-specific configurable weights for the Urgency algorithm (not Level 1 — see `Claude/Level2_Implementation/Scope.md`), which fits more naturally as GUI-side configuration than a server-side per-Team lookup on every read. This phase needs no Task-endpoint wrapping as a result — see `5_GuiClient/Plan.md`, which owns Urgency computation end to end, including the requirement to fetch the whole Project ancestor chain (not just one Task's immediate Project) that computing it correctly depends on.
- **D1.2-3** (decided 2026-08-22)<br>
  **Question:** Revisiting `D1.2-1` — should PostgREST still serve the CRUD surface, now that laying out the concrete shape of this phase (§5–§6) has surfaced what that actually requires?<br>
  **Decision:** no — Option C (§3.3), one hand-written service for everything, no PostgREST. Working through the repository layout and test plan under Option B surfaced two problems severe enough to reopen it: (1) attachment upload needs `content_hash`/`size_bytes` computed before insert, which plain PostgREST CRUD can't do, and which the design never actually assigned to either system; (2) `Team`/`Person`/`PersonRole`'s "read for everyone, admin-gated write" rule doesn't fit PostgREST's Row-Level Security model the way the Team-scoped tables do, and nothing was implementing it either. Both trace to the same root cause: roughly half the "plain CRUD" surface (Dependency, Attachment, Remark, plus the differently-shaped Team/Person/PersonRole rules) already needed hand-written logic, which undercuts PostgREST's core value (skip writing the boring parts) enough that one service for everything is now simpler overall — not just architecturally tidier, genuinely less total work, since it also removes the RLS migration, PostgREST's JWT-claims wiring, and the path-based routing `4_HttpsReverseProxy` would otherwise have needed. `D1.2-1`'s reasoning about the service being a permanent identity/gateway layer (not Level-1-only scaffolding) is unaffected — it now just does more from day one.
- **D1.2-4** (decided 2026-08-23)<br>
  **Question:** Which specific admin/support capabilities does Level 1 need, now that `D1-4` requires admin/support tooling generally?<br>
  **Decision:** the two candidates `Q1.2-4` already named — bulk data export (`GET /admin/export`, a full JSON dump of every table except `attachment`, whose `data` column holds raw file bytes) and a data-integrity check (`GET /admin/integrity-check`: Teams with no `TeamLeadUser`, and `task_resource` assignments where the Person is no longer a resource on that Task's Team). Both `is_organisation_admin`-gated. The old app's storage-backend switching and forced re-sync don't carry forward (§2.2) — they're specific to an architecture this system doesn't have.
- **D1.2-5** (decided 2026-08-23)<br>
  **Question:** `Requirements/DomainModel.md`'s Dependency entity says a Dependency is "governed by the owning Task/Project's Edit permission," but doesn't say what that means when its two sides have different owners or belong to different Teams — whose Edit permission?<br>
  **Decision:** both — creating or deleting a Dependency requires the caller to hold Edit rights (Owner-above-ReadOnly or TeamLeadUser, per `Requirements/UseCases.md` §12) on *both* the predecessor and the successor side, whichever of Task/Project each is. You shouldn't be able to link (or unlink) an item you can't otherwise edit, on either end. No restriction on the two sides belonging to different Teams — only Task moves (`D-DM-10`) and reparenting (`D-DM-9`) are Team-boundary-restricted, not Dependencies, since a cross-Team dependency (e.g. "this billing task depends on infra work owned by a different Team") is a legitimate scheduling relationship, not a structural move.

See `../ImplementationPlan.md` for how this phase fits into the Level 1 plan, and for open questions that span this phase and others.

<a id="implementation-outcome-summary"></a>
## 10. Implementation Outcome Summary

**What was implemented:** the full §2.1 endpoint surface as one hand-written FastAPI service (`V2/rest-api/`, `D1.2-3`) — Team/Person/PersonRole (including the atomic TeamLeadUser bootstrap on Team creation), full CRUD on Project/Component/Task (Team-scoped per `D-UC-4`, with the reparenting and Task-move restrictions from `D-DM-9`/`D-DM-10`), Task↔Person resource assignment (`D-DM-8`), Dependency create/delete (`D1.2-5`), Attachment upload/list/download for File/Link kinds with `content_hash`/`size_bytes` computed here, Remark create/list/edit/delete with owner/TeamLeadUser rules (`D-DM-7`), cross-table Search, the stub auth seam (`POST /auth/login`, `GET /auth/whoami`), and admin export/integrity-check (`D1.2-4`). `errors.py` maps all three DB-enforced business rules plus ordinary constraint violations to clean HTTP responses. `docker-compose.yml`/`.env.example` were extended with the `rest-api` service and `JWT_SECRET`; `scripts/test-api.ps1` was added alongside `setup.ps1`/`verify.ps1`; the OpenAPI contract was generated and saved to `V2/rest-api/openapi.json`. This matches §2.1's scope exactly — nothing was dropped or descoped from what was planned.

**Testing:** unlike `1_DatabaseSetup` (built without Docker available), this phase had a working Docker/Python environment throughout, so it was actually built and tested end to end, not just reviewed. All 44 tests across every category in §6.3 pass, run via `.\scripts\test-api.ps1` against the real running stack (HTTP-level, per §6.1 — no mocks). The suite is re-runnable against the same persistent dev database without a reset (unique names/content generated per run), matching how `test-api.ps1` is meant to be used day to day.

**Issues that arose:**
- The `1_DatabaseSetup` seed data had never given Team 2 a `TeamLeadUser` — it predates the bootstrap requirement this phase's design formalized. Fixed by promoting Tom Baxter to `TeamLeadUser` on Team 2 in `database/seed/001_example_data.sql`, which also makes `/admin/integrity-check` return clean on the seed data.
- Two test-writing mistakes were caught by actually running the suite rather than reasoning about it: a "Team-lead-only" test used Alice, who is also `is_organisation_admin` and so passed for the wrong reason; a "happy path" resource-assignment test picked a Person already assigned to that Task in the seed data, so it hit the dedup path instead of the happy path. Both were test bugs, not application bugs — fixed by picking better fixtures.
- No application-level bugs surfaced during testing beyond the two test-authoring mistakes above.

**Further consideration:**
- `password_hash` (named in `D1-2`) and a `UNIQUE` constraint on `person.external_login` still don't exist in the schema — `3_Authentication` will need a schema change before it can replace the stub login with real password verification.
- Nothing in the automated suite exercises the Remark-authorship-reassignment DB trigger directly, since the API never exposes a field that could attempt it (arguably a stronger guarantee than a test would give — but it means that trigger is currently only manually smoke-tested, not covered by pytest).
- List endpoints have no pagination and only the query-param filters each route defines — fine at Level 1's data volumes, likely needs revisiting once Level 2/3 have real data volumes.
- This API currently runs over plain HTTP (`4_HttpsReverseProxy` hasn't been built yet) — expected and acceptable per Level 1's "security is not the top concern" framing, but worth remembering before this is ever reachable from outside this machine.
- `D1.2-4`'s admin capabilities and `D1.2-5`'s Dependency authorization rule were both decided during this implementation pass rather than pre-agreed — worth a light second look once the GUI phase starts actually exercising them with real usage patterns.
