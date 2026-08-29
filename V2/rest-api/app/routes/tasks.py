"""Task, plus Task <-> Person resource assignment (Plan.md §2.1). Create:
LeadUser/TeamLeadUser on the Task's Project's Team. Edit/Delete: Owner
(above ReadOnly) or TeamLeadUser, per Requirements/UseCases.md §12. Moving a
Task to a different Project is rejected if that Project belongs to a
different Team (D-DM-10, Level 1 only — revisit at Level 2). Assigning a
resource requires that Person to hold is_resource on the Task's own Team,
not merely somewhere (D-DM-8).
"""

from fastapi import APIRouter, Depends, HTTPException, status
from pydantic import BaseModel

from app.db import get_conn, many, one
from app.security.deps import (
    CurrentPerson,
    get_current_person,
    require_owner_or_team_lead,
    require_resource_on_team,
    require_role_at_least,
)

router = APIRouter(prefix="/task", tags=["tasks"])

_COLUMNS = (
    "task_id, project_id, component_id, orig_task_number, priority, description, "
    "detailed_description, external_reference_url, requestor_person_id, owner_person_id, "
    "date_added, effort_in_days, effort_type, percentage_allocation, task_type, status, "
    "status_date, tentative_resource_assignment, start_relative_days_to_project"
)


class CreateTaskRequest(BaseModel):
    project_id: int
    description: str
    component_id: int | None = None
    priority: str | None = None
    detailed_description: str | None = None
    external_reference_url: str | None = None
    requestor_person_id: int | None = None
    owner_person_id: int | None = None
    effort_in_days: float | None = None
    effort_type: str | None = None
    percentage_allocation: float | None = None
    task_type: str | None = None
    status: str = "NotStarted"
    tentative_resource_assignment: bool = False
    start_relative_days_to_project: int | None = None


class UpdateTaskRequest(BaseModel):
    project_id: int | None = None
    component_id: int | None = None
    description: str | None = None
    priority: str | None = None
    detailed_description: str | None = None
    external_reference_url: str | None = None
    requestor_person_id: int | None = None
    owner_person_id: int | None = None
    effort_in_days: float | None = None
    effort_type: str | None = None
    percentage_allocation: float | None = None
    task_type: str | None = None
    status: str | None = None
    status_date: str | None = None
    tentative_resource_assignment: bool | None = None
    start_relative_days_to_project: int | None = None


def _get_or_404(conn, task_id: int) -> dict:
    task = one(conn.execute(f"SELECT {_COLUMNS} FROM task WHERE task_id = %s", (task_id,)))
    if task is None:
        raise HTTPException(status.HTTP_404_NOT_FOUND, "No such Task")
    return task


def _project_team_id(conn, project_id: int) -> int:
    row = one(conn.execute("SELECT team_id FROM project WHERE project_id = %s", (project_id,)))
    if row is None:
        raise HTTPException(status.HTTP_400_BAD_REQUEST, "No such Project")
    return row["team_id"]


@router.get("")
def list_tasks(
    project_id: int | None = None,
    component_id: int | None = None,
    status_filter: str | None = None,
    caller: CurrentPerson = Depends(get_current_person),
):
    clauses, params = [], []
    if project_id is not None:
        clauses.append("project_id = %s")
        params.append(project_id)
    if component_id is not None:
        clauses.append("component_id = %s")
        params.append(component_id)
    if status_filter is not None:
        clauses.append("status = %s")
        params.append(status_filter)
    where = f"WHERE {' AND '.join(clauses)}" if clauses else ""
    with get_conn() as conn:
        return many(conn.execute(f"SELECT {_COLUMNS} FROM task {where} ORDER BY task_id", params))


@router.get("/{task_id}")
def get_task(task_id: int, caller: CurrentPerson = Depends(get_current_person)):
    with get_conn() as conn:
        return _get_or_404(conn, task_id)


