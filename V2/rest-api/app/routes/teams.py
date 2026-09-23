"""Team, Person, PersonRole (Plan.md §2.1/§5.1).

Person is is_organisation_admin-only to create/update, with is_active=false
as the primary removal mechanism (D-DM-4) — plus a narrow DELETE, added back
by D-DM-12 (ManagePeoplePlan.md §4.5/§6): rejected unless the Person is
referenced nowhere at all, so it only ever succeeds for one that's never
actually been used for anything (in practice, undoing a just-made mistake,
not general deletion). An admin also can't remove their own
is_organisation_admin flag (ManagePeoplePlan.md §4.6).

PersonRole writes are allowed for is_organisation_admin *or* the target
Team's own TeamLeadUser. Team creation is is_organisation_admin-only and
atomically bootstraps the new Team's first PersonRole, granting some
existing Person TeamLeadUser — and PersonRole's own writes (update/remove)
now guard the same invariant afterwards too (D1.4-87): a Team is never left
leaderless, not just at creation (Requirements/UseCases.md §12).
"""

from fastapi import APIRouter, Depends, HTTPException, status
from pydantic import BaseModel, Field

from app.db import get_conn, many, one
from app.security.deps import CurrentPerson, get_current_person, require_org_admin
from app.security.passwords import hash_password

router = APIRouter(tags=["teams"])


# --- Team ---------------------------------------------------------------


class CreateTeamRequest(BaseModel):
    name: str
    initial_team_lead_person_id: int


class RenameTeamRequest(BaseModel):
    name: str


@router.get("/team")
def list_teams(caller: CurrentPerson = Depends(get_current_person)):
    with get_conn() as conn:
        return many(conn.execute("SELECT team_id, name FROM team ORDER BY team_id"))


@router.post("/team", status_code=status.HTTP_201_CREATED)
def create_team(body: CreateTeamRequest, caller: CurrentPerson = Depends(get_current_person)):
    require_org_admin(caller)
    with get_conn() as conn:
        with conn.transaction():
            team = one(
                conn.execute(
                    "INSERT INTO team (name) VALUES (%s) RETURNING team_id, name",
                    (body.name,),
                )
            )
            conn.execute(
                "INSERT INTO person_role (person_id, team_id, is_resource, role) "
                "VALUES (%s, %s, false, 'TeamLeadUser')",
                (body.initial_team_lead_person_id, team["team_id"]),
            )
        return team


@router.patch("/team/{team_id}")
def rename_team(
    team_id: int, body: RenameTeamRequest, caller: CurrentPerson = Depends(get_current_person)
):
    require_org_admin(caller)
    with get_conn() as conn:
        team = one(
            conn.execute(
                "UPDATE team SET name = %s WHERE team_id = %s RETURNING team_id, name",
                (body.name, team_id),
            )
        )
        if team is None:
            raise HTTPException(status.HTTP_404_NOT_FOUND, "No such Team")
        return team


# --- Person ---------------------------------------------------------------


class CreatePersonRequest(BaseModel):
    name: str
    external_login: str | None = None
    is_organisation_admin: bool = False
    colour: str | None = None


class UpdatePersonRequest(BaseModel):
    name: str | None = None
    external_login: str | None = None
    is_organisation_admin: bool | None = None
    is_active: bool | None = None
    colour: str | None = None


_PERSON_COLUMNS = "person_id, name, is_active, is_organisation_admin, external_login, colour"


@router.get("/person")
def list_people(caller: CurrentPerson = Depends(get_current_person)):
    with get_conn() as conn:
        return many(conn.execute(f"SELECT {_PERSON_COLUMNS} FROM person ORDER BY person_id"))


@router.get("/person/{person_id}")
def get_person(person_id: int, caller: CurrentPerson = Depends(get_current_person)):
    with get_conn() as conn:
        person = one(
            conn.execute(f"SELECT {_PERSON_COLUMNS} FROM person WHERE person_id = %s", (person_id,))
        )
        if person is None:
            raise HTTPException(status.HTTP_404_NOT_FOUND, "No such Person")
        return person


@router.post("/person", status_code=status.HTTP_201_CREATED)
def create_person(body: CreatePersonRequest, caller: CurrentPerson = Depends(get_current_person)):
    require_org_admin(caller)
    with get_conn() as conn:
        return one(
            conn.execute(
                "INSERT INTO person (name, external_login, is_organisation_admin, colour) "
                f"VALUES (%s, %s, %s, %s) RETURNING {_PERSON_COLUMNS}",
                (body.name, body.external_login, body.is_organisation_admin, body.colour),
            )
        )


