from helpers import auth


def test_project_crud_cycle(api, alice_token):
    create = api.post(
        "/project", json={"team_id": 1, "name": "Test Project"}, headers=auth(alice_token)
    )
    assert create.status_code == 201, create.text
    project_id = create.json()["project_id"]

    read = api.get(f"/project/{project_id}", headers=auth(alice_token))
    assert read.status_code == 200
    assert read.json()["name"] == "Test Project"

    update = api.patch(
        f"/project/{project_id}", json={"name": "Renamed"}, headers=auth(alice_token)
    )
    assert update.status_code == 200
    assert update.json()["name"] == "Renamed"

    delete = api.delete(f"/project/{project_id}", headers=auth(alice_token))
    assert delete.status_code == 204

    assert api.get(f"/project/{project_id}", headers=auth(alice_token)).status_code == 404


def test_project_create_requires_lead_user(api, readonly_token):
    resp = api.post(
        "/project", json={"team_id": 2, "name": "Should be rejected"}, headers=auth(readonly_token)
    )
    assert resp.status_code == 403


def test_project_delete_requires_team_lead_not_just_owner(api, alice_token, bob_token):
    create = api.post(
        "/project",
        json={"team_id": 1, "name": "Owned by Ben", "owner_person_id": 2},
        headers=auth(alice_token),
    )
    project_id = create.json()["project_id"]

    # Ben is only a LeadUser on Team 1, not the TeamLeadUser — even as owner
    # he can't delete a Project (Requirements/UseCases.md §12).
    resp = api.delete(f"/project/{project_id}", headers=auth(bob_token))
    assert resp.status_code == 403

    api.delete(f"/project/{project_id}", headers=auth(alice_token))  # cleanup


def test_reparent_onto_different_team_is_rejected(api, alice_token, other_team_project_id):
    # Project 1 (Platform Modernisation) is Team 1; other_team_project_id is Team 2.
    resp = api.patch(
        "/project/1", json={"parent_project_id": other_team_project_id}, headers=auth(alice_token)
    )
    assert resp.status_code == 403
