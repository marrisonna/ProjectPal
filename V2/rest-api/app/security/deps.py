"""The authentication seam (Plan.md §4.3) and the Team/role authorization
helpers every route uses (D-UC-4: every check is against the caller's role
*on the specific Team that owns the resource*, not any role held elsewhere).
"""

from dataclasses import dataclass

import jwt as pyjwt
from fastapi import Depends, HTTPException, status
from fastapi.security import HTTPAuthorizationCredentials, HTTPBearer

from app.security.jwt import decode_token

_bearer = HTTPBearer(auto_error=False)

_ROLE_RANK = {"ReadOnlyUser": 0, "NormalUser": 1, "LeadUser": 2, "TeamLeadUser": 3}


@dataclass(frozen=True)
class CurrentPerson:
    person_id: int
    is_organisation_admin: bool
    team_roles: list[dict]  # [{"team_id": int, "role": str, "is_resource": bool}, ...]

    def role_on(self, team_id: int) -> str | None:
        for tr in self.team_roles:
            if tr["team_id"] == team_id:
                return tr["role"]
        return None

    def is_resource_on(self, team_id: int) -> bool:
        for tr in self.team_roles:
            if tr["team_id"] == team_id:
                return tr["is_resource"]
        return False

    def has_role_at_least(self, team_id: int, minimum: str) -> bool:
        role = self.role_on(team_id)
        if role is None:
            return False
        return _ROLE_RANK[role] >= _ROLE_RANK[minimum]

    def is_team_lead(self, team_id: int) -> bool:
        return self.role_on(team_id) == "TeamLeadUser"


def get_current_person(
    credentials: HTTPAuthorizationCredentials | None = Depends(_bearer),
) -> CurrentPerson:
    if credentials is None:
        raise HTTPException(status.HTTP_401_UNAUTHORIZED, "Missing bearer token")
    try:
        claims = decode_token(credentials.credentials)
    except pyjwt.InvalidTokenError:
        raise HTTPException(status.HTTP_401_UNAUTHORIZED, "Invalid or expired token")
    return CurrentPerson(
        person_id=claims["person_id"],
        is_organisation_admin=claims["is_organisation_admin"],
        team_roles=claims["team_roles"],
    )


def require_org_admin(caller: CurrentPerson) -> None:
    if not caller.is_organisation_admin:
        raise HTTPException(status.HTTP_403_FORBIDDEN, "Requires is_organisation_admin")


def require_role_at_least(caller: CurrentPerson, team_id: int, minimum: str) -> None:
    if not caller.has_role_at_least(team_id, minimum):
        raise HTTPException(
            status.HTTP_403_FORBIDDEN,
            f"Requires at least {minimum} on team {team_id}",
        )


def require_owner_or_team_lead(
    caller: CurrentPerson, *, owner_person_id: int | None, team_id: int
) -> None:
    """The recurring 'Owner (any role above ReadOnly), or TeamLeadUser' rule
    (Requirements/UseCases.md §12's table) used for editing/deleting Task,
    Project, Component, and Attachment.
    """
    if caller.is_team_lead(team_id):
        return
    if owner_person_id == caller.person_id and caller.has_role_at_least(team_id, "NormalUser"):
        return
    raise HTTPException(
        status.HTTP_403_FORBIDDEN,
        "Requires being the owner (above ReadOnly) or TeamLeadUser on this Team",
    )


def require_resource_on_team(caller_person_id: int, team_id: int, conn) -> None:
    """D-DM-8: a Person is assignable as a Task resource only if they hold
    is_resource=true via PersonRole on the *same* Team as the Task's Project.
    """
    row = conn.execute(
        "SELECT is_resource FROM person_role WHERE person_id = %s AND team_id = %s",
        (caller_person_id, team_id),
    ).fetchone()
    if row is None or not row[0]:
        raise HTTPException(
            status.HTTP_403_FORBIDDEN,
            f"Person {caller_person_id} is not a resource on team {team_id}",
        )
