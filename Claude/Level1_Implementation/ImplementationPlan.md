# ProjectPal V2 — Level 1 Implementation Plan

## Contents

1. [Purpose](#purpose)
2. [Phases](#phases)
3. [Open Questions (Level-Wide)](#open-questions)
4. [Decisions (Level-Wide)](#decisions)

<a id="purpose"></a>
## 1. Purpose

Tracks the phases of building the Level 1 Demonstrator (`Requirements/Goals.md` §4.1; see `Scope.md` in this folder for what's in and out of scope). Each phase's actual plans, decisions, and supporting documents live in its own subfolder here — this document only tracks what the phases are and their current status. See `Claude/Guidelines/ImplementationApproach.md` for the convention this follows.

<a id="phases"></a>
## 2. Phases

| # | Phase | Status | Details |
|---|---|---|---|
| 1 | Database Setup | Done | [`1_DatabaseSetup/`](1_DatabaseSetup/) |
| 2 | REST API | Done | [`2_RestApi/`](2_RestApi/) |
| 3 | Authentication | Not started | [`3_Authentication/`](3_Authentication/) |
| 4 | HTTPS / Reverse Proxy | Not started | [`4_HttpsReverseProxy/`](4_HttpsReverseProxy/) |
| 5 | GUI / Web Client | Not started | [`5_GuiClient/`](5_GuiClient/) |
| 6 | Automated Backups | Not started | [`6_AutomatedBackups/`](6_AutomatedBackups/) |
| 7 | Urgency Calculation | Not started | [`7_UrgencyCalculation/`](7_UrgencyCalculation/) |

Subfolders are prefixed with the phase number so they sort in order (see `Claude/Guidelines/ImplementationApproach.md` §2.3). This initial breakdown and ordering is provisional — carried over from `1_DatabaseSetup/InitialDatabaseSetupPlan.md` §8's "Out of Scope for This Pass" list — and is likely to be refined (reordered, split, or merged — e.g. Urgency Calculation is now settled as GUI-side work per `D1.2-2`, so Phase 7 may end up folding into the GUI/Web Client phase instead) as work on each phase actually begins.

<a id="open-questions"></a>
## 3. Open Questions (Level-Wide)

- **Q1-3: Deployment/packaging mechanism** — how a customer site actually stands this up. Not yet started as its own phase; may need to become one.

  **Needed by:** not urgent. No other Level 1 phase depends on this being resolved, and the actual packaging work can't complete until Phases 2–5 (REST API, Authentication, HTTPS/Reverse Proxy, GUI/Web Client) exist to be packaged. Latest it can be decided: before the Demonstrator is first handed to a real trial site, i.e. by the time Phase 5 (GUI/Web Client) wraps up. It can be settled earlier as just a direction, without blocking anything.

  **Options considered:**
  - *Docker Compose bundle* — extend the existing `docker-compose.yml` (already built for Postgres in Phase 1) to add the API, web app, and reverse proxy as more services.
    - **Pros:** reuses the tooling and skills already built for Database Setup; cross-platform; easy to update (`docker compose pull && up -d`) and cleanly reset, matching `1_DatabaseSetup/DataBaseHostingOptions.md`'s existing architecture.
    - **Cons:** requires Docker installed at the customer site — and Docker Desktop's licensing terms require a paid subscription for larger companies, which may matter for some prospective customers; whoever stands it up needs to be comfortable with a terminal.
  - *Simple installer* — a native Windows/Mac installer bundling Postgres, the API, and a web server into one install.
    - **Pros:** most approachable for a non-technical customer-site person — no Docker or terminal knowledge needed, feels like installing an ordinary desktop app.
    - **Cons:** substantial new engineering effort (a separate installer per OS, embedding/managing Postgres, ongoing installer maintenance) that's disproportionate for a trial-only Demonstrator, and reuses none of the Docker-based tooling already built.
  - *VM image* — a pre-built VM with everything installed and configured; the customer imports and boots it.
    - **Pros:** fully self-contained regardless of the host OS's installed software.
    - **Cons:** large download size; still needs the customer to have virtualization software and be reasonably comfortable importing a VM — a similar bar to Docker, for a heavier resource footprint; building and maintaining the image is itself ongoing work, comparable to or more than the Docker Compose option.

  **Current lean (not a decision):** Docker Compose bundle — it's the lowest-effort extension of what Phase 1 already built, and fits Level 1's "as cheaply as possible, minimal moving parts" framing (`Scope.md` §1) better than building new packaging machinery from scratch.

<a id="decisions"></a>
## 4. Decisions (Level-Wide)

- **D1-1** (decided 2026-08-21)<br>
  **Question:** Client technology for the Demonstrator: browser-based web app vs. an installable cross-platform client (`Scope.md` §2, `Requirements/Goals.md` §4.1).<br>
  **Decision:** browser-based web app for Level 1 — the simpler option, and sufficient on its own for the Demonstrator. A web app looks close to certain to be needed long-term regardless, so building it first is no wasted effort; an installable cross-platform client isn't ruled out, just deferred — similar to how a tool like Slack offers both a browser app and an installable desktop client, a later Level may add one *alongside* the web app rather than replacing it (see `Claude/Level2_Implementation/Scope.md`).
- **D1-2** (decided 2026-08-21)<br>
  **Question:** Auth model for the Demonstrator: a single shared login vs. individually named users (`Scope.md` §2).<br>
  **Decision:** individually named users, not a shared login. The domain model already carries everything this needs (`Person`, per-Team `PersonRole`, `is_organisation_admin`, `external_login`), and the API needs to know which Person is calling for almost every authorization check and ownership field anyway — a shared login would need its own workaround (e.g. a client-supplied "acting as" header) that's worse scaffolding than real per-Person auth, and Level 2/3 need named users regardless. Login is password-based: each Person gets a `password_hash`, and the API issues a JWT carrying `person_id`, Team/role memberships, and `is_organisation_admin` on successful login; every endpoint's authorization reads from that token rather than branching on shared-vs-named. This also raised building an admin-only impersonation capability (mint a JWT for a target Person, carrying an `impersonated_by` claim) so an admin can verify what another Person can/can't see and do — see `3_Authentication/Plan.md`'s `D1.3-1`/`D1.3-2`, which found this specific capability isn't actually needed for Level 1.
- **D1-4** (decided 2026-08-23)<br>
  **Question:** Feature scope — which of `Requirements/UseCases.md`'s use cases are essential for a meaningful Level 1 Demonstrator trial (`Requirements/Goals.md` §4.1's "feature scope" framing question)?<br>
  **Decision:** Manage Projects/Tasks, Assign Resources, Set Dependencies, Search, and Remarks are required — the essential set `UseCases.md`'s own Open Question `Q-UC-1` candidate-listed. Beyond that candidate list: the Gantt/plan view is required, not a deferral candidate — it's a key selling point of the Demonstrator, not a nice-to-have (revises `UseCases.md` §3's framing, which left it open either way). Attachments are required for `File` and `Link` kinds only — captured emails (`Mail` kind) are not required for Level 1. Admin/support tooling is required — re-derived from first principles per `UseCases.md`'s Administer the System use case, not mapped 1:1 from the old app's `AdminWindow` (see `2_RestApi/Plan.md` `Q1.2-4` for which specific capabilities). Concurrent access and editing by multiple users is required for Level 1 — conflicts (two users editing the *same* record at once) are not, since real usage during the Demonstrator avoids that scenario; see `Requirements/DomainModel.md`'s Cross-Cutting Concerns for the canonical statement of this.
