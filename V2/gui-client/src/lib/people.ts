import type { PersonRecord, PersonRoleRecord } from "../api/types";

// Team-scoped display name (D1.4-21): a Person's nickname on the given Team
// (if they have one there) takes priority over their recorded name — e.g.
// showing "Alice" rather than "Alice Chen" wherever a screen's data belongs
// to that Team. Falls back to the plain name when there's no nickname, no
// person_role row for that Team (e.g. Owner/Requestor candidates are listed
// org-wide, not team-scoped — D1.4-15), or no team is known yet.
export function personDisplayName(
  personId: number | null | undefined,
  teamId: number | null | undefined,
  people: PersonRecord[] | undefined,
  personRoles: PersonRoleRecord[] | undefined,
): string {
  if (personId == null) return "";
  const person = people?.find((p) => p.person_id === personId);
  if (!person) return `Person #${personId}`;
  if (teamId != null) {
    const role = personRoles?.find((pr) => pr.person_id === personId && pr.team_id === teamId);
    if (role?.nickname) return role.nickname;
  }
  return person.name;
}
