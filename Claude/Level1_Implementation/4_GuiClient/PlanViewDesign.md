# Plan View (Gantt Display) — Design Specification

This document specifies the observable look and behaviour of the Plan View
(the Gantt-style chart at `V2/gui-client/src/features/plan/PlanPage.tsx`,
backed by the pure layout module `V2/gui-client/src/lib/ganttLayout.ts`), in
enough detail that an independent implementation could reproduce a very
similar result. It describes *rules*, not code structure — colours, sizes,
opacities, coordinate math, and interaction behaviour. For the history of
*why* each rule exists, see the decision log `4_GuiClient/Plan.md` (IDs
D1.4-24 onward); this document only says what the current behaviour *is*.

Source of truth: this document was written by reading the current
implementation directly (`ganttLayout.ts` in full, `PlanPage.tsx` in full,
and the relevant parts of `lib/schedule.ts`) on 2026-09-13. If the code and
this document ever disagree, the code is authoritative and this document is
stale and should be corrected.

## 1. Scope and data

- The view shows either one Project's own subtree (`/plan/:projectId`) or
  every top-level Project the signed-in user belongs to (`/plan`, "Top
  Level Projects").
- Only Projects belonging to a Team the current Person has *any* role on
  are considered at all (every other Project is invisible to this view,
  regardless of scope).
- A Task is excluded entirely if its `status` is `Closed` or `Cancelled`,
  or if it has no resolvable start/end date (see §2).
- A Project is excluded entirely (itself and its whole subtree) if its
  `priority` is `Cancelled` or `Closed`.
- Rows are hierarchical: a Project row, followed by its own direct
  children (Tasks and sub-Projects interleaved together — see ordering
  below), each sub-Project followed recursively by its own children, in a
  depth-first preorder walk. A Task is always a leaf.

## 2. Dates

Every date shown or positioned in this view is *computed*, never stored —
neither Task nor Project persists a start/end date. Computation (in
`lib/schedule.ts`) is out of this document's scope except where it affects
Gantt-specific rules; the important points here are:

- A computed date can legitimately carry a non-midnight time-of-day
  component (an artifact of repeated day-arithmetic starting from a
  DST-affected origin). Anything that compares two dates for "same day" or
  counts days between them must first normalise to a UTC day-index
  (`calendarDayNumber` — `Math.round(Date.UTC(y,m,d)/86400000)`), never
  compare raw timestamps or use `<`/`>` on un-normalised `Date` objects.
  Failing to do this produced a real, once-shipped bug (bars drawn exactly
  one day late whenever "Weekends" was unchecked and a bar's date carried a
  stray time-of-day, D1.4-38).
- Every date shown to the user (tooltips, the hovered-date readout, month
  labels) is formatted `dd-Mmm-yy`, e.g. `08-Jan-26`.

## 3. Coordinate system and base constants

The layout is computed once at a fixed 100%-baseline scale; zoom (§8) is a
pure render-time linear scale applied on top, never fed back into layout.

- `PIXELS_PER_DAY = 10` — one calendar day (or one compressed weekday unit
  when weekends are excluded, §4) is 10px wide at 100% horizontal zoom.
- `ROW_HEIGHT = 20` — every row (Task or Project) occupies a 20px vertical
  band at 100% vertical zoom.
- `BAR_HEIGHT = 10` — an ordinary (non-boxed) bar is drawn 10px tall,
  vertically centred within its own 20px row band (a 5px gap above and
  below).
- Row `y` is simply `rowIndex * ROW_HEIGHT`, where `rowIndex` is the row's
  0-based position in the depth-first preorder walk described in §1 —
  there is no gap between rows.
- Bar `x` is the day-offset (§4) from the chart's own `minDate` (the
  earliest start date across every visible row in the current scope),
  multiplied by `PIXELS_PER_DAY`.
