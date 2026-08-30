# ProjectPal V2 — Automated Backups Phase

**Status:** Not started

**Objective:** Move from the manual `docker compose exec db pg_dump ...` habit noted in `../1_DatabaseSetup/InitialDatabaseSetupPlan.md` §8 to a scheduled/automated backup, once the Demonstrator is holding data anyone cares about losing.

**Overlaps with `../8_ProductionDeployment/Plan.md`**, which builds a real backup-dump-verify-archive mechanism as part of its release safety net (§3.3 there) — this phase most likely ends up being "add a schedule that calls the same script," not a second, independently-built backup subsystem. Check that phase's state before designing this one from scratch.

**Open Questions (Phase-Specific):** none yet — to be filled in once this phase starts, numbered `Q1.6-1`, `Q1.6-2`, ... (see `Claude/Guidelines/ImplementationApproach.md` §3.1).

**Decisions (Phase-Specific):** none yet. When an open question above is answered, its entry moves here as `D1.6-<N>` (same number, `D` prefix), recording the original question, the decision, and the date.

See `../ImplementationPlan.md` for how this phase fits into the Level 1 plan, and for open questions that span this phase and others.
