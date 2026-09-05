-- ProjectPal V2 — Level 1 example data
-- Entirely fictional: people, teams, projects, and tasks invented for demonstration
-- purposes. Shaped (status/priority/effort-type mix, dependency chains, resourcing
-- patterns) after real usage patterns observed in the old V1.2 database (ProjectPalDB_1)
-- on this machine, but no real names, descriptions, or content were copied from it.

SET search_path TO projectpal;

BEGIN;

-- ---------------------------------------------------------------------------
-- Teams
-- ---------------------------------------------------------------------------

-- Collapsed from two teams into one (was Platform=1/Customer Projects=2) so
-- team_id 2 is free for the real V1.2 data imported in 002_team2_from_v1.sql.
INSERT INTO team (team_id, name) VALUES
    (1, 'Platform');
SELECT setval('team_team_id_seq', 1);

-- ---------------------------------------------------------------------------
-- People
-- ---------------------------------------------------------------------------

-- Every seeded Person gets a password, primed as if an admin had already set
-- one (3_Authentication/Plan.md D1.3-4). These are fictional Level 1
-- demonstrator accounts on a local-only database, not real credentials, so
-- the plaintext each hash corresponds to is disclosed here for developer
-- convenience (D1.3-8) — regenerate with:
--   python -c "from argon2 import PasswordHasher; print(PasswordHasher().hash('<password>'))"
--   alice.chen@example.com   -> alice-pass1
--   ben.okafor@example.com   -> ben-pass1
--   priya.sharma@example.com -> priya-pass1
--   tom.baxter@example.com   -> tom-pass1
--   grace.liu@example.com    -> grace-pass1
--   sam.patel@example.com    -> sam-pass1
--   nadia.fischer@example.com -> nadia-pass1
INSERT INTO person (person_id, name, is_active, is_organisation_admin, external_login, password_hash, colour) VALUES
    (1, 'Alice Chen',    true, true,  'alice.chen@example.com',    '$argon2id$v=19$m=65536,t=3,p=4$0O5rQdfv3RCoDwtKV1nfjQ$7w7ozK6gJOqKpZ2n0rWIbn3NNfh92P8/01rKLl/uwK0', '#4C72B0'),
    (2, 'Ben Okafor',    true, false, 'ben.okafor@example.com',    '$argon2id$v=19$m=65536,t=3,p=4$FJGnjluuO0fXrmoQRZVy1w$8dRyZfr2NgXJGGPU5/02WpxROcmfZg1UuAC7xOfPEj8', '#DD8452'),
    (3, 'Priya Sharma',  true, false, 'priya.sharma@example.com',  '$argon2id$v=19$m=65536,t=3,p=4$hXq1ad/jWceucoj3z1S4kw$nFmGHWCrf5lYZfwtIb0QdvGWVEVbOjpwYgA7fIyTQXA', '#55A868'),
    (4, 'Tom Baxter',    true, false, 'tom.baxter@example.com',    '$argon2id$v=19$m=65536,t=3,p=4$flxkFZZ8TglUurQGSDtoRA$X4s2G+EMTBsfRTfM0oTAP6OXj5gjCOLGwW/vLkl3OUQ', '#C44E52'),
    (5, 'Grace Liu',     true, false, 'grace.liu@example.com',     '$argon2id$v=19$m=65536,t=3,p=4$NTFt89RCMOn+C9e6iSepRg$Yok6sknL5t7u438TEfhF3SRubOlzAfhm4tWBRG78h00', '#8172B2'),
    (6, 'Sam Patel',     true, false, 'sam.patel@example.com',     '$argon2id$v=19$m=65536,t=3,p=4$4DcFfvPchaM+C2yCRVq+jg$XjrZz17FYpmjLA8ooHJJLTarMr9mCgY+b6BfGQ0Cogs', NULL),
    (7, 'Nadia Fischer', true, true,  'nadia.fischer@example.com', '$argon2id$v=19$m=65536,t=3,p=4$WdSa5nXa0rfisSET0ENBSQ$6WMwXkDJajexjS2rZWBGqCliKJetCWbfvVUpn3t2BQU', NULL);
SELECT setval('person_person_id_seq', 7);

-- PersonRole: Team membership + per-Team role/resource flag.
-- Collapsed from two Teams into one: Tom was TeamLeadUser of the old Team 2
-- and an ordinary member of Team 1 — merged into a single LeadUser row
-- (Alice already holds Team 1's one TeamLeadUser slot, so Tom can't also
-- hold it — the bootstrap invariant the REST API enforces, Requirements/UseCases.md
-- §12, allows exactly one per Team). Nadia's two rows were identical
-- (ordinary, non-resource) and just dedupe to one.
-- nickname (D1.4-21): populated for a few of Team 1's People to demonstrate
-- the "shorter name known within a team" feature; left null for the rest of
-- Team 1 and for all of Team 2 (002_team2_from_v1.sql) — nickname is
-- read-only in Level 1, seed data is the only way to set one for now.
INSERT INTO person_role (person_id, team_id, is_resource, role, nickname) VALUES
    (1, 1, true,  'TeamLeadUser', 'Alice'),
    (2, 1, true,  'LeadUser',     NULL),
    (3, 1, true,  'NormalUser',   'Priya'),
    (4, 1, true,  'LeadUser',     NULL),
    (5, 1, true,  'NormalUser',   NULL),
    (6, 1, false, 'ReadOnlyUser', NULL),
    (7, 1, false, 'NormalUser',   'Nadia');