@router.patch("/person/{person_id}")
def update_person(
    person_id: int, body: UpdatePersonRequest, caller: CurrentPerson = Depends(get_current_person)
):
    require_org_admin(caller)
    fields = body.model_dump(exclude_unset=True)
    if not fields:
        raise HTTPException(status.HTTP_400_BAD_REQUEST, "No fields to update")
    # ManagePeoplePlan.md §4.6/D1.4-83: an admin may demote a *different*
    # admin, but never remove their own flag — client-side alone (disabling
    # the checkbox on your own row) is never sufficient for something this
    # sensitive, so this is the authoritative check.
    if person_id == caller.person_id and fields.get("is_organisation_admin") is False:
        raise HTTPException(
            status.HTTP_400_BAD_REQUEST, "Cannot remove your own is_organisation_admin flag"
        )
    set_clause = ", ".join(f"{k} = %s" for k in fields)
    with get_conn() as conn:
        person = one(
            conn.execute(
                f"UPDATE person SET {set_clause} WHERE person_id = %s RETURNING {_PERSON_COLUMNS}",
                (*fields.values(), person_id),
            )
        )
        if person is None:
            raise HTTPException(status.HTTP_404_NOT_FOUND, "No such Person")
        return person


# ManagePeoplePlan.md §4.5/§6, D-DM-12: every table that can meaningfully
# reference a Person — ownership, authorship, assignment, or Team
# membership — checked before a delete is allowed. `(table, column,
# description)`; `description` feeds directly into the rejection message,
# matching V1.2's own "here's exactly what's still attached" UX
# (FormManagePeople.cs's buttonDeletePerson_Click).
_PERSON_REFERENCE_CHECKS = [
    ("task", "owner_person_id", "owns {n} Task(s)"),
    ("task", "requestor_person_id", "is the requestor on {n} Task(s)"),
    ("task_resource", "person_id", "is assigned as a Resource on {n} Task(s)"),
    ("project", "owner_person_id", "owns {n} Project(s)"),
    ("component", "owner_person_id", "owns {n} Component(s)"),
    ("remark", "created_by_person_id", "authored {n} Remark(s)"),
    ("attachment", "owner_person_id", "owns {n} Attachment(s)"),
    ("person_role", "person_id", "is a member of {n} Team(s)"),
]

# `modified_by` (person/component/project/task/dependency/attachment) is
# pure audit metadata — who last touched a row, not an ownership/assignment
# relationship — and isn't written by any route in this API today (nothing
# ever sets it), but the column is nullable and genuinely FK-referenced, so
# a delete would still fail at the database level if it were ever populated
# by something else later. Defensively cleared, not treated as blocking,
# rather than assumed to always be empty.
_PERSON_MODIFIED_BY_TABLES = ["person", "component", "project", "task", "dependency", "attachment"]


@router.delete("/person/{person_id}", status_code=status.HTTP_204_NO_CONTENT)
def delete_person(person_id: int, caller: CurrentPerson = Depends(get_current_person)):
    require_org_admin(caller)
    with get_conn() as conn:
        blocking = []
        for table, column, description in _PERSON_REFERENCE_CHECKS:
            row = one(conn.execute(f"SELECT count(*) AS n FROM {table} WHERE {column} = %s", (person_id,)))
            count = row["n"] if row else 0
            if count > 0:
                blocking.append(description.format(n=count))
        if blocking:
            raise HTTPException(
                status.HTTP_409_CONFLICT,
                "Cannot delete this Person because they: " + "; ".join(blocking),
            )
        with conn.transaction():
            for table in _PERSON_MODIFIED_BY_TABLES:
                conn.execute(f"UPDATE {table} SET modified_by = NULL WHERE modified_by = %s", (person_id,))
            deleted = one(
                conn.execute("DELETE FROM person WHERE person_id = %s RETURNING person_id", (person_id,))
            )
        if deleted is None:
            raise HTTPException(status.HTTP_404_NOT_FOUND, "No such Person")


class SetPasswordRequest(BaseModel):
    new_password: str = Field(min_length=8)


@router.post("/person/{person_id}/password", status_code=status.HTTP_204_NO_CONTENT)
def set_person_password(
    person_id: int, body: SetPasswordRequest, caller: CurrentPerson = Depends(get_current_person)
):
    """Admin-set only for Level 1 (3_Authentication/Plan.md D1.3-4) — a
    separate endpoint from PATCH /person/{id} rather than a field on it, so
    the general-purpose Person update route has no path to touching
    credentials at all. Self-service (a Person setting their own password)
    is deferred — see Claude/Level2_Implementation/Scope.md.
    """
    require_org_admin(caller)
    with get_conn() as conn:
        updated = one(
            conn.execute(
                "UPDATE person SET password_hash = %s WHERE person_id = %s RETURNING person_id",
                (hash_password(body.new_password), person_id),
            )
        )
        if updated is None:
            raise HTTPException(status.HTTP_404_NOT_FOUND, "No such Person")


# --- PersonRole ---------------------------------------------------------


class WritePersonRoleRequest(BaseModel):
    person_id: int
    team_id: int
    is_resource: bool = False
    role: str = "NormalUser"
    nickname: str | None = None