- A bar's width is **one calendar day past its own end date**, minus its
  start offset: a Task/Project that starts and ends on the same day is
  drawn as a full one-day-wide bar (10px at 100% zoom), not a zero-width
  sliver — the end date is treated as a whole day the bar occupies, not an
  instant it stops at. Concretely: `endX = dayOffset(minDate, endDate + 1
  calendar day)`, and this "+1 day" is applied *before* the weekend
  compression math runs (§4), not after, so it composes correctly (e.g. a
  bar ending on Friday needs to reach Saturday's own weekend-clamped
  offset).
- Bar width is never less than 1px, even in a degenerate zero-day case.
- Both a bar's start and end offsets are always measured from the same
  `minDate` reference point and then combined — not computed as a direct
  `dayOffset(startDate, endDate)` — so that weekend-compression's
  start/end clamping behaviour (§4) is anchored consistently to one fixed
  weekday reference, not to a bar's own (possibly-weekend) start date.

## 4. "Weekends" checkbox (excluding Saturdays/Sundays)

Unchecked by default. Governs the single day-to-pixel mapping the entire
chart uses (bar positions, grid lines, today-marker, cursor date-readout —
all go through the same function).

- **Checked** ("Weekends" shown): plain calendar-day mapping. Seven days
  between consecutive Monday gridlines, 70px at 100% zoom.
- **Unchecked** (default): Saturdays and Sundays contribute no width at
  all. Five days between consecutive Monday gridlines (50px at 100%
  zoom). The mapping function (`compressedDayOffset(from, to)`) counts the
  number of weekdays in the half-open range `[from, to)`.
  - This single formula also produces the required start/end clamping
    with no separate step: a Saturday, the Sunday right after it, and the
    following Monday all map to the *same* offset. So a bar whose start
    lands on a weekend renders as if it starts at the beginning of the
    following Monday, and a bar whose end lands on a weekend renders as
    if it ends at the end of the preceding Friday — both fall directly out
    of applying the same `to`-offset formula unmodified.
  - The inverse mapping (pixel/day-offset back to a real date,
    `dateAtCompressedOffset`) is needed for the cursor date-readout and
    for keeping a date anchored across the checkbox toggle. Since the
    forward mapping is many-to-one at a weekend boundary, the inverse
    canonically resolves such an offset to the *next real (weekday) date*
    — i.e. the following Monday — since the compressed timeline has no
    pixel space of its own representing a weekend.
- Toggling the checkbox keeps whatever date currently sits at 25% of the
  visible chart width from the left (the same fraction the "Today" button
  targets, §8) fixed at that same screen position: the date under that
  point is read under the *old* mapping before the toggle, then the
  horizontal scroll position is recomputed to put that same date back at
  that same 25%-from-left position under the *new* mapping.

## 5. Row ordering, hierarchy, and manual reorder

### 5.1 Default order

Within one Project's own direct children:

- Tasks are sorted by their own computed start date (ascending).
- Sub-Projects are sorted alphabetically by name.
- Tasks and sub-Projects are then combined into one list — Tasks first,
  then sub-Projects — before any manual reorder (§5.2) is applied. (Manual
  reorder can freely interleave them; only the never-reordered default
  keeps Tasks first.)

Top-level Projects (the unscoped "Top Level Projects" view) are sorted
alphabetically by name.

### 5.2 Manual drag-and-drop reorder

A row's label (in the left-hand text column, §6) can be dragged up or down
to change its position **among its own siblings only** — a "sibling" is
another row with the same parent Project (or, for a top-level Project,
another top-level Project). Hierarchy/parent can never change via this
drag; there is no way to drop a row into a different parent's group,
because the drop calculation only ever compares the dragged row's position
against its own existing sibling list.

- Mouse-down on a label starts the drag: native text selection is
  suppressed (`preventDefault` plus clearing any existing selection) so
  dragging never highlights label text instead of moving the row.
- While dragging, a floating label follows the cursor: the row's own text,
  at 50% opacity, in a small bordered box, offset 12px right and 12px down
  from the cursor, non-interactive (never itself becomes a drop target).
