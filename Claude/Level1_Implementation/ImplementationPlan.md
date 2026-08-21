# ProjectPal V2 — Level 1 Implementation Plan

## Contents

1. [Purpose](#purpose)
2. [Phases](#phases)
3. [Open Questions (Level-Wide)](#open-questions)

<a id="purpose"></a>
## 1. Purpose

Tracks the phases of building the Level 1 Demonstrator (`Requirements/Goals.md` §4.1; see `Scope.md` in this folder for what's in and out of scope). Each phase's actual plans, decisions, and supporting documents live in its own subfolder here — this document only tracks what the phases are and their current status. See `Claude/Guidelines/ImplementationApproach.md` for the convention this follows.

<a id="phases"></a>
## 2. Phases

| # | Phase | Status | Details |
|---|---|---|---|
| 1 | Database Setup | Done | [`DatabaseSetup/`](DatabaseSetup/) |
| 2 | REST API | Not started | [`RestApi/`](RestApi/) |
| 3 | Authentication | Not started | [`Authentication/`](Authentication/) |
| 4 | HTTPS / Reverse Proxy | Not started | [`HttpsReverseProxy/`](HttpsReverseProxy/) |
| 5 | GUI / Web Client | Not started | [`GuiClient/`](GuiClient/) |
| 6 | Automated Backups | Not started | [`AutomatedBackups/`](AutomatedBackups/) |
| 7 | Urgency Calculation | Not started | [`UrgencyCalculation/`](UrgencyCalculation/) |

This initial breakdown and ordering is provisional — carried over from `DatabaseSetup/InitialDatabaseSetupPlan.md` §8's "Out of Scope for This Pass" list — and is likely to be refined (reordered, split, or merged — e.g. Urgency Calculation may end up folding into the REST API phase) as work on each phase actually begins.

<a id="open-questions"></a>
## 3. Open Questions (Level-Wide)

- **Client technology** — browser-based web app vs. an installable cross-platform client (`Scope.md` §2, `Requirements/Goals.md` §4.1). Affects both the GUI / Web Client phase and the Deployment/packaging choice, so it's tracked here rather than in one phase's folder alone.
- **Auth model for the Demonstrator** — a single shared login vs. individually named users (`Scope.md` §2). Affects both the Authentication phase and the REST API phase's design.
- **Deployment/packaging mechanism** — how a customer site actually stands this up (Docker container, simple installer, or VM image). Not yet started as its own phase; may need to become one.
