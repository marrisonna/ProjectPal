# ProjectPal Glossary

## Contents

1. [People, Teams, and Roles](#people-teams-and-roles)
   - [Organisation](#organisation)
   - [Team](#team)
   - [Person](#person)
   - [Resource](#resource)
   - [Role](#role)
   - [Organisation Admin](#organisation-admin)
   - [Display Name / Nickname](#display-name-nickname)
2. [Work Items](#work-items)
   - [Project](#project)
   - [Task](#task)
   - [Component](#component)
   - [Dependency](#dependency)
   - [Attachment](#attachment)
   - [Remark](#remark)
3. [Scheduling, Priority, and Urgency](#scheduling-priority-and-urgency)
   - [Effort](#effort)
   - [Duration](#duration)
   - [Requested Start Date](#requested-start-date)
   - [Planned Start Date](#planned-start-date)
   - [End Date](#end-date)
   - [Priority](#priority)
   - [Status](#status)
   - [Urgency](#urgency)
   - [Tentative Resource Assignment](#tentative-resource-assignment)
4. [The ProjectPal Screens](#the-projectpal-screens)
   - [All Tasks](#all-tasks-glossary)
   - [Plan View (Gantt View)](#plan-view-glossary)
   - [Task Detail](#task-detail-glossary)
   - [Project Detail](#project-detail-glossary)

This glossary explains the terms you'll come across while using ProjectPal. It stays focused on what these things mean and why they matter to you as a user — not on how ProjectPal is built. Where a term is central to one particular screen, this glossary gives you the short version and points you to that screen's own document for the full picture.

<a id="people-teams-and-roles"></a>
## 1. People, Teams, and Roles

<a id="organisation"></a>
### Organisation

Your whole company, or the part of it, that uses this installation of ProjectPal. Everyone you work with in ProjectPal — every Team, Project, and Task you can see — belongs to the same Organisation as you.

<a id="team"></a>
### Team

A group of people within your Organisation who work together — typically a department or a product team. Most of what you see day to day (Projects, Tasks, Components) belongs to one Team. You can belong to more than one Team, and you can hold a different [Role](#role) on each one.

<a id="person"></a>
### Person

Anyone known to ProjectPal — whether or not they ever log in themselves. A Person can be someone who uses the system directly, someone whose time is tracked against work (a [Resource](#resource)), or both. This is why you'll sometimes see someone listed as a Task's Owner or Requestor even if they never open ProjectPal themselves.

<a id="resource"></a>
### Resource

A Person who can be assigned to do work — that is, whose time can be booked against a Task. Being a Resource is something a Person can do on a particular Team; it isn't a separate kind of Person. Only People marked as Resources on a Task's Team can be checked in that Task's Resources list (see [Task Detail](#task-detail-glossary)).

<a id="role"></a>
### Role

What you're allowed to do on a given Team — view only, or also create/edit/delete things there. Role is per-Team: you might be a Team Lead on one Team and have a more limited role on another. From least to most access:

- **Read Only User** — can view everything the Team can see, but can't change any of it.
- **Normal User** — can create and edit records they own (see, for example, [Task Detail](#task-detail-glossary)'s "Who Can Use This Screen").
- **Lead User** — a wider level of access than Normal User on that Team.
- **Team Lead User** — the highest per-Team level: in addition to Normal/Lead User rights, a Team Lead User can edit or delete a record on their Team even if they don't own it, and manage who belongs to their Team and what Role each member holds.

<a id="organisation-admin"></a>
### Organisation Admin

A Person with organisation-wide administrator rights, independent of any one Team's Roles — shown as an "Admin" badge in ProjectPal's navigation bar. Creating, editing, or deleting a Person record is only ever done by an Organisation Admin, regardless of anyone's Team Role.

<a id="display-name-nickname"></a>
### Display Name / Nickname

The name ProjectPal shows for a Person. Normally this is their recorded name, but a Team can optionally record a shorter nickname for one of its members — where one exists, screens showing that Team's own data show the nickname instead, to keep lists like the Resources checklist compact.

<a id="work-items"></a>
## 2. Work Items

<a id="project"></a>
### Project

A container for related work. Projects can be nested — a large piece of work can be broken down into sub-Projects — so a Project's place in that tree tells you how it fits into the bigger picture. "What am I working on" is usually answered by naming a Project before naming a [Task](#task). See [Plan View](#plan-view-glossary) for how a Project and its Tasks are shown together on a timeline.

<a id="task"></a>
### Task

A single, concrete piece of work — closer to a ticket than a bare to-do. Every Task belongs to exactly one Project, carries its own [Priority](#priority), [Status](#status), and effort, and is where most of your day-to-day attention in ProjectPal goes. See [All Tasks](#all-tasks-glossary) and [Task Detail](#task-detail-glossary).

<a id="component"></a>
### Component

A second way of classifying a Task, independent of Project. Where Project answers "what initiative is this for," Component answers "what part of the product or system does this affect" — useful when work on the same part of a system is spread across several Projects. A Task can optionally be tagged with one Component.

<a id="dependency"></a>
### Dependency

A statement that one Task or Project can't start until another one finishes. ProjectPal calls the one that must finish first the **predecessor** and the one waiting on it the **successor** — on a Task's own Dependencies tab these are labelled "Depends upon" (predecessors) and "Dependants" (successors). Dependencies are what turn a list of dates into a real, connected schedule — they're drawn as arrows in [Plan View](#plan-view-glossary).

<a id="attachment"></a>
### Attachment

A file or a hyperlink attached to a Task (or a Project or Component), kept alongside it rather than filed away somewhere separate. See [Task Detail](#task-detail-glossary)'s Attachments tab.

<a id="remark"></a>
### Remark

A comment attached to a Task (or a Project or Component) — a lightweight way to discuss or record context on a work item without leaving ProjectPal. See [Task Detail](#task-detail-glossary)'s Remarks tab.

<a id="scheduling-priority-and-urgency"></a>
## 3. Scheduling, Priority, and Urgency

<a id="effort"></a>
### Effort

One of two ways a Task's size can be recorded (the other is [Duration](#duration)) — a fixed amount of work, measured in person-days, that gets divided across however many Resources are assigned to the Task. Assign a second person to an Effort-sized Task and it finishes sooner; assign a second person to a Duration-sized one and it doesn't.

<a id="duration"></a>
### Duration

The other way a Task's size can be recorded — a fixed amount of elapsed time regardless of how many Resources are assigned (for example, "waiting two weeks for a vendor to respond" doesn't go any faster with more people on it).

<a id="requested-start-date"></a>
### Requested Start Date

The date you'd like a Task to start. You can set this directly on [Task Detail](#task-detail-glossary); ProjectPal stores it as an offset from the Task's own Project's start date, but you never need to think in those terms — just pick the date you want.

<a id="planned-start-date"></a>
### Planned Start Date

The date ProjectPal actually expects a Task to start, once every [Dependency](#dependency) and the Project's own schedule are taken into account. This is always calculated, never entered directly — it can be later than the Requested Start Date if something the Task depends on isn't finished by then.

<a id="end-date"></a>
### End Date

The date a Task is expected to finish — its Planned Start Date plus its [Effort](#effort) or [Duration](#duration). You can also set this directly on [Task Detail](#task-detail-glossary); doing so works backwards to update the Requested Start Date instead.

<a id="priority"></a>
### Priority

How important a Task (or Project) is, from **High** down to **Low**, plus two special values, **Cancelled** and **Closed**, used when a Task is no longer active. Priority is one of the two main inputs to [Urgency](#urgency).

<a id="status"></a>
### Status

Where a Task currently stands in its lifecycle — e.g. Not Started, In Progress, Ready, Support, Tentative, Closed, or Cancelled. Status and Priority are independent of each other: a Task can be Closed with a High Priority still recorded against it, for instance.

<a id="urgency"></a>
### Urgency

A score ProjectPal calculates for every open Task, combining its Priority (and the Priority of the Project — and any parent Projects — it belongs to) with how close it is to its relevant date (its start date if it hasn't started, its midpoint if it's in progress, or its end date otherwise). The more urgent a Task is, the stronger the red highlight colour it's shown with in [All Tasks](#all-tasks-glossary), [Plan View](#plan-view-glossary), and [Task Detail](#task-detail-glossary) — a Task with no Priority set, or one that's Cancelled or Closed in Priority, is always shown in flat grey instead, regardless of its Urgency score.

<a id="tentative-resource-assignment"></a>
### Tentative Resource Assignment

A flag on a Task indicating that its Resource assignment isn't confirmed yet. Shown as a "T" column in [All Tasks](#all-tasks-glossary), visible only to a Team Lead User.

<a id="the-projectpal-screens"></a>
## 4. The ProjectPal Screens

<a id="all-tasks-glossary"></a>
### All Tasks

The main grid listing every Task you have visibility to. See `AllTaskView.md` for the full guide.

<a id="plan-view-glossary"></a>
### Plan View (Gantt View)

A visual timeline showing Projects and their Tasks, laid out against real dates, with Dependency arrows between them. Also called the Gantt View, after the classic "Gantt chart" style of timeline it uses. See `GanttView.md` for the full guide.

<a id="task-detail-glossary"></a>
### Task Detail

The screen showing everything about one Task — its fields, its Resources, and its Dependencies/Attachments/Remarks — opened by double-clicking that Task in All Tasks or Plan View. See `TaskDetailView.md` for the full guide.

<a id="project-detail-glossary"></a>
### Project Detail

The screen showing everything about one Project — its own fields, and a browsable tree of its sub-Projects and Tasks — opened by clicking that Project's name anywhere in ProjectPal, or from the **Projects** button's own top-level browser. See `ProjectDetailView.md` for the full guide.
