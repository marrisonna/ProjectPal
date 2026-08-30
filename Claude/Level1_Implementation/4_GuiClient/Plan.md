# ProjectPal V2 — GUI / Web Client Phase

## Contents

1. [Status and Purpose](#status-and-purpose)
2. [Scope](#scope)
   - 2.1 [In Scope for Level 1](#in-scope)
   - 2.2 [Deferred](#deferred)
3. [Architecture and Technology](#architecture)
   - 3.1 [Constraints Inherited From Other Phases](#inherited-constraints)
   - 3.2 [Framework and Build Tooling](#framework)
   - 3.3 [Component Library](#component-library)
   - 3.4 [Data Fetching and the Typed API Client](#data-fetching)
   - 3.5 [Routing](#routing)
   - 3.6 [Refresh Strategy](#refresh-strategy)
   - 3.7 [App Shell Presentation (PWA)](#app-shell)
   - 3.8 [Business Logic Placement](#business-logic-placement)
4. [Multi-Window / Multi-Monitor Interaction Model](#multi-window)
5. [Screen Inventory](#screen-inventory)
6. [Build Order](#build-order)
   - 6.1 [Stage 1 — Foundation](#stage-1)
   - 6.2 [Stage 2 — Pilot: Task List + Task Detail](#stage-2)
   - 6.3 [Stage 3 — Pilot: Gantt / Plan View](#stage-3)
   - 6.4 [Stage 4 — Remaining Screens](#stage-4)
7. [Testing](#testing)
8. [Definition of Success](#definition-of-success)
9. [Open Questions (Phase-Specific)](#open-questions)
10. [Decisions (Phase-Specific)](#decisions)

<a id="status-and-purpose"></a>
## 1. Status and Purpose

**Status:** In progress — Stage 1 (Foundation, §6.1) underway: `gui-client/` scaffolded (React/TypeScript/Vite), login/JWT/routing/typed-API-client wired and verified end-to-end against the real REST API, PWA app shell in place. Dashboard is still a placeholder pending Stage 2's Task data.

Build a browser-based web app as the client GUI for the Demonstrator, per `D1-1` in `../ImplementationPlan.md`. This is the largest phase in Level 1 by feature surface: it's responsible, end to end, for every screen a user actually touches, plus two pieces of derived logic that live entirely on the client — the Gantt/plan view's layout and the Urgency calculation.

<a id="scope"></a>
## 2. Scope

<a id="in-scope"></a>
### 2.1 In Scope for Level 1

Settled by `D1-4` (`../ImplementationPlan.md`) and `../Scope.md`: Manage Projects/Tasks, Assign Resources, Set Dependencies, Search, Remarks, the Gantt/plan view, and File/Link attachments are all required — not optional trial content. Admin/support tooling is required. Concurrent multi-user access is required; conflict handling for two users editing the same record at once is not (`D-DM-3`).

`Requirements/UseCases.md`'s View the Plan (Gantt) use case is required for Level 1 (`D1-4` — a key selling point, not a deferral candidate) and is this phase's responsibility end to end — the database and API need nothing special for it (`../2_RestApi/Plan.md` §2.2). The plan/Gantt view is built by composing the Task, Project, and Dependency data the REST API already exposes (schedule derivation, layout, and rendering all happen client-side), not by a bespoke aggregation endpoint.

Urgency (`Requirements/KeyConcepts.md` §12) is also this phase's responsibility end to end, per `D1.2-2` in `../2_RestApi/Plan.md` — computed client-side from the Task/Project fields the API already exposes, not served pre-computed. Being dynamic (it changes with the passage of time alone, with no underlying data change) fits naturally with computing it where it's displayed rather than re-fetching it. Team-specific configurable weights for the algorithm are a likely later refinement (not Level 1 — see `Claude/Level2_Implementation/Scope.md`), and fit this GUI-side placement more naturally than a server-side per-Team lookup would.

**Urgency needs the whole Project ancestor chain, not just one Task's Project.** `Requirements/KeyConcepts.md` §12's "effective priority" factor is computed root-first over *every* ancestor Project above a Task (the Task's own Project, that Project's parent, and so on), not just the immediate one — a Project tree can be arbitrarily deep via `parent_project_id`. This phase needs to fetch the whole Project tree (or otherwise be able to walk parent links for any Task's ancestry) to compute Urgency correctly — a single Task fetch plus its one immediate Project is not sufficient. Level 1's data volumes (one Organisation, a handful of Teams/Projects) make fetching the whole tree trivial; this stops being free once Level 2/3 have many tenants/Projects, but that's out of scope here.

`5_UrgencyCalculation/` is expected to fold entirely into this phase (see that folder's stub) — Urgency has no independent existence outside of being displayed somewhere in this GUI, so there's no separate deliverable to track once this phase covers it.

<a id="deferred"></a>
### 2.2 Deferred

- Merge/conflict-resolution UI (`Requirements/UserInterfaceWindows.md` §3.18) — no Level 1 GUI screen needed, per `D-DM-3`.
- The old app's Private-Task visibility toggle and its `ConfigWindow` (§3.15) — dropped from the domain model; nothing to build.
- A dedicated impersonation UI — not needed per `D1.3-1`; an admin can just log in as the target Person directly.
- Real-time push updates (WebSockets/live collaboration) — see §3.6 below.
- Drag-and-drop as the primary interaction for reparenting and dependency creation — not ruled out forever, but not Level 1's starting design; see §5 and §6.2 below for how the initial build stays ready to add it without rework.
- A native/installable client alongside the web app — `Claude/Level2_Implementation/Scope.md`.

<a id="architecture"></a>
## 3. Architecture and Technology

<a id="inherited-constraints"></a>
### 3.1 Constraints Inherited From Other Phases

Three decisions made in earlier phases shape this one before any GUI-specific choice gets made:

- **The build output must be static files.** `6_HttpsReverseProxy/Plan.md`'s `D1.6-4` already commits Caddy to path-based routing where `/api/*` goes to the REST API and `/` serves this phase's static files directly — there is no second "GUI server" process in the stack, and adding one would mean revisiting a decision already made and documented. This rules out any framework whose default deployment model is a persistent Node.js server (e.g. Next.js in SSR mode) unless used purely in a static-export mode. A conventional single-page-application (SPA) build — HTML/CSS/JS bundled by a build tool, served as-is — fits what's already been decided without any further discussion.
- **The API base URL must be configurable, not hardcoded, and a cross-origin dev request needs a real answer before Phase 6 exists.** A browser calling `http://localhost:8000` directly from the Vite dev server's own origin (`http://localhost:5173`) is genuinely cross-origin — it fails on CORS unless the API grants it, which would mean adding (and later removing) API-side CORS configuration purely for a dev convenience. Resolved by `D1.4-11`: the GUI's API base URL is the env-configurable `VITE_API_BASE_URL` (`gui-client/.env.example`), set to the relative `/api`, and Vite's dev server proxies `/api/*` to the REST API (`gui-client/vite.config.ts`) — the same relative path and prefix-stripping behaviour Caddy's `/api/*` routing (`D1.6-4`) will use in production, so this needs no later change at all, and the browser never makes a cross-origin request in the first place.
- **Auth is a bearer JWT, not a cookie/session.** `3_Authentication/Plan.md` already settled the token shape and `POST /auth/login` contract. The GUI's job is: collect credentials, call that endpoint, hold the returned JWT (in memory plus `sessionStorage` so a page refresh doesn't force a re-login mid-session), and attach it as `Authorization: Bearer <token>` on every subsequent API call. No cookie handling, no CSRF concerns, no server-side session store.

<a id="framework"></a>
### 3.2 Framework and Build Tooling

**Decision (see `D1.4-1` below): React, TypeScript, and Vite.**

Reasoning:
- Vite's default build output for a React app *is* a static bundle — it matches §3.1's constraint with no extra configuration or "static export mode" opt-in to get wrong.
- React is the most widely used UI framework by a wide margin, which matters concretely for a project built with heavy AI-assistant involvement: more training data and community precedent means fewer novel mistakes, and a wider pool of maintainers later if this ever needs a second pair of hands.
- TypeScript pairs naturally with the REST API, which already publishes a real OpenAPI document (`../2_RestApi/Plan.md`). Generating types (and a client — §3.4) directly from that document keeps the GUI's understanding of the API's shape mechanically in sync with the API itself, rather than hand-copied and prone to drifting silently out of date.
- This is a conventional, low-risk choice, not an exotic one — there was no strong reason found to prefer Vue, Svelte, or Angular instead; any of them would have satisfied §3.1 equally well, but React's ecosystem depth (component libraries, Gantt-adjacent libraries evaluated in Stage 3, community answers to obscure problems) is the deciding factor given a solo-developer-plus-AI-assistant team shape.

<a id="component-library"></a>
### 3.3 Component Library

**Decision (`D1.4-1`, bundled with §3.2): MUI (Material UI), specifically its core component set plus the `@mui/x-data-grid` and `@mui/x-tree-view` extensions.**

ProjectPal's UI is fundamentally an enterprise data-management tool — grids, trees, detail forms, modal dialogs — not a marketing site or a consumer app, and MUI is built squarely for that shape of application:
- `@mui/x-data-grid`'s free/Community tier (sortable, filterable, paginated columns) directly covers what `TaskWindow`/`ProjectWindow`-style list grids (`Requirements/UserInterfaceWindows.md` §3.2, §3.4) need for Level 1 — nothing here requires the paid Enterprise tier (row grouping, pivoting, etc. aren't Level 1 requirements).
- `@mui/x-tree-view` directly covers the Project/Component hierarchy trees (§3.5, §3.8) without a bespoke tree component being built from scratch.
- MUI is (like React itself) the most widely used React component library, for the same AI-assistant-leverage reason given in §3.2.

Ant Design was considered as a credible alternative (equally comprehensive, equally aimed at admin-tool-style UIs) but wasn't chosen over MUI absent a specific reason to prefer it; either would have worked.

<a id="data-fetching"></a>
### 3.4 Data Fetching and the Typed API Client

**Decision (`D1.4-1`, bundled): TanStack Query for server-state (caching, loading/error states, refetch-after-mutation), fed by a client typed directly from `../2_RestApi/`'s published `openapi.json` (via `openapi-typescript` for types, `openapi-fetch` as the thin typed fetch wrapper).**

Almost everything this GUI holds in memory *is* server state (a Task, a page of search results, the current user's identity) rather than genuine client-only state (which form field is focused, whether a dialog is open). TanStack Query is purpose-built for the former and removes most of the hand-rolled loading-flag/error-flag/stale-cache bookkeeping a simpler `fetch`-in-`useEffect` approach would otherwise need throughout every screen. A heavier general-purpose state library (Redux, MobX) wasn't chosen because there's very little of the *latter* kind of state that would justify it — local component state (`useState`) is enough for what's left.

Generating types from `openapi.json` rather than hand-writing them means a change to the API's contract shows up as a TypeScript compile error in the GUI immediately, rather than as a runtime failure discovered later.

<a id="routing"></a>
### 3.5 Routing

**Decision (`D1.4-1`, bundled): React Router, with a real, stable, deep-linkable URL for every item** — `/tasks/:taskId`, `/projects/:projectId`, `/components/:componentId`, and so on, plus list routes (`/tasks`, `/projects`) and the standalone `/plan` Gantt view. This isn't just a routing-library choice — clean per-item URLs are the load-bearing mechanism behind §4's multi-window decision below, so this is worth calling out as a first-class requirement on every detail screen from Stage 1 onward, not an afterthought.

<a id="refresh-strategy"></a>
### 3.6 Refresh Strategy

**Decision (`D1.4-1`, bundled): no real-time push (no WebSockets, no polling timer).** Data refreshes when a screen is navigated to or a browser tab regains focus (TanStack Query's default `refetchOnWindowFocus` behaviour), and after any mutation the GUI itself makes. `Requirements/DomainModel.md`'s Cross-Cutting Concerns already accepts that Level 1 avoids concurrent-edit conflicts through light real usage rather than building conflict-resolution machinery (`D-DM-3`); a live-push mechanism would be solving a problem (staleness during someone else's edit) that Level 1 has already decided not to treat as a real risk. `Requirements/KeyConcepts.md`'s Merge/Conflict entry already flags real-time collaboration as a Level 2 question, not a Level 1 one — this keeps that boundary intact rather than quietly building half of it now.

<a id="app-shell"></a>
### 3.7 App Shell Presentation (PWA)

**Decision (`D1.4-5` below): package the GUI as an installable Progressive Web App** — a `manifest.json` (`display: "standalone"`, name, icons) plus a minimal service worker, on top of the same React/Vite build already decided (§3.2). No new language, runtime, or packaging pipeline.

An ordinary browser tab carries an address bar, other tabs, and a bookmarks bar — it doesn't read as "an app." A `standalone`-display PWA strips all of that: once installed (via the browser's own "Install app" prompt), it opens in its own chrome-less window, gets its own icon in the Start Menu/taskbar, and appears as its own entry in Alt-Tab, distinct from the browser. This is the same underlying browser windowing behaviour as launching Chromium with `--app=<url>`, but delivered through a W3C-standard mechanism the browser already provides, rather than a custom shortcut/launcher that would need building and distributing separately.

A native wrapper (Electron, Tauri, or a genuine native client in C#/Java) was also considered and set aside for Level 1: a native client is an explicitly *deferred*, not-yet-committed Level 2 option (`Claude/Level2_Implementation/Scope.md`), and building one now would mean walking back `D1-1` for no functional gain the PWA route doesn't already deliver. Tauri specifically is worth keeping in mind as the natural upgrade path if a genuinely native capability is wanted later (a system tray icon, offline use, OS-level drag-and-drop between windows) — it wraps the same web codebase built here in a thin native shell, rather than requiring a rewrite the way a C#/Java client would.

A PWA install prompt needs a secure context (HTTPS, or `localhost`, which browsers already treat as secure for this purpose). Development against `localhost` during Stages 0–3 is unaffected; full installability for real users naturally lands once `6_HttpsReverseProxy` exists, which the build order already has coming after this phase.

<a id="business-logic-placement"></a>
### 3.8 Business Logic Placement

**Decision (`D1.4-6` below): authoritative business rules live server-side, in Python, in the REST API; the GUI is deliberately kept thin.**

The dividing line: anything that must hold true regardless of which client is asking — validation, authorization (Team-scoped checks already enforced in `rest-api/app/security/deps.py` per `D-UC-4`), anything protecting data integrity — belongs in the API, because the API is the actual trust boundary. A browser client today, and any future client, both have to go through it, and only server-side enforcement can be relied on to actually hold.

The **only** logic that legitimately lives client-side, in TypeScript, is the narrow exception already decided elsewhere in this plan: Urgency (`D1.2-2`) and Gantt schedule derivation (§2.1, §6.3) — both pure, presentation-time computations over data the client already legitimately holds, recomputed because they're cheap and dynamic rather than because the GUI is where business rules generally belong. Any future candidate for client-side logic should be tested against the same question before being added, rather than treating this carve-out as a general licence.

Within that carve-out, schedule-derivation math (given Tasks/Dependencies/Resources, compute each Task's effective position on the timeline) is written as plain, dependency-free TypeScript functions, kept separate from the React components that render the Gantt bars — unit-testable on its own, and, usefully, portable to any other JS/TS runtime later (a Tauri shell, a future non-browser client) with no rework, since it has no DOM dependency.

<a id="multi-window"></a>
## 4. Multi-Window / Multi-Monitor Interaction Model

This resolves `Requirements/UserInterfaceWindows.md`'s `Q-Win-3`.

**The old app's behaviour.** V1.2 is a WinForms desktop app where every detail screen is a genuine, independent, freely-positionable OS-level window, with a singleton-per-object pattern (double-clicking the same Task twice re-focuses its existing window rather than opening a duplicate). On a large screen or a multi-monitor setup, a user can have the Task list on one monitor, a specific Task's detail open on another, and the Gantt view on a third, all visible and interactive simultaneously. Several documented interactions (e.g. dragging a Task from one open window onto a Project in another) directly depend on this.

**Options considered for a browser-based web app:**

- **A — App-managed pop-out windows.** Use `window.open()` to spawn genuine separate browser windows per item, each independently draggable to any monitor, with the app tracking which windows are open (to implement singleton-per-object re-focusing) and synchronising state between them (e.g. via `BroadcastChannel`, so editing a Task in one window tells a sibling window showing the same Task to refetch). This most faithfully reproduces V1.2's behaviour, but is real, ongoing engineering: window-registry bookkeeping, cross-window messaging, and popup-blocker edge cases (browsers block `window.open()` calls not triggered directly by a user click/gesture — manageable, since double-clicking a row qualifies, but a real constraint to design around).
- **B — Rely on the browser's own native window handling for placement, but use named window targets for singleton-per-object.** If every item has a real, stable URL (§3.5) and every "open this item" action calls `window.open(url, name)` with a **deterministic name per item** (e.g. `task-123`, not `_blank`), the browser itself refuses to open a duplicate: a second call with the same name navigates and refocuses the *existing* window instead of opening a new one. This is native, standard `window.open` behaviour, not something built by hand. Combined with each such window being an installed PWA window (§3.7 — chrome-less, independently positionable on any monitor), this reproduces V1.2's actual pattern — many *different* items open across a user's monitors, never two windows for the *same* item — without a custom window registry, `BroadcastChannel` messaging, or the popup-blocker concerns Option A's more general approach raises (a `window.open` call triggered directly by a click, which double-clicking a row or clicking "open in new window" both are, isn't blocked).
- **C — An in-app docking/tiling workspace** (VS-Code-style split panes within a single browser window, via a library such as `react-mosaic` or `golden-layout`). Gives a multi-pane experience inside one window, but not genuine separate OS windows spanning multiple physical monitors unless combined with Option A or B anyway — meaning it adds real complexity (a docking-layout library, its own state to persist) without actually answering the multi-*monitor* part of the question on its own.

**Decision (`D1.4-8` below, superseding `D1.4-2`): Option B.** The default interaction — clicking a Task row from a list, following a cross-reference — navigates the *current* window in place; this is the common case, and popping a new window for every click is exactly the "explosion of duplicate windows" the old app's singleton pattern existed to prevent. A separate, explicit "open in new window" action (plus Ctrl/middle-click on links, per normal browser convention) is what calls `window.open(url, name)` with the item's deterministic name — giving both a direct equivalent to V1.2's double-click-to-pop-out habit *and* true singleton-per-object re-focusing, from the start, using a browser primitive rather than hand-rolled bookkeeping. Cross-window data staleness (editing a Task in its own window doesn't instantly update a list showing it in another) is unaffected by this decision and remains covered by the refetch-on-focus behaviour already decided in §3.6.

Cross-window *drag-and-drop* (dragging an item from one open window onto another — the specific old-app example this question was raised over) is **not** delivered by this decision on its own; see §6.2's drag-and-drop-readiness principle and `D1.4-10` below for the spike that determines whether it's built for Level 1 or deferred.

<a id="screen-inventory"></a>
## 5. Screen Inventory

Maps every window in `Requirements/UserInterfaceWindows.md` §3 to its V2 equivalent, and flags where V2 deliberately modernises rather than replicates the old interaction:

| V1.2 Window (§) | V2 Equivalent | Level 1? | Notes |
|---|---|---|---|
| MainWindow (3.1) | App shell + dashboard | Yes | Simplified nav hub; the per-Resource workload report becomes a dashboard panel, not the sole landing page |
| TaskWindow (3.2) | Task List | Yes | `@mui/x-data-grid`; its embedded Gantt tab becomes a link to the standalone `/plan` view rather than a second Gantt renderer |
| TaskDetail (3.3) | Task Detail | Yes | Stage 2 pilot — richest single screen; see §6.2 |
| ProjectWindow (3.4) | Project List | Yes | |
| ProjectDetail (3.5) | Project Detail | Yes | Hierarchical tree via `@mui/x-tree-view`; built in Stage 4 once the pattern is proven |
| NewProject (3.6) | Create/rename Project dialog | Yes | Simple modal |
| Plan Display (3.7) | Gantt / Plan View | Yes (`D1-4`) | Stage 3 pilot — see §6.3 |
| ComponentWindow (3.8) | Component List/Detail | Yes | Reuses Project Detail's tree pattern |
| NewComponent (3.9) | Create/rename Component dialog | Yes | Simple modal |
| RemarkWindow (3.10) | Inline Remarks panel | Yes | Modernised: no separate window — an inline comment thread embedded directly in Task/Project/Component Detail, built once as part of the Stage 2 pilot and reused everywhere |
| Find (3.11) | Search | Yes | |
| Manage People (3.12) | Manage People | Yes | Admin-only |
| New Person (3.13) | Create Person dialog | Yes | Simple modal |
| AdminWindow (3.14) | Admin tooling screen | Yes (`D1-4`) | Thin UI over the already-built `/admin/export` and `/admin/integrity-check` endpoints — trigger buttons plus a results panel, not a rebuild of the old window |
| ConfigWindow (3.15) | — | No | Private-item visibility dropped from the domain model; "hide closed Projects" folds into an ordinary list filter |
| PasswordWindow (3.16) | Login screen | Yes | Real password login (`3_Authentication`) replaces whatever the old app did here |
| Progress Indicator (3.17) | Loading spinners | Yes | An ordinary UI pattern (MUI's own components), not a distinct screen to design |
| Merge Dialogs (3.18) | — | No | Deferred, `D-DM-3` |
| GridFilterSelect (3.19) | Grid column filter | Yes | `@mui/x-data-grid`'s built-in column filtering — not a bespoke component |

Two interactions `Requirements/UseCases.md` explicitly flags as "UX choices to redesign for the new client technology" rather than commitments to preserve get a Level 1 answer here, both favouring explicit controls over drag-and-drop for the first build:

- **Assign Resources to a Task** (`UseCases.md` #2) — kept as an explicit multi-select checklist in Task Detail, matching the old app's actual model; the use case's own open question ("rather than drag-and-drop directly onto the Gantt view") is answered here: not for Level 1. Drag-onto-Gantt is a plausible future enhancement once the Gantt view itself (Stage 3) exists to drop things onto.
- **Set Dependencies** (`UseCases.md` #4) — the old app's drag-between-two-listboxes interaction becomes an explicit "Add Dependency" search-and-pick dialog (search or browse for a Task/Project, add it as a predecessor/successor). Simpler to build correctly than a drag target, and no less usable for Level 1's trial-scale data volumes.

Both are revisitable UX refinements, not architectural commitments. What actually determines whether drag-and-drop can be added later without rework isn't the presence of a DnD library now — it's whether the underlying data mutations (reparent a Task, add a Dependency, assign a Resource) are written as standalone, reusable functions rather than inlined into a dialog's save handler; see `D1.4-7` and §6.2.

<a id="build-order"></a>
## 6. Build Order

Sequenced in stages rather than a flat list, so that the two riskiest technology/architecture questions get answered by building real, feature-rich screens early — per the instruction that shaped this plan, the first screen(s) built should carry enough feature weight to actually exercise the stack, not be a trivial "hello world."

<a id="stage-1"></a>
### 6.1 Stage 1 — Foundation

Not itself a proving-ground screen, but the scaffolding every later stage depends on: project setup (Vite + React + TypeScript), the generated OpenAPI client (§3.4), the login screen and JWT handling (§3.1), the app shell/navigation frame and routing skeleton (§3.5), and the dashboard/workload-report panel (§5's MainWindow equivalent — a simpler build than the pilots, doesn't need its own proving-ground treatment).

<a id="stage-2"></a>
### 6.2 Stage 2 — Pilot: Task List + Task Detail

The first architecture-proving pilot. Task Detail (`UserInterfaceWindows.md` §3.3, "the most heavily used window in the app") is deliberately chosen for its breadth: it exercises permission-gated field rendering (Team-scoped roles from the JWT), a Project/Component picker, the dependency panel (§5's redesigned "Add Dependency" dialog), the Resource-assignment checklist, the inline Remarks panel, and the Attachments panel (upload, list, download/open for File and Link kinds) — nearly every reusable sub-component the rest of the GUI will need gets built once, here, then reused unchanged by Project Detail and Component Detail in Stage 4. A minimal Task List (grid, no advanced filtering yet) is built alongside it purely as the navigation entry point into Task Detail, not as a pilot in its own right — `@mui/x-data-grid` handles most of its complexity already.

By the end of this stage, the framework/library choices in §3 are validated against real, complex, stateful UI, not just a form or two.

**Drag-and-drop readiness (`D1.4-7`).** None of Stage 2's data-changing interactions (reparenting via the Project/Component picker, adding a Dependency, assigning a Resource) are built as logic inlined into a dialog's save button. Each is a standalone, reusable mutation (e.g. a `useReparentTask()` hook wrapping the underlying API call) that the explicit picker/dialog UI calls today, and that a drag-drop handler could call unchanged later — adding drag-and-drop becomes wiring a new interaction on top of an existing mutation, not reworking the mutation itself. This costs nothing extra now (it's already good practice for testability) and is the actual prerequisite for keeping drag-and-drop viable, not any particular library choice. In-window drag-and-drop (e.g. within the Gantt view, or a list next to a tree in the same screen) is a straightforward later addition on this foundation using a library such as `@dnd-kit`.

**Cross-window drag-and-drop spike (`D1.4-10`).** Before the end of this stage, spend a short, scoped spike on the one interaction that most exercises it — dragging a Task from its own open window onto a Project shown in a different open window, wired to the same `useReparentTask()` mutation `D1.4-7` already establishes — to learn the real issues first-hand (native HTML5 drag-and-drop doesn't reliably cross separate top-level browser windows, so this needs hands-on investigation, not a guess from documentation). If the spike lands cleanly, build it out properly as part of this stage's deliverable; if it doesn't, defer cross-window drag-and-drop to Level 2 and record that in `Claude/Level2_Implementation/Scope.md`. Either way, this is decided from what the spike actually shows, not assumed either way ahead of it.

<a id="stage-3"></a>
### 6.3 Stage 3 — Pilot: Gantt / Plan View

The second architecture-proving pilot, kept deliberately separate from Stage 2 because it's a genuinely different kind of technical problem — layout/scheduling computation and custom rendering, not forms and grids — and `UseCases.md` itself flags it as needing its own evaluation as "one of the most technically demanding [screens] to reproduce outside WinForms."

This stage starts with a short, hands-on spike rather than a library choice made from documentation alone: build the same small proof-of-concept plan view (hierarchical Task/Project/Component rows, dependency lines, a "today" marker, colour-coding) against real seeded data, using each of two leading MIT-licensed, React-usable candidates — `gantt-task-react` (a dedicated React Gantt component) and `frappe-gantt` (a lightweight, framework-agnostic Gantt with built-in drag-to-reschedule) — then decide between them, or fall back to a custom SVG/canvas-based build if neither proves adequate. Commercial options (Bryntum Gantt, DHTMLX's paid tier) and DHTMLX's free GPL edition were considered and set aside for this evaluation: Bryntum has no meaningful free tier (out of step with Level 1's "as cheaply as possible" framing, `Scope.md` §1); DHTMLX's GPL edition raises a real open-source-licensing question for a closed-source commercial product that's better resolved later, with legal input, only if the MIT-licensed candidates turn out to be inadequate. The secondary per-Resource workload-over-time chart (distinct from the Gantt bars themselves) is a much lower-risk, standard bar/area chart and doesn't need this evaluation — any conventional React charting library (e.g. `recharts`) is sufficient.

Urgency (§2.1) is computed and displayed here for the first time, since the Gantt view is where it's most naturally surfaced (colour-coding rows by urgency is a plausible use of it) — though it also needs to be available wherever a Task appears (Task List, Task Detail), so the calculation itself is built as a shared utility, not tied to this screen.

<a id="stage-4"></a>
### 6.4 Stage 4 — Remaining Screens

Once both pilots have validated the stack, roll out the rest of §5's inventory, reusing what Stage 2/3 already built:

- Project List + Project Detail (the hierarchical tree pattern, `@mui/x-tree-view`; dependency/attachment/remarks panels reused from Stage 2 unchanged).
- Component List + Component Detail (reuses Project Detail's tree pattern).
- Search/Find.
- Manage People (admin grid + create-Person dialog) — needed before real trial-user onboarding can begin, though not blocking for initial internal testing against the already-seeded People.
- Admin tooling screen (thin UI over `/admin/export` and `/admin/integrity-check`).

This stage is deliberately left at survey level here — it's built from patterns the pilots will have already settled, so detailed screen-by-screen design is deferred until Stage 4 actually starts, consistent with how other not-yet-reached phases in this plan are documented.

<a id="testing"></a>
## 7. Testing

- Component/unit tests for the Urgency calculation (§2.1) against fixed inputs — this is pure, deterministic logic once the ancestor-chain data is fetched, and worth pinning down with tests given it's "a key concept for the product" (§2.1).
- Manual testing against the running `rest-api` (real seeded People, real Argon2id login) for each screen as it's built, rather than a mocked API layer — consistent with how `2_RestApi`/`3_Authentication` were tested, and avoiding a second, parallel "does the mock match the real API" concern.
- A specific manual pass for §4's multi-window model: confirm per-item URLs work when pasted directly into a new tab, confirm the "open in new window" affordance opens a chrome-less PWA window, confirm Ctrl-click/middle-click on cross-references opens a new window rather than navigating away, and confirm opening the same item a second time re-focuses the existing window rather than creating a duplicate.
- No end-to-end/browser-automation suite is being built for Level 1 — deferred to Level 2 (`D1.4-9`); manual testing against the real API is the only coverage for now.

<a id="definition-of-success"></a>
## 8. Definition of Success

- Every item in `D1-4`'s required feature list has a working screen, reachable from the app shell, backed by the real REST API.
- The Gantt/plan view renders real seeded data correctly, including dependency lines and Urgency-driven colour-coding.
- A user can open two or more items (e.g. a Task and its Project) in separate windows, positioned independently on separate monitors, and interact with both; opening the same item a second time re-focuses its existing window rather than creating a duplicate.
- The app installs as a PWA (via the browser's "Install app" prompt against `localhost` during development) and opens in a chrome-less, app-styled window, not a browser tab.
- The GUI's build output is a static bundle with no server process of its own, ready for `6_HttpsReverseProxy` to serve as-is.

<a id="open-questions"></a>
## 9. Open Questions (Phase-Specific)

None currently open — see Decisions below.

<a id="decisions"></a>
## 10. Decisions (Phase-Specific)

- **D1.4-1** (decided 2026-08-30)<br>
  **Question:** Framework, component library, data-fetching, and routing technology for the GUI (§3).<br>
  **Decision:** React + TypeScript + Vite (static SPA build, matching `D1.6-4`'s Caddy-serves-static-files routing); MUI (`@mui/x-data-grid`, `@mui/x-tree-view`) as the component library; TanStack Query for server-state, fed by a client generated from `../2_RestApi/`'s `openapi.json` (`openapi-typescript` + `openapi-fetch`); React Router with a real per-item URL for every entity; no real-time push (WebSockets/polling) — data refreshes on navigation, tab focus, and after mutations. See §3 for full reasoning per sub-choice.
- **D1.4-2** (decided 2026-08-30)<br>
  **Question:** How should V2's browser-based GUI support the multi-window/multi-monitor workflow V1.2 offered natively as a desktop app (`Requirements/UserInterfaceWindows.md`'s `Q-Win-3`)?<br>
  **Decision:** rely on the browser's own native tab/window handling (any tab can be dragged to its own window on any monitor; Ctrl/middle-click opens a new tab) rather than building app-managed pop-out windows with a window registry and cross-window sync. The only engineering requirement this places on the GUI is that every item has a real, stable, deep-linkable URL (§3.5) — plus one small addition, an explicit "open in new tab" action on every detail screen, giving a direct equivalent to V1.2's double-click-to-pop-out muscle memory. Automatic singleton-per-object re-focusing (V1.2's behaviour when the same item is opened twice) is knowingly not reproduced; each tab independently re-fetches current data (§3.6), so this is a minor convenience gap, not a correctness one.<br>
  **Superseded by:** `D1.4-8`
- **D1.4-3** (decided 2026-08-30)<br>
  **Question:** Which GUI screen(s) should be built first to prove the technology/architecture choices, and in what order should the rest follow (§6)?<br>
  **Decision:** two pilots, not one, reflecting that the Gantt view is a genuinely different technical problem from everything else: Stage 2 builds Task List + Task Detail (chosen for breadth — it exercises the dependency panel, resource assignment, remarks, and attachments sub-components in one screen, all of which get reused unchanged later); Stage 3 builds the Gantt/plan view, starting with a hands-on spike comparing `gantt-task-react` and `frappe-gantt` against real data before committing to one. Only once both pilots validate the stack does Stage 4 roll out Project/Component List+Detail, Search, Manage People, and Admin tooling, reusing the patterns the pilots establish. Stage 1 is foundational scaffolding (login, routing, API client) ahead of both pilots, not itself a proving-ground stage.
- **D1.4-4** (decided 2026-08-30)<br>
  **Question:** Should Level 1 preserve the old app's drag-and-drop interactions for Assign Resources (`UseCases.md` #2) and Set Dependencies (`UseCases.md` #4), both explicitly flagged there as "UX choices to redesign"?<br>
  **Decision:** no, not for the initial build — both become explicit controls instead (a multi-select checklist for resource assignment; a search-and-pick "Add Dependency" dialog for dependencies), each simpler to build correctly than a drag target and no less usable at Level 1's trial data volumes. Revisitable later as a UX refinement; not an architectural commitment either way — see `D1.4-7` for how the initial build stays ready for it.
- **D1.4-5** (decided 2026-08-30)<br>
  **Question:** How should the GUI look and feel like a real app rather than a browser page, given ordinary browser chrome (address bar, tabs, bookmarks) undermines that — and should this be a browser-based PWA, a `chromium --app` launcher, or a genuine native client (C#/Java)?<br>
  **Decision:** package the GUI as an installable Progressive Web App (`manifest.json` with `display: "standalone"`, plus a minimal service worker) on top of the same React/Vite build (§3.2) — no new language or runtime. This delivers the same chrome-less, independently-positionable app window as `chromium --app`, through a standard, browser-native install mechanism rather than a custom launcher/shortcut to build and distribute. A native client (C#/Java) was rejected for Level 1: it's an explicitly deferred Level 2 option (`Claude/Level2_Implementation/Scope.md`), would mean walking back `D1-1`, and — per `D1.4-6` — would duplicate business logic in a second language for no functional gain the PWA route doesn't already deliver. See §3.7.
- **D1.4-6** (decided 2026-08-30)<br>
  **Question:** Where should the business logic this GUI needs actually live, and in what language, given it needs to be maintained over time?<br>
  **Decision:** authoritative business rules (validation, authorization, anything protecting data integrity) live server-side, in Python, in the REST API — the API is the real trust boundary, and only server-side enforcement can be relied on to hold regardless of which client is asking. The GUI itself is kept thin; the only client-side (TypeScript) logic is the narrow, already-decided exception of Urgency (`D1.2-2`) and Gantt schedule derivation (§6.3), both pure presentation-time computations over data the client already holds. Schedule-derivation math is written as plain, DOM-independent TypeScript functions separate from rendering, for testability and portability to any future JS/TS runtime. See §3.8.
- **D1.4-7** (decided 2026-08-30)<br>
  **Question:** Given drag-and-drop is dropped from the initial build (`D1.4-4`) but considered important, what should be built now so adding it later doesn't require rework?<br>
  **Decision:** every data-changing interaction (reparenting, adding a Dependency, assigning a Resource) is implemented as a standalone, reusable mutation function/hook, called by the initial explicit-picker UI and, unchanged, by a future drag-drop handler — the mutation and the interaction that triggers it are kept decoupled from the start. This is the actual prerequisite for drag-and-drop readiness, not any DnD library choice, and costs nothing extra now. In-window drag-and-drop is a straightforward later addition on this foundation (e.g. via `@dnd-kit`); cross-window drag-and-drop is materially harder (native HTML5 drag-and-drop doesn't reliably cross top-level browser windows) and is resolved separately by `D1.4-10`'s spike, not assumed solved by this decision. See §6.2.
- **D1.4-8** (decided 2026-08-30)<br>
  **Question:** Revisiting `D1.4-2`: should V2's multi-window model also reproduce V1.2's singleton-per-object re-focusing (never two windows for the same item), rather than accepting independent-tabs-with-possible-duplicates as sufficient?<br>
  **Decision:** yes — every item gets a real, stable, deep-linkable URL (§3.5); ordinary navigation (clicking a row, following a cross-reference) navigates the current window in place; an explicit "open in new window" action (plus Ctrl/middle-click on links) calls `window.open(url, name)` with a deterministic name per item (e.g. `task-123`), which the browser itself uses to refocus an already-open window for that item rather than opening a duplicate — true singleton-per-object behaviour, delivered by a standard browser primitive rather than a hand-built window registry or `BroadcastChannel` sync. Combined with `D1.4-5`'s PWA packaging, each such window is chrome-less and freely positionable on any monitor, matching V1.2's actual multi-monitor usage. Cross-window data staleness remains covered by §3.6's refetch-on-focus behaviour; cross-window drag-and-drop is explicitly out of this decision's scope (`D1.4-10`). Full reasoning: §4.
- **D1.4-9** (decided 2026-08-30)<br>
  **Question:** Does Level 1 need a committed end-to-end/browser-automation test suite for the GUI, beyond the manual testing already planned (§7)?<br>
  **Decision:** no — deferred to Level 2. Level 1 relies on manual testing against the real running `rest-api` (§7) for each screen as it's built; automated browser-driven regression coverage is worth building once release cadence and screen count justify the investment. Recorded as a deferral in `Claude/Level2_Implementation/Scope.md`.
- **D1.4-10** (decided 2026-08-30)<br>
  **Question:** Cross-window drag-and-drop (e.g. dragging a Task from one open window onto a Project in another) is technically harder than in-window drag-and-drop, since native HTML5 drag-and-drop doesn't reliably cross separate top-level browser windows — should Level 1 build it, and how should that be decided?<br>
  **Decision:** not decided in the abstract — run a small, scoped spike during Stage 2 (§6.2) implementing exactly one such interaction (dragging a Task onto a Project across two open windows, reusing the `useReparentTask()` mutation `D1.4-7` already establishes) to learn the real technical issues first-hand. If it lands cleanly, build it out properly as part of Stage 2; if it doesn't, defer it to Level 2 and record that in `Claude/Level2_Implementation/Scope.md` at that time.
- **D1.4-11** (decided 2026-08-30)<br>
  **Question:** How does the GUI reach the REST API during Stage 1 development, given a direct browser request from the Vite dev server's origin to the API's origin is genuinely cross-origin and fails on CORS, and API-side CORS configuration would be dev-only scaffolding to remember and later remove?<br>
  **Decision:** `gui-client/vite.config.ts`'s dev server proxies `/api/*` to the REST API (prefix stripped), and `VITE_API_BASE_URL` (`.env.example`) is set to the relative `/api` — the browser only ever talks to its own origin, so no CORS configuration is needed on the API at all. This deliberately mirrors `D1.6-4`'s Caddy routing (`/api/*` → rest-api, prefix stripped) exactly, so the same base URL and the same relative-path assumption carry through unchanged from Stage 1 all the way to production — not a dev-only workaround to revisit later.

See `../ImplementationPlan.md` for how this phase fits into the Level 1 plan, and for open questions that span this phase and others.
