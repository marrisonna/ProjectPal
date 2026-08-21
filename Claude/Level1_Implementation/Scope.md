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
- **Feature scope** — a workable subset of the ~20 windows/dialogs from `V1.2` (task/project management, Gantt/plan view, find, merge, attachments, admin) sufficient to make a trial meaningful — informed by `Requirements/UseCases.md`.
- **Database choice** — settled: PostgreSQL, locally hosted (see the Database Setup phase).
- **Deployment/packaging** — something a customer site can stand up without a dedicated ops team (Docker container, simple installer, or VM image).
- **Auth** — settled (`D1-2`): individually named users, password-based, plus an admin-only impersonation capability for testing what another Person can see and do. See the Authentication phase.

<a id="foundational-decisions"></a>
## 3. Foundational Decisions Carried Into Level 1

Per `Requirements/Goals.md` §4.1, these need a stated direction of travel now even though they aren't fully built yet:

- **API-first boundary** — the client talks to an API, never the database directly, even though there's no internet-facing security requirement yet.
- **Tenant-shaped data model** — core tables carry an `OrganisationId` (and possibly `TeamId`) now, trivially always the same value at Level 1, rather than retrofitting tenant scoping later.
- **Identity direction** — the intended long-term approach (e.g. federating to external identity providers) is decided now, even though it isn't built yet.

<a id="out-of-scope"></a>
## 4. Out of Scope

- Porting the WinForms/WPF UI code directly to any new client framework.
- Preserving direct Office COM automation (Word/Outlook) in its current form.
- Keeping the old SQL Server schema/stored procedures as the system of record.
- Everything scoped to later Levels in `Requirements/Goals.md` §4.2–4.3 — real internet-facing hosting/security posture, multiple organisations, self-service tenant onboarding, the full team/permission model, mobile clients, high availability/disaster recovery/compliance, and cross-tenant vendor tooling. As these come into focus they're tracked in `Claude/Level2_Implementation/Scope.md` and `Claude/Level3_Implementation/Scope.md`.
