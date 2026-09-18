import type { WhoAmI } from "../api/client";
import { TASK_STATUSES } from "../api/types";

const ROLE_RANK: Record<string, number> = {
  ReadOnlyUser: 0,
  NormalUser: 1,
  LeadUser: 2,
  TeamLeadUser: 3,
};

function roleOn(person: WhoAmI, teamId: number): string | null {
  return person.team_roles.find((tr) => tr.team_id === teamId)?.role ?? null;
}

export function hasRoleAtLeast(
  person: WhoAmI | null,
  teamId: number | null | undefined,
  minimum: string,
): boolean {
  if (!person || teamId == null) return false;
  const role = roleOn(person, teamId);
  return role != null && ROLE_RANK[role] >= ROLE_RANK[minimum];
}

export function isTeamLead(person: WhoAmI | null, teamId: number | null | undefined): boolean {
  if (!person || teamId == null) return false;
  return roleOn(person, teamId) === "TeamLeadUser";
}

/**
 * TeamLeadUser on *some* Team, regardless of which — for a screen that
 * isn't scoped to one Team/Task and so has no single `teamId` to check
 * `isTeamLead` against (the All Tasks grid's "T" column visibility and its
 * Resources filter default, D-Win-15). Mirrors V1.2's own equivalent
 * check, `Permissions.IsSuperUser` in `MainWindow.cs`'s
 * `buttonShowTasks_Click` — a single global flag there since V1.2 had no
 * per-Team roles at all; V2 has to fold its own per-Team roles into one
 * yes/no answer instead.
 */
export function isTeamLeadOfAnyTeam(person: WhoAmI | null): boolean {
  if (!person) return false;
  return person.team_roles.some((tr) => tr.role === "TeamLeadUser");
}

/**
 * Mirrors rest-api/app/security/deps.py's require_owner_or_team_lead
 * exactly — the Task/Project/Component/Attachment edit-or-delete rule
 * (Requirements/UseCases.md §12, D-UC-4): the record's owner (any role
 * above ReadOnly), or a TeamLeadUser on the record's own Team. Used so the
 * GUI can decide up front whether to show edit controls at all, rather
 * than only finding out from a 403 after the user has already tried to
 * save. Deliberately no is_organisation_admin bypass — the server-side
 * check doesn't have one either, so replicating one here would make the
 * GUI show controls a save would then reject.
 */
export function canEditOwnedRecord(
  person: WhoAmI | null,
  teamId: number | null | undefined,
  ownerPersonId: number | null | undefined,
): boolean {
  if (!person || teamId == null) return false;
  if (isTeamLead(person, teamId)) return true;
  return ownerPersonId === person.person_id && hasRoleAtLeast(person, teamId, "NormalUser");
}

/**
 * TaskGrid's/Task Detail's shared, centralised field-permission rule
 * (TaskGridPlan.md §4.3, D1.4-48/51/52) — "what's editable" is never
 * decided per embedding window, only here, so a user's experience of what
 * they can edit is identical everywhere a governed Task field appears.
 */
export type EditableTaskField =
  | "status"
  | "tentative_resource_assignment"
  | "priority"
  | "owner_person_id"
  | "effort_in_days"
  | "percentage_allocation"
  | "task_type"
  | "requestor_person_id"
  | "detailed_description";

// Fields a Task's own assigned Resource may edit even when they don't own
// it and aren't a Team Lead — V1.2's own third tier (GUITask.cs's
// NormalUserEditableColumns), confirmed as wanted behaviour, not legacy
// cruft (D1.4-52).
const RESOURCE_EDITABLE_FIELDS: readonly EditableTaskField[] = ["status", "detailed_description"];

export function canEditTaskField(
  person: WhoAmI | null,
  teamId: number | null | undefined,
  ownerPersonId: number | null | undefined,
  isAssignedResource: boolean,
  field: EditableTaskField,
): boolean {
  // Owner reassignment, and toggling Tentative Resource Assignment, are
  // both reserved for LeadUser+ specifically, not just "the record's own
  // owner" — matching V1.2's own real precedent exactly, not an
  // independently-invented rule (D1.4-51: V1.2's `GUITaskColumns.
  // ColumnIsReadOnly` restricts both of these columns identically, to
  // SuperUser/PowerUser only — V2's TeamLeadUser/LeadUser).
  if (field === "owner_person_id" || field === "tentative_resource_assignment") {
    return hasRoleAtLeast(person, teamId, "LeadUser");
  }
  if (canEditOwnedRecord(person, teamId, ownerPersonId)) return true;
  // Tier 3 (D1.4-52): not the owner, not a Team Lead, but assigned to the
  // Task as a Resource — Status and Detailed Description only.
  return isAssignedResource && RESOURCE_EDITABLE_FIELDS.includes(field);
}

// V1.2's own further restriction on top of the above (GUITaskColumns.
// AdjustComboEditor, D1.4-52): tier 3's own Status edit can move a Task
// along, but can't close it out — Closed/Cancelled stay off the menu
// unless the editor has full (owner-or-TeamLeadUser) rights.
export function editableTaskStatusValues(
  person: WhoAmI | null,
  teamId: number | null | undefined,
  ownerPersonId: number | null | undefined,
): readonly string[] {
  if (canEditOwnedRecord(person, teamId, ownerPersonId)) return TASK_STATUSES;
  return TASK_STATUSES.filter((s) => s !== "Closed" && s !== "Cancelled");
}
