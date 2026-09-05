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
- **Full QA-staged release pipeline** — a proper Dev → QA → Prod promotion process: versioned release packages (both a from-scratch install and an incremental upgrade), QA torn down and recreated as a live copy of Prod before each validation pass, and regression/new-feature testing in QA before promoting the same package to Prod. Level 1 uses a lighter equivalent with no standing QA environment (`Claude/Level1_Implementation/7_ProductionDeployment/Plan.md`) — this Level extends that foundation rather than replacing it.

<a id="deferred-from-level-1"></a>
## 3. Deferred From Level 1

- **Installable cross-platform client, alongside the web app.** Level 1 builds a browser-based web app only (`D1-1` in `Claude/Level1_Implementation/ImplementationPlan.md`), packaged as an installable PWA rather than a `chromium --app` launcher or a genuine native client (`D1.4-5` in `Claude/Level1_Implementation/4_GuiClient/Plan.md`). A native/installable cross-platform client (in the way a tool like Slack offers both a browser app and a desktop app — Slack's desktop app is Electron: a bundled Chromium + Node runtime packaged as a real native executable) wasn't ruled out, just deferred — not yet committed to this Level specifically, but worth considering here as customer needs become clearer.<br>
  Assessed as an additive wrapper around the Level 1 build, not a rework of it: the entire React app carries over essentially untouched — every feature component, the API client (`api/hooks.ts`/`api/types.ts`), `DenseField.tsx`, the multi-window *concept* itself (named singleton windows, `D1.4-8`), the `branding.json` theme system, and the whole REST API/DB layer (Electron just talks HTTP to the same backend), which is exactly what `D1.4-6`'s "keep business logic out of the GUI layer" decision was for. Genuinely new work: a small Electron main process (window creation/lifecycle, `windowNav.ts`'s `window.open()` calls intercepted via `setWindowOpenHandler` instead of the browser handling them — each singleton window becomes a real `BrowserWindow` with its own icon/title, incidentally fixing the OS-shell title-prefixing and shared-icon limitations a PWA window has); minor Vite build changes (relative asset paths, likely `HashRouter` over `BrowserRouter` for `file://` loading); the API base URL becoming real config instead of dev's Vite proxy trick (already anticipated by `D1.6-4`'s reverse-proxy design); and removing the now-pointless PWA manifest/service worker (`vite-plugin-pwa`, `D1.4-5`). The real cost is standing up packaging, code-signing, and auto-update infrastructure (electron-builder/forge) — an ongoing operational surface Level 1 doesn't have at all today — not the application code itself.
- **Team-specific configurable weights for the Urgency algorithm.** Level 1 computes Urgency client-side using the fixed algorithm in `Requirements/KeyConcepts.md` §12 (`D1.2-2` in `Claude/Level1_Implementation/2_RestApi/Plan.md`). Letting each Team configure its own weights is a likely later refinement, not committed to this Level specifically yet.
- **Self-service password change.** Level 1 is admin-set-password only (`D1.3-4` in `Claude/Level1_Implementation/3_Authentication/Plan.md`) — a Person changing their own password is required eventually, just not built yet.
- **Failed-login lockout/rate-limiting.** Level 1 has no protection against repeated failed login attempts (`D1.3-7` in `Claude/Level1_Implementation/3_Authentication/Plan.md`), consistent with `Scope.md`'s "security is not the top concern" framing — worth revisiting alongside this Level's "real security baseline" item above.
- **A standing QA environment and full release-package pipeline.** Level 1 gets by on migration versioning, a verified backup, and a rollback script — real safety without a standing QA environment (`Claude/Level1_Implementation/7_ProductionDeployment/Plan.md`). The full Dev/QA/Prod promotion pipeline (§2 above) is deferred to this Level, once release frequency and more than one customer justify the standing infrastructure.
- **Backup retention/cleanup policy.** Level 1 backs up Prod daily and keeps every backup forever (`D1.7-2` in `Claude/Level1_Implementation/7_ProductionDeployment/Plan.md`) — fine while the database is small and the OneDrive quota backing it is large. Pruning old backups is deferred to this Level, once either of those stops being true.
- **A committed end-to-end/browser-automation test suite for the GUI.** Level 1 relies on manual testing only, against the real running `rest-api`, for every GUI screen (`D1.4-9` in `Claude/Level1_Implementation/4_GuiClient/Plan.md`) — worth building real automated browser coverage once release cadence and screen count justify the investment.
- **A runtime GUI theme/branding editor.** Level 1 centralises visual theming in one build-time config file, `gui-client/branding.json` (`D1.4-12` in `Claude/Level1_Implementation/4_GuiClient/Plan.md`) — a rebrand means editing that file and rebuilding, not a live settings UI. Worth building a real runtime theme editor (and a place to persist the choice) once multiple Organisations plausibly need different branding from the same running deployment simultaneously, which a build-time config can't serve.
- **Editing a Person's per-Team nickname.** Level 1 adds `person_role.nickname` (`D-DM-11` in `Claude/Requirements/DomainModel.md`; `D1.4-21` in `Claude/Level1_Implementation/4_GuiClient/Plan.md`) and displays it wherever a screen's data belongs to a Team, but it's read-only — only ever set via seed data. Editing it is a team-lead-facing capability, almost certainly belonging on a dedicated Team Management screen once one exists (the only place it should be editable, not scattered inline across Task/Project/Component Detail) — that screen doesn't exist yet at Level 1 (Manage People is Stage 4 scope in `4_GuiClient/Plan.md`'s `D1.4-3`, and doesn't itself cover per-Team nickname editing). Needs its own `WritePersonRoleRequest`/`UpdatePersonRoleRequest` field and route support in `rest-api/app/routes/teams.py`, currently deliberately omitted.
