import requests
import pytest

from helpers import auth

BASE_URL = "http://127.0.0.1:8000"


class ApiClient:
    def __init__(self, base_url: str):
        self._base_url = base_url
        self._session = requests.Session()

    def get(self, path, **kw):
        return self._session.get(self._base_url + path, **kw)

    def post(self, path, **kw):
        return self._session.post(self._base_url + path, **kw)

    def patch(self, path, **kw):
        return self._session.patch(self._base_url + path, **kw)

    def delete(self, path, **kw):
        return self._session.delete(self._base_url + path, **kw)


@pytest.fixture(scope="session")
def api():
    return ApiClient(BASE_URL)


# Seeded People and their passwords (V2/database/seed/001_example_data.sql,
# primed as if an admin had already set them per
# Claude/Level1_Implementation/3_Authentication/Plan.md D1.3-4 — fictional
# local-only demo accounts, plaintext committed openly per that plan's D1.3-8):
#   1 Alice Chen   alice-pass1   - is_organisation_admin, TeamLeadUser on Team 1 (Platform)
#   2 Ben Okafor   ben-pass1     - LeadUser on Team 1, no role on Team 2
#   3 Priya Sharma priya-pass1   - NormalUser on Team 1
#   4 Tom Baxter   tom-pass1     - TeamLeadUser on Team 2 (Customer Projects), NormalUser on Team 1
#   5 Grace Liu    grace-pass1   - NormalUser on Team 2
#   6 Sam Patel    sam-pass1     - ReadOnlyUser on Team 2
#   7 Nadia Fischer nadia-pass1  - is_organisation_admin, NormalUser on both Teams (not a resource)
PASSWORDS = {
    "alice.chen@example.com": "alice-pass1",
    "ben.okafor@example.com": "ben-pass1",
    "priya.sharma@example.com": "priya-pass1",
    "tom.baxter@example.com": "tom-pass1",
    "grace.liu@example.com": "grace-pass1",
    "sam.patel@example.com": "sam-pass1",
    "nadia.fischer@example.com": "nadia-pass1",
}


def _login(api: ApiClient, external_login: str, password: str | None = None) -> str:
    resp = api.post(
        "/auth/login",
        json={"external_login": external_login, "password": password or PASSWORDS[external_login]},
    )
    assert resp.status_code == 200, resp.text
    return resp.json()["token"]


@pytest.fixture(scope="session")
def alice_token(api):
    return _login(api, "alice.chen@example.com")


@pytest.fixture(scope="session")
def bob_token(api):
    # Stand-in for "a Person with a role on Team 1 but none on Team 2".
    return _login(api, "ben.okafor@example.com")


@pytest.fixture(scope="session")
def admin_token(api):
    # A "pure" admin distinct from Alice, who also happens to lead Team 1.
    return _login(api, "nadia.fischer@example.com")


@pytest.fixture(scope="session")
def readonly_token(api):
    return _login(api, "sam.patel@example.com")


@pytest.fixture(scope="session")
def tom_token(api):
    # TeamLeadUser on Team 2 (Customer Projects) — used where a test needs to
    # act with real Team-2 standing without relying on an admin/Team-1 identity.
    return _login(api, "tom.baxter@example.com")


@pytest.fixture(scope="session")
def alice_person_id():
    return 1


@pytest.fixture(scope="session")
def bob_person_id():
    return 2


@pytest.fixture(scope="session")
def task_id():
    return 3  # "Stand up REST endpoints for tasks", Project 3 (API Layer, Team 1)


@pytest.fixture(scope="session")
def other_team_project_id():
    return 4  # "Customer Portal Refresh", Team 2 — Team 1 people hold no role here.


@pytest.fixture()
def alice_owned_remark_id(api, alice_token):
    resp = api.post(
        "/remark",
        json={"task_id": 3, "remark_text": "owned by alice for this test"},
        headers=auth(alice_token),
    )
    assert resp.status_code == 201, resp.text
    return resp.json()["remark_id"]
