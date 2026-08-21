# ProjectPal V2 — REST API Phase

**Status:** Not started

**Objective:** Build the secured web API that fronts the Level 1 database (`Requirements/Goals.md` §3.1) — every client (the GUI, and later mobile) talks to this API, never to PostgreSQL directly. `../1_DatabaseSetup/DataBaseHostingOptions.md` suggests PostgREST as the fastest starting point for auto-generated CRUD, with hand-written endpoints (e.g. FastAPI) layered in later for real business operations (dependency-cycle-aware writes, Urgency calculation, etc.).

**Open Questions (Phase-Specific):** none yet — to be filled in once this phase starts, numbered `O1.2-1`, `O1.2-2`, ... (see `Claude/Guidelines/ImplementationApproach.md` §3.1).

**Decisions (Phase-Specific):** none yet. When an open question above is answered, its entry moves here as `D1.2-<N>` (same number, `D` prefix), recording the original question, the decision, and the date.

See `../ImplementationPlan.md` for how this phase fits into the Level 1 plan, and for open questions that span this phase and others.