-- ---------------------------------------------------------------------------
-- Components (classification tree, independent of Project; each belongs to
-- exactly one Team for management purposes — D-DM-6 — though a Task in any
-- Team can still tag any Component). All four owners here happen to be Team 1
-- members, so all four are Team 1's for this fictional dataset.
-- ---------------------------------------------------------------------------

INSERT INTO component (component_id, parent_component_id, team_id, name, owner_person_id) VALUES
    (1, NULL, 1, 'Billing',    1),
    (2, 1,    1, 'Invoicing',  2),
    (3, 1,    1, 'Payments',   2),
    (4, NULL, 1, 'Reporting',  3);
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
    (4, NULL, 1, 'Customer Portal Refresh', 'MedHigh',
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
                  requestor_person_id, owner_person_id, effort_in_days, effort_type, percentage_allocation,
                  task_type, status, status_date, tentative_resource_assignment, start_relative_days_to_project) VALUES
    (1, 2, 2,    'High',    'Design new schema for billing tables',
        'Draft the PostgreSQL DDL for Invoicing/Payments, reviewed against the old SQL Server schema.',
        1, 2, 5,  'PersonDays', 1, 'Infrastructure',  'InProgress', NULL,                        false, 0),
    (2, 2, 3,    'MedHigh', 'Migrate legacy payment records',
        'Write and run the one-off migration for historical payment rows.',
        1, 2, 8,  'PersonDays', 1, 'Infrastructure',  'NotStarted', NULL,                        false, 10),
    (3, 3, NULL, 'High',    'Stand up REST endpoints for tasks',
        'CRUD endpoints for Task, backed by the new schema.',
        1, 3, 6,  'PersonDays', 1, 'NewDevelopment',  'InProgress', NULL,                        false, 0),
    (4, 3, NULL, 'Med',     'Write API auth middleware',
        'Token validation middleware shared by every endpoint.',
        2, 3, 3,  'Duration', 1, 'NewDevelopment',  'Ready',      NULL,                        false, 5),
    (5, 2, 1,    'Low',     'Retire old stored procedures',
        'Remove SQL Server stored procedures superseded by the new schema.',
        1, 2, 2,  'PersonDays', 1, 'Maintenance',     'Closed',     now() - interval '3 days',  false, 20),
    (6, 1, 4,    'Low',     'Old VB6 report cleanup',
        'Archive the legacy reporting scripts that are no longer referenced.',
        1, 1, 1,  'PersonDays', 1, 'Maintenance',     'Closed',     now() - interval '60 days', false, 0),
    (7, 4, NULL, 'High',    'Portal homepage redesign',
        'New layout for the customer portal landing page.',
        6, 4, 10, 'PersonDays', 1, 'Enhancement',     'InProgress', NULL,                        false, 0),
    (8, 4, 2,    'MedHigh', 'Invoice PDF export',
        'Let customers download a PDF copy of any invoice.',
        6, 5, 4,  'PersonDays', 1, 'Enhancement',     'NotStarted', NULL,                        false, 15),
    (9, 4, 3,    'High',    'Fix payment webhook race condition',
        'Concurrent webhook deliveries can double-charge a customer under load.',
        5, 4, 1,  'PersonDays', 1, 'Support',         'Support',    NULL,                        false, -4),
    (10, 1, 4,   'MedLow',  'Explore self-service reporting dashboard',
        'Spike on whether customers could build their own reports.',
        1, 3, 15, 'Duration', 1, 'Enhancement',     'Tentative',  NULL,                        true,  30),
    (11, 3, NULL, 'Cancelled', 'Evaluate NoSQL for attachments',
        'Investigated storing Attachment content outside PostgreSQL; not pursued.',
        1, 3, NULL, NULL, 1,   'Other',           'Cancelled',  now() - interval '90 days', false, NULL);
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
-- Remarks — the owner (or a TeamLeadUser, for delete) may edit/delete these
-- later via the API; only authorship (created_by_person_id) can never change.
-- ---------------------------------------------------------------------------

INSERT INTO remark (task_id, project_id, component_id, remark_text, created_by_person_id, created_time) VALUES
    (1, NULL, NULL, 'Confirmed with Alice that Invoicing and Payments can share a base table.', 2, now() - interval '2 days'),
    (1, NULL, NULL, 'Draft DDL attached — please review before I start the migration script.', 2, now() - interval '1 days'),
    (7, NULL, NULL, 'Design review scheduled for Thursday.', 4, now() - interval '3 days'),
    (9, NULL, NULL, 'Reproduced locally — looks like a missing idempotency key on the webhook handler.', 4, now());

COMMIT;
