"""The authorization check in security/deps.py actually restricts by
Team/role (D-UC-4) — a role held on one Team never grants access to
another Team's resources, not just "any valid token"."""

from helpers import auth


def test_team_scoped_authorization_rejects_wrong_team_role(api, bob_token, other_team_project_id):
    # bob_token (Ben) is a LeadUser on Team 1; other_team_project_id belongs
    # to Team 2, where Ben holds no role at all.
    resp = api.post(
        "/task",
        json={"project_id": other_team_project_id, "description": "Should be rejected"},
        headers=auth(bob_token),
    )
    assert resp.status_code == 403


def test_role_on_own_team_succeeds(api, bob_token):
    # Ben is a LeadUser on Team 1 — Project 2 (Database Migration) is Team 1.
    resp = api.post(
        "/task",
        json={"project_id": 2, "description": "Created by a LeadUser on the right Team"},
        headers=auth(bob_token),
    )
    assert resp.status_code == 201
