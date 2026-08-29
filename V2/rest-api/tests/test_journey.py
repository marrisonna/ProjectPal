from helpers import auth


def test_end_to_end_journey(api, alice_token):
    project = api.post(
        "/project", json={"team_id": 1, "name": "Journey Project"}, headers=auth(alice_token)
    ).json()

    task_a = api.post(
        "/task",
        json={"project_id": project["project_id"], "description": "Journey Task A"},
        headers=auth(alice_token),
    ).json()
    task_b = api.post(
        "/task",
        json={"project_id": project["project_id"], "description": "Journey Task B"},
        headers=auth(alice_token),
    ).json()

    assign = api.post(
        f"/task/{task_a['task_id']}/resources", json={"person_id": 3}, headers=auth(alice_token)
    )
    assert assign.status_code == 201

    dependency = api.post(
        "/dependency",
        json={"pre_task_id": task_a["task_id"], "post_task_id": task_b["task_id"]},
        headers=auth(alice_token),
    )
    assert dependency.status_code == 201

    remark = api.post(
        "/remark",
        json={"task_id": task_a["task_id"], "remark_text": "Journey remark"},
        headers=auth(alice_token),
    )
    assert remark.status_code == 201

    # Confirm it all actually hangs together, not just individually succeeded.
    resources = api.get(f"/task/{task_a['task_id']}/resources", headers=auth(alice_token)).json()
    assert any(r["person_id"] == 3 for r in resources)

    deps = api.get(f"/dependency?task_id={task_a['task_id']}", headers=auth(alice_token)).json()
    assert any(d["dependency_id"] == dependency.json()["dependency_id"] for d in deps)

    remarks = api.get(f"/remark?task_id={task_a['task_id']}", headers=auth(alice_token)).json()
    assert any(r["remark_text"] == "Journey remark" for r in remarks)

    # Cleanup.
    api.delete(f"/dependency/{dependency.json()['dependency_id']}", headers=auth(alice_token))
    api.delete(f"/task/{task_a['task_id']}", headers=auth(alice_token))
    api.delete(f"/task/{task_b['task_id']}", headers=auth(alice_token))
    api.delete(f"/project/{project['project_id']}", headers=auth(alice_token))
