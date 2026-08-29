"""Attachment (Plan.md §2.1) — upload/list/download, File and Link kinds
only for Level 1 (D1-4; Mail is deferred). content_hash/size_bytes are
computed here, before insert — the dedup rule itself is enforced by
1_DatabaseSetup's ux_attachment_dedup unique index (errors.py surfaces it).
Create: anyone above ReadOnlyUser on the owning item's Team. Delete: Owner
(above ReadOnly) or TeamLeadUser.
"""

import hashlib

from fastapi import APIRouter, Depends, File, Form, HTTPException, UploadFile, status
from fastapi.responses import Response

from app.db import get_conn, many, one
from app.security.deps import CurrentPerson, get_current_person, require_owner_or_team_lead, require_role_at_least

router = APIRouter(prefix="/attachment", tags=["attachments"])

_LIST_COLUMNS = (
    "attachment_id, task_id, project_id, component_id, name, kind, url, size_bytes, "
    "created_time, owner_person_id"
)


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


@router.get("")
def list_attachments(
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
        return many(
            conn.execute(
                f"SELECT {_LIST_COLUMNS} FROM attachment {where} ORDER BY attachment_id", params
            )
        )


@router.post("", status_code=status.HTTP_201_CREATED)
async def create_attachment(
    kind: str = Form(...),
    name: str = Form(...),
    task_id: int | None = Form(None),
    project_id: int | None = Form(None),
    component_id: int | None = Form(None),
    url: str | None = Form(None),
    file: UploadFile | None = File(None),
    caller: CurrentPerson = Depends(get_current_person),
):
    if kind not in ("File", "Link"):
        raise HTTPException(status.HTTP_400_BAD_REQUEST, "kind must be 'File' or 'Link' for Level 1")
    with get_conn() as conn:
        team_id = _owning_team_id(conn, task_id=task_id, project_id=project_id, component_id=component_id)
        require_role_at_least(caller, team_id, "NormalUser")

        if kind == "Link":
            if not url:
                raise HTTPException(status.HTTP_400_BAD_REQUEST, "url is required for kind='Link'")
            return one(
                conn.execute(
                    "INSERT INTO attachment (task_id, project_id, component_id, name, kind, url, owner_person_id) "
                    f"VALUES (%s, %s, %s, %s, 'Link', %s, %s) RETURNING {_LIST_COLUMNS}",
                    (task_id, project_id, component_id, name, url, caller.person_id),
                )
            )

        if file is None:
            raise HTTPException(status.HTTP_400_BAD_REQUEST, "file is required for kind='File'")
        data = await file.read()
        content_hash = hashlib.sha256(data).hexdigest()
        return one(
            conn.execute(
                "INSERT INTO attachment (task_id, project_id, component_id, name, kind, data, "
                "size_bytes, content_hash, owner_person_id) "
                f"VALUES (%s, %s, %s, %s, 'File', %s, %s, %s, %s) RETURNING {_LIST_COLUMNS}",
                (task_id, project_id, component_id, name, data, len(data), content_hash, caller.person_id),
            )
        )


@router.get("/{attachment_id}/download")
def download_attachment(attachment_id: int, caller: CurrentPerson = Depends(get_current_person)):
    with get_conn() as conn:
        row = one(
            conn.execute(
                "SELECT name, kind, url, data FROM attachment WHERE attachment_id = %s",
                (attachment_id,),
            )
        )
        if row is None:
            raise HTTPException(status.HTTP_404_NOT_FOUND, "No such Attachment")
        if row["kind"] == "Link":
            return {"url": row["url"]}
        return Response(
            content=bytes(row["data"]),
            media_type="application/octet-stream",
            headers={"Content-Disposition": f'attachment; filename="{row["name"]}"'},
        )


@router.delete("/{attachment_id}", status_code=status.HTTP_204_NO_CONTENT)
def delete_attachment(attachment_id: int, caller: CurrentPerson = Depends(get_current_person)):
    with get_conn() as conn:
        row = one(
            conn.execute(
                "SELECT task_id, project_id, component_id, owner_person_id FROM attachment "
                "WHERE attachment_id = %s",
                (attachment_id,),
            )
        )
        if row is None:
            raise HTTPException(status.HTTP_404_NOT_FOUND, "No such Attachment")
        team_id = _owning_team_id(
            conn, task_id=row["task_id"], project_id=row["project_id"], component_id=row["component_id"]
        )
        require_owner_or_team_lead(caller, owner_person_id=row["owner_person_id"], team_id=team_id)
        conn.execute("DELETE FROM attachment WHERE attachment_id = %s", (attachment_id,))
