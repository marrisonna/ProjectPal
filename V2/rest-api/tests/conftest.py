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


# Seeded People and their passwords, primed as if an admin had already set
# them per Claude/Level1_Implementation/3_Authentication/Plan.md D1.3-4 —
# fictional/demo accounts, plaintext committed openly per that plan's D1.3-8.
#
# Team 1 ("Platform", V2/database/seed/001_example_data.sql) — all seven of
# these People hold a role on Team 1 only; Team 1 and Team 2 used to be two
# small, symmetric example Teams, but were collapsed into this one Team
# (see that file's own header comment) once Team 2 became the real,
# bulk-imported V1.2 data below:
#   1 Alice Chen    alice-pass1   - is_organisation_admin, TeamLeadUser on Team 1
#   2 Ben Okafor    ben-pass1     - LeadUser on Team 1
#   3 Priya Sharma  priya-pass1   - NormalUser on Team 1, is_resource
#   4 Tom Baxter    tom-pass1     - LeadUser on Team 1, is_resource
#   5 Grace Liu     grace-pass1   - NormalUser on Team 1, is_resource
#   6 Sam Patel     sam-pass1     - ReadOnlyUser on Team 1
#   7 Nadia Fischer nadia-pass1   - is_organisation_admin, NormalUser on Team 1 (not a resource)
#
# Team 2 ("V1.2 Import", V2/database/seed/002_team2_from_v1.sql) — the real
# V1.2 data, migrated wholesale; only a handful of its ~50 People can log in
# (see that file's own header comment for the rest):
#   10010 Neil  neil-pass1   - is_organisation_admin, TeamLeadUser on Team 2, is_resource
#   10033 Rahul rahul-pass1  - NormalUser on Team 2, is_resource
#
# Neil's is_organisation_admin (real V1.2 data, not a seeding choice) means
# his token can act on *any* Team via the org-admin path — a test that
# specifically needs "a TeamLeadUser of some Team other than Team 1, and
# nothing more" can't use him for that reason (see
# test_team_lead_cannot_write_person_role_for_other_team's own fresh-team
# bootstrap instead).
PASSWORDS = {
    "alice.chen@example.com": "alice-pass1",
    "ben.okafor@example.com": "ben-pass1",
    "priya.sharma@example.com": "priya-pass1",
    "tom.baxter@example.com": "tom-pass1",
    "grace.liu@example.com": "grace-pass1",
    "sam.patel@example.com": "sam-pass1",
    "nadia.fischer@example.com": "nadia-pass1",
    "neil@example.com": "neil-pass1",
    "rahul@example.com": "rahul-pass1",
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
    # LeadUser on Team 1 (not a TeamLeadUser, and not on any other Team) — a
    # second, non-lead Team-1 identity distinct from Alice/Ben.
    return _login(api, "tom.baxter@example.com")


@pytest.fixture(scope="session")
def neil_token(api):
    # TeamLeadUser on Team 2 ("V1.2 Import") — used where a test needs
    # genuine standing on a Team other than Team 1, now that Team 1 and
    # Team 2 are no longer the two small, symmetric example Teams they used
    # to be (see PASSWORDS above and 002_team2_from_v1.sql).
    return _login(api, "neil@example.com")


@pytest.fixture(scope="session")
def rahul_token(api):
    # An ordinary NormalUser on Team 2, distinct from Neil (its
    # TeamLeadUser) — used where a test needs "someone on the other Team,
    # but not its lead".
    return _login(api, "rahul@example.com")


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
    return 10028  # A real Team 2 ("V1.2 Import") project — Team 1 people hold no role here.


@pytest.fixture(scope="session")
def other_team_task_id():
    return 10960  # A real Task under other_team_project_id (Team 2).


@pytest.fixture()
def alice_owned_remark_id(api, alice_token):
    resp = api.post(
        "/remark",
        json={"task_id": 3, "remark_text": "owned by alice for this test"},
        headers=auth(alice_token),
    )
    assert resp.status_code == 201, resp.text
    return resp.json()["remark_id"]
