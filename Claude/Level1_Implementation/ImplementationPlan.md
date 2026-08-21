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
- **O1-2: Auth model for the Demonstrator** — a single shared login vs. individually named users (`Scope.md` §2). Affects both the Authentication phase and the REST API phase's design.
- **O1-3: Deployment/packaging mechanism** — how a customer site actually stands this up (Docker container, simple installer, or VM image). Not yet started as its own phase; may need to become one.

<a id="decisions"></a>
## 4. Decisions (Level-Wide)

None yet. When a Level-wide open question above is answered, its entry moves here as `D1-<N>` (same number, `D` prefix — see `Claude/Guidelines/ImplementationApproach.md` §3.1), recording the original question, the decision, and the date.