def _require_admin_or_team_lead(caller: CurrentPerson, team_id: int) -> None:
    if caller.is_organisation_admin or caller.is_team_lead(team_id):
        return
    raise HTTPException(
        status.HTTP_403_FORBIDDEN,
        "Requires is_organisation_admin or TeamLeadUser on this Team",
    )


def _would_leave_team_leaderless(conn, team_id: int, excluding_person_id: int) -> bool:
    """True if `team_id` would have no remaining TeamLeadUser once
    `excluding_person_id`'s own row no longer counts as one — either
    because it's being demoted to a different role, or removed outright.
    ManagePeoplePlan.md §5.6/D1.4-87: `teams.py`'s own stated invariant ("a
    Team is never left leaderless") was previously only enforced at Team
    *creation* (`create_team`'s own atomic bootstrap) — nothing stopped a
    Team being edited down to zero afterwards.
    """
    row = one(
        conn.execute(
            "SELECT count(*) AS n FROM person_role "
            "WHERE team_id = %s AND role = 'TeamLeadUser' AND person_id != %s",
            (team_id, excluding_person_id),
        )
    )
    return (row["n"] if row else 0) == 0


@router.get("/person-role")
def list_person_roles(
    team_id: int | None = None, caller: CurrentPerson = Depends(get_current_person)
):
    with get_conn() as conn:
        if team_id is not None:
            return many(
                conn.execute(
                    "SELECT person_id, team_id, is_resource, role, nickname FROM person_role "
                    "WHERE team_id = %s ORDER BY person_id",
                    (team_id,),
                )
            )
        return many(
            conn.execute(
                "SELECT person_id, team_id, is_resource, role, nickname FROM person_role "
                "ORDER BY team_id, person_id"
            )
        )


@router.post("/person-role", status_code=status.HTTP_201_CREATED)
def add_person_role(
    body: WritePersonRoleRequest, caller: CurrentPerson = Depends(get_current_person)
):
    _require_admin_or_team_lead(caller, body.team_id)
    with get_conn() as conn:
        return one(
            conn.execute(
                "INSERT INTO person_role (person_id, team_id, is_resource, role, nickname) "
                "VALUES (%s, %s, %s, %s, %s) "
                "RETURNING person_id, team_id, is_resource, role, nickname",
                (body.person_id, body.team_id, body.is_resource, body.role, body.nickname),
            )
        )


class UpdatePersonRoleRequest(BaseModel):
    is_resource: bool | None = None
    role: str | None = None
    nickname: str | None = None


@router.patch("/person-role/{person_id}/{team_id}")
def update_person_role(
    person_id: int,
    team_id: int,
    body: UpdatePersonRoleRequest,
    caller: CurrentPerson = Depends(get_current_person),
):
    _require_admin_or_team_lead(caller, team_id)
    fields = body.model_dump(exclude_unset=True)
    if not fields:
        raise HTTPException(status.HTTP_400_BAD_REQUEST, "No fields to update")
    set_clause = ", ".join(f"{k} = %s" for k in fields)
    with get_conn() as conn:
        if fields.get("role") is not None and fields["role"] != "TeamLeadUser":
            current = one(
                conn.execute(
                    "SELECT role FROM person_role WHERE person_id = %s AND team_id = %s",
                    (person_id, team_id),
                )
            )
            if (
                current is not None
                and current["role"] == "TeamLeadUser"
                and _would_leave_team_leaderless(conn, team_id, person_id)
            ):
                raise HTTPException(
                    status.HTTP_400_BAD_REQUEST,
                    "Cannot change this Team's only TeamLeadUser to a different role",
                )
        row = one(
            conn.execute(
                f"UPDATE person_role SET {set_clause} WHERE person_id = %s AND team_id = %s "
                "RETURNING person_id, team_id, is_resource, role, nickname",
                (*fields.values(), person_id, team_id),
            )
        )
        if row is None:
            raise HTTPException(status.HTTP_404_NOT_FOUND, "No such PersonRole")
        return row


@router.delete("/person-role/{person_id}/{team_id}", status_code=status.HTTP_204_NO_CONTENT)
def remove_person_role(
    person_id: int, team_id: int, caller: CurrentPerson = Depends(get_current_person)
):
    _require_admin_or_team_lead(caller, team_id)
    with get_conn() as conn:
        current = one(
            conn.execute(
                "SELECT role FROM person_role WHERE person_id = %s AND team_id = %s",
                (person_id, team_id),
            )
        )
        if (
            current is not None
            and current["role"] == "TeamLeadUser"
            and _would_leave_team_leaderless(conn, team_id, person_id)
        ):
            raise HTTPException(
                status.HTTP_400_BAD_REQUEST, "Cannot remove this Team's only TeamLeadUser"
            )
        conn.execute(
            "DELETE FROM person_role WHERE person_id = %s AND team_id = %s",
            (person_id, team_id),
        )
