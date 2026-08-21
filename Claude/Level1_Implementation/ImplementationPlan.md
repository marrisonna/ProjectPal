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
| 2 | REST API | Not started | [`2_RestApi/`](2_RestApi/) |
| 3 | Authentication | Not started | [`3_Authentication/`](3_Authentication/) |
| 4 | HTTPS / Reverse Proxy | Not started | [`4_HttpsReverseProxy/`](4_HttpsReverseProxy/) |
| 5 | GUI / Web Client | Not started | [`5_GuiClient/`](5_GuiClient/) |
| 6 | Automated Backups | Not started | [`6_AutomatedBackups/`](6_AutomatedBackups/) |
| 7 | Urgency Calculation | Not started | [`7_UrgencyCalculation/`](7_UrgencyCalculation/) |

Subfolders are prefixed with the phase number so they sort in order (see `Claude/Guidelines/ImplementationApproach.md` §2.3). This initial breakdown and ordering is provisional — carried over from `1_DatabaseSetup/InitialDatabaseSetupPlan.md` §8's "Out of Scope for This Pass" list — and is likely to be refined (reordered, split, or merged — e.g. Urgency Calculation may end up folding into the REST API phase) as work on each phase actually begins.

<a id="open-questions"></a>
## 3. Open Questions (Level-Wide)

- **O1-1: Client technology** — browser-based web app vs. an installable cross-platform client (`Scope.md` §2, `Requirements/Goals.md` §4.1). Affects both the GUI / Web Client phase and the Deployment/packaging choice, so it's tracked here rather than in one phase's folder alone.
- **O1-3: Deployment/packaging mechanism** — how a customer site actually stands this up (Docker container, simple installer, or VM image). Not yet started as its own phase; may need to become one.

<a id="decisions"></a>
## 4. Decisions (Level-Wide)

- **D1-2** (decided 2026-08-21)<br>
  **Question:** Auth model for the Demonstrator: a single shared login vs. individually named users (`Scope.md` §2).<br>
  **Decision:** individually named users, not a shared login. The domain model already carries everything this needs (`Person`, per-Team `PersonRole`, `is_organisation_admin`, `external_login`), and the API needs to know which Person is calling for almost every authorization check and ownership field anyway — a shared login would need its own workaround (e.g. a client-supplied "acting as" header) that's worse scaffolding than real per-Person auth, and Level 2/3 need named users regardless. Login is password-based: each Person gets a `password_hash`, and the API issues a JWT carrying `person_id`, Team/role memberships, and `is_organisation_admin` on successful login; every endpoint's authorization reads from that token rather than branching on shared-vs-named. This also gives the Demonstrator an admin-only impersonation capability (mint a JWT for a target Person, carrying an `impersonated_by` claim) so an admin can verify what another Person can/can't see and do — see `3_Authentication/Plan.md` for the phase-specific open questions this raises (`O1.3-1`, `O1.3-2`).
