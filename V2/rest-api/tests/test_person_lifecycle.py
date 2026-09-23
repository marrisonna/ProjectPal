"""ManagePeoplePlan.md's own server-side changes (D1.4-83-D1.4-87, D-DM-12):
narrow Person deletion, the admin self-demotion guard, PersonRole's new
`nickname` field, and the never-leaderless guard on PersonRole writes.
"""

import uuid

from helpers import auth


def _create_person(api, admin_token, name):
    resp = api.post("/person", json={"name": name}, headers=auth(admin_token))
    assert resp.status_code == 201, resp.text
    return resp.json()["person_id"]


# --- Person deletion (D-DM-12) ----------------------------------------------


def test_delete_unreferenced_person_succeeds(api, admin_token):
    person_id = _create_person(api, admin_token, f"Scratch Person {uuid.uuid4().hex[:8]}")
    resp = api.delete(f"/person/{person_id}", headers=auth(admin_token))
    assert resp.status_code == 204, resp.text

    # Genuinely gone, not just deactivated.
    resp = api.get(f"/person/{person_id}", headers=auth(admin_token))
    assert resp.status_code == 404


def test_delete_person_referenced_by_a_task_is_rejected(api, admin_token, alice_person_id):
    # Alice owns/leads real seeded Tasks/Projects (conftest.py) — plenty of
    # references to trip this on.
    resp = api.delete(f"/person/{alice_person_id}", headers=auth(admin_token))
    assert resp.status_code == 409
    assert "Task" in resp.json()["detail"] or "Project" in resp.json()["detail"]

    # Confirmed still there — the rejection didn't half-delete anything.
    resp = api.get(f"/person/{alice_person_id}", headers=auth(admin_token))
    assert resp.status_code == 200


def test_delete_person_referenced_only_by_team_membership_is_rejected(api, admin_token):
    # A fresh Person with no Task/Project/Remark/Attachment reference at
    # all, but who *has* been added to a Team — person_role alone should
    # still block the delete (D-DM-12's own "not mentioned anywhere...
    # etc." reading, generalising V1.2's own narrower reference check).
    person_id = _create_person(api, admin_token, f"Scratch Member {uuid.uuid4().hex[:8]}")
    add_role = api.post(
        "/person-role",
        json={"person_id": person_id, "team_id": 1, "role": "NormalUser"},
        headers=auth(admin_token),
    )
    assert add_role.status_code == 201, add_role.text

    resp = api.delete(f"/person/{person_id}", headers=auth(admin_token))
    assert resp.status_code == 409
    assert "Team" in resp.json()["detail"]


def test_delete_person_requires_org_admin(api, bob_token, admin_token):
    person_id = _create_person(api, admin_token, f"Scratch Person {uuid.uuid4().hex[:8]}")
    resp = api.delete(f"/person/{person_id}", headers=auth(bob_token))
    assert resp.status_code == 403

    # Cleanup — not the point of the test, just avoids littering scratch rows.
    api.delete(f"/person/{person_id}", headers=auth(admin_token))


# --- Admin self-demotion guard (D1.4-83) ------------------------------------


def test_admin_cannot_remove_own_admin_flag(api, admin_token):
    # admin_token is Nadia (person 7, conftest.py) - a "pure" admin, so this
    # can't succeed for the unrelated reason of her losing standing some
    # other way.
    resp = api.patch(
        "/person/7", json={"is_organisation_admin": False}, headers=auth(admin_token)
    )
    assert resp.status_code == 400

    # Unaffected — still an admin.
    resp = api.get("/person/7", headers=auth(admin_token))
    assert resp.json()["is_organisation_admin"] is True


def test_admin_can_update_own_other_fields(api, admin_token):
    # The guard is scoped to is_organisation_admin specifically, not a
    # blanket "can't edit your own row" rule. colour used to live on Person
    # and was the field this test exercised; it moved to PersonRole
    # (D-DM-13/D1.4-88). `name` stands in for "some other, unrelated field"
    # now — set to its own already-current value (conftest.py's own Nadia
    # Fischer) rather than a new one, since the DB isn't reset between test
    # runs (Claude/Guidelines/ImplementationApproach.md §5) and this Person's
    # own external_login is other tests' own login fixture — a genuinely
    # different value here would corrupt those on every re-run.
    resp = api.patch("/person/7", json={"name": "Nadia Fischer"}, headers=auth(admin_token))
    assert resp.status_code == 200
    assert resp.json()["name"] == "Nadia Fischer"


# --- PersonRole nickname (D1.4-87) ------------------------------------------


