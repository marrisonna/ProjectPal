# ProjectPal V2 — GUI / Web Client Phase

**Status:** Not started

**Objective:** Build a browser-based web app as the client GUI for the Demonstrator, per `D1-1` in `../ImplementationPlan.md`. Feature scope is settled by `D1-4` (`../ImplementationPlan.md`) and `../Scope.md`: Manage Projects/Tasks, Assign Resources, Set Dependencies, Search, Remarks, the Gantt/plan view, and File/Link attachments are all required — not optional trial content.

`Requirements/UseCases.md`'s View the Plan (Gantt) use case is required for Level 1 (`D1-4` — a key selling point, not a deferral candidate) and is this phase's responsibility end to end — the database and API need nothing special for it (see `../2_RestApi/Plan.md` §2.2). The plan/Gantt view is built by composing the Task, Project, and Dependency data the REST API already exposes (schedule derivation, layout, and rendering all happen client-side), not by a bespoke aggregation endpoint.

Urgency (`Requirements/KeyConcepts.md` §12) is also this phase's responsibility end to end, per `D1.2-2` in `../2_RestApi/Plan.md` — computed client-side from the Task/Project fields the API already exposes, not served pre-computed. Being dynamic (it changes with the passage of time alone, with no underlying data change) fits naturally with computing it where it's displayed rather than re-fetching it. Team-specific configurable weights for the algorithm are a likely later refinement (not Level 1 — see `Claude/Level2_Implementation/Scope.md`), and fit this GUI-side placement more naturally than a server-side per-Team lookup would.

**Urgency needs the whole Project ancestor chain, not just one Task's Project.** `Requirements/KeyConcepts.md` §12's "effective priority" factor is computed root-first over *every* ancestor Project above a Task (the Task's own Project, that Project's parent, and so on), not just the immediate one — a Project tree can be arbitrarily deep via `parent_project_id`. This phase needs to fetch the whole Project tree (or otherwise be able to walk parent links for any Task's ancestry) to compute Urgency correctly — a single Task fetch plus its one immediate Project is not sufficient. Level 1's data volumes (one Organisation, a handful of Teams/Projects) make fetching the whole tree trivial; this stops being free once Level 2/3 have many tenants/Projects, but that's out of scope here. Since the calculation of Urgency is a key concept for the product, this is a Level 1 requirement, not a nice-to-have.

**Open Questions (Phase-Specific):** none yet — to be filled in once this phase starts, numbered `Q1.4-1`, `Q1.4-2`, ... (see `Claude/Guidelines/ImplementationApproach.md` §3.1).

**Decisions (Phase-Specific):** none yet. When an open question above is answered, its entry moves here as `D1.4-<N>` (same number, `D` prefix), recording the original question, the decision, and the date.

See `../ImplementationPlan.md` for how this phase fits into the Level 1 plan, and for open questions that span this phase and others.
