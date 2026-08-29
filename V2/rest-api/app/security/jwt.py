"""Claim shape per D1-2 (Claude/Level1_Implementation/ImplementationPlan.md):
person_id, Team/role memberships, is_organisation_admin.
"""

import time

import jwt as pyjwt

from app.config import JWT_ALGORITHM, JWT_SECRET, JWT_TTL_SECONDS


def encode_token(*, person_id: int, is_organisation_admin: bool, team_roles: list[dict]) -> str:
    now = int(time.time())
    payload = {
        "sub": str(person_id),
        "person_id": person_id,
        "is_organisation_admin": is_organisation_admin,
        # Each entry: {"team_id": int, "role": str, "is_resource": bool}
        "team_roles": team_roles,
        "iat": now,
        "exp": now + JWT_TTL_SECONDS,
    }
    return pyjwt.encode(payload, JWT_SECRET, algorithm=JWT_ALGORITHM)


def decode_token(token: str) -> dict:
    # Raises jwt.InvalidTokenError (or a subclass) on any problem — expired,
    # bad signature, malformed — which security/deps.py turns into a 401.
    return pyjwt.decode(token, JWT_SECRET, algorithms=[JWT_ALGORITHM])
