# ProjectPal V2 — Level 2 Scope

## Contents

1. [Summary](#summary)
2. [Anticipated Work](#anticipated-work)
3. [Deferred From Level 1](#deferred-from-level-1)

This is a placeholder, seeded from `Requirements/Goals.md` §4.2. It will be fleshed out once Level 1 nears completion and Level 2 work begins in earnest — see `Claude/Guidelines/ImplementationApproach.md` §4.

<a id="summary"></a>
## 1. Summary

A small customer base (perhaps 1–2 organisations), infrastructure still kept light, but now hosted *outside* the customer's own network — the first point where real, internet-facing security matters, because data is now leaving the customer's own IT boundary.

<a id="anticipated-work"></a>
## 2. Anticipated Work

Per `Requirements/Goals.md` §4.2, likely decisions/work for this Level:

- **Hosting/cloud choice** — where this actually runs, and how minimal the setup can be for 1–2 customers.
- **Real security baseline** — TLS in transit, encryption at rest, real authentication, even if full audit logging and compliance are still deferred to Level 3.
- **Database-per-tenant in practice, at small scale** — validating the Level 3 architecture for real, without needing its full automation yet.
- **Identity, for real** — minimal custom auth vs. a real external identity provider.
- **Backup/recovery baseline** — who's responsible for backing up customer data now that it's hosted by us.
- **Migration from demonstrator data** — moving a converting Level 1 customer's trial data into the hosted environment.

<a id="deferred-from-level-1"></a>
## 3. Deferred From Level 1

- **Installable cross-platform client, alongside the web app.** Level 1 builds a browser-based web app only (`D1-1` in `Claude/Level1_Implementation/ImplementationPlan.md`). A native/installable cross-platform client (in the way a tool like Slack offers both a browser app and a desktop app) wasn't ruled out, just deferred — not yet committed to this Level specifically, but worth considering here as customer needs become clearer.
- **Team-specific configurable weights for the Urgency algorithm.** Level 1 computes Urgency client-side using the fixed algorithm in `Requirements/KeyConcepts.md` §12 (`D1.2-2` in `Claude/Level1_Implementation/2_RestApi/Plan.md`). Letting each Team configure its own weights is a likely later refinement, not committed to this Level specifically yet.
- **Self-service password change.** Level 1 is admin-set-password only (`D1.3-4` in `Claude/Level1_Implementation/3_Authentication/Plan.md`) — a Person changing their own password is required eventually, just not built yet.
- **Failed-login lockout/rate-limiting.** Level 1 has no protection against repeated failed login attempts (`D1.3-7` in `Claude/Level1_Implementation/3_Authentication/Plan.md`), consistent with `Scope.md`'s "security is not the top concern" framing — worth revisiting alongside this Level's "real security baseline" item above.
