"""POST /auth/login, GET /auth/whoami (Plan.md §4.2/§4.3).

Login does real Argon2id password verification (3_Authentication/Plan.md
§3.3) — the seam it plugs into (JWT shape, every other route authorizing off
the token) was already built and tested in this phase's stub, and doesn't
change here.
"""

from fastapi import APIRouter, Depends, HTTPException, status
from pydantic import BaseModel

from app.db import get_conn, many, one
from app.security.deps import CurrentPerson, get_current_person
from app.security.jwt import encode_token
from app.security.passwords import verify_password

router = APIRouter(prefix="/auth", tags=["auth"])


class LoginRequest(BaseModel):
    external_login: str
    password: str


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
    # Every rejection reason (unknown login, inactive, no password ever set,
    # wrong password) returns the same generic error — deliberately not
    # distinguishable, so this endpoint can't be used to enumerate which
    # logins exist (3_Authentication/Plan.md §3.3).
    invalid = HTTPException(status.HTTP_401_UNAUTHORIZED, "Invalid credentials")
    with get_conn() as conn:
        cur = conn.execute(
            "SELECT person_id, is_active, is_organisation_admin, password_hash "
            "FROM person WHERE external_login = %s",
            (body.external_login,),
        )
        person = one(cur)
        if person is None or not person["is_active"]:
            raise invalid
        if not verify_password(body.password, person["password_hash"]):
            raise invalid
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
