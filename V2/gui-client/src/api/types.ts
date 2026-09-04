// These mirror rest-api's actual SELECT column lists (app/routes/*.py) and
// the task/project/component/person table definitions
// (database/migrations/001_initial_schema.sql) — most GET responses have no
// response_model server-side, so openapi-typescript can't infer them (see
// client.ts's WhoAmI/Team for the same situation).

export interface TaskRecord {
  task_id: number;
  project_id: number;
  component_id: number | null;
  orig_task_number: string | null;
  priority: string | null;
  description: string;
  detailed_description: string | null;
  external_reference_url: string | null;
  requestor_person_id: number | null;
  owner_person_id: number | null;
  date_added: string;
  effort_in_days: number | null;
  effort_type: string | null;
  percentage_allocation: number | null;
  task_type: string | null;
  status: string;
  status_date: string | null;
  tentative_resource_assignment: boolean;
  start_relative_days_to_project: number | null;
}

export interface ProjectRecord {
  project_id: number;
  parent_project_id: number | null;
  team_id: number;
  name: string;
  priority: string | null;
  detailed_description: string | null;
  owner_person_id: number | null;
  start_date: string | null;
  due_date: string | null;
}

export interface ComponentRecord {
  component_id: number;
  parent_component_id: number | null;
  team_id: number;
  name: string;
  owner_person_id: number | null;
}

export interface PersonRecord {
  person_id: number;
  name: string;
  is_active: boolean;
  is_organisation_admin: boolean;
  external_login: string;
  colour: string | null;
}

// GET /person-role has no response_model server-side either (rest-api/app/
// routes/teams.py's list_person_roles) — typed from its actual SELECT.
export interface PersonRoleRecord {
  person_id: number;
  team_id: number;
  is_resource: boolean;
  role: string;
  // A shorter name this Person is known by on this Team — shown instead of
  // person.name wherever the GUI displays a name in this Team's context
  // (D1.4-21). Read-only in Level 1: only ever set via seed data for now.
  nickname: string | null;
}

export interface RemarkRecord {
  remark_id: number;
  task_id: number | null;
  project_id: number | null;
  component_id: number | null;
  remark_text: string;
  created_by_person_id: number;
  created_time: string;
}

export interface DependencyRecord {
  dependency_id: number;
  pre_task_id: number | null;
  pre_project_id: number | null;
  post_task_id: number | null;
  post_project_id: number | null;
}

export interface AttachmentRecord {
  attachment_id: number;
  task_id: number | null;
  project_id: number | null;
  component_id: number | null;
  name: string;
  kind: "File" | "Mail" | "Link";
  url: string | null;
  size_bytes: number | null;
  created_time: string;
  owner_person_id: number;
}

// database/migrations/001_initial_schema.sql's enum definitions — kept here
// rather than fetched, since they're part of the schema, not tenant data.
//
// Ordered to match V1.2's actual dropdown order (GUITaskColumns.cs's
// GetComboValues_static), not the schema's declaration order — dropdown
// order is a UI decision carried over deliberately, not incidental
// (4_GuiClient/Plan.md D1.4-15). Status and Task Type map name-for-name to
// V1.2's values; Priority's V1.2 names (VHigh/High/Med/Low/VLow) don't match
// V2's five active levels 1:1 by name, so this is the same 5-level
// most-urgent-first ordering with V2's own (renamed) values — see
// 8_ValidationAndVerification/Plan.md Q1.8-4 for confirming that mapping.
export const PRIORITY_LEVELS = [
  "High",
  "MedHigh",
  "Med",
  "MedLow",
  "Low",
  "Cancelled",
  "Closed",
] as const;

export const TASK_STATUSES = [
  "Closed",
  "Cancelled",
  "InProgress",
  "NotStarted",
  "Ready",
  "Support",
  "Tentative",
] as const;

export const TASK_TYPES = [
  "Enhancement",
  "Maintenance",
  "NewDevelopment",
  "Other",
  "Support",
  "Infrastructure",
] as const;

// "PersonDays" is V2's gender-neutral rename of V1.2's "ManDays"
// (4_GuiClient/Plan.md D1.4-16).
export const EFFORT_TYPES = ["PersonDays", "Duration"] as const;
