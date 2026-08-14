-- ProjectPal V2 — Stage 1 example data
-- Entirely fictional: people, teams, projects, and tasks invented for demonstration
-- purposes. Shaped (status/priority/effort-type mix, dependency chains, resourcing
-- patterns) after real usage patterns observed in the old V1.2 database (ProjectPalDB_1)
-- on this machine, but no real names, descriptions, or content were copied from it.

SET search_path TO projectpal;

BEGIN;

-- ---------------------------------------------------------------------------
-- Teams
-- ---------------------------------------------------------------------------

INSERT INTO team (team_id, name) VALUES
    (1, 'Platform'),
    (2, 'Customer Projects');
SELECT setval('team_team_id_seq', 2);

-- ---------------------------------------------------------------------------
-- People
-- ---------------------------------------------------------------------------

INSERT INTO person (person_id, name, is_active, is_organisation_admin, external_login, colour) VALUES
    (1, 'Alice Chen',    true, true,  'alice.chen@example.com',    '#4C72B0'),
    (2, 'Ben Okafor',    true, false, 'ben.okafor@example.com',    '#DD8452'),
    (3, 'Priya Sharma',  true, false, 'priya.sharma@example.com',  '#55A868'),
    (4, 'Tom Baxter',    true, false, 'tom.baxter@example.com',    '#C44E52'),
    (5, 'Grace Liu',     true, false, 'grace.liu@example.com',     '#8172B2'),
    (6, 'Sam Patel',     true, false, 'sam.patel@example.com',     NULL),
    (7, 'Nadia Fischer', true, true,  'nadia.fischer@example.com', NULL);
SELECT setval('person_person_id_seq', 7);

-- PersonRole: Team membership + per-Team role/resource flag.
-- Tom belongs to both Teams with a different role in each; Nadia is an
-- organisation admin on Person but an ordinary (non-resource) member of both Teams.
INSERT INTO person_role (person_id, team_id, is_resource, role) VALUES
    (1, 1, true,  'SuperUser'),
    (2, 1, true,  'PowerUser'),
    (3, 1, true,  'NormalUser'),
    (4, 1, false, 'NormalUser'),
    (7, 1, false, 'NormalUser'),
    (4, 2, true,  'PowerUser'),
    (5, 2, true,  'NormalUser'),
    (6, 2, false, 'ReadOnlyUser'),
    (7, 2, false, 'NormalUser');

-- ---------------------------------------------------------------------------
-- Components (classification tree, independent of Project)
-- ---------------------------------------------------------------------------

INSERT INTO component (component_id, parent_component_id, name, owner_person_id) VALUES
    (1, NULL, 'Billing',    1),
    (2, 1,    'Invoicing',  2),
    (3, 1,    'Payments',   2),
    (4, NULL, 'Reporting',  3);
SELECT setval('component_component_id_seq', 4);

-- ---------------------------------------------------------------------------
-- Projects
-- ---------------------------------------------------------------------------

INSERT INTO project (project_id, parent_project_id, team_id, name, priority, detailed_description,
                      owner_person_id, start_date, due_date) VALUES
    (1, NULL, 1, 'Platform Modernisation', 'High',
        'Overall programme to rebuild ProjectPal as a multi-tenant SaaS product.',
        1, DATE '2026-01-05', DATE '2026-12-18'),
    (2, 1,    1, 'Database Migration', 'MedHigh',
        'Move the schema from SQL Server to PostgreSQL and design it for multi-tenancy.',
        2, DATE '2026-01-05', DATE '2026-03-27'),
    (3, 1,    1, 'API Layer', 'Med',
        'Build the REST API that fronts the new database.',
        3, DATE '2026-02-02', DATE '2026-05-15'),
    (4, NULL, 2, 'Customer Portal Refresh', 'MedHigh',
        'Redesign the customer-facing portal and its billing/reporting screens.',
        4, DATE '2026-01-19', DATE '2026-06-12');
SELECT setval('project_project_id_seq', 4);