- The floating label's vertical position is clamped to the dragged row's
  own sibling group's on-screen extent — from the top of the topmost
  sibling to the bottom of the bottommost sibling — so it can never visually
  appear to suggest a position outside the parent's own bounds. Horizontal
  position simply follows the cursor unclamped.
- On mouse-up, the drop position is computed by comparing the (same
  vertically-clamped) release point against the vertical midpoint of each
  other sibling; the dragged row is inserted at the resulting index. If
  the resulting order is unchanged, nothing happens.
- The result is stored as an in-memory override (keyed by parent) that is
  applied on top of the default order (§5.1) every time the layout is
  rebuilt — an explicitly-reordered sibling group is used as given; any
  child not mentioned in a saved order (e.g. one created after the order
  was last saved) is appended after the explicitly-ordered ones, in its
  own default relative order, rather than disappearing.
- This override is **never sent to the server** — it is local-only,
  per-browser display state.
- A "Memorise order" button (below the label column, centred horizontally
  in the space reserved for it) persists the current override to
  `localStorage`, keyed by the signed-in Person's own ID (so a shared
  browser profile does not mix up two different users' preferred
  orderings). It is loaded back automatically the next time that Person
  opens this view. There is no corresponding "forget"/reset button.

## 6. Left-hand label column

- Default width 220px; resizable by dragging a 6px-wide handle between the
  label column and the chart, down to 0px, up to 600px.
- A "Show Names" checkbox (default **checked**) shows/hides this column
  entirely. Unchecking it (or dragging the resize handle down to ≤24px and
  releasing) collapses it to zero width. Re-checking "Show Names" always
  restores the column to its fixed default width (220px) — there is no
  remembered custom width to come back to.
- Each row's label text: a Task shows its own description; a Project
  shows its own name. Text is indented `8 + depth*14` px from the left,
  where `depth` is 0 for a top-level Project.
- Project labels render bold; Task labels render normal weight.
- Label text is vertically centred within its own 20px (row-height,
  zoom-scaled) band, using the text baseline's own font-metric centring,
  not a hand-picked offset.
- The mouse cursor over this whole column is the plain default arrow, not
  a hand pointer or grab cursor — draggability is communicated visually
  (below), not via cursor shape. Text is not selectable in this column.
- **Hover highlight**: as the cursor moves within the column, the row
  currently under it is outlined with a 1px black (60% opacity) dashed
  border (2px dash, 2px gap), inset 1px from the row's own top/left/right
  edges and 1px short of its bottom edge — showing exactly which row a
  drag would currently pick up.
- **Shared name readout**: hovering a row here also populates the
  top-of-chart name readout (§7) with that row's own hover-caption text
  (§7.1) — identical mechanism and identical text to hovering the row's
  own bar in the chart.
- **Truncation tooltip**: if a row's own label text is wider than the
  space available for it (column width minus its own indent, minus a
  small margin), a floating tooltip showing that row's full label text
  appears near the cursor while hovering it (same tooltip mechanism as
  §7.2/§9, offset the same way). Truncation is detected by measuring the
  actual rendered text's width against the available space, not by any
  heuristic character count. Rows whose text already fits show no
  tooltip.
- **Double-click**: double-clicking a Task's label opens that Task's own
  detail window. Double-clicking a Project's label opens the Plan View
  scoped to that Project (`/plan/<projectId>`) — there is no separate
  Project detail window to open instead.
- Tooltips (both the truncation tooltip here and the bar tooltip, §9) are
  suppressed entirely while any drag (row reorder, column resize,
  right-click pan) is in progress.

## 7. Name/date readout row (above the chart)

A row of two read-only text fields sits directly above the chart:

- **Left field** (date under cursor): right-aligned, its own width tied to
  the label column's current effective width (so its right edge lines up
  2em before the chart's own left edge) — shows the calendar date
  currently under the mouse cursor within the chart's drawing area (see
  §11 for how this is computed), cleared when the cursor leaves the
  drawing area.
