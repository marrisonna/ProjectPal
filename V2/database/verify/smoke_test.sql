-- ProjectPal V2 — Level 1 smoke test
-- Read-only sanity checks: confirms the schema and example data loaded correctly.
-- Run via scripts/verify.ps1, or paste into psql/pgAdmin/DBeaver by hand.

SET search_path TO projectpal;

-- Row counts — should be non-zero across the board after loading the seed data.
SELECT 'team' AS table_name, count(*) FROM team
UNION ALL SELECT 'person', count(*) FROM person
UNION ALL SELECT 'person_role', count(*) FROM person_role
UNION ALL SELECT 'component', count(*) FROM component
UNION ALL SELECT 'project', count(*) FROM project
UNION ALL SELECT 'task', count(*) FROM task
UNION ALL SELECT 'task_resource', count(*) FROM task_resource
UNION ALL SELECT 'dependency', count(*) FROM dependency
UNION ALL SELECT 'attachment', count(*) FROM attachment
UNION ALL SELECT 'remark', count(*) FROM remark
ORDER BY 1;

-- Tasks joined out to their Project, Component, Owner and Requestor names —
-- proves the foreign keys and enum columns all resolve sensibly.
SELECT
    t.task_id,
    t.description,
    p.name       AS project,
    c.name       AS component,
    o.name       AS owner,
    req.name     AS requestor,
    t.priority,
    t.status,
    t.effort_in_days,
    t.effort_type
FROM task t
JOIN project p        ON p.project_id = t.project_id
LEFT JOIN component c ON c.component_id = t.component_id
LEFT JOIN person o    ON o.person_id = t.owner_person_id
LEFT JOIN person req  ON req.person_id = t.requestor_person_id
ORDER BY t.task_id;

-- Resource load per person — how many tasks each resourced Person is on.
SELECT pe.name, count(*) AS assigned_tasks
FROM task_resource tr
JOIN person pe ON pe.person_id = tr.person_id
GROUP BY pe.name
ORDER BY assigned_tasks DESC, pe.name;

-- Every Person's Team memberships and per-Team role — proves the multi-Team
-- PersonRole model (e.g. Tom Baxter should appear twice, with two different roles).
SELECT pe.name AS person, tm.name AS team, pr.role, pr.is_resource
FROM person_role pr
JOIN person pe ON pe.person_id = pr.person_id
JOIN team tm   ON tm.team_id = pr.team_id
ORDER BY pe.name, tm.name;

-- Dependency chain, resolved to human-readable descriptions on both sides —
-- confirms the polymorphic Task/Project dependency model reads back correctly.
SELECT
    d.dependency_id,
    COALESCE(pt.description, pp.name)   AS predecessor,
    COALESCE(qt.description, qp.name)   AS successor
FROM dependency d
LEFT JOIN task pt    ON pt.task_id = d.pre_task_id
LEFT JOIN project pp ON pp.project_id = d.pre_project_id
LEFT JOIN task qt    ON qt.task_id = d.post_task_id
LEFT JOIN project qp ON qp.project_id = d.post_project_id
ORDER BY d.dependency_id;

-- Attachments by kind, with owner resolved across the mutually-exclusive
-- Task/Project/Component columns.
SELECT
    a.attachment_id,
    a.kind,
    a.name,
    COALESCE(t.description, p.name, c.name) AS owning_item
FROM attachment a
LEFT JOIN task t      ON t.task_id = a.task_id
LEFT JOIN project p   ON p.project_id = a.project_id
LEFT JOIN component c ON c.component_id = a.component_id
ORDER BY a.attachment_id;
