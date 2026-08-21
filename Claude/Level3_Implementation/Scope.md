# ProjectPal V2 — Level 3 Scope

## Contents

1. [Summary](#summary)
2. [Anticipated Work](#anticipated-work)
3. [Deferred From Level 2](#deferred-from-level-2)

This is a placeholder, seeded from `Requirements/Goals.md` §4.3. It will be fleshed out once Level 2 nears completion and Level 3 work begins in earnest — see `Claude/Guidelines/ImplementationApproach.md` §4.

<a id="summary"></a>
## 1. Summary

The full vision described in `Requirements/Goals.md` §1: many self-service organisations, multiple teams per organisation, mobile clients, full high-availability/disaster-recovery, compliance and audit, automated tenant provisioning and migration, and cross-tenant admin/reporting tooling for us as the vendor.

<a id="anticipated-work"></a>
## 2. Anticipated Work

Per `Requirements/Goals.md` §4.3, likely work for this Level — mostly building out automation and depth on top of a foundation whose shape was already established at Levels 1–2 (API-first boundary, tenant-shaped data model, identity approach, database-per-tenant pattern):

- Automated tenant onboarding (self-service database provisioning, versioned migrations applied across all tenant databases).
- Full team model and per-team permissions within an organisation.
- Native/PWA mobile clients.
- Full data protection posture: HA, DR, audit logging, compliance as required by target customers.
- Cross-tenant tooling for the vendor: usage analytics, aggregate reporting, incident response across customers.
- Revisiting the deferred desktop integrations (Outlook/Word) via a non-COM mechanism, if still required.

<a id="deferred-from-level-2"></a>
## 3. Deferred From Level 2

Nothing recorded yet. Items intentionally pushed out of Level 2 scope land here as they arise — see `Claude/Level2_Implementation/Scope.md` and `Claude/Guidelines/ImplementationApproach.md` §4.
