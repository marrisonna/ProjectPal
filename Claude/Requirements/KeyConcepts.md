# ProjectPal — Key Concepts

*Open questions in this document use the prefix `Q-KC-`; decisions use `D-KC-`.*

## Contents

1. [Organisation](#organisation)
2. [Team](#team)
3. [Tenant](#tenant)
4. [Person](#person)
5. [Resource](#resource)
6. [Project](#project)
7. [Task](#task)
8. [Component](#component)
9. [Dependency](#dependency)
10. [Effort vs. Duration](#effort-vs-duration)
11. [Priority / Status](#priority-status)
12. [Urgency](#urgency)
    - [Current Urgency Algorithm](#current-urgency-algorithm)
    - [Current Urgency-to-Colour Algorithm](#current-urgency-to-colour-algorithm)
13. [Role / Permission Level](#role-permission-level)
14. [Merge / Conflict](#merge-conflict)
15. [Attachment](#attachment)
16. [Remark](#remark)
17. [Demonstrator / MVP / (Level 3) "Everything Else"](#delivery-level-terms)
18. [Foundational Decision](#foundational-decision)
19. [Open Questions](#open-questions)
20. [Decisions](#decisions)

This document defines the concepts that are central to ProjectPal's design. For each one, it covers what the concept is, why it matters enough to call out on its own, and how it fits into the overall solution described in `Goals.md`, `DomainModel.md`, and `UseCases.md`. Where a concept is still an open decision rather than a settled one, that's noted inline — this document should be updated as decisions are made.

<a id="organisation"></a>
## 1. Organisation

An Organisation is a customer of the product — a whole company, or an organisational unit within one, that has adopted ProjectPal.

It's key because it's the thing multi-tenancy exists to protect: every security, isolation, and billing decision in the system is ultimately in service of keeping one Organisation's data and access completely separate from another's.

It sits at the top of the solution's tenancy model. Per `Goals.md` §Multi-tenancy, Organisation *is* the tenant, realized physically as one dedicated database per Organisation. That single decision is why nothing else in the domain model (Project, Task, Person, etc.) needs an explicit `OrganisationId` column — the database itself is the boundary — and it's why `Goals.md` §Multi-tenancy has to separately account for the operational costs of that choice.

<a id="team"></a>
## 2. Team

A Team is a grouping of People within an Organisation, used to scope day-to-day visibility and reporting rather than to provide hard security isolation.

It's key because real customers aren't flat: an Organisation adopting ProjectPal will typically have multiple groups (departments, product teams) that want their own view of "our work" without needing to be treated as separate paying customers. Without Team as a first-class concept, an Organisation with several such groups has no way to scope a task list or report to just one of them.

It sits directly below Organisation in the hierarchy. Full team functionality (per-team permissions, self-service team management) is scoped to Level 3 in `Goals.md`, but its basic shape needed deciding well before then, because Project, Task, and Person all need to know which Team they belong to — see `DomainModel.md`'s Team-scoping decision (a Person can belong to several Teams with an independent role in each; a Project belongs to exactly one Team for Level 1). This is a good example of a decision that's foundational even though the feature built on top of it is deferred.

<a id="tenant"></a>
## 3. Tenant

Tenant is the general SaaS-industry term for "the customer-shaped boundary a multi-tenant system is partitioned by."

It's key precisely because it's ambiguous on its own — a tenant could plausibly mean an Organisation or a Team — and every hosting, security, and cost decision in `Goals.md` is framed around whichever one it turns out to be.

It fits into the solution as a resolved question, not an open one: `Goals.md` explicitly decides tenant = Organisation, with database-per-tenant meaning database-per-Organisation. This entry exists mainly so "tenant" isn't used ambiguously elsewhere in discussion or documentation.

<a id="person"></a>
## 4. Person

A Person is a human known to the system — someone who can log in, someone whose time can be allocated to work, or both.

It's key because nearly every other entity references a Person (an owner, a requestor, an assignee, an author), making it the connective tissue between "who is using the system" and "who is doing the work." Modeling both roles as one entity, rather than splitting them into a separate User and Resource, is what lets the system track effort against a contractor who never logs in, and requests from a stakeholder who's never assigned work, without extra machinery.

It's where identity meets the domain model. How a Person authenticates is an external concern — `Goals.md`'s "identity direction" foundational decision (e.g. federating to an external identity provider; still open, `Q-KC-1`) — while how a Person is used for effort, assignment, and ownership is pure domain data. Keeping that seam clean early avoids having to retrofit authentication into deeply embedded ownership and assignment logic later.

<a id="resource"></a>
## 5. Resource

Resource isn't a separate entity — it's the "assignable capacity" role a Person can play (`IsResource = true`).

It's key because the entire scheduling and effort model depends on knowing how many people are actually available to do a piece of work, and "Resource" is the vocabulary for that without requiring the person doing the work to also be a system login.

It feeds directly into the plan/Gantt view's workload graph (`UseCases.md` #3) and into how a Task's duration is derived from effort divided across assigned resources (`DomainModel.md`, and see Effort vs. Duration below).

<a id="project"></a>
## 6. Project

A Project is a container for related work, arranged in a tree so large initiatives can be broken down into sub-projects.

It's key because it's the organizing unit people think in terms of first — "what am I working on" is answered by Project before it's answered by Task — and it's the natural place to attach priority, ownership, and an overall schedule.

It's central to the plan view and to derived scheduling (see `DomainModel.md`) — Project is where the top-level schedule computation lives.

<a id="task"></a>
## 7. Task

A Task is the atomic unit of work — closer to a ticket than a bare to-do, carrying its own type, priority, status, effort, and requestor.

It's key because it's the entity everything else attaches to (Attachments, Remarks, Dependencies, resource assignment) and the one users spend most of their time looking at and updating. It's also the primary input to Urgency, the mechanism the system uses to tell a user what to look at next.

It sits at the center of the domain model (`DomainModel.md`): it belongs to exactly one Project, optionally relates to one Component, and is where the Effort-vs-Duration and derived-scheduling decisions actually get applied day to day.

<a id="component"></a>
## 8. Component

A Component is a second, independent classification tree, orthogonal to Project, used to tag which part of a product or system a Task touches.

It's key because Project and Component answer two different questions that both matter for reporting: Project answers "what initiative is this for," Component answers "what does this affect." A customer doing product or engineering work needs both axes — e.g. "how much effort went into the billing subsystem this quarter" is a question Project alone can't answer if billing work is spread across several projects.

It's a purely a tagging/reporting concern in the solution — it doesn't participate in scheduling or dependencies at all, which keeps it cheap to build and safe to extend later without touching the scheduling engine.

<a id="dependency"></a>
## 9. Dependency

A Dependency is a predecessor/successor ordering relationship between two work items, where either side can be a Task or a Project.

It's key because real plans aren't independent lists of dates — one piece of work genuinely can't start until another finishes, and a plan view that ignores that gives a false picture of when things will actually be done.

It's the mechanism that turns a set of Tasks and Projects into an actual schedule: every derived date discussed in `DomainModel.md`'s scheduling section is downstream of the dependency graph, and the plan view (`UseCases.md` #3) is essentially a visualization of it.

<a id="effort-vs-duration"></a>
## 10. Effort vs. Duration

Effort and Duration are two different ways to size a piece of work:
- **Effort (man-days)** — a fixed amount of work that gets divided across however many People are assigned (more people assigned → shorter elapsed time).
- **Duration** — a fixed elapsed time regardless of how many people are assigned (e.g. "this takes two weeks no matter who's on it, or how many").

The distinction is key because, without it, "how long will this take" has no good answer once more than one person might be assigned. Adding a second person to a fixed-effort task should shorten it; adding a second person to a fixed-duration task (e.g. "wait for a vendor to respond") should not.

It feeds directly into the derived-scheduling calculation in `DomainModel.md` and into Urgency's time-pressure component below — a subtle but high-value idea worth preserving in the new system.

<a id="priority-status"></a>
## 11. Priority / Status

Priority (e.g. High → Low, plus Closed/Cancelled as special values) and Status (Not Started/In Progress/Closed/Cancelled/etc.) are two related-but-distinct fields on a Task or Project, kept in sync with each other when either reaches Closed/Cancelled.

They're key because they're the two most basic triage signals a user or a report relies on — importance versus lifecycle state — and together they're the primary inputs to Urgency.

They're used throughout reporting and the Urgency calculation below. The exact status vocabulary is open to revisiting for the new system (`Q-KC-2`), but the two-field shape (importance vs. lifecycle state) is worth keeping.

<a id="urgency"></a>
## 12. Urgency

Urgency is a computed, per-Task score used to prioritise a user's worklist and to drive colour-highlighting in the task grid, the Gantt view, and the per-person workload report (the more urgent, the stronger the warning colour). It is not a stored field — it's recalculated on the fly from the Task's own priority and dates, and from the priority of every ancestor Project above it.

For a Closed or Cancelled Task, Urgency decays with time since closure: a task closed within the last 10 days stays urgent (to prompt final review/sign-off); beyond that it rapidly drops towards zero the longer it's been closed.

For an open Task, Urgency is the product of two factors:
- **Effective priority** — the Task's own Priority, weighted by the Priority of each ancestor Project in its chain from the top down. A low-priority task nested under a high-priority project chain reads as more urgent than the same task under a low-priority chain.
- **Time pressure** — how close today is to the Task's relevant date: its start date if it hasn't started, the midpoint between start and end if it's in progress, or its end date if it's due to finish. An overdue task becomes steadily more urgent the further past that date it is; a task due well in the future is damped down, tailing off over roughly a 60-day horizon.

It's key because it's the system's synthesized signal for "what should I actually look at next" — Priority and Status alone tell a user what a task is, but not when it deserves attention relative to everything else on their plate; Urgency is what turns those raw fields into a single ranking a person or a manager can scan at a glance.

The two factors are multiplied and scaled to produce the final score (roughly 0–200+), which callers then feed into a colour-mapping function to render as a highlight colour. It fits into the solution as a presentation-layer calculation built entirely from other domain data (Priority, Status, dates, the Project hierarchy) rather than as data of its own — the specific constants involved (the 10-day closed-task decay window, the 60-day time-pressure horizon, the priority-weighting exponents) are tuned, hand-picked values from the old implementation rather than derived from any external rule, worth treating as a starting point to validate against real usage rather than as fixed requirements when the new system reimplements this (`Q-KC-3`).

<a id="current-urgency-algorithm"></a>
### 12.1 Current Urgency Algorithm

This is the algorithm as implemented today (`GUITask.Urgency`, `V1.2\Apps\ProjectPal\ProjectPal\Tasks\GUITask.cs`), stated in full so it can be evaluated, reused, or deliberately replaced with eyes open.

**Symbols and constants**

- `today` — the current date, with no time component.
- `Pr(x)` — the integer value of a Priority enum member: High = 5, MedHigh = 4, Med = 3, MedLow = 2, Low = 1, Closed = 0, Cancelled = −1. A missing Priority is always treated as Med (`Pr = 3`).
- `n` — the number of ancestor Projects above the Task: the Task's own Project, that Project's parent, and so on up to (but not including) a null parent. `n = 0` if the Task has no Project.
- `P[0 … n−1]` — the ancestor chain's priorities, ordered root-first (`P[0]` = the top-most ancestor's `Pr`, `P[n−1]` = the Task's immediate parent Project's `Pr`).
- `pr_task` — `Pr(Task.Priority)`, or 3 if the Task's own Priority is unset.
- `exaggerateFactor = 1.5` — fixed constant used when folding in each additional level of project nesting.
- `⌊x⌋` — truncation towards zero (what the C# `(int)` cast does; equivalent to floor for the positive values used here).

**Case A — Task Status is Closed or Cancelled**

Let `d = (today − StatusDate).Days`.

- If `StatusDate` is set and `d > 10`:
  `U = ⌊100 / d⌋ / 10`
- Otherwise (no `StatusDate`, or closed ≤ 10 days ago):
  `U = 1`

**Case B — Task Status is anything else (open)**

*Step 1 — effective-priority band `[min, max]` from the Project ancestry:*

- If `n = 0`: `max₀ = 0.5`, `min₀ = max(0, max₀ − 1) = 0`.
- Else: `max₀ = P[0] + 0.5`, `min₀ = max(0, max₀ − 1)`.
- For each further ancestor level `i = 1 … n−1` (only runs when `n ≥ 2`):
  - `pMax_i = (P[i] + 0.5) / 6`
  - `pMin_i = max(0, (P[i] + 0.5) − 1) / 6`
  - `mean_i = pMax_i + pMin_i`
  - `exaggerate_i = mean_i ^ 1.5`
  - `max_i = min_{i−1} + exaggerate_i × pMax_i × (max_{i−1} − min_{i−1})`
  - `min_i = min_{i−1} + exaggerate_i × pMin_i × (max_{i−1} − min_{i−1})`
- The final band is `[min, max] = [min_{n−1}, max_{n−1}]` (or `[min₀, max₀]` if `n ≤ 1`).

*Step 2 — combine with the Task's own priority:*

- `taskFactor = pr_task / 3`
- `finalTaskPriority = taskFactor × (min + max) / 2`
- `taskPriorityMultiplier = (finalTaskPriority − 3) / 3 + 1`

*Step 3 — the Task's relevant date, `taskDate`, based on Status (only if `StartDate` is set, else `taskDate` is undefined):*

- Status = NotStarted, or `EndDate` unset: `taskDate = StartDate`
- Status = InProgress: `taskDate = StartDate + ⌊(EndDate − StartDate).Days / 2⌋` days (the midpoint)
- Any other open status (e.g. Ready, Support, Tentative) with both dates set: `taskDate = EndDate`

*Step 4 — combine priority with time pressure:*

- If `taskDate` is undefined: `U = 100 × taskPriorityMultiplier`
- Else, let `daysUntilDue = (taskDate − today).Days`:
  - If `daysUntilDue ≤ 0` (due today or overdue): `lateMultiplier = 1 − daysUntilDue / 60`, `U = 100 × taskPriorityMultiplier × lateMultiplier`
  - If `daysUntilDue > 0` (due in the future): `earlyMultiplier = 0.5 ^ (daysUntilDue / 60)`, `U = 100 × taskPriorityMultiplier × earlyMultiplier`

**Final rounding (both cases):** `Urgency = ⌊U × 10⌋ / 10` (truncated to one decimal place).

**Worked example 1 — closed task, decaying urgency**

A Task was closed (`Status = Closed`) 25 days ago (`StatusDate = today − 25`). Since `25 > 10`:
`U = ⌊100 / 25⌋ / 10 = ⌊4⌋ / 10 = 4 / 10 = 0.4`.
`Urgency = 0.4` — a long-closed task is essentially "not urgent."

**Worked example 2 — open task, not yet due, damped by time**

A Task sits directly under one Project of Medium priority (`n = 1`, `P[0] = Pr(Med) = 3`), and the Task itself is also Medium priority (`pr_task = 3`). It's `InProgress`, `StartDate = today`, `EndDate = today + 20`.

- Band: `max₀ = 3 + 0.5 = 3.5`, `min₀ = max(0, 2.5) = 2.5` → `[2.5, 3.5]` (loop doesn't run since `n = 1`).
- `taskFactor = 3 / 3 = 1`. `finalTaskPriority = 1 × (2.5 + 3.5)/2 = 3`. `taskPriorityMultiplier = (3−3)/3 + 1 = 1`.
- `taskDate` = midpoint = `today + ⌊20/2⌋ = today + 10`. `daysUntilDue = 10` (future).
- `earlyMultiplier = 0.5 ^ (10/60) ≈ 0.891`.
- `U = 100 × 1 × 0.891 ≈ 89.1`. `Urgency = 89.1`.

A task on-track and comfortably not due yet lands just under the 100 baseline.

**Worked example 3 — open task, overdue and high priority**

Same Project as example 2 (Medium priority, `n = 1`), but the Task itself is High priority (`pr_task = 5`), `Status = NotStarted`, `StartDate = today − 5` (5 days overdue).

- Band is the same as example 2: `[2.5, 3.5]`.
- `taskFactor = 5 / 3 ≈ 1.667`. `finalTaskPriority = 1.667 × 3 = 5`. `taskPriorityMultiplier = (5−3)/3 + 1 ≈ 1.667`.
- `taskDate = StartDate` (NotStarted rule) `= today − 5`. `daysUntilDue = −5` (overdue, so `≤ 0`).
- `lateMultiplier = 1 − (−5/60) ≈ 1.083`.
- `U = 100 × 1.667 × 1.083 ≈ 180.56`. `Urgency = 180.5`.

An overdue, high-priority task under a medium-priority project lands well above 100 — enough to trigger visible colour-highlighting (see 12.2 below).

<a id="current-urgency-to-colour-algorithm"></a>
### 12.2 Current Urgency-to-Colour Algorithm

This is the algorithm as implemented today (`Utils.Colours.UrgencyColour`, `V1.2\Libs\Utils\Utils\Colours.cs`) for turning an Urgency score into a highlight colour.

**Constants**

- `white = RGB(255, 255, 255)` — the "not urgent" colour (`Colours.ReadWriteColour`).
- `maxUrgencyColour = RGB(255, 128, 128)` — the "maximally urgent" colour, a light red (`Colours.MaxUrgency`).
- The mapping is precomputed once into a 101-entry lookup table indexed `0 … 100`.

**Algorithm**

Given an Urgency value `U`:

1. `u = ⌊U⌋` (truncate to a whole number).
2. `m = max(0, min(100, u − 100))` — how far `U` is into the 100–200 range, clamped to `0 … 100`.
3. Look up entry `m` in the lookup table, where table entry `m` is built as a linear blend between `white` and `maxUrgencyColour`:
   - `mixMax = m / 100`, `mixMin = 1 − mixMax`
   - `R = white.R × mixMin + maxUrgencyColour.R × mixMax = 255` (both endpoints have `R = 255`, so `R` never changes)
   - `G = white.G × mixMin + maxUrgencyColour.G × mixMax = 255 − 127 × mixMax`
   - `B` = same formula as `G` (both endpoints have `B = G`)
   - Each channel is truncated to a whole number and clamped to `0 … 255` (the clamp never actually triggers here, since the blend always stays within range).

In short: any Urgency at or below 100 renders as pure white; Urgency at or above 200 renders as the full light-red `RGB(255, 128, 128)`; anything in between is a straight-line fade between the two, driven only by the `G`/`B` channels.

**Worked example 1 — below the threshold**

Urgency = 89.1 (worked example 2 above). `u = 89`, `m = max(0, min(100, 89 − 100)) = max(0, −11) = 0`.
Colour = `RGB(255, 255, 255)` — plain white, no highlighting.

**Worked example 2 — partway through the range**

Urgency = 180.5 (worked example 3 above). `u = 180`, `m = max(0, min(100, 180 − 100)) = 80`.
`mixMax = 0.8`. `G = B = 255 − 127 × 0.8 = 255 − 101.6 = 153.4 → ⌊153.4⌋ = 153`.
Colour = `RGB(255, 153, 153)` — a clearly visible, but not maximal, warm pink highlight.

**Worked example 3 — at and beyond saturation**

Urgency = 250 (e.g. a very overdue, very high-priority task). `u = 250`, `m = max(0, min(100, 250 − 100)) = min(100, 150) = 100`.
`mixMax = 1.0`. `G = B = 255 − 127 × 1.0 = 128`.
Colour = `RGB(255, 128, 128)` — the full `maxUrgencyColour`. Any Urgency ≥ 200 produces exactly this same saturated colour, since `m` is clamped at 100.

<a id="role-permission-level"></a>
## 13. Role / Permission Level

A Role or Permission Level classifies what a Person is allowed to do — create, edit, delete — against each entity type.

It's key because not every user should be able to edit or delete everything; a multi-user tool needs a shared, enforceable understanding of who can change what before real customers trust it with real data.

The old model's roles (SuperUser/PowerUser/NormalUser/ReadOnlyUser) were flat and system-wide; `DomainModel.md` settles this into two tiers now that Organisation and Team are real concepts: a per-Team role (`UserType` on PersonRole — create/edit/delete rights scoped to that Team, and renamed for V2 to TeamLeadUser/LeadUser/NormalUser/ReadOnlyUser) plus a separate organisation-wide administrator flag (`IsOrganisationAdmin` directly on Person, independent of Team membership). The boundary between them sits at the Person/PersonRole split: a Team's TeamLeadUser can manage that Team's own membership and roles (add, remove, or re-role an existing Person on their Team) but not the Person record itself — creating, editing, or deleting a Person is `IsOrganisationAdmin`-only, full stop. This is one of the concepts most directly tied to the "identity direction" foundational decision in `Goals.md` — the login/session model and the permission model need to be designed together.

<a id="merge-conflict"></a>
## 14. Merge / Conflict

Merge/Conflict describes what happens when two people edit the same record at the same time.

It's key because any tool used by more than one person concurrently has to have an answer for this — even if the answer is "make it rare and simple to recover from" rather than "build an elaborate resolution UI."

It directly shapes the API's concurrency model and the client UX for editing. `DomainModel.md` settles this by level: the Demonstrator has multiple named users accessing and modifying data concurrently, but not editing the same record at the same time — real usage avoids that, so no conflict-handling is built at all; Level 2 can no longer rely on that avoidance and needs a real answer, whether that's real-time multi-user editing (users see each other's changes live, a materially bigger commitment) or the old system's interactive field-by-field merge dialog, or both.

<a id="attachment"></a>
## 15. Attachment

An Attachment is a file, a captured email, or a hyperlink, attached to exactly one Task, Project, or Component.

It's key because planning data is rarely self-contained — a task often needs its supporting email thread, spec document, screenshot, or a link to where the real detail lives, kept alongside it rather than filed somewhere else that gets forgotten.

The concept survives into the new system; the old ingestion mechanism (Outlook drag-and-drop, Word COM automation) does not, and needs reinventing as an API-first upload, a simple URL entry for the hyperlink kind, and, if still required, an inbound-email mechanism (see `Goals.md` Non-Goals and `UseCases.md` #5).

<a id="remark"></a>
## 16. Remark

A Remark is a comment or note attached to exactly one Task, Project, or Component.

It's key because it's the system's lightweight collaboration mechanism — a way to discuss or record context on a work item without leaving the tool or needing a separate chat or email thread.

See `DomainModel.md`'s Remark entry for the structural fix needed to correct a data-integrity quirk in the old model, where editing a Remark silently reassigned it to whoever last touched it — a small change, but an important one for Remark to actually function as a trustworthy record of who said what.

<a id="delivery-level-terms"></a>
## 17. Demonstrator / MVP / (Level 3) "Everything Else"

These are the three delivery levels defined in `Goals.md`, distinguished by customer count, security posture, and hosting location rather than by a fixed feature list.

They're key because, without this framing, every design conversation risks either over-building for a future that hasn't arrived yet, or under-building foundations that are expensive to retrofit later. The levels give a shared vocabulary for "is this needed now, or can it wait" that applies consistently across every concept in this document.

They act as the lens the rest of the plan is viewed through: nearly every concept above gets evaluated against them at some point — does the Demonstrator need Merge/Conflict handling? Does Role/Permission Level need to be per-Team by MVP? — which is why `DomainModel.md` and `UseCases.md` both reference these levels directly when flagging open questions.

<a id="foundational-decision"></a>
## 18. Foundational Decision

A Foundational Decision is one that's cheap to make correctly now and expensive to retrofit once real data and real clients depend on it (e.g. the API-first boundary, the tenant-shaped data model, the identity direction).

It's key because it's the escape valve in the level-based approach above: without it, "defer everything possible to a later level" would also defer decisions that actually need to be right from the very first line of code, turning a cheap early choice into an expensive later rewrite.

It's used throughout `Goals.md` and `DomainModel.md` to flag which questions can't simply be left until later. Identity direction is still open (`Q-KC-1` below); derived-vs-stored scheduling and (for Level 1) Team scoping have already been decided this way — settled early, even though the full features built on top of them (real identity federation, full Team management) are scoped to later levels.

<a id="open-questions"></a>
## 19. Open Questions

- **Q-KC-1: Identity direction** — how a Person authenticates (e.g. federating to an external identity provider, per the Person entry above) is a Foundational Decision (§18) flagged throughout this document (Person, Role / Permission Level) as needing a stated direction — still open; see `Goals.md`'s Level 1/2 "identity direction" framing questions.
- **Q-KC-2: Status vocabulary** — the exact set of Status values (Priority / Status entry above) is open to revisiting for the new system; only the two-field shape (importance vs. lifecycle state) is settled.
- **Q-KC-3: Urgency algorithm constants** — the specific tuned constants in the current Urgency algorithm (§12: the 10-day closed-task decay window, the 60-day time-pressure horizon, the priority-weighting exponents) are hand-picked values from the old implementation, not derived from any external rule — worth validating against real usage rather than treating as fixed requirements when `V2` reimplements this.

<a id="decisions"></a>
## 20. Decisions

None yet — when an open question above is answered, its entry moves here as `D-KC-<N>`, in the three-line format described in `Claude/Guidelines/ImplementationApproach.md` §3.2.
