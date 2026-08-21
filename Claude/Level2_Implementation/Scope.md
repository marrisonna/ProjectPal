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

Nothing recorded yet. Items intentionally pushed out of Level 1 scope land here as they arise — see `Claude/Level1_Implementation/Scope.md` §4 and `Claude/Guidelines/ImplementationApproach.md` §4.
