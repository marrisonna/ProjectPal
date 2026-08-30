# ProjectPal V2 — Urgency Calculation Phase

**Status:** Not started

**Objective:** Implement the Urgency algorithm (`Requirements/KeyConcepts.md` §12) as a presentation-layer calculation over stored data, not a database column or view. Settled by `D1.2-2` in `../2_RestApi/Plan.md`: this is client-side GUI work, not REST API work (the guess in `../1_DatabaseSetup/InitialDatabaseSetupPlan.md` §8 that it might fold into the REST API phase didn't hold up). This phase is likely to fold into `../5_GuiClient/` instead, which already owns Urgency computation (including the requirement to walk the whole Project ancestor chain, not just one Task's immediate Project) as part of its own objective.

**Open Questions (Phase-Specific):** none yet — to be filled in once this phase starts, numbered `Q1.7-1`, `Q1.7-2`, ... (see `Claude/Guidelines/ImplementationApproach.md` §3.1).

**Decisions (Phase-Specific):** none yet. When an open question above is answered, its entry moves here as `D1.7-<N>` (same number, `D` prefix), recording the original question, the decision, and the date.

See `../ImplementationPlan.md` for how this phase fits into the Level 1 plan, and for open questions that span this phase and others.
