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
7. [Definition of Success](#definition-of-success)
8. [Open Questions (Phase-Specific)](#open-questions)
9. [Decisions (Phase-Specific)](#decisions)

<a id="status-and-purpose"></a>
## 1. Status and Purpose

**Status:** Not started.

Build the secured web API that fronts the Level 1 database (`Requirements/Goals.md` §3.1) — every client (the GUI, and later mobile) talks to this API, never to PostgreSQL directly, per the API-first foundational decision (`../Scope.md` §3). This document is the design, implementation plan, test plan, and definition of success for this phase; see `../ImplementationPlan.md` for how it fits into the Level 1 plan as a whole.

<a id="scope"></a>
## 2. Scope for Level 1

<a id="in-scope"></a>
### 2.1 Endpoints In Scope

Derived from `Requirements/UseCases.md`'s candidate "essential set" for a meaningful Demonstrator trial (Manage Projects/Tasks, Assign Resources, Set Dependencies, Search, Remarks), plus what's needed to make `D1-2`'s auth model actually work:

- **Team, Person, PersonRole** — read for everyone (needed for assignment pickers and role checks); create/update gated to `is_organisation_admin` (someone has to be able to create Person records at all).
- **Project, Component** — full CRUD, including the self-referencing parent tree for both.
- **Task** — full CRUD.
- **Task ↔ Person resource assignment** (`task_resource`) — assign/unassign.
- **Dependency** — create/delete. The cycle-rejection rule is already enforced by `1_DatabaseSetup`'s `check_dependency_no_cycle` trigger; this phase's job is surfacing that cleanly (§4.4), not re-implementing it.
- **Attachment** — upload/list/download, for `File` and `Link` kinds. The dedup rule is already enforced by `1_DatabaseSetup`'s `ux_attachment_dedup` index; again, this phase surfaces it, not re-implements it.
- **Remark** — create and list only. The database rejects `UPDATE`/`DELETE` outright (`prevent_remark_mutation`), so there's no edit/delete endpoint to build.
- **Search** — across Task/Project/Component/Remark, per `Requirements/UseCases.md`'s Search / Find use case.
- **Auth** — login (Phase 3 builds the real implementation; this phase only needs to agree the seam — see §4.3) and admin-only impersonation (`D1-2`, `Q1.3-1`, `Q1.3-2`).

<a id="deferred"></a>
### 2.2 Deferred Out of Level 1

- **Merge/Conflict resolution endpoints** — Level 1 assumes a single user at a time, no conflict handling (`Requirements/DomainModel.md` Cross-Cutting Concerns; `Requirements/KeyConcepts.md` Merge/Conflict entry). Nothing to build here.
- **Captured-email attachments** (`Mail` kind) — the schema supports it, but the capture *mechanism* (inbound email processing) is its own integration piece, not part of this phase. Manual upload of `File`/`Link` attachments covers Level 1.
- **Broader admin/support tooling** beyond impersonation (the old app's storage-backend switching, forced re-sync, etc.) — per `Requirements/UseCases.md`'s Administer the System use case, these are tied to the old app's specific architecture and aren't being carried forward.

`Requirements/UseCases.md`'s View the Plan (Gantt) use case is deliberately **not** in scope for this API at all, deferred or otherwise — the database needs nothing special for it, and it's handled entirely in the GUI layer by composing the Task/Project/Dependency reads this phase already provides. See `../5_GuiClient/Plan.md`.

This assumes the "essential set" framing from `Requirements/UseCases.md`'s own open question about which use cases matter for the Demonstrator; if that gets resolved differently, this list should be revisited.

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

The CRUD surface listed in §2.1, one route family per table (e.g. `GET/POST /task`, `GET/PATCH/DELETE /task/{id}`, filtering via query params such as `GET /task?project_id=5`), all served by the one hand-written service (`D1.2-3`) — the same service Level 2/3 will later extend with tenant-routing. Full route signatures are nailed down in the API contract (§5.2 step 7), not repeated here.

<a id="custom-endpoints"></a>
### 4.2 Auth & Search Endpoints

Also served by the same service — there's no longer a resource-vs-custom distinction to draw, since nothing is generated.

| Endpoint | Purpose |
|---|---|
| `POST /auth/login` | Verify credentials, issue a JWT (`person_id`, Team/role memberships, `is_organisation_admin`) — `D1-2`. |
| `POST /auth/impersonate/{personId}` | Admin-only: issue a JWT for the target Person carrying an `impersonated_by` claim — `D1-2`, `Q1.3-1`. |
| `GET /search?q=...` | Cross-table search over Task/Project/Component/Remark — `Requirements/UseCases.md` Search / Find. |

<a id="auth-seam"></a>
### 4.3 Authentication Seam

Every endpoint expects a `Bearer` JWT and authorizes off its claims (`D1-2`) — this doesn't wait for Phase 3. Phase 2 stands up the seam with a stub token issuer (e.g. a test-only endpoint or fixture that mints a validly-signed JWT for a chosen Person without checking a real password), so authorization logic is real and testable from the start; Phase 3 replaces the stub with real password verification without touching any other endpoint.

<a id="error-conventions"></a>
### 4.4 Error Conventions

The database already enforces three business rules as triggers/constraints (`1_DatabaseSetup`): dependency-cycle rejection, remark immutability, attachment deduplication. Raw Postgres errors are not an acceptable API response — each must be translated to a clean, consistent HTTP error (a stable error code/message shape, not a leaked exception string), so the GUI can show a sensible message rather than a stack trace. With every write going through this one service (`D1.2-3`), `errors.py` (§5.1) is unambiguously the one place this happens, for these three cases and for ordinary constraint violations alike (e.g. attempting to delete a Person who still owns or is assigned to something, which the schema's plain foreign-key `RESTRICT` behaviour already blocks).

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
│   │   │   ├── jwt.py            — encode/decode; claim shape (`person_id`, Team/role memberships, `is_organisation_admin`, optional `impersonated_by`) per `D1-2`
│   │   │   └── deps.py           — FastAPI dependency that extracts + validates the Bearer token on every route; this **is** the authentication seam (§4.3), and where Team/role checks happen (replaces what RLS would have done under Option B)
│   │   ├── routes/
│   │   │   ├── __init__.py
│   │   │   ├── auth.py           — `POST /auth/login` (stub issuer this phase, §4.3), `GET /auth/whoami`
│   │   │   ├── impersonate.py    — `POST /auth/impersonate/{personId}`, admin-gated
│   │   │   ├── search.py         — `GET /search`
│   │   │   ├── teams.py          — Team, Person, PersonRole (§2.1's "read for everyone, admin-gated write" rule lives here, in code)
│   │   │   ├── projects.py       — Project, Component
│   │   │   ├── tasks.py          — Task, `task_resource`
│   │   │   ├── dependencies.py   — Dependency
│   │   │   ├── attachments.py    — Attachment; computes `content_hash`/`size_bytes` before insert
│   │   │   └── remarks.py        — Remark (create/list routes only — no update/delete route exists at all, matching the database's own rule)
│   │   └── errors.py             — maps Postgres exceptions (the three business rules, plain constraint violations) to clean HTTP responses (§4.4)
│   └── tests/                    — see §6.2
└── scripts/
    └── test-api.ps1              (NEW — mirrors setup.ps1/verify.ps1: brings the stack up, runs the test suite)
```

<a id="build-order"></a>
### 5.2 Build Order

1. Stand up the hand-written service (`D1.2-3`) against the existing `1_DatabaseSetup` schema/data — no schema or migration changes needed for this phase.
2. Expose read-only routes for reference/lookup data first (Team, Person, Component) — lowest risk, no auth-sensitive writes yet.
3. Add the authentication seam (`security/deps.py`), with Team/role authorization checked directly in each route as it's built (Project, Task, `task_resource`, `dependency`, `attachment`, `remark`).
4. Implement Attachment upload (`content_hash`/`size_bytes` computed before insert) and the error-mapping (`errors.py`, §4.4) for the three DB-enforced business rules plus ordinary constraint violations.
5. Add the Search, Auth, and Impersonation routes (§4.2).
6. Write and pass the test suite (§6).
7. Publish an API contract (OpenAPI, auto-generated by FastAPI for every route) so the GUI/Web Client phase has something concrete to build against.

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
├── test_authorization.py     — the authorization check in `security/deps.py` actually restricts by Team/role, not just "any valid token"
├── test_crud_reference_data.py   — Team/Person/PersonRole/Component read, admin-gated create/update
├── test_crud_project.py
├── test_crud_task.py
├── test_resource_assignment.py
├── test_dependency_rules.py  — includes the cycle-rejection case (§6.4)
├── test_attachment_rules.py  — includes the dedup-rejection case
├── test_remark_rules.py      — includes the immutability case
├── test_search.py
├── test_impersonation.py     — admin can; non-admin can't (`Q1.3-1`'s eventual answer plugs in here)
└── test_journey.py           — the end-to-end scenario (§6.3)
```

`V2/scripts/test-api.ps1` (§5.1) is what actually runs this: bring the stack up if it isn't already, then `pytest V2/rest-api/tests`.

<a id="test-categories"></a>
### 6.3 Test Categories

- **CRUD happy paths** — one create/read/update/delete cycle per in-scope resource (§2.1).
- **Authorization** — a request with no token, an expired token, and a token for a Person without the right Team role are all rejected; a request with a valid token and the right role succeeds.
- **Business-rule surfacing** (§4.4) — each of the three DB-enforced rules produces a clean 4xx, not a raw Postgres error.
- **Impersonation** — an org-admin can mint a token for another Person and act as them; a non-admin attempting to impersonate is rejected (`Q1.3-1` will settle exactly who "admin" means here).
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


def test_remark_cannot_be_edited(api, alice_token, existing_remark_id):
    resp = api.patch(
        f"/remark/{existing_remark_id}",
        json={"remark_text": "edited"},
        headers=auth(alice_token),
    )
    assert resp.status_code in (403, 405)


def test_duplicate_attachment_is_rejected(api, alice_token, task_id, sample_file):
    first = api.post(f"/task/{task_id}/attachments", files=sample_file, headers=auth(alice_token))
    assert first.status_code == 201

    second = api.post(f"/task/{task_id}/attachments", files=sample_file, headers=auth(alice_token))
    assert second.status_code == 409


def test_request_without_token_is_rejected(api):
    resp = api.get("/task")
    assert resp.status_code == 401


def test_non_admin_cannot_impersonate(api, bob_token, alice_person_id):
    # bob_token belongs to a Person without is_organisation_admin.
    resp = api.post(f"/auth/impersonate/{alice_person_id}", headers=auth(bob_token))
    assert resp.status_code == 403


def test_admin_can_impersonate_and_act_as_target(api, admin_token, alice_person_id):
    resp = api.post(f"/auth/impersonate/{alice_person_id}", headers=auth(admin_token))
    assert resp.status_code == 200
    impersonated_token = resp.json()["token"]

    whoami = api.get("/auth/whoami", headers=auth(impersonated_token))
    assert whoami.json()["person_id"] == alice_person_id
    assert whoami.json()["impersonated_by"] is not None
```

<a id="definition-of-success"></a>
## 7. Definition of Success

For Level 1, this phase is done when:

- Every endpoint in §2.1 exists, is covered by the test categories in §6.3, and the test suite passes.
- The three DB-enforced business rules are surfaced as clean errors (§4.4), verified by tests, not just manually checked once.
- Every endpoint requires and authorizes off a JWT (§4.3) — even before Phase 3 provides real login, the seam is real and tested via the stub issuer.
- Impersonation works end-to-end and is restricted to the intended role (once `Q1.3-1` settles which).
- The GUI/Web Client phase (Phase 5) can be built entirely against this API, with no direct database access from the client — proving the API-first foundational decision actually holds in practice, not just on paper.
- An API contract (§5.2 step 7) exists for the GUI phase to build against.

Explicitly **not** required for success: any Urgency computation (`D1.2-2` — this is the GUI's job, see `5_GuiClient/Plan.md`), a dedicated plan-view endpoint (§2.2), or anything on the deferred list.

<a id="open-questions"></a>
## 8. Open Questions (Phase-Specific)

None currently open — see §9.

<a id="decisions"></a>
## 9. Decisions (Phase-Specific)

- **D1.2-1** (decided 2026-08-21)<br>
  **Question:** Architecture — PostgREST-only (§3.1) vs. PostgREST + hand-written service (§3.2)?<br>
  **Decision:** Option B, the hybrid. PostgREST serves the CRUD surface (§4.1) — a mature, production-grade tool in its own right, not a shortcut, so this half of the choice isn't in question. The hand-written service handles login, impersonation, and search (§4.2), and is deliberately designed as a *permanent* identity/auth/gateway layer from Level 1 onward, not Level-1-only scaffolding: Level 2/3's database-per-tenant architecture (`Requirements/Goals.md` §3.2) needs an application-layer request router that resolves "which tenant → which database" before a request reaches Postgres, and PostgREST has no way to do that — a given PostgREST instance is wired to one connection string. Option A (everything as Postgres/PL/pgSQL functions) would have no natural place to grow into that routing role, meaning it wouldn't just be login code getting rebuilt at Level 2/3, it would be an entirely new architectural layer bolted on afterward. Building the service now, even though at Level 1 it only has auth and search to do, means Level 2/3 extends an existing piece rather than introduces one. This also echoes `D1-2`'s reasoning: auth is the piece most likely to be rebuilt for Level 2/3 (real OIDC/Google sign-in), and belongs in ordinary application code with mature libraries, not PL/pgSQL, for the same reason `D1-2` chose named users over shared login.<br>
  **Superseded by:** `D1.2-3`.
- **D1.2-2** (decided 2026-08-22)<br>
  **Question:** Where is Urgency (`Requirements/KeyConcepts.md` §12) computed — client-side in the GUI from raw Task fields, or server-side in this API?<br>
  **Decision:** client-side in the GUI, computed from Task/Project fields this API's resource endpoints (§4.1) already expose. `Requirements/KeyConcepts.md` §12 already frames Urgency as a presentation-layer calculation; computing it in the GUI is the most literal reading of that — more so than in this API, which is a data/service layer, not presentation — and it keeps this API's job limited to serving the raw stored facts Urgency is derived from, not derived values themselves. It's also naturally suited to a likely later refinement: team-specific configurable weights for the Urgency algorithm (not Level 1 — see `Claude/Level2_Implementation/Scope.md`), which fits more naturally as GUI-side configuration than a server-side per-Team lookup on every read. This phase needs no Task-endpoint wrapping as a result — see `5_GuiClient/Plan.md`, which owns Urgency computation end to end.
- **D1.2-3** (decided 2026-08-22)<br>
  **Question:** Revisiting `D1.2-1` — should PostgREST still serve the CRUD surface, now that laying out the concrete shape of this phase (§5–§6) has surfaced what that actually requires?<br>
  **Decision:** no — Option C (§3.3), one hand-written service for everything, no PostgREST. Working through the repository layout and test plan under Option B surfaced two problems severe enough to reopen it: (1) attachment upload needs `content_hash`/`size_bytes` computed before insert, which plain PostgREST CRUD can't do, and which the design never actually assigned to either system; (2) `Team`/`Person`/`PersonRole`'s "read for everyone, admin-gated write" rule doesn't fit PostgREST's Row-Level Security model the way the Team-scoped tables do, and nothing was implementing it either. Both trace to the same root cause: roughly half the "plain CRUD" surface (Dependency, Attachment, Remark, plus the differently-shaped Team/Person/PersonRole rules) already needed hand-written logic, which undercuts PostgREST's core value (skip writing the boring parts) enough that one service for everything is now simpler overall — not just architecturally tidier, genuinely less total work, since it also removes the RLS migration, PostgREST's JWT-claims wiring, and the path-based routing `4_HttpsReverseProxy` would otherwise have needed. `D1.2-1`'s reasoning about the service being a permanent identity/gateway layer (not Level-1-only scaffolding) is unaffected — it now just does more from day one.

See `../ImplementationPlan.md` for how this phase fits into the Level 1 plan, and for open questions that span this phase and others.
