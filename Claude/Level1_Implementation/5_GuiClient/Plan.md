# ProjectPal V2 — GUI / Web Client Phase

**Status:** Not started

**Objective:** Build a browser-based web app as the client GUI for the Demonstrator, per `D1-1` in `../ImplementationPlan.md`. Feature scope (which subset of `Requirements/UseCases.md`'s workflows are essential for a meaningful trial) is decided in `Requirements/UseCases.md`'s own open questions and in `../Scope.md`.

`Requirements/UseCases.md`'s View the Plan (Gantt) use case is this phase's responsibility end to end — the database and API need nothing special for it (see `../2_RestApi/Plan.md` §2.2). The plan/Gantt view is built by composing the Task, Project, and Dependency data the REST API already exposes (schedule derivation, layout, and rendering all happen client-side), not by a bespoke aggregation endpoint.

Urgency (`Requirements/KeyConcepts.md` §12) is also this phase's responsibility end to end, per `D1.2-2` in `../2_RestApi/Plan.md` — computed client-side from the Task/Project fields the API already exposes, not served pre-computed. Being dynamic (it changes with the passage of time alone, with no underlying data change) fits naturally with computing it where it's displayed rather than re-fetching it. Team-specific configurable weights for the algorithm are a likely later refinement (not Level 1 — see `Claude/Level2_Implementation/Scope.md`), and fit this GUI-side placement more naturally than a server-side per-Team lookup would.

**Open Questions (Phase-Specific):** none yet — to be filled in once this phase starts, numbered `Q1.5-1`, `Q1.5-2`, ... (see `Claude/Guidelines/ImplementationApproach.md` §3.1).

**Decisions (Phase-Specific):** none yet. When an open question above is answered, its entry moves here as `D1.5-<N>` (same number, `D` prefix), recording the original question, the decision, and the date.

See `../ImplementationPlan.md` for how this phase fits into the Level 1 plan, and for open questions that span this phase and others.
