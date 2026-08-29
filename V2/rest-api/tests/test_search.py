from helpers import auth


def test_search_matches_task_description(api, alice_token):
    resp = api.get("/search", params={"q": "billing"}, headers=auth(alice_token))
    assert resp.status_code == 200
    results = resp.json()
    assert any(r["type"] == "Task" and "billing" in r["label"].lower() for r in results)


def test_search_matches_attachment_name(api, alice_token):
    resp = api.get("/search", params={"q": "schema-draft"}, headers=auth(alice_token))
    assert resp.status_code == 200
    results = resp.json()
    assert any(r["type"] == "Attachment" for r in results)
