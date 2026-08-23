# ProjectPal — Modernization Goals

*Open questions in this document use the prefix `Q-G-`; decisions use `D-G-`.*

## Contents

1. [Vision](#vision)
2. [How to Think About the Current Application (V2)](#how-to-think-about-the-current-application)
3. [Target Capabilities](#target-capabilities)
   - 3.1 [Backend / API](#backend-api)
   - 3.2 [Multi-tenancy](#multi-tenancy)
   - 3.3 [Clients](#clients)
   - 3.4 [Data protection & operational resilience](#data-protection-and-operational-resilience)
4. [Delivery Levels](#delivery-levels)
   - 4.1 [Level 1 — Demonstrator](#level-1-demonstrator)
   - 4.2 [Level 2 — Minimum Viable Product (MVP)](#level-2-mvp)
   - 4.3 [Level 3 — Everything Else](#level-3-everything-else)
5. [Open Questions](#open-questions)
6. [Decisions](#decisions)
7. [Non-Goals (for now)](#non-goals)

<a id="vision"></a>
## 1. Vision

Take the concepts, domain model, and use cases proven out by the existing ProjectPal desktop application (Windows/WinForms + SQL Server, single team, single organisation) and rebuild them as a modern, cloud-hosted, multi-tenant SaaS product — accessible from Windows and Mac desktops, and eventually from iOS/Android — suitable for adoption by multiple organisations, each with multiple teams, with strong security and data isolation between them.

<a id="how-to-think-about-the-current-application"></a>
## 2. How to Think About the Current Application (V2)

The current C#/WinForms/SQL Server app should be treated as a **prototype and reference**, not a codebase to evolve incrementally. It's valuable for what it teaches us, not for the code itself:

**Worth carrying forward (concepts, not code):**
- The domain model: `Task`, `Project`, `Component`, `Person`, `Attachment`, `Remark`, and the relationships between them (task↔project, task↔resource, task dependencies, etc.)
- The workflows and use cases embedded in the UI (task/project merging, Gantt-style resource/plan display, find/search, attachments from email/documents, admin/people management)
- The business rules buried in the data-access layer (e.g. the update-or-insert logic in `SqlInsert`, merge/hide-private-instance semantics)

**Not worth carrying forward:**
- The WinForms UI and the custom hand-rolled grid/Gantt controls (`CustomGUIControls`, `PlanDisplay`) — no path to a cross-platform or web client
- Direct client-to-SQL-Server access (`System.Data.SqlClient` from the desktop process) — a non-starter for multi-tenant, internet-facing use
- Windows-only COM interop (Word automation, Outlook drag-drop) — these are single-platform integrations that will need re-imagining, not porting
- The single-tenant schema (one `TaskMan`/`ProjectPal` schema, one set of tables, no organisation/team boundary anywhere in the data model)

In short: this is a **green-field rebuild informed by a working prototype**, not a migration.

<a id="target-capabilities"></a>
## 3. Target Capabilities

<a id="backend-api"></a>
### 3.1 Backend / API
- All data access goes through a secured web API — no client (desktop, web, or mobile) ever connects to the database directly.
- Authentication and authorization built in from day one (not retrofitted), including per-organisation and per-team roles/permissions.

<a id="multi-tenancy"></a>
### 3.2 Multi-tenancy

**Decision:** a **tenant is an organisation** (the billing/contractual/isolation boundary), and the model is **database-per-tenant**. Teams are a grouping *within* an organisation's database — separated from each other by application-level authorization (a `team_id`/role check), not by a separate database per team.

Rationale:
- Orgs are the natural billing/contractual unit; teams are an internal subdivision that org admins need to manage, report across, and move people between — all of which is trivial within one database and painful across separate ones.
- Database-per-org gives the strongest practical isolation between customers, and lets each organisation be upgraded to a new schema version independently of the others.
- Team-as-tenant would multiply the database count (orgs × teams-per-org) without a corresponding isolation benefit, since teams within the same org don't need billing or contractual separation from each other.

**Costs this decision brings** (accepted, but worth designing for deliberately rather than discovering later):
- **Migration orchestration** — schema changes must be tracked and rolled out per tenant database rather than once. Needs tooling to know which schema version each tenant is on and to run/verify migrations database-by-database (the existing `CreateDB.sql`-style script is a reasonable seed for the *provisioning* half of this, but the *upgrade* half — versioned, repeatable migrations applied to N live databases — still needs to be built).
- **Connection routing** — every API request needs a "which tenant → which database/connection string" resolution step, instead of a single static connection pool. This adds a lookup and connection-management layer that a shared-database design wouldn't need.
- **Cross-tenant operations are harder** — anything the vendor (us) needs to do across all tenants at once (usage analytics, a super-admin view, aggregate reporting, incident response across customers) has no single database to query; it needs a separate mechanism, e.g. a shared metadata/control-plane database that references but doesn't contain tenant data.
- **Cost model depends on how "database-per-tenant" is hosted** — one database *server instance* hosting many tenant *databases* is inexpensive and operationally simple; a dedicated server/instance per tenant is far more expensive and only justified for large or compliance-sensitive customers. Default assumption should be the former (shared instance, isolated databases), with dedicated instances as a later, premium option if needed.
- **Onboarding a new tenant is an operation, not a config change** — creating a new organisation means provisioning an actual new database, which needs to be automated, monitored, and made idempotent/retriable from day one rather than treated as a manual step.

<a id="clients"></a>
### 3.3 Clients
- **Desktop (Windows + Mac):** either a browser-based web app, or a single cross-platform client codebase that runs on both OSes. Needs a deliberate choice, not a default.
- **Mobile (iOS + Android):** a future target. A responsive/PWA-style web client would reach mobile with the least additional investment; a native app is a separate, larger commitment best deferred until there's a concrete need (offline use, push notifications, deep OS integration).

<a id="data-protection-and-operational-resilience"></a>
### 3.4 Data protection & operational resilience
- The data is the core asset and must be protected accordingly: encryption in transit and at rest, regular backups with tested restore procedures, disaster recovery plan, high availability, audit logging of who changed what.
- Hosted on cloud infrastructure suited to "Google/cloud-oriented" organisations — implies thinking about identity (e.g. supporting Google Workspace / OIDC sign-in) alongside pure hosting choice.

<a id="delivery-levels"></a>
## 4. Delivery Levels

Rather than one long list of open questions, the work splits into three levels with different goals, different risk tolerances, and different infrastructure. The organising principle: most decisions can be scoped to the level that actually needs them, except **Foundational Decisions** (see `KeyConcepts.md`), which need a stated direction of travel now even if they aren't fully built until later. Each level below calls those out explicitly.

<a id="level-1-demonstrator"></a>
### 4.1 Level 1 — Demonstrator

**Scope:** one organisation only, deployed *inside* that organisation's own infrastructure. Security is not the top concern. Infrastructure is deliberately cut down (simple database, minimal moving parts). Purpose: let real users try the concepts and the GUI and give feedback, as cheaply as possible.

**Decisions/work needed now:**
- **Client technology** — this is the whole point of the demonstrator, since it's what users will actually react to. See Decisions (`D-G-1`).
- **Feature scope** — the current app has ~20 windows/dialogs. See Decisions (`D-G-2`).
- **Database choice** — see Open Questions (`Q-G-3`).
- **Deployment/packaging** — whoever runs the trial needs to do this without a dedicated ops team. See Open Questions (`Q-G-4`).
- **Auth** — see Decisions (`D-G-5`).

**Foundational decisions to lock in now even though nothing is built yet:**
- **API-first boundary.** Even with no security requirement yet, keep the client talking to an API rather than the database directly. Level 2 requires this anyway (hosted outside the customer's network), so building it this way from day one avoids a rewrite rather than saving effort now.
- **Tenant-shaped data model.** §3.2's database-per-tenant decision means the database itself is the Organisation boundary — no `OrganisationId` column on core tables, now or later (see `DomainModel.md`'s Tenancy Scope Note). `TeamId` *is* included on the tables that need it (e.g. Project) from Level 1 onward, since Team is a real within-database concept regardless of tenancy model.
- **Identity direction.** Doesn't need building now, but decide the intended long-term approach (e.g. federate to external identity providers) so the demonstrator's login isn't built in a way that's a dead end.

<a id="level-2-mvp"></a>
### 4.2 Level 2 — Minimum Viable Product (MVP)

**Scope:** a small customer base (perhaps 1–2 organisations), infrastructure still kept light, but now hosted *outside* the customer's own network — the first point where real, internet-facing security matters, because data is now leaving the customer's own IT boundary.

**Decisions/work needed now:**
- **Hosting/cloud choice** — see Open Questions (`Q-G-6`).
- **Real security baseline** — see Open Questions (`Q-G-7`).
- **Database-per-tenant in practice, at small scale** — see Open Questions (`Q-G-8`).
- **Identity, for real** — see Open Questions (`Q-G-9`).
- **Backup/recovery baseline** — see Open Questions (`Q-G-10`).
- **Migration from demonstrator data** — see Open Questions (`Q-G-11`).

**Foundational decisions to lock in now:**
- Confirm the database-per-tenant pattern actually works operationally at small scale — this is the cheap, low-risk moment to validate the Level 3 architecture before it needs to handle many tenants.
- Commit to the identity direction concretely (even if the implementation is still minimal), since the login/session model touches every client and is painful to change once clients depend on it.

<a id="level-3-everything-else"></a>
### 4.3 Level 3 — Everything Else

**Scope:** the full vision described earlier in this document — many self-service organisations, multiple teams per organisation, mobile clients, full high-availability/disaster-recovery, compliance and audit, automated tenant provisioning and migration, and cross-tenant admin/reporting tooling for us as the vendor.

Nothing here should be *designed* from scratch at this point — Levels 1 and 2 exist specifically to have already established the direction of travel for the items called out above (API-first boundary, tenant-shaped data model, identity approach, database-per-tenant pattern). Level 3's work is mostly building out automation and depth on top of a foundation that shouldn't need to change shape:
- Automated tenant onboarding (self-service database provisioning, versioned migrations applied across all tenant databases)
- Full team model and per-team permissions within an organisation
- Native/PWA mobile clients
- Full data protection posture: HA, DR, audit logging, compliance as required by target customers
- Cross-tenant tooling for the vendor: usage analytics, aggregate reporting, incident response across customers
- Revisiting the deferred desktop integrations (Outlook/Word) via a non-COM mechanism (e.g. inbound email processing via the API, server-side document generation), if still required

<a id="open-questions"></a>
## 5. Open Questions

- **Q-G-3:** Database choice — does the demonstrator reuse SQL Server (matches the existing prototype, and likely matches what an enterprise customer already runs), or use something with less deployment friction for a single-site install (e.g. Postgres/SQLite)? *(Level 1)*
- **Q-G-4:** Deployment/packaging — how does someone at the customer site actually stand this up — a Docker container, a simple installer, a VM image? Whoever runs the trial needs to do this without a dedicated ops team. *(Level 1)*
- **Q-G-6:** Hosting/cloud choice — where does this actually run, and how minimal can the setup be for 1–2 customers (a single small server / managed database) while still being reasonably safe? *(Level 2)*
- **Q-G-7:** Real security baseline — what's the minimum acceptable bar now that data is hosted by us — TLS in transit, encryption at rest, real authentication — even if full audit logging and compliance are still deferred to Level 3? *(Level 2)*
- **Q-G-8:** Database-per-tenant in practice, at small scale — with only 1–2 customers, is manual/scripted provisioning of each tenant database sufficient, deferring the fully automated onboarding pipeline to Level 3? (Likely yes — this is the point where the Level-3 architecture gets validated for real, without needing to build the automation yet.) *(Level 2)*
- **Q-G-9:** Identity, for real — build minimal custom auth now, or integrate a real external identity provider (e.g. Google sign-in) at this level, given it may be cheaper long-term than building throwaway auth twice? *(Level 2)*
- **Q-G-10:** Backup/recovery baseline — who is responsible for backing up customer data now that it's hosted by us, and what's the minimum acceptable recovery story, even if a full DR plan is still Level 3? *(Level 2)*
- **Q-G-11:** Migration from demonstrator data — if a demonstrator customer converts into an MVP customer, does their trial data need to move into the hosted environment — and is a manual one-off export/import acceptable, rather than building a repeatable migration tool? *(Level 2)*

<a id="decisions"></a>
## 6. Decisions

- **D-G-1**<br>
  **Question:** Client technology for the Demonstrator — should users open a browser pointed at a server running inside their own network, or install a client app?<br>
  **Decision:** see `D1-1` in `Level1_Implementation/ImplementationPlan.md`.
- **D-G-2**<br>
  **Question:** Feature scope — which subset of workflows (task/project management, Gantt/plan view, find, merge, attachments, admin) are essential to make the Level 1 trial meaningful, and which can be stubbed or left out entirely?<br>
  **Decision:** see `D1-4` in `Level1_Implementation/ImplementationPlan.md`.
- **D-G-5**<br>
  **Question:** Auth for the Demonstrator — is a single shared login sufficient, or do individual named users matter even now (e.g. because permission-related workflows are part of what's being trialled)?<br>
  **Decision:** see `D1-2` in `Level1_Implementation/ImplementationPlan.md`.

<a id="non-goals"></a>
## 7. Non-Goals (for now)

- Not attempting to port the WinForms/WPF UI code directly to any new client framework.
- Not preserving direct Office COM automation (Word/Outlook) in its current form — revisit only once the core product exists, and likely via a different mechanism (e.g. inbound email processing via the API, server-side document generation library) rather than desktop COM.
- Not trying to keep the old SQL Server schema/stored procedures as the system of record — the new data model should be designed for multi-tenancy from scratch, informed by but not constrained by the old schema.