def test_person_role_nickname_write_and_read(api, admin_token, bob_person_id):
    nickname = f"Bobby-{uuid.uuid4().hex[:6]}"
    resp = api.patch(
        f"/person-role/{bob_person_id}/1", json={"nickname": nickname}, headers=auth(admin_token)
    )
    assert resp.status_code == 200, resp.text
    assert resp.json()["nickname"] == nickname

    roles = api.get("/person-role?team_id=1", headers=auth(admin_token)).json()
    assert any(r["person_id"] == bob_person_id and r["nickname"] == nickname for r in roles)


# --- PersonRole colour (D-DM-13/D1.4-88) ------------------------------------


def test_person_role_colour_write_and_read(api, admin_token, bob_person_id):
    resp = api.patch(
        f"/person-role/{bob_person_id}/1", json={"colour": "#123456"}, headers=auth(admin_token)
    )
    assert resp.status_code == 200, resp.text
    assert resp.json()["colour"] == "#123456"

    roles = api.get("/person-role?team_id=1", headers=auth(admin_token)).json()
    assert any(r["person_id"] == bob_person_id and r["colour"] == "#123456" for r in roles)


def test_person_role_colour_set_on_create(api, admin_token):
    person_id = _create_person(api, admin_token, f"Scratch Coloured Person {uuid.uuid4().hex[:8]}")
    resp = api.post(
        "/person-role",
        json={"person_id": person_id, "team_id": 1, "role": "NormalUser", "colour": "#abcdef"},
        headers=auth(admin_token),
    )
    assert resp.status_code == 201, resp.text
    assert resp.json()["colour"] == "#abcdef"


def test_person_no_longer_has_a_colour_field(api, admin_token, bob_person_id):
    # D-DM-13/D1.4-88 — colour moved off Person entirely; PATCH /person no
    # longer recognises it (a Pydantic model with no such field just drops
    # an unknown key rather than erroring, so this checks the *response*
    # shape, not that the write itself is rejected).
    resp = api.get(f"/person/{bob_person_id}", headers=auth(admin_token))
    assert resp.status_code == 200
    assert "colour" not in resp.json()


# --- Never-leaderless guard (D1.4-87) ---------------------------------------


def test_cannot_demote_teams_only_team_lead_user(api, admin_token, alice_person_id):
    # Alice (person 1) is Team 1's sole TeamLeadUser (conftest.py).
    resp = api.patch(
        f"/person-role/{alice_person_id}/1", json={"role": "LeadUser"}, headers=auth(admin_token)
    )
    assert resp.status_code == 400

    roles = api.get("/person-role?team_id=1", headers=auth(admin_token)).json()
    assert any(
        r["person_id"] == alice_person_id and r["role"] == "TeamLeadUser" for r in roles
    )


def test_cannot_remove_teams_only_team_lead_user(api, admin_token, alice_person_id):
    resp = api.delete(f"/person-role/{alice_person_id}/1", headers=auth(admin_token))
    assert resp.status_code == 400

    roles = api.get("/person-role?team_id=1", headers=auth(admin_token)).json()
    assert any(r["person_id"] == alice_person_id for r in roles)


def test_can_demote_a_team_lead_user_when_another_remains(api, admin_token, bob_person_id):
    # A fresh Team so this doesn't touch real seeded Team 1/Team 2 state —
    # bootstrap gives it one TeamLeadUser (Bob), then promote a second
    # Person (a scratch one) so demoting Bob afterwards has somewhere safe
    # to land.
    new_team = api.post(
        "/team",
        json={
            "name": f"Scratch Leaderless-Guard Team {uuid.uuid4().hex[:8]}",
            "initial_team_lead_person_id": bob_person_id,
        },
        headers=auth(admin_token),
    )
    assert new_team.status_code == 201, new_team.text
    team_id = new_team.json()["team_id"]

    second_lead_id = _create_person(api, admin_token, f"Scratch Co-Lead {uuid.uuid4().hex[:8]}")
    add_role = api.post(
        "/person-role",
        json={"person_id": second_lead_id, "team_id": team_id, "role": "TeamLeadUser"},
        headers=auth(admin_token),
    )
    assert add_role.status_code == 201, add_role.text

    # Now two TeamLeadUsers on this Team — demoting Bob should succeed.
    resp = api.patch(
        f"/person-role/{bob_person_id}/{team_id}",
        json={"role": "NormalUser"},
        headers=auth(admin_token),
    )
    assert resp.status_code == 200, resp.text
    assert resp.json()["role"] == "NormalUser"
