from helpers import auth


def test_remark_owner_can_edit_their_own(api, alice_token, alice_owned_remark_id):
    resp = api.patch(
        f"/remark/{alice_owned_remark_id}",
        json={"remark_text": "edited by its own owner"},
        headers=auth(alice_token),
    )
    assert resp.status_code == 200
    assert resp.json()["remark_text"] == "edited by its own owner"


def test_remark_non_owner_cannot_edit(api, bob_token, alice_owned_remark_id):
    resp = api.patch(
        f"/remark/{alice_owned_remark_id}",
        json={"remark_text": "edited by someone else"},
        headers=auth(bob_token),
    )
    assert resp.status_code == 403


def test_remark_readonly_user_can_create_and_edit_their_own(api, readonly_token):
    # Sam is ReadOnlyUser on Team 1. Task 7 belongs to Project 4 (Team 1).
    create = api.post(
        "/remark",
        json={"task_id": 7, "remark_text": "a ReadOnlyUser's remark"},
        headers=auth(readonly_token),
    )
    assert create.status_code == 201, create.text
    remark_id = create.json()["remark_id"]

    edit = api.patch(
        f"/remark/{remark_id}", json={"remark_text": "edited"}, headers=auth(readonly_token)
    )
    assert edit.status_code == 200

    delete = api.delete(f"/remark/{remark_id}", headers=auth(readonly_token))
    assert delete.status_code == 204


def test_team_lead_can_delete_but_not_edit_a_remark_they_dont_own(
    api, alice_token, rahul_token, neil_token, other_team_task_id
):
    # other_team_task_id belongs to Team 2, where Neil is TeamLeadUser and
    # Rahul is an ordinary NormalUser (not its owner).
    create = api.post(
        "/remark",
        json={"task_id": other_team_task_id, "remark_text": "Rahul's remark"},
        headers=auth(rahul_token),
    )
    remark_id = create.json()["remark_id"]

    # Alice (TeamLeadUser on Team 1, not Team 2) is neither the owner nor a
    # TeamLeadUser on this Remark's Team — rejected outright.
    other_team_edit = api.patch(
        f"/remark/{remark_id}", json={"remark_text": "hijacked"}, headers=auth(alice_token)
    )
    assert other_team_edit.status_code == 403

    # Neil (Team 2's actual TeamLeadUser) can't edit a Remark he doesn't own either.
    lead_edit = api.patch(
        f"/remark/{remark_id}", json={"remark_text": "hijacked by the lead"}, headers=auth(neil_token)
    )
    assert lead_edit.status_code == 403

    # But he can delete it.
    lead_delete = api.delete(f"/remark/{remark_id}", headers=auth(neil_token))
    assert lead_delete.status_code == 204
