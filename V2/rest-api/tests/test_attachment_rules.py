import uuid

from helpers import auth


def _unique_bytes() -> bytes:
    # Tests run against a persistent dev database (Claude/Guidelines/
    # ImplementationApproach.md §5) that isn't reset between runs, so content
    # needs to be unique per run to avoid tripping ux_attachment_dedup against
    # a *previous* run's leftover attachment rather than this test's own pair.
    return uuid.uuid4().bytes


def test_create_link_attachment(api, alice_token):
    resp = api.post(
        "/attachment",
        data={"kind": "Link", "name": "Spec doc", "task_id": 4, "url": "https://example.com/spec"},
        headers=auth(alice_token),
    )
    assert resp.status_code == 201, resp.text
    assert resp.json()["kind"] == "Link"


def test_duplicate_attachment_is_rejected(api, alice_token):
    content = _unique_bytes()
    files = {"file": ("note.txt", content)}
    first = api.post(
        "/attachment",
        data={"kind": "File", "name": "note.txt", "task_id": 4},
        files=files,
        headers=auth(alice_token),
    )
    assert first.status_code == 201, first.text

    second = api.post(
        "/attachment",
        data={"kind": "File", "name": "note.txt", "task_id": 4},
        files=files,
        headers=auth(alice_token),
    )
    assert second.status_code == 409


def test_download_file_attachment(api, alice_token):
    content = _unique_bytes()
    files = {"file": ("download-me.txt", content)}
    create = api.post(
        "/attachment",
        data={"kind": "File", "name": "download-me.txt", "task_id": 5},
        files=files,
        headers=auth(alice_token),
    )
    assert create.status_code == 201, create.text
    attachment_id = create.json()["attachment_id"]

    resp = api.get(f"/attachment/{attachment_id}/download", headers=auth(alice_token))
    assert resp.status_code == 200
    assert resp.content == content
