from helpers import auth


def test_export_requires_org_admin(api, bob_token):
    resp = api.get("/admin/export", headers=auth(bob_token))
    assert resp.status_code == 403


def test_export_succeeds_for_org_admin(api, admin_token):
    resp = api.get("/admin/export", headers=auth(admin_token))
    assert resp.status_code == 200
    assert "task" in resp.json()


def test_integrity_check_requires_org_admin(api, bob_token):
    resp = api.get("/admin/integrity-check", headers=auth(bob_token))
    assert resp.status_code == 403


def test_integrity_check_finds_no_leaderless_teams(api, admin_token):
    resp = api.get("/admin/integrity-check", headers=auth(admin_token))
    assert resp.status_code == 200
    assert resp.json()["teams_without_a_team_lead_user"] == []