- **Right field** (hovered name): left-aligned, starts exactly at the
  chart's own left edge — shows the hover-caption text (below) of
  whichever Task/Project bar or label is currently under the cursor,
  cleared on mouse-leave.

### 7.1 Hover-caption text format

Both the bar tooltip (§9) and this name readout show the same text for a
given row, built as follows:

- A Project: `P: <name>` if top-level (no ancestor chain), or
  `P: <name> : [<ancestor chain>]` otherwise, where `<ancestor chain>` is
  every ancestor Project's name from the top-level ancestor down, joined
  with `=>` (bare, no spaces or brackets) — **excluding** the Project
  itself.
- A Task: always `T: <description> : [<project chain>]`, where
  `<project chain>` is the same `=>`-joined ancestor list but
  **including** the Task's own direct Project (a Task's own chain never
  omits its immediate Project, since the Task's displayed name is its
  description, not a Project name — there's nothing to avoid repeating).
- Chain-building guards against a cyclic `parent_project_id` chain
  (defensive; should not occur in valid data).

## 8. Zoom

Two independent axes, each a percentage, default 100%, clamped to
[10%, 1000%]. Zoom is purely a render-time scale factor (`zoom/100`)
multiplied onto every already-computed base coordinate — it never changes
the underlying layout data.

- **Horizontal zoom** scales `x`/`width` of everything (bars, arrows, grid
  lines, today-marker) and the day-to-pixel ratio.
- **Vertical zoom** scales `y` of everything, row height, bar height, and
  font size (`fontSize = 11 * zoomY/100`) — text and rows grow/shrink
  together. The label column's own *width* never changes with either zoom
  axis.
- **Controls**: a "Zoom:" label, then "H" and a numeric percent field for
  horizontal zoom, then "V" and a numeric percent field for vertical zoom,
  then a "Zoom Reset" button (sets both back to 100%), then a "Today"
  button (see §8.2). Percent fields commit on blur or Enter, not on every
  keystroke.
- **Mouse-wheel zoom**: while the cursor is over the chart area (label
  column + drawing area together), holding Ctrl while scrolling zooms
  horizontally; holding Shift zooms vertically; both together zoom both,
  from the same wheel event. A plain (no-modifier) wheel scrolls normally
  and is completely unaffected. The zoom factor per wheel event is
  `1.0015 ** -deltaY` (so scrolling up/away zooms in, down zooms out,
  scaling naturally for both notchy mice and smooth trackpads). Outside
  the chart area, Ctrl/Shift+wheel does whatever the browser would
  normally do (e.g. native page zoom) — this is a side effect of the
  listener only being attached to the chart area itself, not a separate
  check.

### 8.1 Zoom anchoring — what stays fixed

Every way zoom can change keeps one reference point fixed on screen:

- **Wheel-driven zoom**: anchored to the mouse cursor's own position at
  the time of the wheel event — the point under the cursor stays under
  the cursor after the zoom changes.
- **Typing a percentage, or "Zoom Reset"**: no cursor position is
  relevant, so it falls back to the visible pane's own centre (both
  horizontally and vertically).
- Mechanically: before the zoom state changes, the anchor is captured as
  a *content-relative offset* (a day-offset for X, a row-offset for Y —
  resolution-independent, meaningful across a scale change) together with
  its *viewport-relative pixel offset* (how far into the currently-visible
  pane it sits). After the new scale has actually rendered, the scroll
  position is recomputed from that same pair so the same content lands
  back at the same viewport position.

### 8.2 "Today" button and initial scroll position

- Clicking "Today" scrolls the chart horizontally so that "today"'s own
  vertical marker line (§10) sits at **25% of the way across the visible
  chart width from the left** (clamped so it never scrolls past either
  end of the content).
