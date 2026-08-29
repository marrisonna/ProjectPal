from helpers import auth


def test_request_without_token_is_rejected(api):
    resp = api.get("/task")
    assert resp.status_code == 401


def test_stub_login_issues_a_usable_token(api, alice_token):
    resp = api.get("/auth/whoami", headers=auth(alice_token))
    assert resp.status_code == 200
    assert resp.json()["person_id"] == 1


def test_login_with_unknown_login_is_rejected(api):
    resp = api.post("/auth/login", json={"external_login": "nobody@example.com"})
    assert resp.status_code == 401


def test_invalid_token_is_rejected(api):
    resp = api.get("/task", headers=auth("not-a-real-token"))
    assert resp.status_code == 401
