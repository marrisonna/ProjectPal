# Task Detail

## Contents

1. [Overview](#overview)
2. [Who Can Use This Screen](#who-can-use-this-screen)
3. [What You See](#what-you-see)
   - 3.1 [Header](#header)
   - 3.2 [Main Fields](#main-fields)
   - 3.3 [Resources](#resources)
   - 3.4 [Detailed Description](#detailed-description)
   - 3.5 [Project and Component](#project-and-component)
   - 3.6 [Dependencies / Attachments / Remarks Tabs](#tabs)
4. [How to Use It](#how-to-use-it)
   - 4.1 [Editing and Saving](#editing-and-saving)
   - 4.2 [The Three Dates](#the-three-dates)
   - 4.3 [Effort or Duration](#effort-or-duration)
   - 4.4 [Assigning Resources](#assigning-resources)
   - 4.5 [Choosing a Project or Component](#choosing-a-project-or-component)
   - 4.6 [Managing Dependencies](#managing-dependencies)
   - 4.7 [Managing Attachments](#managing-attachments)
   - 4.8 [Adding Remarks](#adding-remarks)
   - 4.9 [Its Own Window](#its-own-window)
5. [Related Screens](#related-screens)

<a id="overview"></a>
## 1. Overview

Task Detail shows everything about a single [Task](Glossary.md#task): its own fields, who's assigned to it, and its Dependencies, Attachments, and Remarks. It's a compact, standalone window — opened by double-clicking a Task in [All Tasks](AllTaskView.md) or [Plan View](GanttView.md) — meant to sit alongside a handful of other Task Detail windows rather than take over your screen.

<a id="who-can-use-this-screen"></a>
## 2. Who Can Use This Screen

Anyone who can see a Task in [All Tasks](AllTaskView.md) or [Plan View](GanttView.md) can open its Task Detail window and view everything on it. Whether you can *change* anything depends on your [Role](Glossary.md#role):

- **You can edit the main fields, the Resources list, and Save your changes** if you're the Task's Owner (and hold at least Normal User Role on the Task's Team), **or** you're a [Team Lead User](Glossary.md#role) on the Task's Team.
- **Otherwise, every field is shown read-only** and there's no Save button — you can still see everything, just not change it.

The Dependencies, Attachments, and Remarks tabs (§3.6) work a little differently: adding or removing an entry there is currently available to anyone who can open the window at all, regardless of whether you can edit the Task's own main fields.

<a id="what-you-see"></a>
## 3. What You See

<a id="header"></a>
### 3.1 Header

Across the top: a small badge (double as a drag handle — see §4.6), the Task's number and Project name, and its Description shown as an editable title. To the right of that: the Task's **Owner**, its **Urgency** score in a box shaded by the same colour-coding used elsewhere in ProjectPal (see [Urgency](Glossary.md#urgency)), an **All Tasks** button that takes you back to the grid, and — only if you can edit this Task — a **Save** button.

<a id="main-fields"></a>
### 3.2 Main Fields

Below the header, two rows of fields:

- **Row 1:** Priority, Status, Task Type, Requestor.
- **Row 2:** Effort, an Effort-type choice (§4.3), % Allocation, Requested Start Date, Planned Start Date, End Date (§4.2).

<a id="resources"></a>
### 3.3 Resources

To the right of the field rows, a scrollable checklist of everyone who could be assigned to this Task, with those currently assigned ticked and listed first.

<a id="detailed-description"></a>
### 3.4 Detailed Description

A larger free-text box below the main fields, for anything that doesn't fit in the short Description in the header.

<a id="project-and-component"></a>
### 3.5 Project and Component

Two side-by-side pickers showing which [Project](Glossary.md#project) the Task belongs to and which [Component](Glossary.md#component) (if any) it's tagged with, each showing the full path down to the chosen item.

<a id="tabs"></a>
### 3.6 Dependencies / Attachments / Remarks Tabs

At the bottom, one tab strip covering the Task's [Dependencies](Glossary.md#dependency), [Attachments](Glossary.md#attachment), and [Remarks](Glossary.md#remark) — each tab's label shows how many entries it has, and only one tab's contents are shown at a time.

<a id="how-to-use-it"></a>
## 4. How to Use It

<a id="editing-and-saving"></a>
### 4.1 Editing and Saving

If you can edit this Task (§2), change any field and the **Save** button becomes active; click it to write your changes back. If something goes wrong when saving, an error message appears above the fields explaining that the save failed.

<a id="the-three-dates"></a>
### 4.2 The Three Dates

Task Detail shows three related dates, but only two of them can actually be typed in:

- **Requested Start Date** — editable. Pick the date you'd like the Task to start.
- **Planned Start Date** — read-only. This is what ProjectPal actually calculates once every [Dependency](Glossary.md#dependency) and the Project's own schedule are accounted for; it can land later than your Requested Start Date.
- **End Date** — editable. Changing this works backwards from your chosen date, using the Task's own [Effort](Glossary.md#effort)/[Duration](Glossary.md#duration), to update the Requested Start Date so the two stay consistent.

Changing Effort, % Allocation, or the Resources list (§4.4) updates the Planned Start/End Date immediately, even before you Save.

<a id="effort-or-duration"></a>
### 4.3 Effort or Duration

Next to the Effort field, two radio buttons choose how that number is interpreted: **Days** (person-days — [Effort](Glossary.md#effort), divided across however many Resources are assigned) or **Duration** (a fixed length of elapsed time regardless of how many Resources are assigned).

<a id="assigning-resources"></a>
### 4.4 Assigning Resources

Tick or untick names in the Resources checklist to change who's assigned. Only people who are set up as a [Resource](Glossary.md#resource) on this Task's Team appear in the list at all. Changes here aren't sent to ProjectPal until you click Save, alongside any other changes you've made.

<a id="choosing-a-project-or-component"></a>
### 4.5 Choosing a Project or Component

Click the Project or Component picker to browse its tree and choose a different one; the Component picker also lets you choose "none." Moving a Task to a different Project only ever offers Projects on the same Team.

<a id="managing-dependencies"></a>
### 4.6 Managing Dependencies

The Dependencies tab lists two groups: **Depends upon** (this Task's predecessors — work that must finish before this Task can start) and **Dependants** (its successors — work that's waiting on this one). To add one, click **Add Dependency**, choose whether the other Task is something this Task **Depends upon** or **Is depended upon by**, search for and select it, then confirm. Click the trash icon next to any entry to remove it.

If you already have that other Task open in its own Task Detail window, you can also create a Dependency by dragging: hold **Ctrl** and drag the small badge in that other window's header across onto this window, dropping it onto either the "Depends upon" or "Dependants" list.

<a id="managing-attachments"></a>
### 4.7 Managing Attachments

The Attachments tab lists every file or link attached to this Task. **Add Link** records a name and a web address; **Add File** opens a file picker to upload one. Click a link's name to open it in a new tab; click a file's name to download it.

<a id="adding-remarks"></a>
### 4.8 Adding Remarks

The Remarks tab shows every comment left on this Task, each with its author's name and when it was posted, oldest first. Type into the box at the bottom and click **Add** to post a new one.

<a id="its-own-window"></a>
### 4.9 Its Own Window

Task Detail always opens in its own small window, separate from wherever you opened it from, so you can have several Tasks' details open side by side. Opening the same Task a second time brings its existing window to the front instead of opening a duplicate.

<a id="related-screens"></a>
## 5. Related Screens

- [All Tasks](AllTaskView.md) — the grid you'll most often open a Task Detail window from.
- [Plan View (Gantt View)](GanttView.md) — double-clicking a Task's bar there also opens this screen.
- [Glossary](Glossary.md) — definitions for the terms used throughout this document.