-- ---------------------------------------------------------------------------
-- Tasks — a deliberate mix of priority/status/effort-type/task-type values,
-- a recently-closed task and a long-closed task (for exercising Urgency decay
-- once that's implemented at the API layer), a tentative assignment, and a
-- cancelled task.
-- ---------------------------------------------------------------------------

INSERT INTO task (task_id, project_id, component_id, priority, description, detailed_description,
                  requestor_person_id, owner_person_id, effort_in_days, effort_type, task_type, status,
                  status_date, tentative_resource_assignment, start_relative_days_to_project) VALUES
    (1, 2, 2,    'High',    'Design new schema for billing tables',
        'Draft the PostgreSQL DDL for Invoicing/Payments, reviewed against the old SQL Server schema.',
        1, 2, 5,  'ManDays',  'Infrastructure',  'InProgress', NULL,                        false, 0),
    (2, 2, 3,    'MedHigh', 'Migrate legacy payment records',
        'Write and run the one-off migration for historical payment rows.',
        1, 2, 8,  'ManDays',  'Infrastructure',  'NotStarted', NULL,                        false, 10),
    (3, 3, NULL, 'High',    'Stand up REST endpoints for tasks',
        'CRUD endpoints for Task, backed by the new schema.',
        1, 3, 6,  'ManDays',  'NewDevelopment',  'InProgress', NULL,                        false, 0),
    (4, 3, NULL, 'Med',     'Write API auth middleware',
        'Token validation middleware shared by every endpoint.',
        2, 3, 3,  'Duration', 'NewDevelopment',  'Ready',      NULL,                        false, 5),
    (5, 2, 1,    'Low',     'Retire old stored procedures',
        'Remove SQL Server stored procedures superseded by the new schema.',
        1, 2, 2,  'ManDays',  'Maintenance',     'Closed',     now() - interval '3 days',  false, 20),
    (6, 1, 4,    'Low',     'Old VB6 report cleanup',
        'Archive the legacy reporting scripts that are no longer referenced.',
        1, 1, 1,  'ManDays',  'Maintenance',     'Closed',     now() - interval '60 days', false, 0),
    (7, 4, NULL, 'High',    'Portal homepage redesign',
        'New layout for the customer portal landing page.',
        6, 4, 10, 'ManDays',  'Enhancement',     'InProgress', NULL,                        false, 0),
    (8, 4, 2,    'MedHigh', 'Invoice PDF export',
        'Let customers download a PDF copy of any invoice.',
        6, 5, 4,  'ManDays',  'Enhancement',     'NotStarted', NULL,                        false, 15),
    (9, 4, 3,    'High',    'Fix payment webhook race condition',
        'Concurrent webhook deliveries can double-charge a customer under load.',
        5, 4, 1,  'ManDays',  'Support',         'Support',    NULL,                        false, -4),
    (10, 1, 4,   'MedLow',  'Explore self-service reporting dashboard',
        'Spike on whether customers could build their own reports.',
        1, 3, 15, 'Duration', 'Enhancement',     'Tentative',  NULL,                        true,  30),
    (11, 3, NULL, 'Cancelled', 'Evaluate NoSQL for attachments',
        'Investigated storing Attachment content outside PostgreSQL; not pursued.',
        1, 3, NULL, NULL,     'Other',           'Cancelled',  now() - interval '90 days', false, NULL);
SELECT setval('task_task_id_seq', 11);

-- ---------------------------------------------------------------------------
-- Resource assignment (Task <-> Person)
-- ---------------------------------------------------------------------------

INSERT INTO task_resource (task_id, person_id) VALUES
    (1, 2), (1, 3),
    (2, 2),
    (3, 3),
    (4, 3),
    (5, 2),
    (6, 1),
    (7, 4), (7, 5),
    (8, 5),
    (9, 4),
    (10, 3);

-- ---------------------------------------------------------------------------
-- Dependencies — Task-to-Task, and one Project-to-Project (either side of a
-- Dependency can be a Task or a Project, per DomainModel.md).
-- ---------------------------------------------------------------------------

INSERT INTO dependency (pre_task_id, post_task_id) VALUES (1, 2);
INSERT INTO dependency (pre_task_id, post_task_id) VALUES (1, 3);
INSERT INTO dependency (pre_task_id, post_task_id) VALUES (2, 9);
INSERT INTO dependency (pre_project_id, post_project_id) VALUES (2, 3);

-- ---------------------------------------------------------------------------
-- Attachments — one of each kind (Link, File, Mail), demonstrating the
-- content_hash-based dedup rule for File/Mail.
-- ---------------------------------------------------------------------------

INSERT INTO attachment (task_id, project_id, component_id, name, kind, url, owner_person_id)
VALUES (NULL, 1, NULL, 'Modernisation roadmap (Confluence)', 'Link',
        'https://example.atlassian.net/wiki/spaces/PP/pages/12345/Roadmap', 1);

INSERT INTO attachment (task_id, project_id, component_id, name, kind, data, size_bytes, content_hash, owner_person_id)
SELECT 1, NULL, NULL, 'schema-draft.sql', 'File', d, octet_length(d), encode(sha256(d), 'hex'), 2
FROM (SELECT convert_to('-- draft DDL for billing tables (placeholder content)', 'UTF8') AS d) x;

INSERT INTO attachment (task_id, project_id, component_id, name, kind, mail_from, data, size_bytes, content_hash, owner_person_id)
SELECT 7, NULL, NULL, 'Homepage feedback thread', 'Mail', 'sam.patel@example.com', d, octet_length(d), encode(sha256(d), 'hex'), 4
FROM (SELECT convert_to(
    'From: sam.patel@example.com' || E'\n' ||
    'Subject: Homepage feedback' || E'\n\n' ||
    'A few thoughts on the new layout...', 'UTF8') AS d) x;

-- ---------------------------------------------------------------------------
-- Remarks — immutable/append-only, so each insert here is final.
-- ---------------------------------------------------------------------------

INSERT INTO remark (task_id, project_id, component_id, remark_text, created_by_person_id, created_time) VALUES
    (1, NULL, NULL, 'Confirmed with Alice that Invoicing and Payments can share a base table.', 2, now() - interval '2 days'),
    (1, NULL, NULL, 'Draft DDL attached — please review before I start the migration script.', 2, now() - interval '1 days'),
    (7, NULL, NULL, 'Design review scheduled for Thursday.', 4, now() - interval '3 days'),
    (9, NULL, NULL, 'Reproduced locally — looks like a missing idempotency key on the webhook handler.', 4, now());

COMMIT;
