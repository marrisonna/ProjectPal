from helpers import auth


def test_assign_resource_on_own_team_succeeds(api, alice_token, task_id):
    # Task 3 belongs to Project 3 (API Layer, Team 1). Ben (person 2) is a
    # resource on Team 1 in the seed data, and not already assigned to task 3
    # (unlike Priya, person 3, who already is — picking her would 409 on the
    # existing task_resource row instead of exercising the happy path).
    resp = api.post(f"/task/{task_id}/resources", json={"person_id": 2}, headers=auth(alice_token))
    assert resp.status_code == 201, resp.text

    unassign = api.delete(f"/task/{task_id}/resources/2", headers=auth(alice_token))
    assert unassign.status_code == 204


def test_assign_non_resource_is_rejected(api, alice_token, task_id):
    # Sam (person 6) is ReadOnlyUser on Team 1 — has a role there, but
    # is_resource=false, so still not assignable to a Team-1 Task.
    resp = api.post(f"/task/{task_id}/resources", json={"person_id": 6}, headers=auth(alice_token))
    assert resp.status_code == 403


def test_assign_resource_from_a_different_team_is_rejected(api, alice_token, task_id):
    # Rahul (person 10033) is a resource, but on Team 2 — not on Team 1,
    # which owns task_id's Project, and holds no role there at all.
    # D-DM-8: must be a resource on *this* Team.
    resp = api.post(f"/task/{task_id}/resources", json={"person_id": 10033}, headers=auth(alice_token))
    assert resp.status_code == 403
