# ProjectPal V2 — Authentication Phase

**Status:** Not started

**Objective:** Build password-based, per-Person login for the Level 1 Demonstrator, per `D1-2` in `../ImplementationPlan.md`: each Person authenticates individually (not a shared login), and the API issues a JWT carrying `person_id`, Team/role memberships, and `is_organisation_admin`, which every endpoint's authorization reads from. Also build an admin-only impersonation capability — mint a JWT for a target Person, carrying an `impersonated_by` claim — so an admin can verify what another Person can and can't see and do. `../1_DatabaseSetup/DataBaseHostingOptions.md` suggests starting simple (e.g. PostgREST + JWT) rather than building this into the database layer. Feeds the Identity foundational decision in `Requirements/Goals.md` §4.1.

**Open Questions (Phase-Specific):**
- **Q1.3-1:** Which role is authorized to impersonate another Person — `is_organisation_admin`, or a narrower dedicated permission (e.g. reintroducing something like the old app's `SuperUser` role)?
- **Q1.3-2:** Does Level 1 need an audit trail of actions taken while impersonating (who was impersonated, by whom, when), or is that reasonable to defer to Level 2/3 given `Scope.md`'s "security is not the top concern" framing?

**Decisions (Phase-Specific):** none yet. When an open question above is answered, its entry moves here as `D1.3-<N>` (same number, `D` prefix), recording the original question, the decision, and the date.

See `../ImplementationPlan.md` for how this phase fits into the Level 1 plan, and for open questions that span this phase and others.
