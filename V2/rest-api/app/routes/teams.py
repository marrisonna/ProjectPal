"""Team, Person, PersonRole (Plan.md §2.1/§5.1).

Person is is_organisation_admin-only to create/update, with no delete route
ever — is_active=false is the only "removal" (D-DM-4). PersonRole writes are
allowed for is_organisation_admin *or* the target Team's own TeamLeadUser.
Team creation is is_organisation_admin-only and atomically bootstraps the
new Team's first PersonRole, granting some existing Person TeamLeadUser —
a Team is never left leaderless (Requirements/UseCases.md §12).
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


def _require_admin_or_team_lead(caller: CurrentPerson, team_id: int) -> None:
    if caller.is_organisation_admin or caller.is_team_lead(team_id):
        return
    raise HTTPException(
        status.HTTP_403_FORBIDDEN,
        "Requires is_organisation_admin or TeamLeadUser on this Team",
    )


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
                "INSERT INTO person_role (person_id, team_id, is_resource, role) "
                "VALUES (%s, %s, %s, %s) "
                "RETURNING person_id, team_id, is_resource, role, nickname",
                (body.person_id, body.team_id, body.is_resource, body.role),
            )
        )


class UpdatePersonRoleRequest(BaseModel):
    is_resource: bool | None = None
    role: str | None = None


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
        conn.execute(
            "DELETE FROM person_role WHERE person_id = %s AND team_id = %s",
            (person_id, team_id),
        )
