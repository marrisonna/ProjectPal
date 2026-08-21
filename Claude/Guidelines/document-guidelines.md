# Document Guidlines

Use these rules when generating documentation to be read by humans.

They take precedence above the guidance given elsewhere.

If there is a conflict, then the earlier rule specified takes precedence.

1. If a document has multiple numbered sections, then include a table of contents at the top of the document, 
with links to each section.  The table of contents should be generated automatically from the section headings and 
kept up to date as the document is edited.  The table of contents should be kept up to date as the document 
evolves and it should be placed after any front matter, such as a title, author, date, and abstract.
Create the table of contents using the following mechanism (as used in
`LatticeHLABridgeStrategyEvaluation/LatticeIntegration/EvaluationDocumentation/Lattice_DataPlane_HLA_Integration_Evaluation.md`):
   - Place an explicit HTML anchor on its own line immediately before each section and subsection
     heading, e.g. `<a id="purpose-and-scope"></a>` directly above `## 1. Purpose & Scope`. Use a stable,
     descriptive kebab-case slug — do **not** rely on the renderer auto-generating an anchor from the
     heading text, because those slugs vary by renderer and break when a heading is reworded.
   - Under a `## Contents` heading, write the table of contents as a Markdown list in which each entry is
     a link of the form `[Heading text](#slug)` pointing at the matching anchor.
   - Mirror the heading hierarchy in the list: a numbered list for the top-level sections (the number
     matching the section number), with nested sub-items indented (e.g. two spaces per level) as `-`
     bullets for subsections; list any appendices as un-numbered `-` bullets at the end.
   - Keep the slug stable when a heading's wording changes so existing links do not break; only add a new
     anchor/entry when a new section is introduced, and remove both together when a section is deleted.

2. Avoid repetition across documents.  That is, when an idea, concept, requirement, or whatever is described/defined, it
   should be done in one place.  Other documents that need it should then reference the source.  So we have a 'single canonical source
   of truth' and we don't have multiple descriptions of the same thing in multiple places that may become inconsistent with
   each other and are a burden to maintain.  To make this easier, use upto three
level of heading numbering in the documents and reflect that in the table of contents of each document.

3. Give recurring items that get cross-referenced from other documents (e.g. open questions, decisions) a stable,
   unique ID, so they can be referenced by that ID elsewhere instead of being restated (per rule 2's single
   canonical source of truth). The concrete ID format is chosen per document family rather than fixed here — e.g.
   `Claude/Guidelines/ImplementationApproach.md` §3.1 defines the ID scheme used for open questions in the
   Level/Phase implementation-tracking documents.


Claude should ignore all the text below this line.  This will be used at a later date.
4. As a document evolves, avoid adding asides that clutter the document with details about how it has evolved.  A 
  reader is not interested in how the document has evolved, they are only interested in the final state.
5. State each point directly in its final form.  Do not describe what something *is not*, or contrast it
  against alternatives that were considered but not chosen, unless the contrast is genuinely needed for the
  reader to understand the final state.  For example, write "`X` is a list of positions" rather than
  "`X` is a list of positions, not a boolean".
