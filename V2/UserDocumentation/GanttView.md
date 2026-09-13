# Plan View (Gantt View)

## Contents

1. [Overview](#overview)
2. [Who Can Use This Screen](#who-can-use-this-screen)
3. [What You See](#what-you-see)
   - 3.1 [Layout](#layout)
   - 3.2 [The Project/Task List](#the-project-task-list)
   - 3.3 [The Timeline](#the-timeline)
   - 3.4 [Bar Colours](#bar-colours)
   - 3.5 [Dependency Arrows and the Today Line](#dependency-arrows-and-the-today-line)
4. [How to Use It](#how-to-use-it)
   - 4.1 [Scrolling and Panning](#scrolling-and-panning)
   - 4.2 [Zooming](#zooming)
   - 4.3 [Jumping to Today](#jumping-to-today)
   - 4.4 [Showing, Hiding, and Resizing the Name List](#showing-hiding-and-resizing-the-name-list)
   - 4.5 [Weekends](#weekends)
   - 4.6 [Boxed Mode](#boxed-mode)
   - 4.7 [Hovering for Details](#hovering-for-details)
   - 4.8 [Reordering Rows](#reordering-rows)
   - 4.9 [Opening a Task or Project](#opening-a-task-or-project)
5. [Related Screens](#related-screens)

<a id="overview"></a>
## 1. Overview

Plan View — also called the Gantt View, after the classic style of chart it uses — is a visual timeline of your [Projects](Glossary.md#project) and their [Tasks](Glossary.md#task). Rather than a list of dates, it shows every Task as a bar positioned and sized against a real calendar, grouped under the Project it belongs to, with arrows showing which Tasks and Projects are waiting on which others. It's the screen to use when you want to see the shape of a schedule at a glance, rather than read it row by row.

You can open it scoped to everything ("Top Level Projects"), or scoped to just one Project and its own sub-Projects and Tasks — opening it from a specific Project narrows it to that Project's own subtree.

<a id="who-can-use-this-screen"></a>
## 2. Who Can Use This Screen

Every signed-in Person can open Plan View, scoped to the [Teams](Glossary.md#team) they belong to — the same Team-based visibility [All Tasks](AllTaskView.md) uses. Plan View itself doesn't let you edit anything directly: it's a way of looking at and navigating your schedule, not changing it. There's no difference in what different [Roles](Glossary.md#role) can do on this screen itself — double-clicking a Task here to edit it is subject to [Task Detail](TaskDetailView.md)'s own rules, not this screen's.

The one thing that is personal to you here is row order (§4.8): rearranging rows, and choosing to remember that order, only ever affects your own view in your own browser — it's never shared with, or visible to, anyone else.

<a id="what-you-see"></a>
## 3. What You See

<a id="layout"></a>
### 3.1 Layout

Plan View opens in its own window. At the top is a heading naming what you're looking at ("Top Level Projects," or the name of the Project you scoped to), followed by a row of controls (zoom fields, buttons, and checkboxes — covered in §4). Below that is a strip showing the date under your mouse cursor on the left, and the name of whatever Project or Task your cursor is currently over on the right. The rest of the window is the chart itself: a list of Project/Task names down the left, and the scrollable timeline to its right.

<a id="the-project-task-list"></a>
### 3.2 The Project/Task List

Down the left side, one row per Project or Task, in a nested list: each Project's row is shown in bold, immediately followed by its own Tasks and any sub-Projects (each sub-Project followed, in turn, by its own children), indented one step further at each level. By default, a Project's Tasks are ordered by their own start date and its sub-Projects alphabetically; see §4.8 for changing this yourself.

<a id="the-timeline"></a>
### 3.3 The Timeline

To the right, each row gets a horizontal bar positioned against a shared calendar: how far left or right a bar sits, and how wide it is, shows when that Task or Project starts and ends. Faint vertical lines mark the start of each week and each month; month names appear along the bottom edge of the chart.

<a id="bar-colours"></a>
### 3.4 Bar Colours

A Project's bar is always a flat blue-grey. A Task's bar is coloured by its [Urgency](Glossary.md#urgency) — white when it isn't urgent, shading towards light red the more urgent it is — the same colour-coding used in [All Tasks](AllTaskView.md) and [Task Detail](TaskDetailView.md). A Task with no Priority set, or a Cancelled/Closed Priority, is always shown in flat grey instead.

A Closed or Cancelled Task, or a Closed/Cancelled-priority Project, doesn't appear on the chart at all.

<a id="dependency-arrows-and-the-today-line"></a>
### 3.5 Dependency Arrows and the Today Line

An arrow runs from the end of one bar to the start of another wherever a [Dependency](Glossary.md#dependency) links them. A dashed red vertical line marks today's date, running the full height of the chart.

<a id="how-to-use-it"></a>
## 4. How to Use It

<a id="scrolling-and-panning"></a>
### 4.1 Scrolling and Panning

Use the chart's scrollbars as normal, or hold down the **right** mouse button anywhere over the timeline and drag — the chart pans in whichever direction you drag, exactly as if you'd used the scrollbars, without changing the zoom level.

<a id="zooming"></a>
### 4.2 Zooming

Zoom is controlled separately for the two directions: **H** stretches or compresses the timeline (dates), **V** stretches or compresses the rows (and their text). You can:

- Type a percentage directly into the H or V box (from 10% up to 1000%).
- Click **Zoom Reset** to put both back to 100%.
- Hold **Ctrl** and scroll your mouse wheel over the chart to zoom horizontally, hold **Shift** and scroll to zoom vertically, or hold both to zoom both at once — whatever's under your mouse cursor at the time stays under it as the zoom changes, so you can zoom straight into whatever you're pointing at.

<a id="jumping-to-today"></a>
### 4.3 Jumping to Today

Click **Today** to scroll the chart so today's own line sits about a quarter of the way in from the left edge of the visible chart. Plan View also starts there automatically the first time you open a given scope.

<a id="showing-hiding-and-resizing-the-name-list"></a>
### 4.4 Showing, Hiding, and Resizing the Name List

Uncheck **Show Names** to hide the Project/Task name list entirely and give the timeline more room; check it again to bring it back at its normal width. You can also drag the boundary between the name list and the timeline to resize the name list yourself — dragging it closed all the way has the same effect as unchecking **Show Names**.

<a id="weekends"></a>
### 4.5 Weekends

By default (**Weekends** unchecked), Saturdays and Sundays take up no space on the timeline at all — the chart shows only five days between one Monday and the next, so a full working week fits in less width. A Task or Project that would otherwise start on a weekend is shown starting at the beginning of the following Monday instead; one that would otherwise finish on a weekend is shown finishing at the end of the preceding Friday.

Check **Weekends** to show all seven days of every week, spaced evenly, with nothing shifted.

Toggling this checkbox keeps whatever date was sitting a quarter of the way in from the left in view at that same position, so you don't lose your place.

<a id="boxed-mode"></a>
### 4.6 Boxed Mode

Check **Boxed** to change how Projects are drawn: instead of a thin bar at just its own row, a Project's bar grows downward to enclose all of its own Tasks and sub-Projects, like a box drawn around them. Nested Projects appear as progressively darker boxes the deeper they're nested, so you can see at a glance how many levels of Project a given Task sits under.

<a id="hovering-for-details"></a>
### 4.7 Hovering for Details

- Move your mouse anywhere over the timeline and the date-under-cursor readout above the chart updates continuously.
- Hover any bar, or any name in the list on the left, and the name readout above the chart shows that Project's or Task's full name, along with its place in the Project hierarchy.
- Hovering a bar also shows a small popup with its name and date range.
- If a name in the left-hand list is too long to fit, hovering it shows a small popup with its full text.
- Moving your mouse over the name list highlights whichever row is currently under the cursor with a dotted outline, showing you which row you'd pick up if you started dragging (§4.8) from there.

<a id="reordering-rows"></a>
### 4.8 Reordering Rows

You can drag a Project or Task's name in the left-hand list up or down to change its order among its own siblings (the other items directly under the same parent Project) — this never changes which Project something belongs to, only where it sits within its own group. While dragging, a floating label follows your cursor to show what you're moving, and you can only drop it somewhere within its own group's own range.

This reordering is personal and temporary: it only changes what you see, in your own browser, and it's forgotten again once you navigate away — unless you click **Memorise order**, which saves your current arrangement so it's still there the next time you open this same view.

<a id="opening-a-task-or-project"></a>
### 4.9 Opening a Task or Project

Double-click a Task's bar or its name to open its [Task Detail](TaskDetailView.md) window. Double-click a Project's name to open Plan View scoped to just that Project.

<a id="related-screens"></a>
## 5. Related Screens

- [Task Detail](TaskDetailView.md) — opened by double-clicking a Task here.
- [All Tasks](AllTaskView.md) — a grid view of the same Tasks, reachable via the **Tasks** button in the navigation bar of any other screen.
- [Glossary](Glossary.md) — definitions for the terms used throughout this document.
