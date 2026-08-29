"""Dependency (Plan.md §2.1) — create/delete only; the cycle-rejection rule
is enforced by 1_DatabaseSetup's check_dependency_no_cycle trigger, so this
route just surfaces that cleanly (errors.py), not re-implements it.

Authorization: DomainModel.md's Dependency entity says a Dependency is
"governed by the owning Task/Project's Edit permission" but doesn't say
what that means when the two sides belong to different owners/Teams. This
implementation requires the caller to hold Edit rights (Owner-above-ReadOnly
or TeamLeadUser) on *both* sides — you shouldn't be able to link an item you
can't edit, on either end.
"""

from fastapi import APIRouter, Depends, HTTPException, status
from pydantic import BaseModel

from app.db import get_conn, many, one
from app.security.deps import CurrentPerson, get_current_person, require_owner_or_team_lead

router = APIRouter(prefix="/dependency", tags=["dependencies"])


class CreateDependencyRequest(BaseModel):
    pre_task_id: int | None = None
    pre_project_id: int | None = None
    post_task_id: int | None = None
    post_project_id: int | None = None


def _side_owner_and_team(conn, *, task_id: int | None, project_id: int | None) -> tuple[int | None, int]:
    if task_id is not None:
        row = one(
            conn.execute(
                "SELECT t.owner_person_id, p.team_id FROM task t "
                "JOIN project p ON p.project_id = t.project_id WHERE t.task_id = %s",
                (task_id,),
            )
        )
        if row is None:
            raise HTTPException(status.HTTP_400_BAD_REQUEST, "No such Task")
        return row["owner_person_id"], row["team_id"]
    row = one(conn.execute("SELECT owner_person_id, team_id FROM project WHERE project_id = %s", (project_id,)))
    if row is None:
        raise HTTPException(status.HTTP_400_BAD_REQUEST, "No such Project")
    return row["owner_person_id"], row["team_id"]


@router.get("")
def list_dependencies(
    task_id: int | None = None,
    project_id: int | None = None,
    caller: CurrentPerson = Depends(get_current_person),
):
    clauses, params = [], []
    if task_id is not None:
        clauses.append("(pre_task_id = %s OR post_task_id = %s)")
        params.extend([task_id, task_id])
    if project_id is not None:
        clauses.append("(pre_project_id = %s OR post_project_id = %s)")
        params.extend([project_id, project_id])
    where = f"WHERE {' AND '.join(clauses)}" if clauses else ""
    with get_conn() as conn:
        return many(
            conn.execute(
                "SELECT dependency_id, pre_task_id, pre_project_id, post_task_id, post_project_id "
                f"FROM dependency {where} ORDER BY dependency_id",
                params,
            )
        )


@router.post("", status_code=status.HTTP_201_CREATED)
def create_dependency(
    body: CreateDependencyRequest, caller: CurrentPerson = Depends(get_current_person)
):
    with get_conn() as conn:
        pre_owner, pre_team = _side_owner_and_team(
            conn, task_id=body.pre_task_id, project_id=body.pre_project_id
        )
        post_owner, post_team = _side_owner_and_team(
            conn, task_id=body.post_task_id, project_id=body.post_project_id
        )
        require_owner_or_team_lead(caller, owner_person_id=pre_owner, team_id=pre_team)
        require_owner_or_team_lead(caller, owner_person_id=post_owner, team_id=post_team)
        return one(
            conn.execute(
                "INSERT INTO dependency (pre_task_id, pre_project_id, post_task_id, post_project_id) "
                "VALUES (%s, %s, %s, %s) "
                "RETURNING dependency_id, pre_task_id, pre_project_id, post_task_id, post_project_id",
                (body.pre_task_id, body.pre_project_id, body.post_task_id, body.post_project_id),
            )
        )


@router.delete("/{dependency_id}", status_code=status.HTTP_204_NO_CONTENT)
def delete_dependency(dependency_id: int, caller: CurrentPerson = Depends(get_current_person)):
    with get_conn() as conn:
        dep = one(
            conn.execute(
                "SELECT pre_task_id, pre_project_id, post_task_id, post_project_id "
                "FROM dependency WHERE dependency_id = %s",
                (dependency_id,),
            )
        )
        if dep is None:
            raise HTTPException(status.HTTP_404_NOT_FOUND, "No such Dependency")
        pre_owner, pre_team = _side_owner_and_team(
            conn, task_id=dep["pre_task_id"], project_id=dep["pre_project_id"]
        )
        post_owner, post_team = _side_owner_and_team(
            conn, task_id=dep["post_task_id"], project_id=dep["post_project_id"]
        )
        require_owner_or_team_lead(caller, owner_person_id=pre_owner, team_id=pre_team)
        require_owner_or_team_lead(caller, owner_person_id=post_owner, team_id=post_team)
        conn.execute("DELETE FROM dependency WHERE dependency_id = %s", (dependency_id,))
