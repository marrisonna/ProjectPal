# Implementation Approach

## Contents

1. [Purpose](#purpose)
2. [Structure Per Level](#structure-per-level)
   - 2.1 [ImplementationPlan.md](#implementation-plan)
   - 2.2 [Scope.md](#scope-doc)
   - 2.3 [Phase Subfolders](#phase-subfolders)
3. [Open Questions: Level-Wide vs. Phase-Specific](#open-questions-placement)
   - 3.1 [Numbering Scheme](#open-questions-numbering)
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
## 3. Open Questions: Level-Wide vs. Phase-Specific

An open question belongs in a Level's `ImplementationPlan.md` if it affects how more than one phase of that Level is approached, or the Level's scope/direction as a whole. A question that only affects one phase's design belongs among that phase's own documents instead.

<a id="open-questions-numbering"></a>
### 3.1 Numbering Scheme

Per `document-guidelines.md` rule 3, every open question is given a stable ID so it can be referenced from elsewhere without restating it, in the form `O<Level>[.<Phase>]-<N>`:

- `O` marks the ID as an open-question reference.
- `<Level>` is the Level number the question belongs to.
- `.<Phase>`, if present, is the phase number (from that Level's `ImplementationPlan.md` phase table) the question is specific to. It's omitted for a Level-wide question.
- `-<N>` is the question's sequential number within its scope (its Level, or its Level+Phase), starting at 1.

Examples:
- `O1-2` — Level 1's 2nd Level-wide open question (recorded in `Level1_Implementation/ImplementationPlan.md`).
- `O1.2-3` — Level 1, Phase 2's 3rd open question (recorded among Phase 2's own documents, e.g. `Level1_Implementation/2_RestApi/Plan.md`).

A sub-phase (§2.3) keeps its parent phase's number for this purpose — a question specific to sub-phase `2a` is still numbered `O1.2-<N>`, since the `.<Phase>` component identifies the phase, not the sub-phase.

<a id="placeholder-levels"></a>
## 4. Placeholder Levels and Deferral

Level 2 and Level 3 exist from the outset as placeholder folders (`Level2_Implementation`, `Level3_Implementation`), each with its own `ImplementationPlan.md` and `Scope.md`, even before any of their phases are underway. When something is deliberately deferred out of Level 1 (or, later, out of Level 2), it gets recorded in the later Level's `Scope.md` or `ImplementationPlan.md` as it arises, rather than being lost as a passing note in the Level it was deferred *from*.
