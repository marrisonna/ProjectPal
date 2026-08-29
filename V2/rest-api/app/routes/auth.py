"""POST /auth/login, GET /auth/whoami (Plan.md §4.2/§4.3).

Login is a stub this phase: it mints a valid JWT for the named Person
without checking a password at all. Phase 3 (3_Authentication) replaces the
body of login() with real password verification — nothing else about the
seam changes, since every other route already authorizes off the token,
not off how it was obtained.
"""

from fastapi import APIRouter, Depends, HTTPException, status
from pydantic import BaseModel

from app.db import get_conn, many, one
from app.security.deps import CurrentPerson, get_current_person
from app.security.jwt import encode_token

router = APIRouter(prefix="/auth", tags=["auth"])


class LoginRequest(BaseModel):
    external_login: str


class LoginResponse(BaseModel):
    token: str


def _team_roles_for(conn, person_id: int) -> list[dict]:
    cur = conn.execute(
        "SELECT team_id, role, is_resource FROM person_role WHERE person_id = %s",
        (person_id,),
    )
    return many(cur)


@router.post("/login", response_model=LoginResponse)
def login(body: LoginRequest):
    with get_conn() as conn:
        cur = conn.execute(
            "SELECT person_id, is_active, is_organisation_admin "
            "FROM person WHERE external_login = %s",
            (body.external_login,),
        )
        person = one(cur)
        if person is None or not person["is_active"]:
            raise HTTPException(status.HTTP_401_UNAUTHORIZED, "Unknown or inactive login")
        team_roles = _team_roles_for(conn, person["person_id"])
    token = encode_token(
        person_id=person["person_id"],
        is_organisation_admin=person["is_organisation_admin"],
        team_roles=team_roles,
    )
    return LoginResponse(token=token)


@router.get("/whoami")
def whoami(caller: CurrentPerson = Depends(get_current_person)):
    return {
        "person_id": caller.person_id,
        "is_organisation_admin": caller.is_organisation_admin,
        "team_roles": caller.team_roles,
    }
