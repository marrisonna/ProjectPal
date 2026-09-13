# All Tasks

## Contents

1. [Overview](#overview)
2. [Who Can Use This Screen](#who-can-use-this-screen)
3. [What You See](#what-you-see)
   - 3.1 [The Navigation Bar](#navigation-bar)
   - 3.2 [The Columns](#the-columns)
   - 3.3 [Row Colour](#row-colour)
4. [How to Use It](#how-to-use-it)
   - 4.1 [Sorting](#sorting)
   - 4.2 [Filtering a Column](#filtering-a-column)
   - 4.3 [Default Filters](#default-filters)
   - 4.4 [Paging](#paging)
   - 4.5 [Opening a Task](#opening-a-task)
5. [Related Screens](#related-screens)

<a id="overview"></a>
## 1. Overview

All Tasks is ProjectPal's main grid of work: every [Task](Glossary.md#task) you have visibility to, one row per Task, with rich sorting and filtering on every column. It's normally the first thing you see after logging in, and it's the natural home base for scanning, triaging, and prioritising your own or your Team's work — each row is colour-coded by [Urgency](Glossary.md#urgency), so the Tasks that most need attention stand out at a glance.

<a id="who-can-use-this-screen"></a>
## 2. Who Can Use This Screen

Every signed-in Person can open All Tasks, and everyone sees the same set of columns except one (the "T" column, below). What you actually see rows for is scoped and defaulted as follows:

- **Team scope.** You only ever see Tasks belonging to a [Team](Glossary.md#team) you're a member of, regardless of [Role](Glossary.md#role) — this can't be widened by changing a filter.
- **The "T" (Tentative Resource Assignment) column** is only shown if you're a [Team Lead User](Glossary.md#role) on at least one Team — an ordinary Team member has no need to see which of other people's assignments are still unconfirmed.
- **Your starting filter** differs by Role: if you aren't a Team Lead User on any Team, All Tasks opens already filtered to Tasks where you're a Resource — your own work. A Team Lead User instead starts seeing their whole Team's work, unfiltered by Resource. Either way, this is only a starting point — anyone can change or clear any filter themselves afterwards (§4.3).

Whether you can actually change anything about a Task you open from here is decided on [Task Detail](TaskDetailView.md), not on this screen — All Tasks itself has no separate editing of its own.

<a id="what-you-see"></a>
## 3. What You See

<a id="navigation-bar"></a>
### 3.1 The Navigation Bar

Across the top: the ProjectPal logo, a **Tasks** button (brings you back here), and a **Plan** button (opens the [Plan View](GanttView.md) in its own window). If you're an [Organisation Admin](Glossary.md#organisation-admin), an "Admin" badge appears; your own Person number and a **Log out** button sit at the far right.

<a id="the-columns"></a>
### 3.2 The Columns

Below the navigation bar is one large, scrollable grid. Each column has a two-line header: the column's title on top, and a filter control underneath it (§4.2). The columns, left to right:

| Column | Shows |
|---|---|
| ID | The Task's own number. |
| Urgency | The Task's current [Urgency](Glossary.md#urgency) score. |
| Resources | Everyone currently assigned to the Task. |
| Status | The Task's [Status](Glossary.md#status). |
| T | Whether the Task's Resource assignment is [tentative](Glossary.md#tentative-resource-assignment) — Team Lead Users only (§2). |
| Description | The Task's own short description — its name, in effect. |
| Component | The [Component](Glossary.md#component) the Task is tagged with, if any. |
| Project | The [Project](Glossary.md#project) the Task belongs to. |
| Priority | The Task's [Priority](Glossary.md#priority). |
| End Date | The Task's computed [End Date](Glossary.md#end-date). |
| Planned Start | The Task's computed [Planned Start Date](Glossary.md#planned-start-date). |
| Attachments | How many [Attachments](Glossary.md#attachment) the Task has. |
| Remarks | How many [Remarks](Glossary.md#remark) the Task has. |
| Owner | The Task's Owner. |
| Requested By | The Task's Requestor. |
| Date Added | When the Task was created. |
| Effort | The Task's recorded [Effort](Glossary.md#effort) or [Duration](Glossary.md#duration) size. |
| Effort Type | Whether Effort is measured as person-days or a fixed duration. |
| % Allocation | What fraction of each assigned Resource's time this Task is expected to take. |
| Task Type | A category for the kind of work (Enhancement, Maintenance, New Development, Support, Infrastructure, Other). |
| Status Date | When the Task's Status was last changed. |
| Ref URL | An optional external reference link. |
| Detailed Description | A longer free-text description. |

<a id="row-colour"></a>
### 3.3 Row Colour

Every row is shaded by the same [Urgency](Glossary.md#urgency) colour-coding used on [Plan View](GanttView.md) and [Task Detail](TaskDetailView.md): plain white for a Task that isn't urgent, shading towards light red as Urgency increases. A Task with no Priority set, or a Cancelled/Closed Priority, is always shown in flat grey instead, no matter its Urgency score.

<a id="how-to-use-it"></a>
## 4. How to Use It

<a id="sorting"></a>
### 4.1 Sorting

Click a column's title to sort by it; click again to reverse the sort direction. All Tasks opens sorted by Urgency, highest first, so the most pressing work naturally rises to the top.

<a id="filtering-a-column"></a>
### 4.2 Filtering a Column

Every column has its own filter, directly under its title, with two parts you can use together:

- **Type to filter** — type into the small text box to keep only rows where that column's value contains what you typed.
- **Pick exact values** — click the funnel button next to the text box to open a checklist of every value currently appearing in that column (given whatever's already filtered in the other columns). Tick or untick values, or use **All**/**None**, then **OK** to apply, or **Cancel** to back out without changing anything.

A column with an active filter (of either kind) is highlighted so you can see at a glance which columns are currently narrowing the list. Filters on different columns combine — a row has to satisfy every active column filter at once to be shown.

<a id="default-filters"></a>
### 4.3 Default Filters

All Tasks opens with two filters already applied, both of which you're free to change or clear:

- **Status** starts filtered to hide Closed and Cancelled Tasks, so finished/abandoned work doesn't clutter your view by default.
- **Resources** starts filtered to just you, unless you're a Team Lead User on some Team, in which case it starts unfiltered (§2).

<a id="paging"></a>
### 4.4 Paging

Rows are shown a page at a time (up to 100 rows per page), with page controls at the bottom of the grid — if your filtered list has more Tasks than fit on one page, use those controls to see the rest.

<a id="opening-a-task"></a>
### 4.5 Opening a Task

Double-click any row to open that Task's own [Task Detail](TaskDetailView.md) window. Double-clicking the same Task again brings its already-open window back to the front rather than opening a second copy of it.

<a id="related-screens"></a>
## 5. Related Screens

- [Task Detail](TaskDetailView.md) — opened by double-clicking a row here.
- [Plan View (Gantt View)](GanttView.md) — a timeline view of the same Projects and Tasks, opened via the **Plan** button in the navigation bar.
- [Glossary](Glossary.md) — definitions for the terms used throughout this document.
