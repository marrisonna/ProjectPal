"""Admin/support tooling (D1-4, resolved by D1.2-4): bulk data export and a
data-integrity check, both is_organisation_admin-gated
(Requirements/UseCases.md's Administer the System use case).
"""

from fastapi import APIRouter, Depends

from app.db import get_conn, many
from app.security.deps import CurrentPerson, get_current_person, require_org_admin

router = APIRouter(prefix="/admin", tags=["admin"])

_TABLES = [
    "team",
    "person",
    "person_role",
    "component",
    "project",
    "task",
    "task_resource",
    "dependency",
    "remark",
]  # attachment omitted — its `data` column holds raw file bytes, not JSON-safe


@router.get("/export")
def export_all_data(caller: CurrentPerson = Depends(get_current_person)):
    require_org_admin(caller)
    with get_conn() as conn:
        return {table: many(conn.execute(f"SELECT * FROM {table}")) for table in _TABLES}


@router.get("/integrity-check")
def integrity_check(caller: CurrentPerson = Depends(get_current_person)):
    require_org_admin(caller)
    with get_conn() as conn:
        teams_without_lead = many(
            conn.execute(
                "SELECT t.team_id, t.name FROM team t "
                "WHERE NOT EXISTS ("
                "  SELECT 1 FROM person_role pr "
                "  WHERE pr.team_id = t.team_id AND pr.role = 'TeamLeadUser'"
                ")"
            )
        )
        resources_no_longer_valid = many(
            conn.execute(
                "SELECT tr.task_id, tr.person_id, p.team_id FROM task_resource tr "
                "JOIN task t ON t.task_id = tr.task_id "
                "JOIN project p ON p.project_id = t.project_id "
                "WHERE NOT EXISTS ("
                "  SELECT 1 FROM person_role pr "
                "  WHERE pr.person_id = tr.person_id AND pr.team_id = p.team_id AND pr.is_resource"
                ")"
            )
        )
        return {
            "teams_without_a_team_lead_user": teams_without_lead,
            "task_resource_assignments_no_longer_valid": resources_no_longer_valid,
        }
