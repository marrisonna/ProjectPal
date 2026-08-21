# ProjectPal V2 — Stage 1 Local Database

## Contents

1. [What This Is](#what-this-is)
2. [First-Time Setup](#first-time-setup)
3. [Everyday Use](#everyday-use)
4. [Verifying the Install](#verifying)
5. [Connecting a GUI Tool](#connecting-a-gui-tool)
6. [Trying Out the Business Rules](#trying-out-the-business-rules)
7. [Resetting / Rebuilding](#resetting)
8. [Adding a New Migration](#adding-a-migration)

See [`InitialDatabaseSetupPlan.md`](../Claude/DatabaseSetUp1/InitialDatabaseSetupPlan.md) for the full plan (downloads, configuration, schema design rationale, what's out of scope). This document is just the how-to.

<a id="what-this-is"></a>
## 1. What This Is

A PostgreSQL database, running in Docker on this PC, implementing the Stage 1 schema from `Claude/Requirements/DomainModel.md`, with a small example dataset loaded. Nothing else (API, GUI, auth) is set up yet — see `InitialDatabaseSetupPlan.md` §8.

<a id="first-time-setup"></a>
## 2. First-Time Setup

1. **Install Docker Desktop**: <https://www.docker.com/products/docker-desktop/>. Accept the WSL2 backend prompt if asked. Reboot if the installer asks you to, then start Docker Desktop and wait for it to say it's running.
2. **Open a PowerShell prompt in this folder** (`V2`).
3. **Create your `.env` file**:
   ```powershell
   Copy-Item .env.example .env
   ```
   Edit `.env` and change `POSTGRES_PASSWORD` to a real password (any local password is fine — this database isn't exposed beyond your own machine).
4. **Run setup**:
   ```powershell
   .\scripts\setup.ps1
   ```
   This starts the container, waits for PostgreSQL to be ready, applies the schema (`database/migrations/001_initial_schema.sql`), and loads the example data (`database/seed/001_example_data.sql`). It'll take a minute or two the first time while Docker downloads the `postgres:16` image.
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
- **Remarks can't be edited or deleted**: `UPDATE remark SET remark_text = 'edited' WHERE remark_id = 1;` should fail with `Remarks are append-only and cannot be updated or deleted`.
- **Duplicate attachments are rejected**: re-running the `schema-draft.sql` `INSERT` from `database/seed/001_example_data.sql` a second time (same task, same content) should fail on the unique index, since the content hash would match the copy that's already there.

<a id="resetting"></a>
## 7. Resetting / Rebuilding

`.\scripts\reset.ps1` stops the container and **deletes the database volume** (asks for confirmation first). Afterwards, `.\scripts\setup.ps1` rebuilds everything from scratch — this is the way to pick up schema changes that can't be applied as a simple additive migration, or just to get back to a clean example dataset.

<a id="adding-a-migration"></a>
## 8. Adding a New Migration

As `DomainModel.md` evolves, add a new file `database/migrations/002_<description>.sql` (never edit `001_initial_schema.sql` once it's been applied anywhere) containing just the incremental change — new table, new column, new constraint. `.\scripts\setup.ps1` applies every migration file in `database/migrations/` in filename order, so numbering them sequentially keeps the history honest. This is the same pattern `DataBaseHostingOptions.md` recommends for the eventual hosted version, so nothing about this habit needs to change later.
