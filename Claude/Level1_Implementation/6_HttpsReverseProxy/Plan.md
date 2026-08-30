# ProjectPal V2 — HTTPS / Reverse Proxy Phase Plan

## Contents

1. [Status and Purpose](#status-and-purpose)
2. [Scope for Level 1](#scope)
   - 2.1 [In Scope](#in-scope)
   - 2.2 [Deferred Out of Level 1](#deferred)
3. [Design](#design)
   - 3.1 [Reverse Proxy Technology](#proxy-technology)
   - 3.2 [Network Topology](#network-topology)
   - 3.3 [Certificate and Trust Strategy](#certificate-strategy)
   - 3.4 [Routing — the API Now, the GUI Later](#routing)
   - 3.5 [Cloudflare Tunnel Setup (Manual, One-Time)](#cloudflare-setup)
4. [Implementation Plan](#implementation-plan)
   - 4.1 [Files Touched](#files-touched)
   - 4.2 [Build Order](#build-order)
5. [Testing](#testing)
   - 5.1 [Approach](#testing-approach)
   - 5.2 [Test Categories](#test-categories)
6. [Definition of Success](#definition-of-success)
7. [Open Questions (Phase-Specific)](#open-questions)
8. [Decisions (Phase-Specific)](#decisions)

<a id="status-and-purpose"></a>
## 1. Status and Purpose

**Status:** Not started — the design is now fully settled (§8); nothing below has been built yet, and the manual Cloudflare setup in §3.5 hasn't been done yet either.

Put TLS/HTTPS in front of the REST API, per `1_DatabaseSetup/DataBaseHostingOptions.md`'s recommendation (Caddy) and the API-first foundational decision (`../Scope.md` §3). Until now, `db` and `rest-api` have both been bound to `127.0.0.1` only — nothing outside this one machine can reach either of them. That's fine for building and testing the API and login, but it means the Demonstrator is currently unreachable by anyone but the developer, which directly conflicts with `Requirements/Goals.md` §4.1's actual purpose for Level 1: "let real users try the concepts and the GUI and give feedback."

This phase turned out to be less "expose a port on a network" and more "get real, warning-free HTTPS to trial users with zero customer IT involvement" — those two constraints together ruled out every straightforward option (§3.3), because genuine browser trust for a private/internal address doesn't exist as a public-CA product, and every path that keeps the demonstrator strictly inside a customer's own network still needed their IT to do *something*. The resolution: a Cloudflare Named Tunnel. The stack keeps running wherever it's convenient to run it (a developer machine, a customer's own PC), with **zero inbound ports opened anywhere** — Cloudflare's edge is the actual public-facing endpoint and holds the real, publicly-trusted certificate, relaying traffic back over a connection the local machine itself initiated.

<a id="scope"></a>
## 2. Scope for Level 1

<a id="in-scope"></a>
### 2.1 In Scope

- A reverse proxy (Caddy) doing internal, path-based routing in front of the REST API (§3.4) — no longer doing TLS itself (§3.1).
- A Cloudflare Named Tunnel as the actual internet-facing entry point, terminating TLS with a real, publicly-trusted, automatically-managed certificate (§3.3).
- Registering a domain through Cloudflare Registrar for a stable, branded hostname (§3.3), and the one-time manual account/tunnel setup that needs (§3.5).
- The routing split anticipating the future GUI (`5_GuiClient`) sharing the same origin, to sidestep CORS entirely (§3.4).
- How testing splits between the existing suite (unchanged) and a small check of the tunnel/proxy path itself (§5).

<a id="deferred"></a>
### 2.2 Deferred Out of Level 1

- **A Web Application Firewall / API gateway, rate limiting at the edge, intrusion detection.** `DataBaseHostingOptions.md`'s own "Phase 3 — production hardening" list; not appropriate for a Level 1 trial, tunnel or not.
- **Multi-tenant routing** (which tenant → which backend). Not relevant until Level 2/3's database-per-tenant architecture actually has more than one tenant to route between.
- **A permanently-running public deployment.** The tunnel is deliberately session-scoped — run for a trial, torn down afterward — not a standing public service. A permanent, always-on public presence is a Level 2-shaped decision (`Requirements/Goals.md` §4.2's real hosting), not something this phase commits to.

<a id="design"></a>
## 3. Design

<a id="proxy-technology"></a>
### 3.1 Reverse Proxy Technology

Caddy — still used, per `DataBaseHostingOptions.md`'s recommendation, but its job narrows once `D1.4-1` settles on a Cloudflare Named Tunnel (§3.3): Cloudflare's edge is what the public actually connects to, and where the real certificate lives, so Caddy no longer needs to manage TLS or a certificate of its own at all. Its remaining job is exactly `D1.4-4`'s path-based routing (§3.4), serving plain HTTP inside the Docker network to whatever reaches it — still a better fit for that one job than nginx/Traefik, and still a single small binary/config file.

<a id="network-topology"></a>
### 3.2 Network Topology

`db` stays exactly as it is — bound to `127.0.0.1` only, reachable solely from this machine for local admin tools (`README.md` §5). `rest-api` also keeps its existing `127.0.0.1` binding, kept deliberately for local dev/test convenience (`D1.4-3`). The reverse proxy (Caddy) doesn't need a host port binding *at all* — with the Cloudflare Tunnel approach (`D1.4-1`), the only thing that talks to Caddy from outside its own container is `cloudflared`, and that happens over the Docker network itself, never a host port.

**Nothing in this stack ends up with an inbound port open to the outside world, anywhere.** `cloudflared` is the one component that talks to the outside, and it only ever makes outbound connections, never accepts them. This is a stronger position than the phase's original plan assumed (which had the proxy itself needing a host-facing bind) — the tunnel model gets to "reachable from anywhere" with *less* exposure than "reachable from the customer's LAN" would have needed, not more.

<a id="certificate-strategy"></a>
### 3.3 Certificate and Trust Strategy

**Decision (`D1.4-1`, `D1.4-2`, §8):** a Cloudflare Named Tunnel, fronting a domain registered through Cloudflare Registrar.

The reasoning that got here: a hard constraint — public CAs will never issue a browser-trusted certificate for a private/internal IP address — ruled out "no warning, no new CA, raw IP" as an achievable combination from the start. The two paths that *could* get a real, warning-free certificate without that combination — a hostname validated via ACME DNS-01 against an internally-resolved address, or leaning on a customer's own existing internal CA — both still needed the customer's IT to do something (add a DNS entry, or issue a certificate), which the actual requirement (no customer IT involvement, at all) ruled out too.

A Cloudflare Tunnel resolves this cleanly: the local stack — wherever it happens to run, a developer machine or a customer's own PC — makes an outbound-only connection to Cloudflare's edge, which is what the public internet actually reaches. Cloudflare issues and manages a real, publicly-trusted certificate for the chosen hostname automatically. Genuinely zero certificate handling anywhere in this stack, and zero action needed from any customer's IT department, since nothing about their network changes at all — outbound connections need no firewall or router change.

This is a deliberate, consciously-made trade: it makes the demonstrator briefly internet-reachable via Cloudflare's edge, rather than staying strictly inside a customer's own network boundary — a real departure from `Requirements/Goals.md`'s original Level 1 framing ("deployed inside that organisation's own infrastructure"), closer to what Level 2 was meant to introduce first. Accepted anyway because it's session-scoped (the tunnel runs only while a trial is active, not a standing public service, §2.2) and the data never has to live on infrastructure we provision, unlike genuinely hosting Level 1 publicly would.

**Domain:** registered via Cloudflare Registrar specifically to avoid a second vendor — Cloudflare sells domains at cost (no markup, free WHOIS privacy included), and a domain registered there lands on Cloudflare's own DNS automatically, which is exactly what a Named Tunnel needs. The actual domain name is chosen at registration time (§3.5), not fixed here.

<a id="routing"></a>
### 3.4 Routing — the API Now, the GUI Later

**Decision (`D1.4-4`):** path-route now, even though `5_GuiClient` doesn't exist yet to plug into it — building it in from day one avoids reopening the Caddyfile (and re-testing it) once the GUI lands, for very little cost today. `/api/*` is proxied to `rest-api`, with the `/api` prefix *stripped* before forwarding (Caddy's `handle_path` directive) — so `rest-api`'s existing routes (`/task`, `/team`, `/auth/login`, …) need zero changes now or later. Everything else (`/`) serves a placeholder today, and will serve the GUI's static files once `5_GuiClient` exists — keeping the API and the GUI on one origin from the browser's point of view avoids CORS entirely, without this phase needing to know anything about the GUI's eventual framework to reserve the shape. Caddy serves all of this over plain HTTP inside the Docker network (§3.2) — Cloudflare's edge is the actual TLS boundary (`D1.4-1`), so Caddy itself has no certificate to manage.

<a id="cloudflare-setup"></a>
### 3.5 Cloudflare Tunnel Setup (Manual, One-Time)

Before any of §4's code changes can actually work, someone needs to do the following once, outside this repository — account creation, domain registration, and payment are things only a human can do, not Claude Code. Cloudflare's dashboard wording shifts occasionally, but the shape of this hasn't changed in years.

**1. Create a Cloudflare account.**
- Go to `dash.cloudflare.com/sign-up`.
- Email + password, verify the email address. Free — no payment details needed for this step.

**2. Register a domain through Cloudflare Registrar** (§3.3) — this is the one step that costs money (roughly $10–15/year, charged by Cloudflare, at cost with no markup):
- In the dashboard, find **Domain Registration** in the left sidebar (sometimes labelled **Register Domains**).
- Search for the name you want (e.g. try a few TLDs — `.com`, `.app`, `.dev` are all supported) and confirm it's available and offered through Cloudflare specifically — not every TLD is.
- Add it to the cart, enter payment details, complete the purchase.
- Once registered this way, the domain is *automatically* set up as a Cloudflare-managed DNS zone — there's no separate "point my nameservers at Cloudflare" step to do, which is the whole reason for registering here rather than elsewhere.

**3. Set up Zero Trust** (first time only, still free for this use) — Cloudflare's Tunnel feature lives under a section of the dashboard called **Zero Trust**, which can sound more alarming than it is; nothing here involves configuring actual access policies, just its Tunnel feature.
- From the main dashboard, find **Zero Trust** in the sidebar (or go directly to `one.dash.cloudflare.com`).
- First visit prompts you to choose a team name — this is just an internal label for the account, not something end users ever see. Pick anything.
- No payment needed to proceed past this for Tunnel usage at this scale.

**4. Create the Named Tunnel:**
- Inside Zero Trust: **Networks → Tunnels → Create a tunnel.**
- Connector type: **Cloudflared** (the default/standard option).
- Name it something identifiable (e.g. `projectpal-demo`) — purely descriptive, not user-facing.
- Save. Cloudflare will show an install command for various platforms (Windows/macOS/Docker/etc.) with a token embedded in it — you only need the **token itself**, not the full command; it's a long string, usually shown once prominently but retrievable again later from the tunnel's own settings page if you navigate away too fast.

**5. Configure the public hostname**, still on this tunnel's settings:
- Go to its **Public Hostname** tab → **Add a public hostname.**
- **Domain:** select the one you registered in step 2.
- **Subdomain:** whatever you'd like the demonstrator to be called (e.g. `demo`) — the full address becomes `demo.<yourdomain>`.
- **Type:** `HTTP` (not `HTTPS`) — Caddy serves plain HTTP internally (§3.4); Cloudflare's edge is what adds TLS for the public side, so the *internal* hop deliberately doesn't need it.
- **URL:** `reverse-proxy:80` — the Docker service name and port Caddy listens on inside the compose network (§4.1's `Caddyfile`).
- Save. Cloudflare creates the necessary DNS record for this hostname automatically — there's nothing further to configure in DNS by hand.

**6. Get the token into this project:**
- Copy the tunnel token from step 4 (or the tunnel's overview/configure page if you need to retrieve it again).
- Add it to `.env` (not `.env.example`, since it's a real secret) as `CLOUDFLARE_TUNNEL_TOKEN=<token>` — same pattern as `JWT_SECRET`/`POSTGRES_PASSWORD` already follow.

That's everything manual. The token is the only thing the implementation actually needs from this whole setup — it's what the `cloudflared` container (§4.1) authenticates with once it's added to `docker-compose.yml`. Nothing else from steps 1–5 touches the codebase; they only need doing once, not per release.

<a id="implementation-plan"></a>
## 4. Implementation Plan

<a id="files-touched"></a>
### 4.1 Files Touched

```
V2/
├── docker-compose.yml     (edited — adds a `reverse-proxy` service (no host port, §3.2)
│                            and a `cloudflared` service; rest-api's host port mapping
│                            stays, per D1.4-3)
├── .env.example           (edited — adds CLOUDFLARE_TUNNEL_TOKEN)
├── scripts/
│   └── verify-https.ps1   (NEW — the Tier 2a smoke check, §5.2; mirrors verify.ps1's shape)
└── reverse-proxy/         (NEW)
    └── Caddyfile          (plain HTTP; handle_path /api/* → rest-api, stripped; / → placeholder, D1.4-4)
```

Nothing in `rest-api` itself needs to change — it has no logic that cares about the request scheme (no secure-cookie flags, no HTTP→HTTPS redirects of its own), and `D1.4-4`'s prefix-stripping means its route paths don't need to change either, so TLS terminating upstream of it is transparent to the application code.

<a id="build-order"></a>
### 4.2 Build Order

`D1.4-1` through `D1.4-4` are all settled — nothing here is blocked on an open question, only on the manual setup in §3.5:

1. Complete the one-time manual Cloudflare setup (§3.5) and obtain the tunnel token.
2. Add `CLOUDFLARE_TUNNEL_TOKEN` to `.env`/`.env.example`.
3. Add the `reverse-proxy` (Caddy, no host port) and `cloudflared` services to `docker-compose.yml`.
4. Write the `Caddyfile`: plain HTTP; `handle_path /api/*` → `rest-api` with the prefix stripped; `/` → a placeholder for now (`D1.4-4`).
5. Write `scripts/verify-https.ps1` (§5.2's Tier 2a checks).
6. Rebuild and re-run the full existing test suite (`.\scripts\test-api.ps1`, unchanged per `D1.4-3`) plus the new local smoke check, to confirm nothing regressed. Tier 2b (§5.2) is checked manually once a real tunnel is actually configured for a trial.

<a id="testing"></a>
## 5. Testing

<a id="testing-approach"></a>
### 5.1 Approach

**Decision (`D1.4-3`):** a hybrid, two-tier split, rather than routing everything through the proxy for testing purposes. The existing 51-test suite (`2_RestApi`/`3_Authentication`) is Tier 1 and stays exactly as it is — hitting `rest-api` directly, unaffected by TLS/certificate handling, so it stays fast and doesn't need `verify=False`/cert-pinning workarounds sprinkled through 51+ tests. This only works because `rest-api`'s host port mapping deliberately stays exposed for local dev/test convenience (§3.2) — a documented exception, in the same spirit as `db`'s existing dev-only exposure. Tier 2 (§5.2) is small and separate, and — because real trust now depends on genuinely external infrastructure (`D1.4-1`'s Cloudflare Tunnel) rather than a locally-generated certificate — splits further into what can run in an ordinary local dev loop and what actually needs a real tunnel configured.

<a id="test-categories"></a>
### 5.2 Test Categories

**Tier 1 — the existing suite, unchanged.** All 51 tests from `2_RestApi`/`3_Authentication` keep hitting `rest-api` directly; nothing about them changes because of this phase.

**Tier 2a — local routing smoke check, automatable** (`scripts/verify-https.ps1`, §4.1) — runs against the Docker network directly, no real Cloudflare Tunnel needed:
- Caddy's `/api/*` routing (with the prefix stripped) reaches `rest-api` correctly and returns the same result a direct request would.
- `db` and `rest-api` remain unreachable from outside the Docker network, bar the deliberate Tier-1 exception above.

**Tier 2b — live tunnel check, manual/deployment-time** — needs a real, configured Named Tunnel (§3.5), so it isn't part of the routine local test loop:
- The public hostname resolves and presents a real, browser-trusted certificate — no warning.
- A real request through the public URL (e.g. login, then `whoami`) succeeds end to end.
- `cloudflared` is genuinely making an outbound-only connection — no inbound port is open anywhere in the stack.

<a id="definition-of-success"></a>
## 6. Definition of Success

For Level 1, this phase is done when:

- A trial user anywhere can reach the Demonstrator over a real, browser-trusted HTTPS URL (`D1.4-1`/`D1.4-2`) — no warning, no new certificate or CA installed on their machine.
- No customer IT action is needed to make this work.
- Nothing in the stack has an inbound port open to the outside world except `cloudflared`'s own outbound connection (§3.2) — `db` and `rest-api` remain unreachable from outside the Docker network, bar `D1.4-3`'s deliberate local-dev exception for `rest-api`.
- The existing 51-test suite (Tier 1) passes unchanged, Tier 2a's local routing check passes in the ordinary dev loop, and Tier 2b's live check passes once a real tunnel is configured.
- The Caddyfile's routing shape doesn't need revisiting when `5_GuiClient` actually lands (`D1.4-4`).

<a id="open-questions"></a>
## 7. Open Questions (Phase-Specific)

None currently open — see Decisions below.

<a id="decisions"></a>
## 8. Decisions (Phase-Specific)

- **D1.4-1** (decided 2026-08-30)<br>
  **Question:** Certificate/trust strategy — given Level 1 has no public domain and runs inside one customer's own network, how should a trial user's browser actually come to trust the connection, without accepting a browser warning or installing a new root CA on client machines, and without any customer IT involvement at all?<br>
  **Decision:** a Cloudflare Named Tunnel (§3.3). The stack, wherever it runs, makes an outbound-only connection to Cloudflare's edge, which terminates public HTTPS with a real, automatically-managed, publicly-trusted certificate — zero certificate handling in this stack, and zero customer IT action, since outbound connections need no firewall/router change on anyone's part. Rejected: ACME DNS-01 against an internally-resolved hostname, and reliance on a customer's own internal CA — both still needed the customer to do something (a DNS entry, or issue a certificate), which the "no IT involvement at all" requirement ruled out. Consciously trades away part of Level 1's original "stays inside the customer's own network" framing (`Requirements/Goals.md` §4.1) for session-scoped, non-permanent public reachability — see §3.3 for the full reasoning.
- **D1.4-2** (decided 2026-08-30)<br>
  **Question:** Addressing the demonstrator — a raw IP, or a hostname?<br>
  **Decision:** a hostname, under a domain registered specifically for this via Cloudflare Registrar (§3.3) — required by the Named Tunnel approach (`D1.4-1`), and gets a stable, branded URL (e.g. `demo.<domain>`) rather than one that changes every session, for a small, one-time-per-vendor cost (roughly $10–15/year for the domain itself).
- **D1.4-3** (decided 2026-08-30)<br>
  **Question:** Should the existing test suite, Swagger UI, and manual-testing walkthroughs keep hitting `rest-api` directly on its own port for convenience, or should everything — including our own testing — go through the new HTTPS proxy exclusively?<br>
  **Decision:** hybrid (§5.1/§5.2) — the existing 51-test suite keeps hitting `rest-api` directly (Tier 1, unchanged), and a small new smoke check (`scripts/verify-https.ps1`) proves the proxy's own routing works (Tier 2a), rather than routing the whole suite through the proxy just to re-prove logic Tier 1 already covers. Requires `rest-api`'s host port mapping to stay exposed for local dev/test — a deliberate, documented exception alongside `db`'s existing one (§3.2).
- **D1.4-4** (decided 2026-08-30)<br>
  **Question:** Should this phase's routing anticipate `5_GuiClient` serving its web app from the same origin (avoiding CORS entirely), or should that be left for Phase 5 to design once its client technology is actually chosen?<br>
  **Decision:** design it in now (§3.4), to avoid reopening and re-testing the Caddyfile once the GUI lands — `handle_path /api/*` proxies to `rest-api` with the prefix stripped (so `rest-api`'s existing routes need no changes, now or later), and `/` serves a placeholder today, the GUI's static files once `5_GuiClient` exists. Doesn't require knowing the GUI's eventual framework, only the routing shape.

See `../ImplementationPlan.md` for how this phase fits into the Level 1 plan, and for open questions that span this phase and others.