- The same 25%-from-left positioning is applied automatically once, the
  first time a given scope (a specific `/plan/:projectId`, or the
  unscoped "Top Level Projects" view) finishes its first layout — not
  re-applied on every later layout recompute (a "Weekends" toggle or a
  manual reorder both rebuild the layout too, and must not reset the
  user's own scroll position each time). Navigating to a different scope
  resets this "have we auto-scrolled yet" state, so the new scope gets
  its own initial auto-scroll.

## 9. Bar tooltip

Hovering any bar (Task or Project, boxed or not) shows a custom floating
tooltip — not the browser's native SVG `<title>` tooltip (a native one's
position is entirely browser-controlled and ends up covering its own
first letter under the cursor).

- Text: the bar's hover-caption (§7.1) followed by a line break, then its
  date range `<start> → <end>` (both `dd-Mmm-yy`, §2).
- Positioned near the cursor, offset **20px right, 10px down** from the
  current mouse position (`clientX + 20`, `clientY + 10`), `position:
  fixed`.
- Appearance: white background, 1px border (black, 40% opacity), 2px
  border radius, 4px horizontal / 2px vertical padding, small dense font,
  `white-space: pre` (so the line break renders), non-interactive
  (`pointer-events: none`), rendered above everything else on the page.
- Suppressed entirely while any drag is in progress (row reorder, column
  resize, right-click pan) — cleared the instant a drag starts, and not
  shown again until the drag ends and the cursor next moves over a bar.
- The label-column truncation tooltip (§6) uses the identical visual
  styling and offset, just different trigger condition and text.

## 10. Grid lines, today marker, extent guides, arrows

Drawn in this back-to-front order (later items paint over earlier ones):

