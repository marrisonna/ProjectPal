from helpers import auth


def test_component_crud_cycle(api, alice_token):
    create = api.post(
        "/component", json={"team_id": 1, "name": "Test Component"}, headers=auth(alice_token)
    )
    assert create.status_code == 201, create.text
    component_id = create.json()["component_id"]

    update = api.patch(
        f"/component/{component_id}", json={"name": "Renamed"}, headers=auth(alice_token)
    )
    assert update.status_code == 200

    delete = api.delete(f"/component/{component_id}", headers=auth(alice_token))
    assert delete.status_code == 204


def test_component_create_requires_lead_user(api, readonly_token):
    resp = api.post(
        "/component", json={"team_id": 2, "name": "Should be rejected"}, headers=auth(readonly_token)
    )
    assert resp.status_code == 403


def test_task_in_different_team_can_still_reference_component(api, alice_token):
    # Component 2 ("Invoicing") belongs to Team 1 (see seed data). Task 8
    # belongs to Project 4 (Team 2), and already tags Component 2 in the seed
    # data. Tagging across Teams is allowed — only Component *management* is
    # Team-scoped (D-DM-6).
    task = api.get("/task/8", headers=auth(alice_token)).json()
    component = api.get(f"/component/{task['component_id']}", headers=auth(alice_token)).json()
    project = api.get("/project/4", headers=auth(alice_token)).json()
    assert component["team_id"] != project["team_id"]


def test_reparent_onto_different_team_is_rejected(api, alice_token, tom_token):
    team1_component = api.post(
        "/component", json={"team_id": 1, "name": "Team 1 Component"}, headers=auth(alice_token)
    ).json()
    team2_component = api.post(
        "/component", json={"team_id": 2, "name": "Team 2 Component"}, headers=auth(tom_token)
    ).json()

    resp = api.patch(
        f"/component/{team1_component['component_id']}",
        json={"parent_component_id": team2_component["component_id"]},
        headers=auth(alice_token),
    )
    assert resp.status_code == 403
