-- ProjectPal V2 — Level 1 database schema
-- PostgreSQL. Implements the Level 1 scope of Claude/Requirements/DomainModel.md.
-- This is migration 001 — the whole schema, since there's no prior version to migrate from.
-- Later structural changes should be added as new, additive migration files (002_..., 003_...),
-- never by editing this file, so the database can always be rebuilt from an ordered list of migrations
-- (see Claude/Level1_Implementation/1_DatabaseSetup/DataBaseHostingOptions.md's "database migrations" section).

BEGIN;

CREATE SCHEMA IF NOT EXISTS projectpal;
SET search_path TO projectpal;

-- ---------------------------------------------------------------------------
-- Enumerated types
-- ---------------------------------------------------------------------------

-- Ordered Cancelled(-1) .. High(5) to match KeyConcepts.md's Urgency algorithm
-- (Pr(x): High=5, MedHigh=4, Med=3, MedLow=2, Low=1, Closed=0, Cancelled=-1).
-- Postgres enums compare in declaration order, so this ordering is meaningful.
CREATE TYPE priority_level AS ENUM
    ('Cancelled', 'Closed', 'Low', 'MedLow', 'Med', 'MedHigh', 'High');

CREATE TYPE task_status AS ENUM
    ('NotStarted', 'Ready', 'InProgress', 'Tentative', 'Support', 'Closed', 'Cancelled');

CREATE TYPE task_type AS ENUM
    ('NewDevelopment', 'Enhancement', 'Maintenance', 'Support', 'Infrastructure', 'Other');

-- 'PersonDays' is V2's gender-neutral rename of V1.2's equivalent concept,
-- historically called "ManDays" (Requirements/KeyConcepts.md §10,
-- V1.2/Libs/DBProjectPal/DBProjectPal/Enums.cs's EffortTypeValue).
CREATE TYPE effort_type AS ENUM
    ('PersonDays', 'Duration');

-- Per-Team role — see DomainModel.md's PersonRole entity and
-- KeyConcepts.md's Role / Permission Level entry. V2 names (renamed from
-- V1.2's ReadOnlyUser/NormalUser/PowerUser/SuperUser — see UseCases.md Annex A).
CREATE TYPE team_role AS ENUM
    ('ReadOnlyUser', 'NormalUser', 'LeadUser', 'TeamLeadUser');

-- An Attachment's content kind — see DomainModel.md's Attachment entity
-- (file / captured email / hyperlink).
CREATE TYPE attachment_kind AS ENUM
    ('File', 'Mail', 'Link');

-- ---------------------------------------------------------------------------
-- Team
-- ---------------------------------------------------------------------------

CREATE TABLE team (
    team_id      serial PRIMARY KEY,
    name         text NOT NULL UNIQUE,
    modified_time timestamptz NOT NULL DEFAULT now()
);

-- ---------------------------------------------------------------------------
-- Person / PersonRole
-- ---------------------------------------------------------------------------

CREATE TABLE person (
    person_id             serial PRIMARY KEY,
    name                  text NOT NULL,
    is_active             boolean NOT NULL DEFAULT true,
    -- Organisation-wide administrator flag — lives on Person, not PersonRole,
    -- because it isn't scoped to any one Team (DomainModel.md Decisions D-DM-4).
    is_organisation_admin boolean NOT NULL DEFAULT false,
    -- Placeholder for a real external identity reference (Goals.md's identity
    -- direction). Level 1 can put a simple username/email here for now. UNIQUE
    -- since it's the login identifier (3_Authentication/Plan.md D1.3-3) — multiple
    -- NULLs are still allowed, for People who never log in.
    external_login        text UNIQUE,
    -- Argon2id hash (3_Authentication/Plan.md D1.3-3/D1.3-5). NULL means this
    -- Person has no password yet and can't log in — not an error state, e.g. a
    -- resource-only Person who never needs to.
    password_hash         text,
    -- Gantt bar colour, a per-Person per-Organisation setting (DomainModel.md PersonRole entry).
    colour                text,
    modified_by            integer REFERENCES person(person_id),
    modified_time            timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE person_role (
    person_id    integer NOT NULL REFERENCES person(person_id),
    team_id      integer NOT NULL REFERENCES team(team_id),
    -- Whether this Person can be assigned to work items on this Team.
    is_resource  boolean NOT NULL DEFAULT false,
    role         team_role NOT NULL DEFAULT 'NormalUser',
    -- A shorter name this Person is known by on this Team (e.g. "Alice"
    -- rather than "Alice Chen"), shown in place of person.name wherever a
    -- screen displays a name in the context of this Team. Null means no
    -- override. Level 1 is read-only (seed data only) — editing is deferred
    -- to a dedicated Team Management screen in Level 2 (D1.4-21).
    nickname     text,
    PRIMARY KEY (person_id, team_id)
);

CREATE INDEX ix_person_role_team ON person_role(team_id);

-- ---------------------------------------------------------------------------
-- Component (self-referencing tree, independent of Project)
-- ---------------------------------------------------------------------------

CREATE TABLE component (
    component_id        serial PRIMARY KEY,
    parent_component_id integer REFERENCES component(component_id),
    -- Level 1: every Component belongs to exactly one Team, mirroring Project
    -- (DomainModel.md Decisions D-DM-6). Governs who may create/edit/delete
    -- this Component, not which Team's Tasks may reference it — a Task in
    -- any Team can still tag a Component belonging to a different Team.
    team_id              integer NOT NULL REFERENCES team(team_id),
    name                 text NOT NULL,
    owner_person_id       integer REFERENCES person(person_id),
    modified_by            integer REFERENCES person(person_id),
    modified_time            timestamptz NOT NULL DEFAULT now(),
    CHECK (parent_component_id IS NULL OR parent_component_id <> component_id)
);

CREATE INDEX ix_component_parent ON component(parent_component_id);
CREATE INDEX ix_component_team ON component(team_id);

-- ---------------------------------------------------------------------------
-- Project (self-referencing tree; end date is derived, never stored —
-- see DomainModel.md's Project entry and its "keep derived scheduling" decision)
-- ---------------------------------------------------------------------------

CREATE TABLE project (
    project_id        serial PRIMARY KEY,
    parent_project_id integer REFERENCES project(project_id),
    -- Level 1: every Project belongs to exactly one Team
    -- (DomainModel.md Open Questions/Decisions #1; may broaden later — see Future Extensions).
    team_id             integer NOT NULL REFERENCES team(team_id),
    name                 text NOT NULL,
    priority              priority_level,
    detailed_description   text,
    owner_person_id         integer REFERENCES person(person_id),
    start_date               date,
    due_date                  date,
    modified_by                integer REFERENCES person(person_id),
    modified_time                timestamptz NOT NULL DEFAULT now(),
    CHECK (parent_project_id IS NULL OR parent_project_id <> project_id)
);

CREATE INDEX ix_project_parent ON project(parent_project_id);
CREATE INDEX ix_project_team ON project(team_id);

-- ---------------------------------------------------------------------------
-- Task (exactly one Project; optionally relates to one Component — see
-- DomainModel.md's Task entry, confirmed against the old schema as nullable)
-- ---------------------------------------------------------------------------

CREATE TABLE task (
    task_id                        serial PRIMARY KEY,
    project_id                      integer NOT NULL REFERENCES project(project_id),
    component_id                     integer REFERENCES component(component_id),
    orig_task_number                  text,
    priority                           priority_level,
    description                         text NOT NULL,
    detailed_description                  text,
    external_reference_url                  text,
    requestor_person_id                       integer REFERENCES person(person_id),
    owner_person_id                             integer REFERENCES person(person_id),
    date_added                                    timestamptz NOT NULL DEFAULT now(),
    effort_in_days                                  double precision,
    effort_type                                       effort_type,
    percentage_allocation                               double precision,
    task_type                                             task_type,
    status                                                  task_status NOT NULL DEFAULT 'NotStarted',
    status_date                                               timestamptz,
    tentative_resource_assignment                               boolean NOT NULL DEFAULT false,
    -- Derived-scheduling: a business-day offset from the owning Project's
    -- start_date, not an absolute date (DomainModel.md's kept decision).
    start_relative_days_to_project                                integer,
    modified_by                                                     integer REFERENCES person(person_id),
    modified_time                                                     timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX ix_task_project ON task(project_id);
CREATE INDEX ix_task_component ON task(component_id);
CREATE INDEX ix_task_owner ON task(owner_person_id);
CREATE INDEX ix_task_requestor ON task(requestor_person_id);
CREATE INDEX ix_task_status ON task(status);

-- ---------------------------------------------------------------------------
-- Resource assignment (Task <-> Person, many-to-many, resource-scoped —
-- see DomainModel.md's Resource assignment entity)
-- ---------------------------------------------------------------------------

CREATE TABLE task_resource (
    task_id    integer NOT NULL REFERENCES task(task_id),
    person_id  integer NOT NULL REFERENCES person(person_id),
    PRIMARY KEY (task_id, person_id)
);

CREATE INDEX ix_task_resource_person ON task_resource(person_id);

-- ---------------------------------------------------------------------------
-- Dependency (predecessor/successor ordering; either side can be a Task or
-- a Project — see DomainModel.md's Dependency entity)
-- ---------------------------------------------------------------------------

CREATE TABLE dependency (
    dependency_id    serial PRIMARY KEY,
    pre_task_id        integer REFERENCES task(task_id),
    pre_project_id        integer REFERENCES project(project_id),
    post_task_id            integer REFERENCES task(task_id),
    post_project_id           integer REFERENCES project(project_id),
    modified_by                 integer REFERENCES person(person_id),
    modified_time                  timestamptz NOT NULL DEFAULT now(),
    -- Exactly one of {pre_task_id, pre_project_id} and one of {post_task_id, post_project_id}.
    CHECK (num_nonnulls(pre_task_id, pre_project_id) = 1),
    CHECK (num_nonnulls(post_task_id, post_project_id) = 1)
);

CREATE INDEX ix_dependency_pre_task ON dependency(pre_task_id);
CREATE INDEX ix_dependency_pre_project ON dependency(pre_project_id);
CREATE INDEX ix_dependency_post_task ON dependency(post_task_id);
CREATE INDEX ix_dependency_post_project ON dependency(post_project_id);

-- ---------------------------------------------------------------------------
-- Attachment (exactly one owner among Task/Project/Component; a file, a
-- captured email, or a hyperlink — see DomainModel.md's Attachment entity)
-- ---------------------------------------------------------------------------

CREATE TABLE attachment (
    attachment_id     serial PRIMARY KEY,
    task_id             integer REFERENCES task(task_id),
    project_id            integer REFERENCES project(project_id),
    component_id             integer REFERENCES component(component_id),
    name                       text NOT NULL,
    kind                        attachment_kind NOT NULL,
    url                          text,     -- set when kind = 'Link'
    mail_from                     text,    -- set when kind = 'Mail'
    data                           bytea,  -- set when kind = 'File' or 'Mail'
    size_bytes                       integer,
    -- SHA-256 hex digest of `data`, used to enforce the "don't re-attach the
    -- identical file/email twice" rule below. Computed by the API layer on upload.
    content_hash                       text,
    created_time                         timestamptz NOT NULL DEFAULT now(),
    owner_person_id                       integer REFERENCES person(person_id),
    modified_by                             integer REFERENCES person(person_id),
    modified_time                             timestamptz NOT NULL DEFAULT now(),
    CHECK (num_nonnulls(task_id, project_id, component_id) = 1),
    CHECK ((kind = 'Link' AND url IS NOT NULL AND data IS NULL)
        OR (kind <> 'Link' AND data IS NOT NULL))
);

CREATE INDEX ix_attachment_task ON attachment(task_id);
CREATE INDEX ix_attachment_project ON attachment(project_id);
CREATE INDEX ix_attachment_component ON attachment(component_id);

-- Deduplication: the same content_hash cannot be attached twice to the same owner.
-- COALESCE(..., -1) folds the three mutually-exclusive owner columns into one
-- comparable key so the uniqueness check spans whichever one is actually set.
CREATE UNIQUE INDEX ux_attachment_dedup ON attachment (
    COALESCE(task_id, -1), COALESCE(project_id, -1), COALESCE(component_id, -1), content_hash
) WHERE content_hash IS NOT NULL;

-- ---------------------------------------------------------------------------
-- Remark (exactly one owner; immutable/append-only — see DomainModel.md's
-- Remark entity and its fix for the old model's reassigned-authorship quirk)
-- ---------------------------------------------------------------------------

CREATE TABLE remark (
    remark_id             serial PRIMARY KEY,
    task_id                 integer REFERENCES task(task_id),
    project_id                integer REFERENCES project(project_id),
    component_id                 integer REFERENCES component(component_id),
    remark_text                    text NOT NULL,
    created_by_person_id             integer NOT NULL REFERENCES person(person_id),
    created_time                       timestamptz NOT NULL DEFAULT now(),
    CHECK (num_nonnulls(task_id, project_id, component_id) = 1)
);

CREATE INDEX ix_remark_task ON remark(task_id);
CREATE INDEX ix_remark_project ON remark(project_id);
CREATE INDEX ix_remark_component ON remark(component_id);

-- ---------------------------------------------------------------------------
-- Behaviour: keep modified_time current on UPDATE
-- ---------------------------------------------------------------------------

CREATE OR REPLACE FUNCTION set_modified_time() RETURNS trigger AS $$
BEGIN
    NEW.modified_time := now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_person_modified_time BEFORE UPDATE ON person
    FOR EACH ROW EXECUTE FUNCTION set_modified_time();
CREATE TRIGGER trg_component_modified_time BEFORE UPDATE ON component
    FOR EACH ROW EXECUTE FUNCTION set_modified_time();
CREATE TRIGGER trg_project_modified_time BEFORE UPDATE ON project
    FOR EACH ROW EXECUTE FUNCTION set_modified_time();
CREATE TRIGGER trg_task_modified_time BEFORE UPDATE ON task
    FOR EACH ROW EXECUTE FUNCTION set_modified_time();
CREATE TRIGGER trg_dependency_modified_time BEFORE UPDATE ON dependency
    FOR EACH ROW EXECUTE FUNCTION set_modified_time();
CREATE TRIGGER trg_attachment_modified_time BEFORE UPDATE ON attachment
    FOR EACH ROW EXECUTE FUNCTION set_modified_time();

-- ---------------------------------------------------------------------------
-- Behaviour: a Remark's authorship can never be reassigned (DomainModel.md
-- Decisions D-DM-7). The Remark's own owner may edit or delete their own
-- Remark (even a ReadOnlyUser owner) or a TeamLeadUser may delete one they
-- don't own; nobody may reassign who created it. Checking that the caller
-- actually *is* the owner (or a TeamLeadUser, for delete) is this API's job
-- (D1.2-3's authorization-lives-in-the-API-layer design) — the database has
-- no notion of "the calling Person" to check that against. All the database
-- enforces is the narrower, caller-independent data-integrity rule below.
-- ---------------------------------------------------------------------------

CREATE OR REPLACE FUNCTION prevent_remark_reassignment() RETURNS trigger AS $$
BEGIN
    IF NEW.created_by_person_id <> OLD.created_by_person_id THEN
        RAISE EXCEPTION 'A Remark''s authorship cannot be reassigned (remark_id = %)', OLD.remark_id;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_remark_no_reassignment BEFORE UPDATE ON remark
    FOR EACH ROW EXECUTE FUNCTION prevent_remark_reassignment();

-- ---------------------------------------------------------------------------
-- Behaviour: reject a Dependency that would introduce a cycle (DomainModel.md's
-- Dependency entity: "the old model detects cycles before allowing a new link").
-- Walks forward from the proposed successor through existing edges; if that
-- walk can reach the proposed predecessor, the new edge would close a loop.
-- ---------------------------------------------------------------------------

CREATE OR REPLACE FUNCTION check_dependency_no_cycle() RETURNS trigger AS $$
DECLARE
    v_pre_kind  char(1);
    v_pre_id    integer;
    v_post_kind char(1);
    v_post_id   integer;
    v_cycle     boolean;
BEGIN
    v_pre_kind  := CASE WHEN NEW.pre_task_id IS NOT NULL THEN 'T' ELSE 'P' END;
    v_pre_id    := COALESCE(NEW.pre_task_id, NEW.pre_project_id);
    v_post_kind := CASE WHEN NEW.post_task_id IS NOT NULL THEN 'T' ELSE 'P' END;
    v_post_id   := COALESCE(NEW.post_task_id, NEW.post_project_id);

    IF v_pre_kind = v_post_kind AND v_pre_id = v_post_id THEN
        RAISE EXCEPTION 'A dependency cannot link an item to itself';
    END IF;

    WITH RECURSIVE reachable(kind, id) AS (
        SELECT v_post_kind, v_post_id
        UNION
        SELECT
            CASE WHEN d.post_task_id IS NOT NULL THEN 'T' ELSE 'P' END::char(1),
            COALESCE(d.post_task_id, d.post_project_id)
        FROM projectpal.dependency d
        JOIN reachable r
          ON (d.pre_task_id IS NOT NULL AND r.kind = 'T' AND d.pre_task_id = r.id)
          OR (d.pre_project_id IS NOT NULL AND r.kind = 'P' AND d.pre_project_id = r.id)
    )
    SELECT EXISTS (SELECT 1 FROM reachable WHERE kind = v_pre_kind AND id = v_pre_id)
    INTO v_cycle;

    IF v_cycle THEN
        RAISE EXCEPTION 'This dependency would create a cycle';
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_dependency_no_cycle
    BEFORE INSERT OR UPDATE ON dependency
    FOR EACH ROW EXECUTE FUNCTION check_dependency_no_cycle();

COMMIT;
