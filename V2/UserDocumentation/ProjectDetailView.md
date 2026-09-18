# Project Detail

## Contents

1. [Overview](#overview)
2. [Who Can Use This Screen](#who-can-use-this-screen)
3. [What You See](#what-you-see)
   - 3.1 [Header](#header)
   - 3.2 [Main Fields](#main-fields)
   - 3.3 [Sub-Projects and Tasks Tree](#sub-projects-and-tasks-tree)
   - 3.4 [Dependencies / Attachments / Remarks Tabs](#tabs)
4. [How to Use It](#how-to-use-it)
   - 4.1 [Editing and Saving](#editing-and-saving)
   - 4.2 [Reparenting a Project](#reparenting-a-project)
   - 4.3 [Browsing the Tree](#browsing-the-tree)
   - 4.4 [The Task-Visibility Filter](#the-task-visibility-filter)
   - 4.5 [Adding a Sub-Project](#adding-a-sub-project)
   - 4.6 [Renaming or Deleting a Project](#renaming-or-deleting-a-project)
   - 4.7 [Adding a Task](#adding-a-task)
   - 4.8 [Managing Dependencies, Attachments, and Remarks](#managing-dependencies-attachments-and-remarks)
   - 4.9 [Its Own Window](#its-own-window)
5. [Related Screens](#related-screens)

<a id="overview"></a>
## 1. Overview

Project Detail shows everything about one [Project](Glossary.md#project): its own fields, and a browsable tree of its sub-Projects and Tasks underneath. Opened with no particular Project at all, it instead shows every top-level Project you can see — a "Top Level Projects" browser, reached from the **Projects** button in the navigation bar. Clicking any Project's name, anywhere in ProjectPal, takes you into its own Project Detail.

<a id="who-can-use-this-screen"></a>
## 2. Who Can Use This Screen

Anyone who can see a Project can open its Project Detail window. Whether you can change anything depends on your [Role](Glossary.md#role):

- **Editing the main fields and Save** — available if you're the Project's Owner (with at least Normal User Role on its Team), or a [Team Lead User](Glossary.md#role) on its Team. Otherwise every field is read-only, with no Save button.
- **Adding a new Project or a new Task** — available only to a Lead User or Team Lead User on the relevant Team. The **Add New Project** and **Add Task** buttons themselves don't appear at all for anyone below that — being the Project's owner isn't enough on its own.
- **Deleting a Project** — available only to a Team Lead User on its Team. Not even the Project's own owner can delete it.
- **Dependencies, Attachments, and Remarks** — adding or removing an entry is available to anyone who can open the window at all, the same as on [Task Detail](TaskDetailView.md).

<a id="what-you-see"></a>
## 3. What You See

<a id="header"></a>
### 3.1 Header

Across the top: a small badge, the Project's own number, and its Name shown as an editable title (when a specific Project is open). If this Project has a parent, its name appears as a clickable link that takes you there. To the right: the Project's **Owner**, an **All Projects** button that takes you to the top-level browser, and — only if you're allowed to — **Save** and **Delete** buttons.

<a id="main-fields"></a>
### 3.2 Main Fields

Shown only when a specific Project is open (not in the top-level browser): **Priority**, **Start Date**, **Due Date**, and a read-only, calculated **End Date** — the same [Urgency](Glossary.md#urgency)-independent scheduling ProjectPal already uses elsewhere, since a Project's own end is always the latest end date across everything underneath it, never typed in directly. Below that, a **Parent Project** picker (see §4.2) and a **Detailed Description** box.

<a id="sub-projects-and-tasks-tree"></a>
### 3.3 Sub-Projects and Tasks Tree

The screen's main feature: an expandable tree listing this Project's own Tasks and sub-Projects (or, in the top-level browser, every top-level Project you can see). Each Task shows its description and its current [Urgency](Glossary.md#urgency), coloured the same way as [All Tasks](AllTaskView.md). Each sub-Project shows its name; one with no open Task and no active sub-Project of its own is shown in a lighter grey, though it's still there to click into. Sibling Projects — including the top-level ones in the browser — are ordered by Priority, highest first, then alphabetically within the same Priority. A **Tasks** filter above the tree (**None** / **Open** / **All**) controls which Tasks are shown, at every level and in every mode (including the top-level browser) — it defaults to **None** and never affects what's actually stored, only what you're currently looking at.

<a id="tabs"></a>
### 3.4 Dependencies / Attachments / Remarks Tabs

Below the tree, the same tab strip [Task Detail](TaskDetailView.md) uses for its own [Dependencies](Glossary.md#dependency), [Attachments](Glossary.md#attachment), and [Remarks](Glossary.md#remark) — each tab's label shows how many entries it has. A Project's own Dependencies can link to either a Task or another Project.

<a id="how-to-use-it"></a>
## 4. How to Use It

<a id="editing-and-saving"></a>
### 4.1 Editing and Saving

If you can edit this Project, change any field and the **Save** button becomes active; click it to write your changes back.

<a id="reparenting-a-project"></a>
### 4.2 Reparenting a Project

Use the **Parent Project** picker to move this Project under a different parent (or clear it to make it top-level) — browse the tree it opens, or choose "(none)." Only Projects on the same Team are offered. This takes effect once you Save, the same as any other field.

<a id="browsing-the-tree"></a>
### 4.3 Browsing the Tree

Click the arrow beside a sub-Project to expand or collapse it. Click a sub-Project's own name to open its Project Detail. Click a Task to open its [Task Detail](TaskDetailView.md) window.

<a id="the-task-visibility-filter"></a>
### 4.4 The Task-Visibility Filter

Choose **None** (the default) to hide every Task from the tree (showing only the Project structure itself), **Open** to hide Closed/Cancelled Tasks, or **All** to show every Task regardless of status.

<a id="adding-a-sub-project"></a>
### 4.5 Adding a Sub-Project

If you're allowed to (§2), click **Add New Project** above the tree. A small dialog asks for a Name (the only thing required, matching how little a brand-new Project needs) and shows which Project it will be created under; click **Create**.

<a id="renaming-or-deleting-a-project"></a>
### 4.6 Renaming or Deleting a Project

Each sub-Project row shows a pencil and a trash icon if you're allowed to manage it (§2). The pencil opens the same small dialog as creating one, pre-filled with its current name, to rename it. The trash icon deletes it after asking you to confirm — this cannot be undone, and fails with an explanation if anything (a sub-Project, Task, Dependency, Attachment, or Remark) still refers to it. The header's own **Delete** button does the same for the Project you currently have open, taking you back to the top-level browser afterward.

<a id="adding-a-task"></a>
### 4.7 Adding a Task

If you're allowed to (§2), click **Add Task** above the tree. A dialog asks for a Description, a Component, and a Task Type (all required), plus a Priority and a Requestor that already start filled in with a sensible default but can be changed. Click **Add** to create the Task and open its own [Task Detail](TaskDetailView.md) window, where everything else about it (dates, effort, Resources, and so on) can be filled in.

<a id="managing-dependencies-attachments-and-remarks"></a>
### 4.8 Managing Dependencies, Attachments, and Remarks

Works exactly as described in [Task Detail](TaskDetailView.md#managing-dependencies)'s own sections on these three tabs — the only difference here is that a Dependency's search can find and link either a Task or another Project.

<a id="its-own-window"></a>
### 4.9 Its Own Window

Project Detail always opens in its own window, separate from wherever you opened it from, and reopening the same Project brings its existing window to the front instead of opening a duplicate — the same behaviour [Task Detail](TaskDetailView.md#its-own-window) has.

<a id="related-screens"></a>
## 5. Related Screens

- [Task Detail](TaskDetailView.md) — opened by clicking a Task in the tree here.
- [All Tasks](AllTaskView.md) — a grid view of every Task, including this Project's own.
- [Plan View (Gantt View)](GanttView.md) — a timeline view of Projects and Tasks.
- [Glossary](Glossary.md) — definitions for the terms used throughout this document.
