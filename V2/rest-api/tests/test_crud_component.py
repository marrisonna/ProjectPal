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


def test_task_in_different_team_can_still_reference_component(
    api, neil_token, other_team_project_id
):
    # Component 2 ("Invoicing") belongs to Team 1. other_team_project_id
    # belongs to Team 2, where Neil is TeamLeadUser. Tagging a Task with a
    # Component from a different Team is allowed — only Component
    # *management* is Team-scoped (D-DM-6).
    create = api.post(
        "/task",
        json={
            "project_id": other_team_project_id,
            "description": "Cross-team component tag",
            "component_id": 2,
        },
        headers=auth(neil_token),
    )
    assert create.status_code == 201, create.text
    task_id = create.json()["task_id"]

    component = api.get("/component/2", headers=auth(neil_token)).json()
    project = api.get(f"/project/{other_team_project_id}", headers=auth(neil_token)).json()
    assert component["team_id"] != project["team_id"]

    api.delete(f"/task/{task_id}", headers=auth(neil_token))  # cleanup


def test_reparent_onto_different_team_is_rejected(api, alice_token, neil_token):
    team1_component = api.post(
        "/component", json={"team_id": 1, "name": "Team 1 Component"}, headers=auth(alice_token)
    ).json()
    team2_component = api.post(
        "/component", json={"team_id": 2, "name": "Team 2 Component"}, headers=auth(neil_token)
    ).json()

    resp = api.patch(
        f"/component/{team1_component['component_id']}",
        json={"parent_component_id": team2_component["component_id"]},
        headers=auth(alice_token),
    )
    assert resp.status_code == 403
