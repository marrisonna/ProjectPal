# Implementation Approach

## Contents

1. [Purpose](#purpose)
2. [Structure Per Level](#structure-per-level)
   - 2.1 [ImplementationPlan.md](#implementation-plan)
   - 2.2 [Scope.md](#scope-doc)
   - 2.3 [Phase Subfolders](#phase-subfolders)
3. [Open Questions and Decisions](#open-questions-placement)
   - 3.1 [Numbering Scheme](#open-questions-numbering)
   - 3.2 [Recording a Decision](#recording-a-decision)
4. [Placeholder Levels and Deferral](#placeholder-levels)

<a id="purpose"></a>
## 1. Purpose

This document describes how implementation work for ProjectPal V2 is planned and tracked, Level by Level (see `Requirements/Goals.md` §4 for what a Level is). It's a process guideline, followed alongside `document-guidelines.md`.

<a id="structure-per-level"></a>
## 2. Structure Per Level

Each Level (`Requirements/Goals.md` §4) has its own folder directly under `Claude/`, named `LevelN_Implementation` (e.g. `Level1_Implementation`). Each Level folder contains two tracking documents and one subfolder per phase of that Level's work.

<a id="implementation-plan"></a>
### 2.1 ImplementationPlan.md

The single tracking document for that Level. It lists every phase of the Level, in order, with its current status, and links to that phase's subfolder for details. It does **not** contain phase details itself — per `document-guidelines.md` rule 2, those live once, in the phase's own subfolder, and this document only carries enough of a description to identify the phase and its state. It also carries open questions that apply to the Level as a whole (see §3 below).

<a id="scope-doc"></a>
### 2.2 Scope.md

What is in and out of scope for that Level, seeded from `Requirements/Goals.md`'s definition of the Level and refined as work proceeds.

<a id="phase-subfolders"></a>
### 2.3 Phase Subfolders

Each phase of a Level's implementation gets its own subfolder inside that Level's folder. All documentation for that phase — plans, decisions, supporting research, open questions specific to it — lives there. `ImplementationPlan.md` links to it rather than duplicating its content.

Phase subfolders are named `N_PhaseName` (e.g. `1_DatabaseSetup`, `2_RestApi`), where `N` is the phase's position in `ImplementationPlan.md`'s phase table, so they sort in build order when listed. If a phase later needs to split into sub-phases, name those `NaPhaseName`, `NbPhaseName`, etc. (e.g. `2a_RestApiCrud`, `2b_RestApiBusinessRules`), keeping them sorted immediately after phase `N` and before phase `N+1`.

<a id="open-questions-placement"></a>
## 3. Open Questions and Decisions

An open question, and later the decision that answers it, belongs in a Level's `ImplementationPlan.md` if it affects how more than one phase of that Level is approached, or the Level's scope/direction as a whole. One that only affects a single phase's design belongs among that phase's own documents instead. Every document that has an "Open Questions" section also has a "Decisions" section immediately after it, even while that section is still empty.

<a id="open-questions-numbering"></a>
### 3.1 Numbering Scheme

Per `document-guidelines.md` rule 3, every open question and decision is given a stable ID so it can be referenced from elsewhere without restating it:

- An open question is `O<Level>[.<Phase>]-<N>`; the decision that answers it is `D<Level>[.<Phase>]-<N>` — the same `<Level>`, `<Phase>`, and `<N>` as the question it resolves, only the letter changes.
- `<Level>` is the Level number the item belongs to.
- `.<Phase>`, if present, is the phase number (from that Level's `ImplementationPlan.md` phase table) the item is specific to. It's omitted for a Level-wide item.
- `<N>` is the item's sequential number within its scope (its Level, or its Level+Phase).

Examples:
- `O1-2` / `D1-2` — Level 1's 2nd Level-wide open question, and the decision that answers it (recorded in `Level1_Implementation/ImplementationPlan.md`).
- `O1.2-3` / `D1.2-3` — Level 1, Phase 2's 3rd open question, and the decision that answers it (recorded among Phase 2's own documents, e.g. `Level1_Implementation/2_RestApi/Plan.md`).

A sub-phase (§2.3) keeps its parent phase's number for this purpose — an item specific to sub-phase `2a` is still numbered `O1.2-<N>` / `D1.2-<N>`, since the `.<Phase>` component identifies the phase, not the sub-phase.

Within a given scope, `<N>` is drawn from a single counter shared by that scope's open questions and decisions together, and is permanent and immutable once assigned: it is never reassigned, reused, or changed, whether or not the question it was given to is later answered. Concretely, if `O1.2-1`, `O1.2-2`, and `O1.2-3` exist and `O1.2-2` is answered, it becomes `D1.2-2` (the number doesn't change, only the letter) — `O1.2-3` is unaffected, and the next new question raised in that scope is `O1.2-4`, never a reused `O1.2-2`.

<a id="recording-a-decision"></a>
### 3.2 Recording a Decision

When an open question is answered, remove its entry from "Open Questions" and add a corresponding entry to "Decisions", using the same number with the `D` prefix (§3.1). Decisions are listed in order of their reference number (i.e. the order the original questions were raised in), not the order they were decided in or their decision date — so answering a later question before an earlier one still leaves the earlier one's eventual decision listed first. A decision entry is always three lines, in this order and format:

1. `**D<ID>** (decided <YYYY-MM-DD>)` — the ID and the date the decision was made.
2. `**Question:** <question text>` — the original question, restated (not just a link back, since the "Open Questions" entry it came from no longer exists once it moves).
3. `**Decision:** <decision text>` — the decision that was made.

Write it as a single list item with the three lines separated by `<br>` (not blank lines, and not three separate list items), so it reads as one entry:

```
- **D1-2** (decided 2026-08-21)<br>
  **Question:** Auth model for the Demonstrator: a single shared login vs. individually named users.<br>
  **Decision:** individually named users, not a shared login, because ...
```

<a id="placeholder-levels"></a>
## 4. Placeholder Levels and Deferral

Level 2 and Level 3 exist from the outset as placeholder folders (`Level2_Implementation`, `Level3_Implementation`), each with its own `ImplementationPlan.md` and `Scope.md`, even before any of their phases are underway. When something is deliberately deferred out of Level 1 (or, later, out of Level 2), it gets recorded in the later Level's `Scope.md` or `ImplementationPlan.md` as it arises, rather than being lost as a passing note in the Level it was deferred *from*.
