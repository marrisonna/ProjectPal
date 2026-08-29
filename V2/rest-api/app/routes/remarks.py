"""Remark (Plan.md §2.1) — create, list, edit, delete. Create: anyone who
holds *some* role (even ReadOnlyUser) on the owning item's Team. Edit: the
Remark's own owner only, regardless of role. Delete: the owner, or a
TeamLeadUser on that Team (D-DM-7). The database only enforces that
authorship (created_by_person_id) can never be reassigned
(prevent_remark_reassignment) — the owner/TeamLeadUser check itself is here.
"""

from fastapi import APIRouter, Depends, HTTPException, status
from pydantic import BaseModel

from app.db import get_conn, many, one
from app.security.deps import CurrentPerson, get_current_person

router = APIRouter(prefix="/remark", tags=["remarks"])

_COLUMNS = "remark_id, task_id, project_id, component_id, remark_text, created_by_person_id, created_time"


class CreateRemarkRequest(BaseModel):
    remark_text: str
    task_id: int | None = None
    project_id: int | None = None
    component_id: int | None = None


class UpdateRemarkRequest(BaseModel):
    remark_text: str


def _owning_team_id(conn, *, task_id, project_id, component_id) -> int:
    if task_id is not None:
        row = one(
            conn.execute(
                "SELECT p.team_id FROM task t JOIN project p ON p.project_id = t.project_id "
                "WHERE t.task_id = %s",
                (task_id,),
            )
        )
    elif project_id is not None:
        row = one(conn.execute("SELECT team_id FROM project WHERE project_id = %s", (project_id,)))
    else:
        row = one(conn.execute("SELECT team_id FROM component WHERE component_id = %s", (component_id,)))
    if row is None:
        raise HTTPException(status.HTTP_400_BAD_REQUEST, "No such owning Task/Project/Component")
    return row["team_id"]


def _get_or_404(conn, remark_id: int) -> dict:
    remark = one(conn.execute(f"SELECT {_COLUMNS} FROM remark WHERE remark_id = %s", (remark_id,)))
    if remark is None:
        raise HTTPException(status.HTTP_404_NOT_FOUND, "No such Remark")
    return remark


@router.get("")
def list_remarks(
    task_id: int | None = None,
    project_id: int | None = None,
    component_id: int | None = None,
    caller: CurrentPerson = Depends(get_current_person),
):
    clauses, params = [], []
    if task_id is not None:
        clauses.append("task_id = %s")
        params.append(task_id)
    if project_id is not None:
        clauses.append("project_id = %s")
        params.append(project_id)
    if component_id is not None:
        clauses.append("component_id = %s")
        params.append(component_id)
    where = f"WHERE {' AND '.join(clauses)}" if clauses else ""
    with get_conn() as conn:
        return many(conn.execute(f"SELECT {_COLUMNS} FROM remark {where} ORDER BY created_time", params))


@router.post("", status_code=status.HTTP_201_CREATED)
def create_remark(body: CreateRemarkRequest, caller: CurrentPerson = Depends(get_current_person)):
    with get_conn() as conn:
        team_id = _owning_team_id(
            conn, task_id=body.task_id, project_id=body.project_id, component_id=body.component_id
        )
        if caller.role_on(team_id) is None:
            raise HTTPException(
                status.HTTP_403_FORBIDDEN, "Requires holding some role on this item's Team"
            )
        return one(
            conn.execute(
                "INSERT INTO remark (task_id, project_id, component_id, remark_text, created_by_person_id) "
                f"VALUES (%s, %s, %s, %s, %s) RETURNING {_COLUMNS}",
                (body.task_id, body.project_id, body.component_id, body.remark_text, caller.person_id),
            )
        )


@router.patch("/{remark_id}")
def update_remark(
    remark_id: int, body: UpdateRemarkRequest, caller: CurrentPerson = Depends(get_current_person)
):
    with get_conn() as conn:
        existing = _get_or_404(conn, remark_id)
        if existing["created_by_person_id"] != caller.person_id:
            raise HTTPException(
                status.HTTP_403_FORBIDDEN, "Only the Remark's own owner may edit it"
            )
        return one(
            conn.execute(
                f"UPDATE remark SET remark_text = %s WHERE remark_id = %s RETURNING {_COLUMNS}",
                (body.remark_text, remark_id),
            )
        )


@router.delete("/{remark_id}", status_code=status.HTTP_204_NO_CONTENT)
def delete_remark(remark_id: int, caller: CurrentPerson = Depends(get_current_person)):
    with get_conn() as conn:
        existing = _get_or_404(conn, remark_id)
        if existing["created_by_person_id"] == caller.person_id:
            conn.execute("DELETE FROM remark WHERE remark_id = %s", (remark_id,))
            return
        team_id = _owning_team_id(
            conn,
            task_id=existing["task_id"],
            project_id=existing["project_id"],
            component_id=existing["component_id"],
        )
        if not caller.is_team_lead(team_id):
            raise HTTPException(
                status.HTTP_403_FORBIDDEN,
                "Only the Remark's own owner or a TeamLeadUser on its Team may delete it",
            )
        conn.execute("DELETE FROM remark WHERE remark_id = %s", (remark_id,))
