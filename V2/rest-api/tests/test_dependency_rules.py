from helpers import auth


def test_dependency_cycle_is_rejected(api, alice_token):
    # Task 2 already depends on Task 1 (pre=1, post=2) in the seed data.
    resp = api.post(
        "/dependency",
        json={"pre_task_id": 2, "post_task_id": 1},
        headers=auth(alice_token),
    )
    assert resp.status_code == 409
    assert "cycle" in resp.json()["error"].lower()
    assert "psycopg" not in resp.text.lower()


def test_dependency_create_and_delete_cycle(api, alice_token):
    # Task 4 and Task 5 have no existing dependency between them.
    create = api.post(
        "/dependency", json={"pre_task_id": 4, "post_task_id": 5}, headers=auth(alice_token)
    )
    assert create.status_code == 201, create.text
    dependency_id = create.json()["dependency_id"]

    delete = api.delete(f"/dependency/{dependency_id}", headers=auth(alice_token))
    assert delete.status_code == 204
