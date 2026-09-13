# User Documentation Guidelines

## Contents

1. [Purpose](#purpose)
2. [Location and Document Set](#location-and-document-set)
   - 2.1 [Per-GUI Documents](#per-gui-documents)
   - 2.2 [ProjectPal-Wide Documents](#projectpal-wide-documents)
   - 2.3 [Glossary.md](#glossary)
   - 2.4 [Self-Containment](#self-containment)
3. [Audience and Tone](#audience-and-tone)
4. [When to Write or Update a GUI Document](#when-to-write)
5. [Standard Structure for a GUI Document](#standard-structure)
6. [Keeping User Documentation Up to Date](#keeping-up-to-date)
   - 6.1 [An Ongoing, Important Responsibility](#ongoing-responsibility)
   - 6.2 [The Check Before Each Major Commit](#check-before-commit)

<a id="purpose"></a>
## 1. Purpose

This document describes the `V2/UserDocumentation/` directory: what belongs in it, who it's written for, how each document should be structured, and — critically — the ongoing obligation to keep it in sync with the actual GUIs as they're built and changed. It's a process guideline, followed alongside `document-guidelines.md`, which still governs mechanics such as tables of contents.

`V2/UserDocumentation/` is end-user-facing documentation for ProjectPal itself — how to use the product. It is a different thing from everything else under `Claude/`, which is internal project documentation about *building* ProjectPal (requirements, design decisions, implementation plans) and is never a suitable audience or style reference for anything written here.

<a id="location-and-document-set"></a>
## 2. Location and Document Set

All end-user documentation lives under `V2/UserDocumentation/` (a sibling of `V2/gui-client/`, `V2/rest-api/`, etc. — shipped alongside the product it documents, not under `Claude/`).

<a id="per-gui-documents"></a>
### 2.1 Per-GUI Documents

One Markdown document per GUI/screen (e.g. an `AllTasks.md` for the All Tasks grid, a `PlanView.md` for the Gantt/Plan View, a `TaskDetail.md` for Task Detail, and so on as further screens are built). Each document is named after the screen it covers, clearly enough that a reader can match it to what they see in the app.

A per-GUI document can only be written, or brought up to date, once the GUI it covers actually exists in that form — it documents the real, current screen, not a planned or in-progress one. Do not create a placeholder document for a screen that isn't built yet, and do not describe planned-but-unimplemented behaviour in one that is. When a screen is amended after its document already exists, the document is updated to match — see §6.

<a id="projectpal-wide-documents"></a>
### 2.2 ProjectPal-Wide Documents

Alongside the per-GUI documents, `V2/UserDocumentation/` also holds documents that cover ProjectPal as a whole rather than one screen — an `IntroductionToProjectPal.md`, a `UserGuide.md`, and similar, added over time as they become useful. These cover cross-cutting themes (what ProjectPal is for, core concepts that span multiple screens, common end-to-end workflows) rather than duplicating what a per-GUI document already covers for its own screen (`document-guidelines.md` rule 2 — a per-GUI document is the canonical source for its own screen's own details; a ProjectPal-wide document references it rather than restating it).

None of these exist yet as of this document's writing. This section is a placeholder for when they're added — no other action is needed here now.

<a id="glossary"></a>
### 2.3 Glossary.md

`V2/UserDocumentation/Glossary.md` defines terms meaningful to someone *using* ProjectPal — Project, Task, Component, Resource, Dependency, Urgency, the various role names, and so on — in plain, user-facing language. It stays away from implementation details (database column names, internal type names, API endpoints, code structure) unless a user genuinely needs that detail to understand or use the product (which should be rare).

Every other document in `V2/UserDocumentation/` should link to `Glossary.md` for a term's definition the first time that term is used, rather than redefining it inline (`document-guidelines.md` rule 2's single canonical source of truth applies here too — the Glossary is that source for term definitions).

<a id="self-containment"></a>
### 2.4 Self-Containment

The whole `V2/UserDocumentation/` directory must be self-contained. A document within it may link to another document within it — that's how §2.2/§2.3's "reference rather than restate" avoidance of duplication actually works — but it must never link to, or assume the reader has access to, anything else in the repository: nothing under `Claude/`, not `V2/README.md`, no source file, no other document outside `V2/UserDocumentation/` itself.

This bounds where `document-guidelines.md` rule 2's "single canonical source of truth" can point to, for anything living in this directory: the canonical source for a fact belongs *within* `V2/UserDocumentation/` (another document there, or `Glossary.md`), never outside it. Where a fact is already recorded elsewhere in the repository for an implementation/build purpose (a design decision, a Plan.md entry, a requirements document), it is restated here directly, in end-user terms, rather than linked out to that source — the two descriptions serve different readers and are allowed to coexist without one deferring to the other.

The reason for this: `V2/UserDocumentation/` is meant to be usable as a standalone unit — handed to, or shipped alongside a build for, an end user who has no access to (and no interest in) the rest of the repository. A link out of the directory would be a dead end for that reader.

<a id="audience-and-tone"></a>
## 3. Audience and Tone

Every document under `V2/UserDocumentation/` is written for someone *using* ProjectPal — a Team Lead, a Resource, an Organisation Admin — not for someone building or maintaining it. Concretely:

- Describe what a screen shows and what a user can do with it, in terms of on-screen elements a user would actually see and name (buttons, fields, columns, checkboxes) — not the code, component, or data structures behind them.
- Avoid implementation vocabulary entirely: no React, no API endpoints, no database tables/columns, no internal function or file names, no framework names. If a rule exists only because of how something happens to be built (rather than being a deliberate product behaviour a user should understand), it usually doesn't belong here at all.
- Where behaviour differs by role (an Organisation Admin sees a control a Resource doesn't, say), say so explicitly and name the roles as a user-facing concept (linking to `Glossary.md` for what each role means) — don't describe the mechanism that enforces it.
- Prefer plain, direct instructions ("Click **Add Dependency** to link this Task to another one") over describing behaviour abstractly.

This is the opposite emphasis from every other document under `Claude/` (which are implementation/design-focused, for whoever is building ProjectPal) — when in doubt about whether a detail belongs here, ask whether a real end user, with no interest in how the software is built, would find it useful.

<a id="when-to-write"></a>
## 4. When to Write or Update a GUI Document

A GUI's own user-documentation file is written (or, once it exists, updated) only once the screen it describes is actually implemented, or has actually been amended, in that form — never ahead of the work, and never describing a planned change that hasn't landed yet. This mirrors `4_GuiClient/Plan.md`'s own approach of describing what's actually true today (its §7.2 Manual Testing section), not a roadmap.

As each new GUI is built (`4_GuiClient/Plan.md`'s own Build Order, §6, and equivalent build-order sections in later phases/Levels), add its own document here once it's real. `4_GuiClient/Plan.md` carries a permanent note to this effect (§7.3 there) so this isn't only discoverable from this Guidelines document.

<a id="standard-structure"></a>
## 5. Standard Structure for a GUI Document

Every per-GUI document uses the same major section headings, in the same order, so a reader familiar with one can navigate any other without relearning its layout. Per `document-guidelines.md` rule 1, every document also carries a `## Contents` table of contents built from these headings using that rule's anchor mechanism.

1. **Overview** — one or two paragraphs: what this screen is, what it's for, and when/why a user would open it.
2. **Who Can Use This Screen** — which role(s) (`Glossary.md`) can open this screen at all, and, where behaviour or available actions differ by role, what each role can specifically see or do here. If every role sees and can do exactly the same thing, say so explicitly rather than omitting the section.
3. **What You See** — a walkthrough of the screen's own layout and content: its major areas, what each shows, and what any colours, icons, or symbols mean. Organised top-to-bottom or by area, matching how a user would actually scan the screen.
4. **How to Use It** — every way a user can interact with the screen: clicking, double-clicking, dragging, typing, keyboard shortcuts, right-click behaviour, checkboxes/toggles and what they change, resizing, zooming — whatever genuinely applies to that screen. Organised by feature/interaction, not by how it happens to be implemented.
5. **Related Screens** — links to other GUI documents a user would naturally move to or from (e.g. Task Detail linking to All Tasks, or the Plan View linking to Task Detail), avoiding repeating what those documents already cover.

A document may add its own numbered subsections within any of these (per `document-guidelines.md` rule 2's up-to-three-level heading depth) where a screen is complex enough to need them (the Plan View's "How to Use It", for example, is expected to need several subsections — zoom, panning, reordering, tooltips). The five major headings themselves, and their order, do not vary between documents.

<a id="keeping-up-to-date"></a>
## 6. Keeping User Documentation Up to Date

<a id="ongoing-responsibility"></a>
### 6.1 An Ongoing, Important Responsibility

Keeping every document under `V2/UserDocumentation/` in sync with the real, current behaviour of the product is an ongoing and important activity for the lifetime of this project — not a one-off task completed when a document is first written. A stale user-facing document (one describing a screen as it used to behave, or missing a feature that's since shipped) actively misleads the people it's written for, which is worse than having no document at all.

This responsibility does not require reviewing or touching these documents on every single turn or every small change — that would be excessive overhead for a documentation set aimed at users, not a live build log. Instead, see §6.2.

<a id="check-before-commit"></a>
### 6.2 The Check Before Each Major Commit

Before each major commit (a commit that lands a GUI-visible feature, behaviour change, or fix a user would notice — not a purely internal refactor, test-only change, or documentation-only commit), check whether any document under `V2/UserDocumentation/` needs updating as a result:

- If the change is significant enough that a user could plausibly rely on out-of-date guidance (a new control, a changed interaction, a removed feature, a behaviour that now differs by role), and the fix to the documentation is not obviously small, **say so to the user** rather than silently deciding either way — flag which document(s) look like they need updating and let the user decide whether to do it now or later.
- If the needed update is genuinely trivial (a small wording fix, updating one sentence to reflect a minor tweak, adding one bullet for a small new option) — **make the change directly**, without asking first, the same way any other clearly-correct small fix would be handled.
- If a commit introduces a GUI for the first time, its own document is part of "the commit is finished" for that GUI, not a follow-up — see §4.

When in doubt about whether an update is "trivial" or needs flagging first, flag it — the cost of a short question is much lower than the cost of a user-facing document silently drifting from reality.
