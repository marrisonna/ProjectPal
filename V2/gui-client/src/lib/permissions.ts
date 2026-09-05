import type { WhoAmI } from "../api/client";

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