1. **Week/month grid lines**: a thin vertical line at every real Monday
   (1px, `rgba(0,0,0,0.07)`) and a thicker vertical line at every real
   1st-of-month (2px, same colour) — both computed by walking real
   calendar days but positioning each by its own *compressed* (weekend-
   excluded, if applicable) offset, so the lines still land at the true
   calendar Monday/1st even though their pixel spacing reflects the
   compressed mapping (§4). Redundant, and not specially suppressed, when
   "Boxed" mode is on (a box's own border already marks similar edges).
2. **Project extent guide lines** (non-Boxed mode only): for every Project
   bar that has at least one visible descendant, two faint vertical lines
   — one at the bar's own left edge, one at its own right edge — running
   from just below the Project's own bar down to the bottom of its last
   descendant row. Colour `rgba(0,0,0,0.3)`, 1px wide (same colour family
   as the grid lines, just far more opaque, since these mark one specific
   bar's own edges rather than the whole chart). Not drawn in Boxed mode
   (the box itself already shows this extent).
3. **Today marker**: one full-height vertical dashed line (`#d32f2f` /
   red, 1px, dash pattern `4 3`) at today's own x position.
4. **Dependency arrows**: a thin line (`rgba(0,0,0,0.4)`, 1px, arrowhead
   marker) from the right edge of the predecessor's bar to the left edge
   of the successor's bar, both vertically centred on their own bar's
   midline. An arrow is only drawn if both ends resolve to a real,
   nonzero-width bar currently in the layout (a dependency whose Task/
   Project isn't visible is simply skipped, not drawn to a missing
   position).
5. **Bars themselves** (§11/§12), drawn in the same preorder as the row
   list — so a nested Project's or Task's own bar always paints on top of
   whatever Project box it's visually nested inside (relevant for Boxed
   mode's hit-testing and alpha-compositing, §12).

## 11. Bar rendering — normal (non-Boxed) mode

- Every bar is a plain rectangle, `BAR_HEIGHT` tall (zoom-scaled),
  vertically centred within its own row band (`(ROW_HEIGHT - BAR_HEIGHT)/2`
  gap above and below, zoom-scaled) — this centres the bar on the same
  vertical centreline the row's own label text is centred on.
- **Fill colour**:
  - A Project bar: a fixed flat colour, `#607d8b` (a muted blue-grey).
  - A Task bar: colour is driven by Urgency (`computeTaskRowColour` in
    `lib/schedule.ts` — the same function/colour scale used for Task rows
    elsewhere in the app, e.g. All Tasks). If the Task's own Priority is
    unset, `Cancelled`, or `Closed`, it is always flat grey
    (`rgb(190,190,190)`) regardless of computed Urgency; otherwise it is
    the white-to-light-red Urgency blend (white at Urgency ≤100, blending
    toward `rgb(255,128,128)` as Urgency approaches/exceeds 200).
- **Border**: `rgba(0,0,0,0.3)`, 1.5px for a Project bar, 1px for a Task
  bar.
- **Cursor**: pointer (hand) over a Task bar (it's double-clickable);
  default arrow over a Project bar.
- **Double-click**: opens the Task's own detail window (Task bars only —
  a Project bar's own double-click-to-open behaviour lives on its label
  in the left column, §6, not on the bar itself).
- **Background canvas**: the chart's own SVG background is a very pale
  green (`#f5fff7`) when not Boxed — carried over from V1.2's own Gantt
  background tint. It switches to plain white when Boxed mode is on (see
  §12 for why: stacking the green tint underneath Boxed mode's own
  translucent blue boxes produced a muddy look — two different hues
  layered as translucent washes).

## 12. "Boxed" mode

A checkbox (default **unchecked**) next to "Show Names". Changes how a
*Project's own bar* renders; Task bars are unaffected.

- With Boxed on, a Project's own bar is no longer a thin strip at just its
  own row: it grows downward to span its entire visible subtree, from its
  own row's top down to the bottom of its last descendant row (falling
  back to just its own row's height if it has no visible children at
  all, so it still reads as a normal bar rather than collapsing to
  nothing).
- Fill: `rgba(52, 108, 158, 0.15)` — a muted steel/denim blue at 15%
  opacity (chosen over an earlier, more desaturated blue-grey that read as
  plain washed-out grey rather than a colour, and was reported as
  "depressing").
- Border: `rgba(0,0,0,0.3)` (same colour as the extent guide lines it
  effectively replaces), 1px.
- Because nested Project boxes are painted in outer-to-inner (preorder)
  order, each one painting the same translucent fill over whatever is
  already there, a deeper level of nesting reads as progressively darker
  purely through alpha compositing — no separate per-depth colour is
  computed.
- Hit-testing (hover/tooltip) "just works" for the same reason: the same
  rectangle used for painting is the one used for mouse events, and
  because inner boxes/bars paint (and therefore sit) on top of outer
  ones in the DOM, the browser's own topmost-element-under-cursor
  resolution naturally picks the innermost box or bar without any extra
  "find deepest match" logic.
- Boxed mode is not stackable with the plain-mode extent guide lines
  (§10.2) or the pale-green background tint (§11) — both are suppressed
  while Boxed is on, since the box itself already communicates the same
  "how deep is this" information.

## 13. Right-click-drag panning

Holding the right mouse button down anywhere over the chart's own drawing
area (not the label column) and moving the mouse pans the chart on both
axes at once — exactly as if the scrollbars themselves had been dragged.

- No zoom change, so nothing needs anchor-preserving math here: it's a
  direct 1:1 scroll-position delta from the cursor's own movement.
- The browser's native context menu is suppressed over this area — a
  right-click here always pans, never opens a menu.
- Cursor changes to a "grabbing" hand while the button is held.
- Suppresses tooltips for the duration of the drag, same as every other
  drag interaction (§9).

## 14. Month-start footer

A fixed-height strip below the chart, above its horizontal scrollbar,
scrolling horizontally in lockstep with the chart (sharing the same
horizontal scroll container) but never scrolling vertically.

- Height: 26px, independent of vertical zoom (it's positioned along the
  horizontal/date axis, which vertical zoom doesn't affect).
- Shows one small, horizontally-centred text label at each month-start
  gridline's own x position: the month's 3-letter abbreviation (`Jan`,
  `Feb`, …), except for January, which shows `Jan-YY` (the 2-digit year)
  so a marker crossing a year boundary is still identifiable without
  every marker needing to carry a year.
- Background white, thin top border separating it from the chart above.

## 15. Two-pane scrolling structure

(Included because it constrains several of the rules above, not because
an implementer necessarily needs to reproduce this exact DOM shape — but
the *effect* — independent, correctly-clamped scrolling — is a real
requirement.)

- The label column and the chart area are two separate, independently
  vertically-scrolling panes, kept in sync (whichever one the user
  scrolls, the other's vertical scroll position is mirrored to match) —
  not a single shared scroll region with a `position: sticky` label
  column, which was tried and rejected (a sticky column necessarily
  paints over whatever bar has scrolled underneath it).
- The chart's own horizontal scroll is a separate scroll container one
  level "outside" its vertical scroll, wrapping both the chart itself and
  the month-footer strip together — so the footer tracks horizontal
  scrolling for free as an ordinary sibling, while remaining unaffected
  by the chart's independent vertical scroll. (An earlier single-container
  design with the footer pinned via `position: sticky` hit a real browser
  bug where the computed maximum `scrollTop` didn't fully account for the
  sticky child's own height, permanently hiding the last row or two behind
  the footer with no way to scroll further.)
- The label column reserves a bottom strip (matching the chart's own
  month-footer height plus its native horizontal scrollbar's actual
  measured height, so the two panes' visible row ranges line up exactly)
  — the "Memorise order" button (§5.2) lives in that reserved strip,
  centred horizontally.
- The whole Gantt area fills whatever vertical space is actually left
  below the title/controls rows, tracking window resizes — not a fixed
  viewport-height fraction, which does not account for the header's own
  roughly-constant pixel height and can overflow the page at small window
  heights.

## 16. Constants reference

| Constant | Value | Meaning |
|---|---|---|
| `PIXELS_PER_DAY` | 10 | px per day (or compressed weekday unit) at 100% H zoom |
| `ROW_HEIGHT` | 20 | px per row at 100% V zoom |
| `BAR_HEIGHT` | 10 | px, bar thickness at 100% V zoom (non-Boxed) |
| Project bar colour | `#607d8b` | flat, non-Boxed Project bars |
| Boxed Project fill | `rgba(52,108,158,0.15)` | 15%-opacity steel blue |
| Task Cancelled/Closed/no-priority colour | `rgb(190,190,190)` | flat grey |
| Task urgency colour range | white → `rgb(255,128,128)` | Urgency 100 → ≥200 |
| Non-Boxed canvas background | `#f5fff7` | pale green |
| Boxed canvas background | `#fff` | plain white |
| Extent guide line colour | `rgba(0,0,0,0.3)` | 1px |
| Grid line colour | `rgba(0,0,0,0.07)` | week 1px, month 2px |
| Today marker | `#d32f2f`, 1px, dash `4 3` | |
| Dependency arrow | `rgba(0,0,0,0.4)`, 1px | |
| Bar border | `rgba(0,0,0,0.3)` | 1.5px Project / 1px Task (non-Boxed); Boxed uses extent colour, 1px |
| Tooltip offset | +20px x, +10px y from cursor | |
| Tooltip style | white bg, `rgba(0,0,0,0.4)` 1px border, 2px radius | |
| Label hover-highlight | `rgba(0,0,0,0.6)`, 1px dashed `2,2` | |
| Default label column width | 220px | range [0, 600] |
| Label column resize handle | 6px wide | |
| Label column collapse threshold | ≤24px on release → "Show Names" off | |
| Month footer height | 26px | fixed, not V-zoom-scaled |
| Zoom range | [10%, 1000%] | both axes, default 100% |
| "Today" scroll fraction | 25% from left | button and initial auto-scroll |
| Base font size | 11px | at 100% V zoom |
