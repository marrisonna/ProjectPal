"""GET /search (Plan.md §4.2, D1-4) — across Task/Project/Component/Remark,
plus Attachment metadata (name, url, mail_from — not content/full-text,
which stays a later enhancement per Requirements/UseCases.md's Search / Find
use case).
"""

from fastapi import APIRouter, Depends

from app.db import get_conn, many
from app.security.deps import CurrentPerson, get_current_person

router = APIRouter(tags=["search"])

_QUERY = """
SELECT 'Task' AS type, task_id AS id, description AS label FROM task WHERE description ILIKE %(q)s
UNION ALL
SELECT 'Project', project_id, name FROM project WHERE name ILIKE %(q)s
UNION ALL
SELECT 'Component', component_id, name FROM component WHERE name ILIKE %(q)s
UNION ALL
SELECT 'Remark', remark_id, remark_text FROM remark WHERE remark_text ILIKE %(q)s
UNION ALL
SELECT 'Attachment', attachment_id, name FROM attachment
    WHERE name ILIKE %(q)s OR url ILIKE %(q)s OR mail_from ILIKE %(q)s
"""


@router.get("/search")
def search(q: str, caller: CurrentPerson = Depends(get_current_person)):
    with get_conn() as conn:
        return many(conn.execute(_QUERY, {"q": f"%{q}%"}))
