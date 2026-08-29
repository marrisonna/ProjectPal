# ProjectPal V2 — Authentication Phase

**Status:** Not started

**Objective:** Build password-based, per-Person login for the Level 1 Demonstrator, per `D1-2` in `../ImplementationPlan.md`: each Person authenticates individually (not a shared login), and the API issues a JWT carrying `person_id`, Team/role memberships, and `is_organisation_admin`, which every endpoint's authorization reads from. `../1_DatabaseSetup/DataBaseHostingOptions.md` suggests starting simple (e.g. PostgREST + JWT) rather than building this into the database layer. Feeds the Identity foundational decision in `Requirements/Goals.md` §4.1.

`D1-2` also raised building a dedicated admin-only impersonation capability (mint a JWT for a target Person) so an admin could verify what another Person can/can't do — `D1.3-1` below found this isn't actually needed for Level 1: since every Person already has their own login credentials, an admin can verify another Person's view by logging in as them directly. Nothing in this phase (or `2_RestApi`) builds a separate impersonation mechanism as a result.

**Open Questions (Phase-Specific):** none currently open — see Decisions below.

**Decisions (Phase-Specific):**
- **D1.3-1** (decided 2026-08-23)<br>
  **Question:** Which role is authorized to impersonate another Person — `is_organisation_admin`, or a narrower dedicated permission (e.g. reintroducing something like the old app's `SuperUser` role)?<br>
  **Decision:** moot for Level 1 — no dedicated impersonation mechanism is needed at all. Each Person already has their own login username/password (`D1-2`); verifying what another Person can/can't see and do is achieved by logging in as that Person directly with their own credentials, not by a separate admin-minted token. This revises `D1-2`'s "admin-only impersonation capability" — that specific capability isn't built for Level 1; `D1-2`'s core (named users, password-based JWT auth) is unaffected.
- **D1.3-2** (decided 2026-08-23)<br>
  **Question:** Does Level 1 need an audit trail of actions taken while impersonating (who was impersonated, by whom, when), or is that reasonable to defer to Level 2/3 given `Scope.md`'s "security is not the top concern" framing?<br>
  **Decision:** no audit trail of any kind is required for Level 1 — not just for the now-moot impersonation case (`D1.3-1`), but generally. If a later Level needs one, it's designed fresh at that point, not retrofitted from partial Level 1 logging.

See `../ImplementationPlan.md` for how this phase fits into the Level 1 plan, and for open questions that span this phase and others.
