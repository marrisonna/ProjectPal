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


def test_search_multi_word_and_matches_across_fields(api, alice_token):
    # Task 1's description contains "billing"; its detailed_description
    # contains "PostgreSQL" but not "billing" (SearchPlan.md D1.4-75) — each
    # word only needs to match *one* of a Task's own searchable columns, not
    # both words in the same column.
    resp = api.get("/search", params={"q": "billing postgresql"}, headers=auth(alice_token))
    assert resp.status_code == 200
    results = resp.json()
    assert any(r["type"] == "Task" and r["id"] == 1 for r in results)


def test_search_multi_word_and_requires_every_word(api, alice_token):
    # "billing" matches Task 1; the second word matches nothing anywhere —
    # the whole row must fail to match once AND'd with a word that isn't
    # present at all.
    resp = api.get(
        "/search", params={"q": "billing zzz-no-such-word-zzz"}, headers=auth(alice_token)
    )
    assert resp.status_code == 200
    results = resp.json()
    assert not any(r["type"] == "Task" and r["id"] == 1 for r in results)
