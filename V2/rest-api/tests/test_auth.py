import uuid

from helpers import auth


def test_request_without_token_is_rejected(api):
    resp = api.get("/task")
    assert resp.status_code == 401


def test_correct_password_issues_a_usable_token(api, alice_token):
    resp = api.get("/auth/whoami", headers=auth(alice_token))
    assert resp.status_code == 200
    assert resp.json()["person_id"] == 1


def test_login_with_unknown_login_is_rejected(api):
    resp = api.post(
        "/auth/login", json={"external_login": "nobody@example.com", "password": "whatever"}
    )
    assert resp.status_code == 401


def test_login_with_wrong_password_is_rejected(api):
    resp = api.post(
        "/auth/login", json={"external_login": "alice.chen@example.com", "password": "wrong-password"}
    )
    assert resp.status_code == 401


def test_wrong_password_and_unknown_login_give_the_same_error(api):
    # Deliberately indistinguishable, so this endpoint can't be used to
    # enumerate which logins exist (3_Authentication/Plan.md D1.3-3 point 4).
    unknown = api.post(
        "/auth/login", json={"external_login": "nobody@example.com", "password": "whatever"}
    )
    wrong_password = api.post(
        "/auth/login", json={"external_login": "alice.chen@example.com", "password": "wrong-password"}
    )
    assert unknown.status_code == wrong_password.status_code == 401
    assert unknown.json() == wrong_password.json()


def test_person_with_no_password_set_cannot_log_in(api, admin_token):
    # person.external_login is UNIQUE and this DB isn't reset between test
    # runs (Claude/Guidelines/ImplementationApproach.md §5) — unique per run.
    login = f"no-password-yet-{uuid.uuid4().hex[:8]}@example.com"
    create = api.post(
        "/person",
        json={"name": "No Password Yet", "external_login": login},
        headers=auth(admin_token),
    )
    assert create.status_code == 201, create.text

    resp = api.post("/auth/login", json={"external_login": login, "password": "anything"})
    assert resp.status_code == 401


def test_invalid_token_is_rejected(api):
    resp = api.get("/task", headers=auth("not-a-real-token"))
    assert resp.status_code == 401


def test_admin_can_set_a_password_and_person_can_then_log_in(api, admin_token):
    external_login = f"password-test-{uuid.uuid4().hex[:8]}@example.com"
    create = api.post(
        "/person",
        json={"name": "Password Test Person", "external_login": external_login},
        headers=auth(admin_token),
    )
    person_id = create.json()["person_id"]

    set_password = api.post(
        f"/person/{person_id}/password",
        json={"new_password": "a-new-password1"},
        headers=auth(admin_token),
    )
    assert set_password.status_code == 204

    login = api.post(
        "/auth/login", json={"external_login": external_login, "password": "a-new-password1"}
    )
    assert login.status_code == 200, login.text


def test_non_admin_cannot_set_a_password(api, bob_token, alice_person_id):
    resp = api.post(
        f"/person/{alice_person_id}/password",
        json={"new_password": "trying-to-hijack1"},
        headers=auth(bob_token),
    )
    assert resp.status_code == 403


def test_non_admin_cannot_set_their_own_password_either(api, bob_token, bob_person_id):
    # Self-service is deferred for Level 1 (3_Authentication/Plan.md D1.3-4) —
    # even setting your *own* password requires is_organisation_admin for now.
    resp = api.post(
        f"/person/{bob_person_id}/password",
        json={"new_password": "self-service-attempt1"},
        headers=auth(bob_token),
    )
    assert resp.status_code == 403


def test_new_password_must_meet_minimum_length(api, admin_token, alice_person_id):
    resp = api.post(
        f"/person/{alice_person_id}/password",
        json={"new_password": "short"},
        headers=auth(admin_token),
    )
    assert resp.status_code == 422