@router.post("", status_code=status.HTTP_201_CREATED)
def create_task(body: CreateTaskRequest, caller: CurrentPerson = Depends(get_current_person)):
    with get_conn() as conn:
        team_id = _project_team_id(conn, body.project_id)
        require_role_at_least(caller, team_id, "LeadUser")
        fields = body.model_dump()
        columns = ", ".join(fields)
        placeholders = ", ".join(["%s"] * len(fields))
        return one(
            conn.execute(
                f"INSERT INTO task ({columns}) VALUES ({placeholders}) RETURNING {_COLUMNS}",
                list(fields.values()),
            )
        )


@router.patch("/{task_id}")
def update_task(
    task_id: int, body: UpdateTaskRequest, caller: CurrentPerson = Depends(get_current_person)
):
    with get_conn() as conn:
        existing = _get_or_404(conn, task_id)
        team_id = _project_team_id(conn, existing["project_id"])
        require_owner_or_team_lead(caller, owner_person_id=existing["owner_person_id"], team_id=team_id)
        fields = body.model_dump(exclude_unset=True)
        if not fields:
            return existing
        if "project_id" in fields and fields["project_id"] != existing["project_id"]:
            new_team_id = _project_team_id(conn, fields["project_id"])
            if new_team_id != team_id:
                raise HTTPException(
                    status.HTTP_403_FORBIDDEN,
                    "A Task can only move to a Project on its own Team for Level 1 (D-DM-10)",
                )
        set_clause = ", ".join(f"{k} = %s" for k in fields)
        return one(
            conn.execute(
                f"UPDATE task SET {set_clause} WHERE task_id = %s RETURNING {_COLUMNS}",
                (*fields.values(), task_id),
            )
        )


@router.delete("/{task_id}", status_code=status.HTTP_204_NO_CONTENT)
def delete_task(task_id: int, caller: CurrentPerson = Depends(get_current_person)):
    with get_conn() as conn:
        existing = _get_or_404(conn, task_id)
        team_id = _project_team_id(conn, existing["project_id"])
        require_owner_or_team_lead(caller, owner_person_id=existing["owner_person_id"], team_id=team_id)
        conn.execute("DELETE FROM task WHERE task_id = %s", (task_id,))


# --- Resource assignment (task_resource) ---------------------------------


class AssignResourceRequest(BaseModel):
    person_id: int


@router.get("/{task_id}/resources")
def list_resources(task_id: int, caller: CurrentPerson = Depends(get_current_person)):
    with get_conn() as conn:
        _get_or_404(conn, task_id)
        return many(
            conn.execute("SELECT person_id FROM task_resource WHERE task_id = %s", (task_id,))
        )


@router.post("/{task_id}/resources", status_code=status.HTTP_201_CREATED)
def assign_resource(
    task_id: int, body: AssignResourceRequest, caller: CurrentPerson = Depends(get_current_person)
):
    with get_conn() as conn:
        existing = _get_or_404(conn, task_id)
        team_id = _project_team_id(conn, existing["project_id"])
        require_owner_or_team_lead(caller, owner_person_id=existing["owner_person_id"], team_id=team_id)
        require_resource_on_team(body.person_id, team_id, conn)
        conn.execute(
            "INSERT INTO task_resource (task_id, person_id) VALUES (%s, %s)",
            (task_id, body.person_id),
        )
        return {"task_id": task_id, "person_id": body.person_id}


@router.delete("/{task_id}/resources/{person_id}", status_code=status.HTTP_204_NO_CONTENT)
def unassign_resource(
    task_id: int, person_id: int, caller: CurrentPerson = Depends(get_current_person)
):
    with get_conn() as conn:
        existing = _get_or_404(conn, task_id)
        team_id = _project_team_id(conn, existing["project_id"])
        require_owner_or_team_lead(caller, owner_person_id=existing["owner_person_id"], team_id=team_id)
        conn.execute(
            "DELETE FROM task_resource WHERE task_id = %s AND person_id = %s",
            (task_id, person_id),
        )
