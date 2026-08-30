# ProjectPal V2 — Level 1 Scope

## Contents

1. [Summary](#summary)
2. [In Scope](#in-scope)
3. [Foundational Decisions Carried Into Level 1](#foundational-decisions)
4. [Out of Scope](#out-of-scope)

Seeded from `Requirements/Goals.md` §4.1 and §5 — see that document for the full rationale behind these decisions. This document is refined as Level 1 work proceeds; see `Claude/Guidelines/ImplementationApproach.md` for how it relates to `ImplementationPlan.md`.

<a id="summary"></a>
## 1. Summary

One organisation only, deployed *inside* that organisation's own infrastructure. Security is not the top concern. Infrastructure is deliberately cut down (simple database, minimal moving parts). Purpose: let real users try the concepts and the GUI and give feedback, as cheaply as possible.

<a id="in-scope"></a>
## 2. In Scope

- **Client technology** — settled (`D1-1`): a browser-based web app. See the GUI / Web Client phase.
- **Feature scope** — settled (`D1-4`): Manage Projects/Tasks, Assign resources, Set dependencies, Search, and Remarks are required; the Gantt/plan view is required (a key selling point, not a nice-to-have); Attachments are required for files and hyperlinks, not captured emails; admin/support tooling is required. Multiple users may access and modify data concurrently — see the Concurrency bullet below.
- **Concurrency** — settled, see `Requirements/DomainModel.md`'s Cross-Cutting Concerns: Level 1 supports multiple named users concurrently, but not two of them editing the same record at the same time — real usage during the Demonstrator avoids that, so no conflict-handling is built.
- **Database choice** — settled: PostgreSQL, locally hosted (see the Database Setup phase).
- **Deployment/packaging** — something a customer site can stand up without a dedicated ops team (Docker container, simple installer, or VM image). Reachability for trial users is via a Cloudflare Tunnel (`4_HttpsReverseProxy/Plan.md`'s `D1.4-1`), a deliberate, documented departure from "stays inside the customer's own network" (§1) made specifically to avoid any customer IT involvement — see that decision for the full trade-off.
- **Auth** — settled (`D1-2`): individually named users, password-based. See the Authentication phase. (`D1-2` originally also raised a dedicated admin-impersonation capability for testing what another Person can see and do; the Authentication phase's `D1.3-1` found Level 1 doesn't need one, since an admin can just log in as that Person directly.)

<a id="foundational-decisions"></a>
## 3. Foundational Decisions Carried Into Level 1

Per `Requirements/Goals.md` §4.1, these need a stated direction of travel now even though they aren't fully built yet:

- **API-first boundary** — the client talks to an API, never the database directly, even though there's no internet-facing security requirement yet.
- **Tenant-shaped data model** — per `Requirements/DomainModel.md`'s Tenancy Scope Note, the database itself is the Organisation boundary (database-per-tenant, per `Requirements/Goals.md` §3.2), so core tables carry **no** `OrganisationId` column — there's nothing to retrofit later since the boundary was never row-level. `TeamId` *is* present now on the tables that need it (e.g. `Project`), since Team is a real within-database concept from Level 1 onward.
- **Identity direction** — the intended long-term approach (e.g. federating to external identity providers) is decided now, even though it isn't built yet.

<a id="out-of-scope"></a>
## 4. Out of Scope

- Porting the WinForms/WPF UI code directly to any new client framework.
- Preserving direct Office COM automation (Word/Outlook) in its current form.
- Keeping the old SQL Server schema/stored procedures as the system of record.
- Everything scoped to later Levels in `Requirements/Goals.md` §4.2–4.3 — real internet-facing hosting/security posture, multiple organisations, self-service tenant onboarding, the full team/permission model, mobile clients, high availability/disaster recovery/compliance, and cross-tenant vendor tooling. As these come into focus they're tracked in `Claude/Level2_Implementation/Scope.md` and `Claude/Level3_Implementation/Scope.md`.
