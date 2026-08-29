# ProjectPal V2 — Authentication Phase Plan

## Contents

1. [Status and Purpose](#status-and-purpose)
2. [Scope for Level 1](#scope)
   - 2.1 [In Scope](#in-scope)
   - 2.2 [Deferred Out of Level 1](#deferred)
3. [Design](#design)
   - 3.1 [Password Hashing](#password-hashing)
   - 3.2 [Schema Changes](#schema-changes)
   - 3.3 [Login Endpoint](#login-endpoint)
   - 3.4 [Password-Setting Endpoint](#password-endpoint)
   - 3.5 [Token Lifecycle](#token-lifecycle)
4. [Test Data — Seeded Passwords](#seeded-passwords)
5. [Implementation Plan](#implementation-plan)
   - 5.1 [Files Touched](#files-touched)
   - 5.2 [Build Order](#build-order)
6. [Testing](#testing)
   - 6.1 [Approach](#testing-approach)
   - 6.2 [Test Categories](#test-categories)
   - 6.3 [Manual Testing](#manual-testing)
7. [Definition of Success](#definition-of-success)
8. [Open Questions (Phase-Specific)](#open-questions)
9. [Decisions (Phase-Specific)](#decisions)
10. [Implementation Outcome Summary](#implementation-outcome-summary)

<a id="status-and-purpose"></a>
## 1. Status and Purpose

**Status:** Done.

Replace `2_RestApi`'s stub login with real password verification, per `D1-2` (`../ImplementationPlan.md`): each Person authenticates individually with their own password, and the API issues a JWT carrying `person_id`, Team/role memberships, and `is_organisation_admin`. `2_RestApi/Plan.md` §4.3 deliberately built the seam so this phase only needs to change what happens *inside* `POST /auth/login` — every other endpoint already authorizes off the JWT's claims, not off how the token was obtained, so nothing else in the API changes shape.

`D1-2` also raised an admin-only impersonation capability; `D1.3-1`/`D1.3-2` (§9) already found Level 1 needs neither that nor an audit trail, and this phase doesn't revisit either.

<a id="scope"></a>
## 2. Scope for Level 1

<a id="in-scope"></a>
### 2.1 In Scope

- Real password verification in `POST /auth/login`, replacing the stub (`D1.3-5`, §3.1/§3.3).
- The schema change real passwords need: a `password_hash` column, and a uniqueness guarantee on `external_login` (`D1.3-3`, §3.2).
- A way for a Person to actually get a password: admin-set only for Level 1 (`D1.3-4`, §3.4).
- Every seeded Person primed with a working password, as if an admin had already set one (`D1.3-4`, §4) — so the Demonstrator's test accounts are usable immediately.
- Making the JWT's lifetime configurable rather than a hardcoded constant (`D1.3-6`, §3.5).

<a id="deferred"></a>
### 2.2 Deferred Out of Level 1

- **Self-service password change** (a Person changing their own password) — `D1.3-4`. Required eventually; tracked in `Claude/Level2_Implementation/Scope.md`.
- **Failed-login lockout/rate-limiting** — `D1.3-7`. Consistent with `Scope.md`'s "security is not the top concern" framing; tracked in `Claude/Level2_Implementation/Scope.md` alongside that Level's "real security baseline" item.
- **Impersonation and audit logging** — already settled as out of scope by `D1.3-1`/`D1.3-2`; unaffected by this phase.
- **Real external identity provider (OIDC/Google sign-in)** — `Requirements/Goals.md`'s Level 2 "identity, for real" framing question (`Q-G-9`); Level 1 stays with locally-stored passwords.

<a id="design"></a>
## 3. Design

<a id="password-hashing"></a>
### 3.1 Password Hashing

Argon2id, via the `argon2-cffi` library, using its own default parameters (time cost 3, 64 MiB memory, parallelism 4) rather than hand-tuned ones — Level 1 has no specific throughput/latency target to tune against. See `D1.3-5` (§9) for why this replaces the `bcrypt` dependency `2_RestApi` had stubbed into `requirements.txt` ahead of time.

A new `app/security/passwords.py` module owns this end to end:
- `hash_password(password: str) -> str` — used by the password-setting endpoint (§3.4) and by seeding test data (§4).
- `verify_password(password: str, password_hash: str) -> bool` — used by login (§3.3); returns `False` on any mismatch or malformed-hash error rather than raising, so callers don't need to know `argon2`'s exception types.

<a id="schema-changes"></a>
### 3.2 Schema Changes

Edited directly into `V2/database/migrations/001_initial_schema.sql` (no incremental migration — nothing is deployed anywhere yet, per `Claude/Guidelines/ImplementationApproach.md` §5):

- `person.password_hash text` — nullable. A Person can validly exist with no password yet (e.g. a resource-only record that never logs in, per `Requirements/DomainModel.md`'s Person entity), so this isn't `NOT NULL`; `NULL` simply means "can't log in."
- `UNIQUE` constraint on `person.external_login` — Postgres treats multiple `NULL`s as distinct under a `UNIQUE` constraint, so Person records with no login identifier at all are unaffected.

<a id="login-endpoint"></a>
### 3.3 Login Endpoint

`POST /auth/login`'s request body gains a `password` field. Behaviour:
1. Look up the Person by `external_login`.
2. If no match, the Person is inactive, or `password_hash` is `NULL` (no password ever set) — reject.
3. Otherwise verify `password` against `password_hash` (§3.1) — reject on mismatch.
4. All three rejection cases return the *same* generic `401` (`"Invalid credentials"`) — deliberately not distinguishing "unknown login" from "wrong password" from "no password set," so the endpoint can't be used to enumerate which logins exist.
5. On success, issue a JWT exactly as the stub already did (`person_id`, Team/role memberships, `is_organisation_admin`) — nothing downstream of a successful login changes.

<a id="password-endpoint"></a>
### 3.4 Password-Setting Endpoint

`POST /person/{person_id}/password` (in `routes/teams.py`, alongside the other Person routes) — `is_organisation_admin`-gated, same boundary as every other Person write (`D-DM-4`). Body: `{"new_password": str}`, minimum 8 characters (Pydantic-validated; no further complexity rules for Level 1). Hashes via `passwords.hash_password` and overwrites `password_hash`. Deliberately a separate endpoint from `PATCH /person/{id}` rather than a field on it — keeps password-setting a distinct, auditable-later action, and means the general-purpose Person update endpoint has no path to touching credentials at all.

<a id="token-lifecycle"></a>
### 3.5 Token Lifecycle

Stays an 8-hour, non-refreshable JWT — "log in again" on expiry remains acceptable for Level 1 (`D1.3-6`). The lifetime moves from a hardcoded constant to a `JWT_TTL_SECONDS` environment variable in `app/config.py` (default: 8 hours, i.e. `28800`), following the same pattern as `JWT_SECRET`, so it can be tuned per environment without a code change.

<a id="seeded-passwords"></a>
## 4. Test Data — Seeded Passwords

Every seeded Person (`V2/database/seed/001_example_data.sql`) gets a working password, primed as if an admin had already set it (`D1.3-4`). These are fictional Level 1 demonstrator accounts on a database that only exists locally — not real credentials — so, per `D1.3-8` (§9), the plaintext values are committed openly (as a comment beside the hash it produced in the seed file, and directly in the REST API test suite's fixtures, which need the real plaintext to exercise real login):

| Person | `external_login` | Password |
|---|---|---|
| Alice Chen | `alice.chen@example.com` | `alice-pass1` |
| Ben Okafor | `ben.okafor@example.com` | `ben-pass1` |
| Priya Sharma | `priya.sharma@example.com` | `priya-pass1` |
| Tom Baxter | `tom.baxter@example.com` | `tom-pass1` |
| Grace Liu | `grace.liu@example.com` | `grace-pass1` |
| Sam Patel | `sam.patel@example.com` | `sam-pass1` |
| Nadia Fischer | `nadia.fischer@example.com` | `nadia-pass1` |

<a id="implementation-plan"></a>
## 5. Implementation Plan

<a id="files-touched"></a>
### 5.1 Files Touched

```
V2/
├── database/
│   ├── migrations/001_initial_schema.sql   (edited — §3.2)
│   └── seed/001_example_data.sql           (edited — password_hash values, §4)
├── .env.example                            (edited — optional JWT_TTL_SECONDS)
├── docker-compose.yml                      (edited — passes JWT_TTL_SECONDS through)
└── rest-api/
    ├── requirements.txt                    (edited — argon2-cffi replaces bcrypt)
    └── app/
        ├── config.py                       (edited — JWT_TTL_SECONDS from environment)
        ├── security/
        │   └── passwords.py                (NEW — hash_password/verify_password, §3.1)
        └── routes/
            ├── auth.py                     (edited — real verification, §3.3)
            └── teams.py                    (edited — POST /person/{id}/password, §3.4)
    └── tests/
        ├── conftest.py                     (edited — login fixtures send real passwords)
        └── test_auth.py                    (edited — wrong-password/no-password cases, set-password flow)
```

<a id="build-order"></a>
### 5.2 Build Order

1. Schema: add `password_hash` and the `external_login` uniqueness constraint (§3.2).
2. Swap `bcrypt` for `argon2-cffi` in `requirements.txt`; add `security/passwords.py` (§3.1).
3. Add `POST /person/{person_id}/password` to `teams.py` (§3.4).
4. Replace `auth.py`'s stub login body with real verification (§3.3).
5. Make `JWT_TTL_SECONDS` configurable (`config.py`, `docker-compose.yml`, `.env.example`) (§3.5).
6. Prime seed data with every seeded Person's password hash (§4).
7. Update `tests/conftest.py`'s login fixtures to send real passwords; extend `test_auth.py` with the cases in §6.2.
8. Rebuild the dev database from scratch (`.\scripts\reset.ps1` then `.\scripts\setup.ps1`) and re-run the full suite (`.\scripts\test-api.ps1`) to confirm nothing in `2_RestApi`'s existing 44 tests regressed.
9. Regenerate and republish `rest-api/openapi.json`.
10. Update `V2/README.md` §9 to describe real login instead of the stub, pointing at §4 above for test credentials.

<a id="testing"></a>
## 6. Testing

<a id="testing-approach"></a>
### 6.1 Approach

Same HTTP-level integration testing against the live Docker stack as `2_RestApi/Plan.md` §6.1 — extends the existing suite rather than starting a new one, since `2_RestApi`'s 44 tests already cover everything downstream of a successful login.

<a id="test-categories"></a>
### 6.2 Test Categories

- **Correct password succeeds** — logging in with a seeded Person's real password (§4) returns a usable token, same shape as before.
- **Wrong password is rejected** — with the same generic `401`/message as an unknown `external_login`, not a distinguishable one (§3.3 point 4).
- **A Person with no password set can't log in** — `password_hash IS NULL` behaves like a wrong password, not a different error.
- **Admin can set a Person's password, and they can then log in with it** — the full `POST /person/{id}/password` → `POST /auth/login` round trip.
- **A non-admin can't set anyone's password**, including their own (self-service is deferred, §2.2) — rejected the same way any other admin-only Person write already is.

<a id="manual-testing"></a>
### 6.3 Manual Testing

The Swagger UI walkthrough in `2_RestApi/Plan.md` §6.5 still applies as-is for making sure the stack is running, the `Authorize` button mechanics, and `GET /auth/whoami` — nothing about that changed. What's different now: `POST /auth/login` needs a real `password`, and there's a new password-management endpoint worth trying.

**1. Log in with a real password.** `POST /auth/login`'s body now needs both fields, e.g.:
```json
{"external_login": "alice.chen@example.com", "password": "alice-pass1"}
```
Every seeded Person has a working password — see §4 for the full list. Try an unknown `external_login` and a known one with the wrong password side by side: both return the same generic `401 Invalid credentials` (§3.3) — that indistinguishability is deliberate, not a bug to report.

**2. Try the admin-only password-set endpoint.** Authorized as an admin (Alice or Nadia, §4), call `POST /person/{person_id}/password` with `{"new_password": "a-new-password1"}` (minimum 8 characters) for some other Person. Then log in again as that Person with the new password to confirm it actually took effect.

**3. Confirm self-service is still rejected.** Authorized as a non-admin (e.g. Ben, `person_id` 2), try `POST /person/2/password` — i.e. Ben setting *his own* password. Still `403`: self-service is deferred (§2.2) even for your own account, not just for other People's.

<a id="definition-of-success"></a>
## 7. Definition of Success

For Level 1, this phase is done when:

- `password_hash` and the `external_login` uniqueness constraint exist in the schema.
- `POST /auth/login` verifies a real password via Argon2id; every rejection reason produces the same generic `401`.
- `POST /person/{id}/password` lets an `is_organisation_admin` set or reset any Person's password; rejected for anyone else.
- Every seeded Person has a working password (§4), and this is documented for local testing.
- `JWT_TTL_SECONDS` is configurable via environment variable, defaulting to 8 hours.
- `2_RestApi`'s full test suite, plus the new cases in §6.2, all pass against the rebuilt stack.
- Impersonation and audit-trail scope are unchanged (`D1.3-1`/`D1.3-2`).

<a id="open-questions"></a>
## 8. Open Questions (Phase-Specific)

None currently open — see Decisions below.

<a id="decisions"></a>
## 9. Decisions (Phase-Specific)

- **D1.3-1** (decided 2026-08-23)<br>
  **Question:** Which role is authorized to impersonate another Person — `is_organisation_admin`, or a narrower dedicated permission (e.g. reintroducing something like the old app's `SuperUser` role)?<br>
  **Decision:** moot for Level 1 — no dedicated impersonation mechanism is needed at all. Each Person already has their own login username/password (`D1-2`); verifying what another Person can/can't see and do is achieved by logging in as that Person directly with their own credentials, not by a separate admin-minted token. This revises `D1-2`'s "admin-only impersonation capability" — that specific capability isn't built for Level 1; `D1-2`'s core (named users, password-based JWT auth) is unaffected.
- **D1.3-2** (decided 2026-08-23)<br>
  **Question:** Does Level 1 need an audit trail of actions taken while impersonating (who was impersonated, by whom, when), or is that reasonable to defer to Level 2/3 given `Scope.md`'s "security is not the top concern" framing?<br>
  **Decision:** no audit trail of any kind is required for Level 1 — not just for the now-moot impersonation case (`D1.3-1`), but generally. If a later Level needs one, it's designed fresh at that point, not retrofitted from partial Level 1 logging.
- **D1.3-3** (decided 2026-08-29)<br>
  **Question:** What schema changes does real password-based login need?<br>
  **Decision:** add a nullable `password_hash text` column to `person`, and a `UNIQUE` constraint on `external_login` — see §3.2 for why each is shaped that way.
- **D1.3-4** (decided 2026-08-29)<br>
  **Question:** How does a Person actually get a password — admin-set, self-service, or both — for Level 1?<br>
  **Decision:** admin-set only, via `POST /person/{id}/password` (§3.4); self-service is required eventually but deferred (`Claude/Level2_Implementation/Scope.md`). Seed data primes every seeded Person with a password as if an admin had already set one, so the Demonstrator's test accounts work immediately — see §4 for the actual passwords.
- **D1.3-5** (decided 2026-08-29)<br>
  **Question:** `bcrypt` was already stubbed into `2_RestApi/requirements.txt` ahead of real login being built — is it still the right choice, or should this phase use something else?<br>
  **Decision:** Argon2id, via `argon2-cffi`, not `bcrypt`. OWASP's current password-storage guidance ranks Argon2id first — it's memory-hard and resists GPU/ASIC cracking better than `bcrypt`'s fixed, comparatively small memory footprint — with `bcrypt` as the fallback recommendation when Argon2 isn't available, which isn't the case here (a well-maintained Python binding exists, and it needs a compiled native extension either way, so it's not a heavier dependency than `bcrypt` was). No reason found to prefer `bcrypt` instead.
- **D1.3-6** (decided 2026-08-29)<br>
  **Question:** Is an 8-hour, non-refreshable JWT acceptable for Level 1, and should its lifetime be configurable?<br>
  **Decision:** yes to both — 8 hours with "log in again" on expiry remains acceptable (no refresh-token mechanism this phase), but the lifetime moves from a hardcoded constant to a `JWT_TTL_SECONDS` environment variable (default 8 hours) so it can be tuned per environment without a code change.
- **D1.3-7** (decided 2026-08-29)<br>
  **Question:** Does Level 1 need lockout or rate-limiting after repeated failed login attempts?<br>
  **Decision:** no — deferred, consistent with `D1.3-2`'s "no audit trail" framing and `Scope.md`'s "security is not the top concern." Tracked in `Claude/Level2_Implementation/Scope.md` alongside that Level's "real security baseline" item.
- **D1.3-8** (decided 2026-08-29)<br>
  **Question:** Seed data now needs real passwords for every seeded Person (`D1.3-4`) — should the plaintext values be committed to the repository, or kept out of version control?<br>
  **Decision:** commit them — in the seed SQL (as a comment beside the hash it produced) and in the REST API test suite's fixtures, which need the real plaintext to exercise real login. These are fictional Level 1 demonstrator accounts on a database that only exists on a developer machine, not real credentials — unlike `JWT_SECRET`/`POSTGRES_PASSWORD` (real per-install secrets, which stay out of git via `.env`), there's nothing here that protects anything real.

See `../ImplementationPlan.md` for how this phase fits into the Level 1 plan, and for open questions that span this phase and others.

<a id="implementation-outcome-summary"></a>
## 10. Implementation Outcome Summary

**What was implemented:** exactly the §2.1 scope — `person.password_hash` (nullable) and a `UNIQUE` constraint on `person.external_login` (edited directly into `001_initial_schema.sql`, `D1.3-3`); `app/security/passwords.py` (Argon2id hash/verify, `D1.3-5`); `POST /auth/login` now verifies a real password with a single generic `401` for every rejection reason (unknown login, inactive, no password set, wrong password); `POST /person/{id}/password` (admin-gated, minimum 8 characters); `JWT_TTL_SECONDS` moved from a hardcoded constant to a configurable environment variable, defaulting to 8 hours (`D1.3-6`); every seeded Person primed with a working password (§4, `D1.3-4`); `bcrypt` replaced by `argon2-cffi` in `requirements.txt`. Nothing here was dropped or descoped from §2.1.

**Testing:** the dev database was rebuilt from scratch (`docker compose down -v`, `.\scripts\setup.ps1`) against the updated schema and seed data, and the full test suite — `2_RestApi`'s existing 44 tests plus 7 new ones covering §6.2's categories (correct/wrong password, no-password-set, admin set-password round trip, non-admin rejected including for their own account, minimum-length validation) — all 51 pass via `.\scripts\test-api.ps1`, confirmed re-runnable against the persistent dev database without a reset. Manually verified end to end afterwards too (real login, wrong-password rejection, `/auth/whoami`).

**Issues that arose:**
- `person.external_login` becoming `UNIQUE` meant several existing tests that created People with a fixed `external_login` (or a fixed `team.name`, from `2_RestApi`'s suite) would collide on a second run against the persistent dev database. Same class of issue `2_RestApi` already hit once with attachment content and team names — fixed the same way, with `uuid`-based uniqueness per test run.
- No application bugs surfaced beyond that test-isolation pattern repeating itself.

**Further consideration:**
- Self-service password change and failed-login lockout/rate-limiting remain deferred, per `D1.3-4`/`D1.3-7` — both tracked in `Claude/Level2_Implementation/Scope.md`.
- The Argon2 parameters used are the library's own defaults (§3.1) — worth revisiting only if Level 2/3's real hosting environment has a specific latency/throughput budget to tune against; no reason to before then.
- `POST /person/{id}/password` has no rate-limiting either, consistent with `D1.3-7`, but worth remembering it's the one write endpoint that changes what "being able to log in as someone" means — a natural first candidate if any lightweight audit logging is ever added, despite `D1.3-2`'s "none for Level 1."
