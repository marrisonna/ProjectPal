# ProjectPal V2 — Level 1 Local Stack

## Contents

1. [What This Is](#what-this-is)
2. [First-Time Setup](#first-time-setup)
3. [Everyday Use](#everyday-use)
4. [Verifying the Install](#verifying)
5. [Connecting a GUI Tool](#connecting-a-gui-tool)
6. [Trying Out the Business Rules](#trying-out-the-business-rules)
7. [Resetting / Rebuilding](#resetting)
8. [Adding a New Migration](#adding-a-migration)
9. [REST API](#rest-api)
10. [GUI / Web Client](#gui-client)

See [`InitialDatabaseSetupPlan.md`](../Claude/Level1_Implementation/1_DatabaseSetup/InitialDatabaseSetupPlan.md) and [`2_RestApi/Plan.md`](../Claude/Level1_Implementation/2_RestApi/Plan.md) for the full plans (design rationale, decisions, what's out of scope). This document is just the how-to.

<a id="what-this-is"></a>
## 1. What This Is

A PostgreSQL database plus a REST API fronting it (§9), both running in Docker on this PC, implementing the Level 1 schema from `Claude/Requirements/DomainModel.md` with a small example dataset loaded. Login uses real password verification (`Claude/Level1_Implementation/3_Authentication/Plan.md`) — see §9 for seeded test credentials. A browser-based GUI (§10) is under construction outside Docker, in `gui-client/`.

<a id="first-time-setup"></a>
## 2. First-Time Setup

1. **Install Docker Desktop**: <https://www.docker.com/products/docker-desktop/>. Accept the WSL2 backend prompt if asked. Reboot if the installer asks you to, then start Docker Desktop and wait for it to say it's running.
2. **Open a PowerShell prompt in this folder** (`V2`).
3. **Create your `.env` file**:
   ```powershell
   Copy-Item .env.example .env
   ```
   Edit `.env` and change `POSTGRES_PASSWORD` and `JWT_SECRET` to real values (any local values are fine — nothing here is exposed beyond your own machine yet).
4. **Run setup**:
   ```powershell
   .\scripts\setup.ps1
   ```
   This starts the `db` container, waits for PostgreSQL to be ready, applies the schema (`database/migrations/001_initial_schema.sql`), and loads the example data (`database/seed/001_example_data.sql`). It'll take a minute or two the first time while Docker downloads the `postgres:16` image. Run `docker compose up -d --build` afterwards (or `.\scripts\test-api.ps1`, §9) to also build and start the REST API.
5. **Verify**:
   ```powershell
   .\scripts\verify.ps1
   ```
   You should see row counts for every table (11 tasks, 7 people, etc.) followed by a few readable reports. If anything errors, see the troubleshooting note at the end of this section.

If you'd rather load the schema without the example data (e.g. to start from a genuinely empty database), run `.\scripts\setup.ps1 -SkipSeed` instead.

**If something goes wrong**: the most common cause is Docker Desktop not actually running yet (`setup.ps1` will say `docker compose up failed`) — start Docker Desktop from the Start menu, wait for its whale icon to stop animating, and re-run the script. `docker compose logs db` (from this folder) shows PostgreSQL's own log output if you need to dig further.

<a id="everyday-use"></a>
## 3. Everyday Use

- **Start the database** (after the first-time setup above): `docker compose up -d`
- **Stop it** (keeps your data): `docker compose down`
- **Stop it and view logs**: `docker compose logs -f db`
- The data persists in a Docker volume between `docker compose up`/`down` cycles — it's only deleted by `.\scripts\reset.ps1` (§7).

<a id="verifying"></a>
## 4. Verifying the Install

`.\scripts\verify.ps1` re-runs `database/verify/smoke_test.sql` at any time — safe to run repeatedly, it's read-only. Use it after every `setup.ps1` run, and any time you want a quick readable dump of what's in the database.

<a id="connecting-a-gui-tool"></a>
## 5. Connecting a GUI Tool

If you installed DBeaver or pgAdmin (`InitialDatabaseSetupPlan.md` §2), connect with:

| Setting | Value |
|---|---|
| Host | `localhost` |
| Port | `5432` |
| Database | value of `POSTGRES_DB` in your `.env` (default `projectpal`) |
| User | value of `POSTGRES_USER` in your `.env` (default `projectpal`) |
| Password | value of `POSTGRES_PASSWORD` in your `.env` |

All the tables live in the `projectpal` schema (not the default `public` schema) — most GUI tools show this as a expandable node under the database.

<a id="trying-out-the-business-rules"></a>
## 6. Trying Out the Business Rules

Three rules from `DomainModel.md` are enforced by the database itself (`InitialDatabaseSetupPlan.md` §5). Worth trying by hand once, e.g. via `docker compose exec db psql -U projectpal -d projectpal` (adjust the username if you changed it):

- **Dependency cycles are rejected**: task 1 already depends-on task 2 in the example data (`INSERT INTO dependency (pre_task_id, post_task_id) VALUES (1, 2);`). Try the reverse — `INSERT INTO dependency (pre_task_id, post_task_id) VALUES (2, 1);` — and it should fail with `This dependency would create a cycle`.
- **A Remark's authorship can't be reassigned**: `UPDATE remark SET created_by_person_id = 3 WHERE remark_id = 1;` should fail with `A Remark's authorship cannot be reassigned`. Editing `remark_text` itself, or deleting a Remark, is allowed at the database level — e.g. `UPDATE remark SET remark_text = 'edited' WHERE remark_id = 1;` succeeds. It's the API's job (not built yet) to check the caller is actually that Remark's own owner (or a TeamLeadUser, for delete) before allowing it.
- **Duplicate attachments are rejected**: re-running the `schema-draft.sql` `INSERT` from `database/seed/001_example_data.sql` a second time (same task, same content) should fail on the unique index, since the content hash would match the copy that's already there.

<a id="resetting"></a>
## 7. Resetting / Rebuilding

`.\scripts\reset.ps1` stops the container and **deletes the database volume** (asks for confirmation first). Afterwards, `.\scripts\setup.ps1` rebuilds everything from scratch — this is the way to pick up schema changes that can't be applied as a simple additive migration, or just to get back to a clean example dataset.

<a id="adding-a-migration"></a>
## 8. Adding a New Migration

As `DomainModel.md` evolves, add a new file `database/migrations/002_<description>.sql` (never edit `001_initial_schema.sql` once it's been applied anywhere) containing just the incremental change — new table, new column, new constraint. `.\scripts\setup.ps1` applies every migration file in `database/migrations/` in filename order, so numbering them sequentially keeps the history honest. This is the same pattern `DataBaseHostingOptions.md` recommends for the eventual hosted version, so nothing about this habit needs to change later.

**Until this stack is deployed anywhere outside this development environment**, per `Claude/Guidelines/ImplementationApproach.md` §5, there's no live data to preserve — it's simpler to edit `001_initial_schema.sql` directly and rebuild (`.\scripts\reset.ps1` then `.\scripts\setup.ps1`) than to add a new migration file for a change that was never actually deployed. Switch to real incremental migrations once that's no longer true.

<a id="rest-api"></a>
## 9. REST API

The hand-written FastAPI service in `rest-api/` (`Claude/Level1_Implementation/2_RestApi/Plan.md`) — every route requires a Bearer JWT and authorizes off its claims, Team-scoped per `D-UC-4`. `docker compose up -d --build` builds and starts it alongside the database; it listens on `http://127.0.0.1:8000` (also bound to localhost only). Interactive API docs are at `http://127.0.0.1:8000/docs` once it's running.

**Logging in** requires a real password (`3_Authentication/Plan.md`): `POST /auth/login` with `{"external_login": "...", "password": "..."}` returns a JWT on success. Use it as `Authorization: Bearer <token>` on every other request; it expires after `JWT_TTL_SECONDS` (default 8 hours, configurable in `.env`). Every seeded Person has a working password, primed as if an admin had already set one — see `3_Authentication/Plan.md` §4 for the full list, or `rest-api/tests/conftest.py`'s `PASSWORDS` dict:

| `external_login` | Password |
|---|---|
| `alice.chen@example.com` | `alice-pass1` |
| `ben.okafor@example.com` | `ben-pass1` |
| `priya.sharma@example.com` | `priya-pass1` |
| `tom.baxter@example.com` | `tom-pass1` |
| `grace.liu@example.com` | `grace-pass1` |
| `sam.patel@example.com` | `sam-pass1` |
| `nadia.fischer@example.com` | `nadia-pass1` |

That table is Team 1 ("Platform"). Team 2 ("V1.2 Import", `database/seed/002_team2_from_v1.sql`) is a much larger dataset — 43 People migrated from the real old V1.2 database — but only 10 of them have a login at all; see that file's own header comment for the full `external_login`/password list. `neil@example.com` / `neil-pass1` is Team 2's TeamLeadUser and the only organisation admin among the imported People.

An admin (`is_organisation_admin`) can set or reset any Person's password via `POST /person/{person_id}/password` — self-service (changing your own) is deferred, see `3_Authentication/Plan.md` §2.2.

**Running the test suite**: `.\scripts\test-api.ps1` builds and starts the stack, waits for the API to respond, and runs `rest-api/tests` (creating a Python virtualenv at `rest-api/.venv-test` the first time). The tests run against the same persistent dev database everything else uses, not a throwaway one — they're written to be safely re-runnable (unique names/content per run) rather than assuming a clean slate.

See `Claude/Level1_Implementation/2_RestApi/Plan.md` §2.1 for the full endpoint list, and `rest-api/openapi.json` (or `/docs`) for the generated API contract.

<a id="gui-client"></a>
## 10. GUI / Web Client

The React/TypeScript/Vite web app in `gui-client/` (`Claude/Level1_Implementation/4_GuiClient/Plan.md`), currently Stage 1 (Foundation) — login, routing, the typed API client, and a placeholder dashboard. Not part of `docker-compose.yml` yet; run it separately:

```powershell
cd gui-client
npm install
Copy-Item .env.example .env.local   # first time only
npm run dev
```

Then open <http://localhost:5173> and log in with any seeded Person (§9's table). The dev server proxies `/api/*` to the REST API on `:8000` (`vite.config.ts`), the same relative path Caddy will use in production (`6_HttpsReverseProxy/Plan.md` `D1.6-4`) — so this needs the REST API already running (`docker compose up -d`) but talks to it without any CORS configuration on the API side.

**Regenerating the typed API client** after a REST API contract change: `npm run gen-api` (regenerates `src/api/schema.d.ts` from `../rest-api/openapi.json`).

**Building for production**: `npm run build` — output is a static bundle in `dist/`, including the PWA manifest and service worker (`D1.4-5`), ready for `6_HttpsReverseProxy` to serve as-is once that phase exists.
