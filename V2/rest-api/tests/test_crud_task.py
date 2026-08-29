from helpers import auth


def test_task_crud_cycle(api, alice_token):
    create = api.post(
        "/task", json={"project_id": 1, "description": "Test Task"}, headers=auth(alice_token)
    )
    assert create.status_code == 201, create.text
    task_id = create.json()["task_id"]

    update = api.patch(f"/task/{task_id}", json={"priority": "High"}, headers=auth(alice_token))
    assert update.status_code == 200
    assert update.json()["priority"] == "High"

    delete = api.delete(f"/task/{task_id}", headers=auth(alice_token))
    assert delete.status_code == 204

    assert api.get(f"/task/{task_id}", headers=auth(alice_token)).status_code == 404


def test_task_move_to_different_team_project_is_rejected(api, alice_token, other_team_project_id):
    create = api.post(
        "/task", json={"project_id": 1, "description": "Movable task"}, headers=auth(alice_token)
    )
    task_id = create.json()["task_id"]

    resp = api.patch(
        f"/task/{task_id}", json={"project_id": other_team_project_id}, headers=auth(alice_token)
    )
    assert resp.status_code == 403

    api.delete(f"/task/{task_id}", headers=auth(alice_token))  # cleanup


def test_task_move_within_same_team_succeeds(api, alice_token):
    create = api.post(
        "/task", json={"project_id": 1, "description": "Movable task"}, headers=auth(alice_token)
    )
    task_id = create.json()["task_id"]

    # Project 2 (Database Migration) is also Team 1.
    resp = api.patch(f"/task/{task_id}", json={"project_id": 2}, headers=auth(alice_token))
    assert resp.status_code == 200
    assert resp.json()["project_id"] == 2

    api.delete(f"/task/{task_id}", headers=auth(alice_token))  # cleanup
