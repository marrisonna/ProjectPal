# ProjectPal — Glossary

Short definitions for acronyms and terms used across the other documents in this repository. Entries are in alphabetical order. This list is seeded, not exhaustive — add to it as new terms come up.

<a id="api"></a>
## API

Application Programming Interface. In this project, "the API" specifically means the secured web API every client (web, desktop, mobile) talks to — no client ever connects to the database directly (`Requirements/Goals.md` §3.1's "API-first" boundary).

<a id="com"></a>
## COM

Component Object Model — the Windows-only interop mechanism the old `V1.2` app used to automate Word and Outlook (e.g. generating Word documents, drag-dropping emails from Outlook). Explicitly not carried forward to `V2` (`Requirements/Goals.md` Non-Goals); if that functionality is still needed later, it's rebuilt via a non-COM, server-side mechanism instead.

<a id="crud"></a>
## CRUD

Create, Read, Update, Delete — the four basic data-operations a resource typically supports. Used as shorthand for "the ordinary create/edit/browse/delete workflow" for an entity, as opposed to something more specialised (e.g. `Requirements/UseCases.md` #1 describes Manage Projects and Tasks as "core CRUD + browsing").

<a id="dr"></a>
## DR

Disaster Recovery — the plan and procedures for restoring service after a major failure (e.g. losing a database server or a whole hosting region). Called out in `Requirements/Goals.md` §3.4 as something the data protection posture needs, but explicitly deferred to Level 3 (`Requirements/Goals.md` §4.3) rather than built for the Level 1 Demonstrator or Level 2 MVP.

<a id="ha"></a>
## HA

High Availability — designing a system so it keeps running (or fails over quickly) despite individual component failures, rather than going down whenever one server or database has a problem. Like DR above, named in `Requirements/Goals.md` §3.4 as part of the full data-protection posture, but scoped to Level 3.

<a id="jwt"></a>
## JWT

JSON Web Token — a signed, self-contained token format used to carry claims about who's making a request. `D1-2` (`Level1_Implementation/ImplementationPlan.md`) uses a JWT to carry a logged-in Person's `person_id`, Team/role memberships, and `is_organisation_admin` flag, so the API can authorize a request without a separate lookup on every call.

<a id="mvp"></a>
## MVP

Minimum Viable Product — Level 2 of `Requirements/Goals.md`'s delivery levels (§4.2): a small customer base (1–2 organisations), hosted outside the customer's own network for the first time, with real internet-facing security. Not to be confused with the more generic "smallest thing worth shipping" sense of the term — here it names a specific, scoped Level with its own decisions and phases.

<a id="oidc"></a>
## OIDC

OpenID Connect — an identity/authentication protocol built on top of OAuth 2.0, used for "sign in with an external provider" flows (e.g. Google sign-in). Referenced in `Requirements/Goals.md` §3.4 and the Level 1/2 "identity direction" framing questions as a candidate approach for federating login to an external identity provider, rather than building and maintaining bespoke authentication indefinitely.

<a id="pwa"></a>
## PWA

Progressive Web App — a web app built to behave more like a native app (installable, works offline, can receive push notifications) while still being delivered through a browser. `Requirements/Goals.md` §3.3 names a responsive/PWA-style web client as the lowest-investment way to reach mobile users, ahead of building a fully native app.

<a id="rls"></a>
## RLS

Row-Level Security — a database feature (e.g. in PostgreSQL) that restricts which rows a given database role/query can see or modify, enforced by the database itself rather than by application code. ProjectPal doesn't use RLS for tenant isolation — `Requirements/Goals.md` §3.2 chose database-per-tenant instead, so there's no shared table of multiple organisations' rows for RLS to filter. It remains relevant *within* a tenant's database as a possible (not yet decided) mechanism for enforcing Team-scoped or role-scoped access at the database layer, as an extra layer beneath the API's own authorization checks.

<a id="saas"></a>
## SaaS

Software as a Service — a hosted, subscription-style delivery model where the vendor runs the software and customers access it remotely, rather than installing and running it themselves. `Requirements/Goals.md` §1 (Vision) frames the whole `V2` rebuild as turning the old single-organisation desktop app into a multi-tenant SaaS product.

<a id="tls"></a>
## TLS

Transport Layer Security — the protocol that encrypts data in transit between a client and a server (what makes a connection "HTTPS" rather than "HTTP"). Named in `Requirements/Goals.md` §3.4 and §4.2's "real security baseline" framing question (`Q-G-7`) as part of the minimum acceptable security bar once ProjectPal is hosting customer data itself, from Level 2 onward.
