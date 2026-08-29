import uuid

from helpers import auth


def test_list_teams(api, alice_token):
    resp = api.get("/team", headers=auth(alice_token))
    assert resp.status_code == 200
    assert {t["name"] for t in resp.json()} >= {"Platform", "Customer Projects"}


def test_list_people(api, alice_token):
    resp = api.get("/person", headers=auth(alice_token))
    assert resp.status_code == 200
    assert len(resp.json()) >= 7


def test_person_create_is_admin_only(api, bob_token):
    resp = api.post("/person", json={"name": "Should be rejected"}, headers=auth(bob_token))
    assert resp.status_code == 403


def test_person_create_by_admin_succeeds(api, admin_token):
    resp = api.post("/person", json={"name": "Test Person"}, headers=auth(admin_token))
    assert resp.status_code == 201


def test_team_lead_can_write_person_role_for_own_team(api, alice_token, bob_person_id):
    resp = api.patch(
        f"/person-role/{bob_person_id}/1",
        json={"is_resource": True},
        headers=auth(alice_token),
    )
    assert resp.status_code == 200


def test_team_lead_cannot_write_person_role_for_other_team(api, tom_token, bob_person_id):
    # Tom is TeamLeadUser on Team 2 only (and not an org admin) — Team 1 is
    # out of his reach.
    resp = api.post(
        "/person-role",
        json={"person_id": bob_person_id, "team_id": 1, "role": "NormalUser"},
        headers=auth(tom_token),
    )
    assert resp.status_code == 403


def test_team_creation_bootstraps_team_lead_user(api, admin_token, alice_person_id):
    # team.name is UNIQUE and this DB isn't reset between test runs
    # (Claude/Guidelines/ImplementationApproach.md §5) — give each run its own name.
    resp = api.post(
        "/team",
        json={"name": f"New Team {uuid.uuid4().hex[:8]}", "initial_team_lead_person_id": alice_person_id},
        headers=auth(admin_token),
    )
    assert resp.status_code == 201, resp.text
    new_team_id = resp.json()["team_id"]

    roles = api.get(f"/person-role?team_id={new_team_id}", headers=auth(admin_token)).json()
    assert any(r["person_id"] == alice_person_id and r["role"] == "TeamLeadUser" for r in roles)


def test_team_creation_requires_initial_lead(api, admin_token):
    resp = api.post("/team", json={"name": "Leaderless Team"}, headers=auth(admin_token))
    assert resp.status_code == 422  # missing required field
