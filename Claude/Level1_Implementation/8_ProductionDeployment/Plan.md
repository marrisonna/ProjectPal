# ProjectPal V2 — Production Deployment Phase Plan

## Contents

1. [Status and Purpose](#status-and-purpose)
2. [Scope for Level 1](#scope)
   - 2.1 [In Scope](#in-scope)
   - 2.2 [Deferred Out of Level 1](#deferred)
3. [Design](#design)
   - 3.1 [Migration Versioning](#migration-versioning)
   - 3.2 [Release Versioning](#release-versioning)
   - 3.3 [Backup, Verify, Archive](#backup-verify-archive)
   - 3.4 [Deploy Procedure](#deploy-procedure)
   - 3.5 [Rollback Procedure](#rollback-procedure)
4. [Implementation Plan](#implementation-plan)
   - 4.1 [Files Touched](#files-touched)
   - 4.2 [Build Order](#build-order)
5. [Testing](#testing)
   - 5.1 [Approach](#testing-approach)
   - 5.2 [Test Categories](#test-categories)
6. [Definition of Success](#definition-of-success)
7. [Open Questions (Phase-Specific)](#open-questions)
8. [Decisions (Phase-Specific)](#decisions)

<a id="status-and-purpose"></a>
## 1. Status and Purpose

**Status:** Not started.

Establish a lightweight but genuinely safe production release process for the Level 1 Demonstrator — enough discipline (migration versioning, backup, rollback) to make it hard to break the one running Production instance, without the full staged Dev→QA→Prod promotion pipeline `Claude/Level2_Implementation/Scope.md` anticipates for later. This phase exists to support a specific, deliberately tight feedback loop: take a trial user's feedback, make the change, deploy it, and let them see the impact quickly — the actual point of running a small, friendly Level 1 trial rather than a fully staged rollout.

No standing QA environment is built here — Level 1's small, friendly user base doesn't carry the risk profile that justifies one, and a scratch-database dry run (§3.3) gets most of the same safety property (validating a change against real, Prod-shaped data before it touches Prod for real) for a fraction of the standing infrastructure cost.

<a id="scope"></a>
## 2. Scope for Level 1

<a id="in-scope"></a>
### 2.1 In Scope

- A `schema_migrations` tracking table, so it's possible to know which migration files a given database has already applied — the prerequisite for "apply just what's new," used by both `setup.ps1` (dev) and this phase's new Prod deploy script, not two independently-maintained migration runners (§3.1).
- The convention this puts in place from Prod's first real deploy onward: every schema change is a new numbered migration file, never an edit to one already applied to Prod. Dev keeps rebuilding freely forever (`Claude/Guidelines/ImplementationApproach.md` §5); only Prod graduates out of that habit, and only from the point it first goes live.
- A simple release version marker, so a deploy can be confirmed to have actually taken effect (§3.2).
- A backup script that dumps, verifies (via a scratch-database restore that doubles as a migration dry run), and archives Prod's database before any deploy touches it (§3.3).
- A deploy script composing backup → migrate → redeploy → smoke-check (§3.4).
- A rollback script — restore the pre-upgrade backup and redeploy the previous release (§3.5).

<a id="deferred"></a>
### 2.2 Deferred Out of Level 1

- **A standing QA environment**, and the full build-two-packages / clone-Prod-into-QA / validate-in-QA-before-promoting pipeline described and agreed during this phase's design discussion. A deliberate Level 2 activity — see `Claude/Level2_Implementation/Scope.md`. The scratch-restore dry run (§3.3) is Level 1's proportionate substitute, not a lesser version of the same thing built halfway.
- **Fully automated, zero-downtime deploys.** Level 1 explicitly accepts brief downtime during a deploy; blue-green or rolling-upgrade infrastructure is Level 2/3-shaped investment for one Production instance serving a small trial.
- **Distributable release packages for multiple customers.** This phase deploys updates *to* the one running Level 1 Production instance — it doesn't produce an installable package for a *new* customer site to stand up their own from scratch. That's `Q1-3` in `../ImplementationPlan.md`, still open, and a separate concern.

<a id="design"></a>
## 3. Design

<a id="migration-versioning"></a>
### 3.1 Migration Versioning

```sql
CREATE TABLE IF NOT EXISTS projectpal.schema_migrations (
    filename    text PRIMARY KEY,
    applied_at  timestamptz NOT NULL DEFAULT now()
);
```

An "apply pending migrations" step replaces `setup.ps1`'s current "blindly run every migration file" behaviour: for each `*.sql` file in `database/migrations/`, in filename order, skip it if it's already recorded in `schema_migrations`; otherwise apply it and record it, in the same transaction. This is one shared implementation (`scripts/apply-migrations.ps1`, §4.1), used by both `setup.ps1` (dev) and the new Prod deploy script (§3.4) — on a genuinely empty dev database "pending" is simply "all of them," so dev's behaviour and the existing rebuild-freely convention are completely unaffected; the distinction only starts to matter once a database (Prod) already has some migrations applied and needs just the new ones.

Since Prod doesn't exist yet, the `schema_migrations` table itself is added by editing `001_initial_schema.sql` directly, same as any other pre-Prod change (`Claude/Guidelines/ImplementationApproach.md` §5). The very next schema change *after* Prod's first real deploy is what starts the "always a new numbered file" discipline — `001` is frozen from that point on, not from today.

<a id="release-versioning"></a>
### 3.2 Release Versioning

A git tag marks each Prod release (e.g. `prod-v1`, `prod-v2`, …). At deploy time, the tag is written into a `VERSION` file baked into the application's Docker image, and exposed via a small, deliberately unauthenticated `GET /version` endpoint (`{"version": "prod-v3", "deployed_at": "..."}`) — a health-check-style exception to "every endpoint requires a JWT" (`2_RestApi/Plan.md` §4.3), since it needs to be checkable with a plain request, not a login first. Cheap way to confirm a deploy actually took effect, without inspecting containers or logs.

<a id="backup-verify-archive"></a>
### 3.3 Backup, Verify, Archive

Before any deploy touches Prod:

1. `pg_dump` a full backup of Prod's database.
2. Restore that dump into a throwaway scratch database (created for this purpose, discarded immediately after) and apply the pending migration(s) there. This single step verifies two things at once: the backup actually restores cleanly (an unverified backup isn't a real backup), and the new migration(s) apply cleanly against real, Prod-shaped data — the closest Level 1 gets to a QA environment's "validate the upgrade path against real data" property, without maintaining a standing one.
3. If the scratch dry run succeeds, archive the original dump to `BACKUP_ARCHIVE_DIR` (`D1.8-1`, §8) — each backup gets its own subfolder named by timestamp and the release tag being deployed (e.g. `<BACKUP_ARCHIVE_DIR>\2026-08-30_143000_prod-v3\`), so every historical backup is kept, never overwritten. Durability comes from that directory already being an actively-synced OneDrive folder — no separate upload step for this phase to build.

If the dry run fails, the deploy stops here. Nothing has touched real Prod yet.

<a id="deploy-procedure"></a>
### 3.4 Deploy Procedure

1. Tag the release in git.
2. Run the backup-and-verify step (§3.3) — mandatory, never skipped, regardless of how small the change feels.
3. Apply pending migrations to Prod (§3.1).
4. Rebuild/restart Prod's application containers from the new release tag.
5. Run a smoke check directly against Prod, mirroring `4_HttpsReverseProxy/Plan.md`'s Tier 2b — the one thing a QA environment could never have validated anyway, since QA doesn't exist here and wouldn't have had the real Cloudflare Tunnel configuration even if it did.
6. Confirm via `GET /version` (§3.2) that the new release is actually the one live.

<a id="rollback-procedure"></a>
### 3.5 Rollback Procedure

A script, not just written steps — deliberately, since a manual procedure is more error-prone precisely when it's needed, under pressure, mid-incident. Stops Prod's containers, restores the pre-upgrade backup taken in §3.3 (the real archived one, not the throwaway scratch copy), redeploys the previous release tag's containers, and re-runs the smoke check. Restores the whole database from backup rather than attempting reverse/"down" migrations — simpler, and avoids needing to write and maintain a down-migration for every up-migration going forward.

<a id="implementation-plan"></a>
## 4. Implementation Plan

<a id="files-touched"></a>
### 4.1 Files Touched

```
V2/
├── database/
│   └── migrations/
│       └── 001_initial_schema.sql   (edited — adds schema_migrations, §3.1; last time this file
│                                      is edited directly, once Prod's first deploy happens)
├── .env.example               (edited — adds BACKUP_ARCHIVE_DIR, §3.3)
├── scripts/
│   ├── apply-migrations.ps1   (NEW — shared by setup.ps1 and deploy-prod.ps1; schema_migrations-aware)
│   ├── backup-prod.ps1        (NEW — dump, scratch-restore verify, archive to BACKUP_ARCHIVE_DIR; §3.3)
│   ├── deploy-prod.ps1        (NEW — orchestrates §3.4)
│   ├── rollback-prod.ps1      (NEW — §3.5)
│   └── setup.ps1              (edited — delegates its migration step to apply-migrations.ps1)
└── rest-api/
    └── app/
        └── routes/
            └── version.py     (NEW — GET /version, §3.2, unauthenticated)
```

`BACKUP_ARCHIVE_DIR` defaults to `C:\Users\Neil\OneDrive\ProjectPal\Backups` (`D1.8-1`) — kept as an environment variable rather than hardcoded in `backup-prod.ps1`, consistent with every other machine-specific value in this project, even though this one's genuinely tied to this particular machine's OneDrive setup for now.

<a id="build-order"></a>
### 4.2 Build Order

1. Add the `schema_migrations` table (§3.1).
2. Extract the migration-runner logic into `apply-migrations.ps1`; update `setup.ps1` to call it (dev's behaviour is unchanged, since an empty database always has everything "pending").
3. Add `GET /version` and the `VERSION`-file-baking step to the Docker build (§3.2).
4. Write `backup-prod.ps1` (dump, scratch-restore dry run, archive to `BACKUP_ARCHIVE_DIR`, `D1.8-1`).
5. Write `deploy-prod.ps1`, composing the steps above (§3.4).
6. Write `rollback-prod.ps1` (§3.5).
7. Rehearse the whole flow at least once against a disposable copy of the stack before the first real trial release ever depends on it working.

<a id="testing"></a>
## 5. Testing

<a id="testing-approach"></a>
### 5.1 Approach

Mostly operational scripts rather than application code — proven by actually running them against a disposable copy of the stack, not by the automated pytest suite (which stays focused on API behaviour, per `2_RestApi/Plan.md` §6.1).

<a id="test-categories"></a>
### 5.2 Test Categories

- **Idempotence** — running `apply-migrations.ps1` twice in a row is a no-op the second time.
- **The dry run actually catches bad migrations** — a deliberately broken migration (e.g. a `NOT NULL` column with no default, added against a scratch database seeded with real-shaped rows) is caught in §3.3's scratch step, not discovered against real Prod.
- **Rollback genuinely restores service** — a simulated bad deploy, then a rollback, ends with Prod back on the previous release and passing its smoke check.
- **`GET /version` reflects reality** — matches the tag actually running after a deploy, and after a rollback.

<a id="definition-of-success"></a>
## 6. Definition of Success

- Prod has never had a schema change applied by directly editing an already-applied migration file.
- Every deploy is preceded by a verified, archived backup — no exceptions, regardless of how small the change looks.
- A bad migration is caught by the scratch-restore dry run before it ever reaches real Prod.
- Rolling back is a single script, rehearsed at least once before it's ever needed for real.
- `GET /version` reliably confirms what's actually deployed after both a deploy and a rollback.

<a id="open-questions"></a>
## 7. Open Questions (Phase-Specific)

None currently open — see Decisions below.

<a id="decisions"></a>
## 8. Decisions (Phase-Specific)

- **D1.8-1** (decided 2026-08-30)<br>
  **Question:** Where are archived backups actually stored? Needs to be durable and off this one machine.<br>
  **Decision:** `C:\Users\Neil\OneDrive\ProjectPal\Backups`, each backup in its own subfolder (§3.3). Durability comes from this already being an actively-synced OneDrive folder — no separate off-machine upload step for this phase to build. Kept as the `BACKUP_ARCHIVE_DIR` environment variable (§4.1) rather than hardcoded, in case this ever needs to change.

See `../ImplementationPlan.md` for how this phase fits into the Level 1 plan, and for open questions that span this phase and others.
