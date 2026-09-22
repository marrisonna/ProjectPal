"""GET /search (Plan.md §4.2, D1-4) — across Task/Project/Component/Remark,
plus Attachment metadata (name, url, mail_from — not content/full-text,
which stays a later enhancement per Requirements/UseCases.md's Search / Find
use case).

Multi-word matching (SearchPlan.md D1.4-75): `q` is split on whitespace into
separate words, and every word must match *somewhere* on a given row (AND
across words) — but a single word only needs to match *one* of that type's
own searchable columns (OR across columns per word), exactly mirroring
V1.2's own `Task.ContainsText`'s `Description OR DetailedDescription` shape
(`FormFind.cs`/`Task.cs`): a word found in `description` and a different
word found only in `detailed_description` on the same Task still matches.
Done here, in SQL, rather than client-side — the GUI's own reference-data
hooks never fetch Attachment's `mail_from` column at all, so client-side
AND-matching would have a blind spot this doesn't.
"""

from fastapi import APIRouter, Depends

from app.db import get_conn, many
from app.security.deps import CurrentPerson, get_current_person

router = APIRouter(tags=["search"])

# Each type's own searchable columns, OR'd together for a single word.
_SEARCHABLE_COLUMNS: dict[str, list[str]] = {
    "task": ["description", "detailed_description"],
    "project": ["name"],
    "component": ["name"],
    "remark": ["remark_text"],
    "attachment": ["name", "url", "mail_from"],
}


def _and_clause(columns: list[str], word_count: int) -> str:
    """`(col1 ILIKE %(w0)s OR col2 ILIKE %(w0)s) AND (col1 ILIKE %(w1)s OR ...) ...`"""
    word_clauses = []
    for i in range(word_count):
        ors = " OR ".join(f"{column} ILIKE %(w{i})s" for column in columns)
        word_clauses.append(f"({ors})")
    return " AND ".join(word_clauses)


@router.get("/search")
def search(q: str, caller: CurrentPerson = Depends(get_current_person)):
    words = q.split()
    if not words:
        return []
    params = {f"w{i}": f"%{word}%" for i, word in enumerate(words)}

    query = f"""
    SELECT 'Task' AS type, task_id AS id, description AS label FROM task
        WHERE {_and_clause(_SEARCHABLE_COLUMNS["task"], len(words))}
    UNION ALL
    SELECT 'Project', project_id, name FROM project
        WHERE {_and_clause(_SEARCHABLE_COLUMNS["project"], len(words))}
    UNION ALL
    SELECT 'Component', component_id, name FROM component
        WHERE {_and_clause(_SEARCHABLE_COLUMNS["component"], len(words))}
    UNION ALL
    SELECT 'Remark', remark_id, remark_text FROM remark
        WHERE {_and_clause(_SEARCHABLE_COLUMNS["remark"], len(words))}
    UNION ALL
    SELECT 'Attachment', attachment_id, name FROM attachment
        WHERE {_and_clause(_SEARCHABLE_COLUMNS["attachment"], len(words))}
    """
    with get_conn() as conn:
        return many(conn.execute(query, params))
