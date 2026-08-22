# ProjectPal V2 — REST API Phase Plan

## Contents

1. [Status and Purpose](#status-and-purpose)
2. [Scope for Level 1](#scope)
   - 2.1 [Endpoints In Scope](#in-scope)
   - 2.2 [Deferred Out of Level 1](#deferred)
3. [Architecture](#architecture)
   - 3.1 [Option A: PostgREST-Only](#option-a)
   - 3.2 [Option B: PostgREST + Hand-Written Service](#option-b)
   - 3.3 [Current Lean](#architecture-lean)
4. [API Design](#api-design)
   - 4.1 [Resource Endpoints](#resource-endpoints)
   - 4.2 [Custom / RPC Endpoints](#custom-endpoints)
   - 4.3 [Authentication Seam](#auth-seam)
   - 4.4 [Error Conventions](#error-conventions)
5. [Implementation Plan](#implementation-plan)
6. [Testing](#testing)
   - 6.1 [Approach](#testing-approach)
   - 6.2 [Test Categories](#test-categories)
   - 6.3 [Example Tests](#example-tests)
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
- **Auth** — login (Phase 3 builds the real implementation; this phase only needs to agree the seam — see §4.3) and admin-only impersonation (`D1-2`, `O1.3-1`, `O1.3-2`).

<a id="deferred"></a>
### 2.2 Deferred Out of Level 1

- **Merge/Conflict resolution endpoints** — Level 1 assumes a single user at a time, no conflict handling (`Requirements/DomainModel.md` Cross-Cutting Concerns; `Requirements/KeyConcepts.md` Merge/Conflict entry). Nothing to build here.
- **Captured-email attachments** (`Mail` kind) — the schema supports it, but the capture *mechanism* (inbound email processing) is its own integration piece, not part of this phase. Manual upload of `File`/`Link` attachments covers Level 1.
- **A dedicated Gantt/plan-view aggregation endpoint** — `Requirements/UseCases.md`'s View the Plan use case can likely be served by the GUI composing existing Task/Project/Dependency reads rather than a bespoke endpoint. Revisit only if that proves too slow or awkward once the GUI/Web Client phase is underway.
- **Broader admin/support tooling** beyond impersonation (the old app's storage-backend switching, forced re-sync, etc.) — per `Requirements/UseCases.md`'s Administer the System use case, these are tied to the old app's specific architecture and aren't being carried forward.

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

<a id="architecture-lean"></a>
### 3.3 Current Lean

Not yet decided — see `O1.2-1`. Leaning towards Option B, on the same grounds as `D1-2`: auth is the piece most likely to be rebuilt for Level 2/3, and building it in an ordinary application codebase now makes that rebuild a swap, not a rewrite. Where Urgency (`Requirements/KeyConcepts.md` §12) gets computed is a related but separate question — see `O1.2-2`.

<a id="api-design"></a>
## 4. API Design

<a id="resource-endpoints"></a>
### 4.1 Resource Endpoints

The CRUD surface listed in §2.1, one PostgREST-style resource per table (path = table name; filtering via query params, e.g. `GET /task?project_id=eq.5`; related-resource embedding via PostgREST's `select` parameter) regardless of which architecture option is chosen — Option A exposes these directly; Option B exposes the identical shape, just proxied or exposed side-by-side.

<a id="custom-endpoints"></a>
### 4.2 Custom / RPC Endpoints

| Endpoint (Option B shape) | Option A shape | Purpose |
|---|---|---|
| `POST /auth/login` | `POST /rpc/login` | Verify credentials, issue a JWT (`person_id`, Team/role memberships, `is_organisation_admin`) — `D1-2`. |
| `POST /auth/impersonate/{personId}` | `POST /rpc/impersonate` | Admin-only: issue a JWT for the target Person carrying an `impersonated_by` claim — `D1-2`, `O1.3-1`. |
| `GET /search?q=...` | `GET /rpc/search` | Cross-table search over Task/Project/Component/Remark — `Requirements/UseCases.md` Search / Find. |

<a id="auth-seam"></a>
### 4.3 Authentication Seam

Every endpoint expects a `Bearer` JWT and authorizes off its claims (`D1-2`) — this doesn't wait for Phase 3. Phase 2 stands up the seam with a stub token issuer (e.g. a test-only endpoint or fixture that mints a validly-signed JWT for a chosen Person without checking a real password), so authorization logic is real and testable from the start; Phase 3 replaces the stub with real password verification without touching any other endpoint.

<a id="error-conventions"></a>
### 4.4 Error Conventions

The database already enforces three business rules as triggers/constraints (`1_DatabaseSetup`): dependency-cycle rejection, remark immutability, attachment deduplication. Raw Postgres errors are not an acceptable API response — each must be translated to a clean, consistent HTTP error (a stable error code/message shape, not a leaked exception string), so the GUI can show a sensible message rather than a stack trace. Mapping these three cases correctly is part of this phase's job, not an afterthought.

<a id="implementation-plan"></a>
## 5. Implementation Plan

1. Resolve `O1.2-1` (architecture) and `O1.2-2` (Urgency placement).
2. Stand up the chosen architecture against the existing `1_DatabaseSetup` schema/data — no schema changes expected for this phase.
3. Expose read-only endpoints for reference/lookup data first (Team, Person, Component) — lowest risk, no auth-sensitive writes yet.
4. Add the authentication seam (§4.3) and wire per-Team/role authorization into the remaining endpoints (Project, Task, `task_resource`, `dependency`, `attachment`, `remark`).
5. Add the three error-mapping cases (§4.4) and the Search/Auth/Impersonation custom endpoints (§4.2).
6. Write and pass the test suite (§6).
7. Publish an API contract (e.g. OpenAPI) so the GUI/Web Client phase has something concrete to build against.

<a id="testing"></a>
## 6. Testing

<a id="testing-approach"></a>
### 6.1 Approach

HTTP-level integration tests against a running instance (the same `docker compose` stack `1_DatabaseSetup` already established, with the API service added to it), rather than unit-testing PostgREST/PL/pgSQL internals directly — the thing that needs proving is the API's external behaviour, not its implementation. This also means the test suite doesn't need to change shape if `O1.2-1` is answered one way or the other.

<a id="test-categories"></a>
### 6.2 Test Categories

- **CRUD happy paths** — one create/read/update/delete cycle per in-scope resource (§2.1).
- **Authorization** — a request with no token, an expired token, and a token for a Person without the right Team role are all rejected; a request with a valid token and the right role succeeds.
- **Business-rule surfacing** (§4.4) — each of the three DB-enforced rules produces a clean 4xx, not a raw Postgres error.
- **Impersonation** — an org-admin can mint a token for another Person and act as them; a non-admin attempting to impersonate is rejected (`O1.3-1` will settle exactly who "admin" means here).
- **End-to-end journey** — one test walking through a realistic sequence (create Project → create Task → assign a resource → add a Dependency → add a Remark) to prove the pieces work together, not just in isolation.

<a id="example-tests"></a>
### 6.3 Example Tests

Illustrative — assumes Option B's endpoint shapes (§4.2) and a Python/`pytest`/`requests`-style client; the same scenarios apply under Option A against the `/rpc/...` paths instead.

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

- Every endpoint in §2.1 exists, is covered by the test categories in §6.2, and the test suite passes.
- The three DB-enforced business rules are surfaced as clean errors (§4.4), verified by tests, not just manually checked once.
- Every endpoint requires and authorizes off a JWT (§4.3) — even before Phase 3 provides real login, the seam is real and tested via the stub issuer.
- Impersonation works end-to-end and is restricted to the intended role (once `O1.3-1` settles which).
- The GUI/Web Client phase (Phase 5) can be built entirely against this API, with no direct database access from the client — proving the API-first foundational decision actually holds in practice, not just on paper.
- An API contract (§5 step 7) exists for the GUI phase to build against.

Explicitly **not** required for success: a resolved Urgency-computation question (`O1.2-2` can land in either the GUI or this API, decided when Phase 7 is scoped), a dedicated plan-view endpoint (§2.2), or anything on the deferred list.

<a id="open-questions"></a>
## 8. Open Questions (Phase-Specific)

- **O1.2-1:** Architecture — PostgREST-only (§3.1) vs. PostgREST + hand-written service (§3.2)?
- **O1.2-2:** Where is Urgency (`Requirements/KeyConcepts.md` §12) computed — client-side in the GUI from raw Task fields, or server-side in this API? Affects whether Task read endpoints need wrapping later (Phase 7) or can stay pure PostgREST/RPC indefinitely.

<a id="decisions"></a>
## 9. Decisions (Phase-Specific)

None yet. When an open question above is answered, its entry moves here as `D1.2-<N>` (same number, `D` prefix), recording the original question, the decision, and the date.

See `../ImplementationPlan.md` for how this phase fits into the Level 1 plan, and for open questions that span this phase and others.
