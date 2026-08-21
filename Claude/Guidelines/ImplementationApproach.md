# Implementation Approach

## Contents

1. [Purpose](#purpose)
2. [Structure Per Level](#structure-per-level)
   - 2.1 [ImplementationPlan.md](#implementation-plan)
   - 2.2 [Scope.md](#scope-doc)
   - 2.3 [Phase Subfolders](#phase-subfolders)
3. [Open Questions: Level-Wide vs. Phase-Specific](#open-questions-placement)
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

<a id="open-questions-placement"></a>
## 3. Open Questions: Level-Wide vs. Phase-Specific

An open question belongs in a Level's `ImplementationPlan.md` if it affects how more than one phase of that Level is approached, or the Level's scope/direction as a whole. A question that only affects one phase's design belongs among that phase's own documents instead.

<a id="placeholder-levels"></a>
## 4. Placeholder Levels and Deferral

Level 2 and Level 3 exist from the outset as placeholder folders (`Level2_Implementation`, `Level3_Implementation`), each with its own `ImplementationPlan.md` and `Scope.md`, even before any of their phases are underway. When something is deliberately deferred out of Level 1 (or, later, out of Level 2), it gets recorded in the later Level's `Scope.md` or `ImplementationPlan.md` as it arises, rather than being lost as a passing note in the Level it was deferred *from*.
