"""Component (Plan.md §2.1) — full CRUD including its self-referencing
parent tree. Carries its own team_id (D-DM-6), governing who may
create/edit/delete it — this does *not* restrict which Team's Tasks may
reference it (see attachments.py/tasks.py, which don't check Component's
Team at all). Reparenting can't cross a Team boundary (D-DM-9), same as
Project.
"""

from fastapi import APIRouter, Depends, HTTPException, status
from pydantic import BaseModel

from app.db import get_conn, many, one
from app.security.deps import (
    CurrentPerson,
    get_current_person,
    require_owner_or_team_lead,
    require_role_at_least,
)

router = APIRouter(prefix="/component", tags=["components"])

_COLUMNS = "component_id, parent_component_id, team_id, name, owner_person_id"


class CreateComponentRequest(BaseModel):
    team_id: int
    name: str
    parent_component_id: int | None = None
    owner_person_id: int | None = None


class UpdateComponentRequest(BaseModel):
    parent_component_id: int | None = None
    name: str | None = None
    owner_person_id: int | None = None


def _get_or_404(conn, component_id: int) -> dict:
    component = one(
        conn.execute(f"SELECT {_COLUMNS} FROM component WHERE component_id = %s", (component_id,))
    )
    if component is None:
        raise HTTPException(status.HTTP_404_NOT_FOUND, "No such Component")
    return component


@router.get("")
def list_components(
    team_id: int | None = None,
    parent_component_id: int | None = None,
    caller: CurrentPerson = Depends(get_current_person),
):
    clauses, params = [], []
    if team_id is not None:
        clauses.append("team_id = %s")
        params.append(team_id)
    if parent_component_id is not None:
        clauses.append("parent_component_id = %s")
        params.append(parent_component_id)
    where = f"WHERE {' AND '.join(clauses)}" if clauses else ""
    with get_conn() as conn:
        return many(
            conn.execute(f"SELECT {_COLUMNS} FROM component {where} ORDER BY component_id", params)
        )


@router.get("/{component_id}")
def get_component(component_id: int, caller: CurrentPerson = Depends(get_current_person)):
    with get_conn() as conn:
        return _get_or_404(conn, component_id)


@router.post("", status_code=status.HTTP_201_CREATED)
def create_component(
    body: CreateComponentRequest, caller: CurrentPerson = Depends(get_current_person)
):
    require_role_at_least(caller, body.team_id, "LeadUser")
    with get_conn() as conn:
        if body.parent_component_id is not None:
            parent = _get_or_404(conn, body.parent_component_id)
            if parent["team_id"] != body.team_id:
                raise HTTPException(
                    status.HTTP_403_FORBIDDEN,
                    "Parent Component belongs to a different Team (D-DM-9)",
                )
        return one(
            conn.execute(
                "INSERT INTO component (parent_component_id, team_id, name, owner_person_id) "
                f"VALUES (%s, %s, %s, %s) RETURNING {_COLUMNS}",
                (body.parent_component_id, body.team_id, body.name, body.owner_person_id),
            )
        )


@router.patch("/{component_id}")
def update_component(
    component_id: int,
    body: UpdateComponentRequest,
    caller: CurrentPerson = Depends(get_current_person),
):
    with get_conn() as conn:
        existing = _get_or_404(conn, component_id)
        require_owner_or_team_lead(
            caller, owner_person_id=existing["owner_person_id"], team_id=existing["team_id"]
        )
        fields = body.model_dump(exclude_unset=True)
        if not fields:
            return existing
        if "parent_component_id" in fields and fields["parent_component_id"] is not None:
            parent = _get_or_404(conn, fields["parent_component_id"])
            if parent["team_id"] != existing["team_id"]:
                raise HTTPException(
                    status.HTTP_403_FORBIDDEN,
                    "New parent Component belongs to a different Team (D-DM-9)",
                )
        set_clause = ", ".join(f"{k} = %s" for k in fields)
        return one(
            conn.execute(
                f"UPDATE component SET {set_clause} WHERE component_id = %s RETURNING {_COLUMNS}",
                (*fields.values(), component_id),
            )
        )


@router.delete("/{component_id}", status_code=status.HTTP_204_NO_CONTENT)
def delete_component(component_id: int, caller: CurrentPerson = Depends(get_current_person)):
    with get_conn() as conn:
        existing = _get_or_404(conn, component_id)
        require_owner_or_team_lead(
            caller, owner_person_id=existing["owner_person_id"], team_id=existing["team_id"]
        )
        conn.execute("DELETE FROM component WHERE component_id = %s", (component_id,))
