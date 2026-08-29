"""Project (Plan.md §2.1) — full CRUD including its self-referencing parent
tree. Create: LeadUser/TeamLeadUser on the target Team. Edit: Owner (above
ReadOnly) or TeamLeadUser. Delete: TeamLeadUser only, not even the owner
(Requirements/UseCases.md §12). Reparenting can't cross a Team boundary
(D-DM-9).
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

router = APIRouter(prefix="/project", tags=["projects"])

_COLUMNS = (
    "project_id, parent_project_id, team_id, name, priority, detailed_description, "
    "owner_person_id, start_date, due_date"
)


class CreateProjectRequest(BaseModel):
    team_id: int
    name: str
    parent_project_id: int | None = None
    priority: str | None = None
    detailed_description: str | None = None
    owner_person_id: int | None = None
    start_date: str | None = None
    due_date: str | None = None


class UpdateProjectRequest(BaseModel):
    parent_project_id: int | None = None
    name: str | None = None
    priority: str | None = None
    detailed_description: str | None = None
    owner_person_id: int | None = None
    start_date: str | None = None
    due_date: str | None = None


def _get_or_404(conn, project_id: int) -> dict:
    project = one(conn.execute(f"SELECT {_COLUMNS} FROM project WHERE project_id = %s", (project_id,)))
    if project is None:
        raise HTTPException(status.HTTP_404_NOT_FOUND, "No such Project")
    return project


@router.get("")
def list_projects(
    team_id: int | None = None,
    parent_project_id: int | None = None,
    caller: CurrentPerson = Depends(get_current_person),
):
    clauses, params = [], []
    if team_id is not None:
        clauses.append("team_id = %s")
        params.append(team_id)
    if parent_project_id is not None:
        clauses.append("parent_project_id = %s")
        params.append(parent_project_id)
    where = f"WHERE {' AND '.join(clauses)}" if clauses else ""
    with get_conn() as conn:
        return many(
            conn.execute(
                f"SELECT {_COLUMNS} FROM project {where} ORDER BY project_id", params
            )
        )


@router.get("/{project_id}")
def get_project(project_id: int, caller: CurrentPerson = Depends(get_current_person)):
    with get_conn() as conn:
        return _get_or_404(conn, project_id)


@router.post("", status_code=status.HTTP_201_CREATED)
def create_project(
    body: CreateProjectRequest, caller: CurrentPerson = Depends(get_current_person)
):
    require_role_at_least(caller, body.team_id, "LeadUser")
    with get_conn() as conn:
        if body.parent_project_id is not None:
            parent = _get_or_404(conn, body.parent_project_id)
            if parent["team_id"] != body.team_id:
                raise HTTPException(
                    status.HTTP_403_FORBIDDEN,
                    "Parent Project belongs to a different Team (D-DM-9)",
                )
        return one(
            conn.execute(
                "INSERT INTO project (parent_project_id, team_id, name, priority, "
                "detailed_description, owner_person_id, start_date, due_date) "
                f"VALUES (%s, %s, %s, %s, %s, %s, %s, %s) RETURNING {_COLUMNS}",
                (
                    body.parent_project_id,
                    body.team_id,
                    body.name,
                    body.priority,
                    body.detailed_description,
                    body.owner_person_id,
                    body.start_date,
                    body.due_date,
                ),
            )
        )


@router.patch("/{project_id}")
def update_project(
    project_id: int,
    body: UpdateProjectRequest,
    caller: CurrentPerson = Depends(get_current_person),
):
    with get_conn() as conn:
        existing = _get_or_404(conn, project_id)
        require_owner_or_team_lead(
            caller, owner_person_id=existing["owner_person_id"], team_id=existing["team_id"]
        )
        fields = body.model_dump(exclude_unset=True)
        if not fields:
            return existing
        if "parent_project_id" in fields and fields["parent_project_id"] is not None:
            parent = _get_or_404(conn, fields["parent_project_id"])
            if parent["team_id"] != existing["team_id"]:
                raise HTTPException(
                    status.HTTP_403_FORBIDDEN,
                    "New parent Project belongs to a different Team (D-DM-9)",
                )
        set_clause = ", ".join(f"{k} = %s" for k in fields)
        return one(
            conn.execute(
                f"UPDATE project SET {set_clause} WHERE project_id = %s RETURNING {_COLUMNS}",
                (*fields.values(), project_id),
            )
        )


@router.delete("/{project_id}", status_code=status.HTTP_204_NO_CONTENT)
def delete_project(project_id: int, caller: CurrentPerson = Depends(get_current_person)):
    with get_conn() as conn:
        existing = _get_or_404(conn, project_id)
        if not caller.is_team_lead(existing["team_id"]):
            raise HTTPException(
                status.HTTP_403_FORBIDDEN,
                "Only TeamLeadUser may delete a Project, not even its owner",
            )
        conn.execute("DELETE FROM project WHERE project_id = %s", (project_id,))
