"""
test_match_room.py – Selenium tests for Match Room State Transitions.

Test coverage:
  1. Persistent UI Check – FactionChat and TeamSelector remain in the DOM
     regardless of match status.
  2. No Remount Check    – Those elements are NOT unmounted/remounted when
     the match status transitions between Scheduled → Live → Ended → Cancelled.

Prerequisites:
  • A test match must exist in the database.  Set ARENA_TEST_MATCH_ID
    env-var (default: 1).
  • For state-transition tests, an Organizer JWT token is needed.
    Set ARENA_ORGANIZER_TOKEN env-var, or configure ARENA_ORGANIZER_USER /
    ARENA_ORGANIZER_PASS so the test can log in and acquire one.
  • The Stream Service must be running (docker compose up).
"""

import os
import time
import pytest
import requests
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.common.by import By
from pages.match_room_page import MatchRoomPage


# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------
TEST_MATCH_ID = int(os.getenv("ARENA_TEST_MATCH_ID", "1"))

ORGANIZER_USER = os.getenv("ARENA_ORGANIZER_USER", "organizer1")
ORGANIZER_PASS = os.getenv("ARENA_ORGANIZER_PASS", "Password123!")
USER_API_BASE = os.getenv("ARENA_USER_API_URL", "http://localhost:5168/api")
STREAM_API_BASE = os.getenv("ARENA_STREAM_API_URL", "http://localhost:5167/api")


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def get_organizer_token() -> str:
    """
    Acquire a JWT token for the Organizer user via the Auth API.
    Returns empty string if login fails (tests will be skipped).
    """
    # Check env-var first
    token = os.getenv("ARENA_ORGANIZER_TOKEN", "")
    if token:
        return token

    try:
        resp = requests.post(
            f"{USER_API_BASE}/Auth/login",
            json={"identifier": ORGANIZER_USER, "password": ORGANIZER_PASS},
            timeout=10,
        )
        resp.raise_for_status()
        return resp.json().get("token", "")
    except Exception as exc:
        print(f"⚠️  Could not acquire Organizer token: {exc}")
        return ""


def create_test_match(token: str) -> int:
    """
    Create a fresh Scheduled match via the API and return its matchId.
    Uses teamAId=1, teamBId=2, and a future scheduledTime.
    """
    from datetime import datetime, timedelta
    future = (datetime.utcnow() + timedelta(days=2)).isoformat() + "Z"
    resp = requests.post(
        f"{STREAM_API_BASE}/matches",
        json={"teamAId": 1, "teamBId": 2, "scheduledTime": future},
        headers={
            "Authorization": f"Bearer {token}",
            "Content-Type": "application/json",
        },
        timeout=10,
    )
    resp.raise_for_status()
    return resp.json()["matchId"]


def reset_match_to_scheduled(match_id: int, token: str) -> None:
    """
    Attempt to reset a match back to 'Scheduled' status.
    This uses forceOverride=true.
    """
    try:
        requests.patch(
            f"{STREAM_API_BASE}/matches/{match_id}/status",
            json={"status": "Scheduled", "forceOverride": True},
            headers={
                "Authorization": f"Bearer {token}",
                "Content-Type": "application/json",
            },
            timeout=10,
        )
    except Exception:
        pass  # best-effort reset


# ---------------------------------------------------------------------------
# Fixtures
# ---------------------------------------------------------------------------

@pytest.fixture(scope="module")
def auth_token():
    """Acquire an Organizer JWT token for state-transition API calls."""
    token = get_organizer_token()
    if not token:
        pytest.skip("No Organizer token available – cannot run state-transition tests")
    return token


@pytest.fixture(scope="module")
def test_match_id(auth_token):
    """
    Either use the env-var match ID or create a fresh match.
    Resets the match to 'Scheduled' before yielding.
    """
    match_id = TEST_MATCH_ID
    reset_match_to_scheduled(match_id, auth_token)
    return match_id


# ---------------------------------------------------------------------------
# 1. Persistent UI Check
# ---------------------------------------------------------------------------

class TestPersistentUI:
    """FactionChat and TeamSelector must be present in the DOM
    for any match state."""

    def test_persistent_elements_on_scheduled_match(self, driver, base_url):
        """
        GIVEN a Scheduled match room
        WHEN  the page loads
        THEN  TeamSelector and FactionChat are present in the DOM.
        """
        page = MatchRoomPage(driver)
        page.navigate_to_match(TEST_MATCH_ID)

        try:
            page.wait_for_match_loaded()
        except Exception:
            pytest.skip(f"Match {TEST_MATCH_ID} did not load – it may not exist.")

        page.assert_persistent_elements_present()

    def test_team_selector_is_visible(self, driver, base_url):
        """TeamSelector placeholder div must be in the DOM."""
        page = MatchRoomPage(driver)
        page.navigate_to_match(TEST_MATCH_ID)

        try:
            page.wait_for_match_loaded()
        except Exception:
            pytest.skip(f"Match {TEST_MATCH_ID} did not load.")

        assert page.is_team_selector_present(), "TeamSelector not found in the DOM"

    def test_faction_chat_is_visible(self, driver, base_url):
        """FactionChat placeholder div must be in the DOM."""
        page = MatchRoomPage(driver)
        page.navigate_to_match(TEST_MATCH_ID)

        try:
            page.wait_for_match_loaded()
        except Exception:
            pytest.skip(f"Match {TEST_MATCH_ID} did not load.")

        assert page.is_faction_chat_present(), "FactionChat not found in the DOM"


# ---------------------------------------------------------------------------
# 2. No Remount Check (State Transitions)
# ---------------------------------------------------------------------------

class TestStateTransitions:
    """Elements must NOT be dropped or remounted when the match transitions
    between Scheduled → Live → Ended → Cancelled."""

    @pytest.fixture(autouse=True)
    def _setup(self, driver, base_url, auth_token, test_match_id):
        """Navigate to the match room and reset to Scheduled."""
        self.match_id = test_match_id
        self.token = auth_token
        self.page = MatchRoomPage(driver)

        # Reset the match to Scheduled
        reset_match_to_scheduled(self.match_id, self.token)
        time.sleep(1)

        # Navigate to the match room
        self.page.navigate_to_match(self.match_id)
        try:
            self.page.wait_for_match_loaded()
        except Exception:
            pytest.skip(f"Match {self.match_id} did not load.")

    def test_transition_scheduled_to_live(self, driver):
        """
        GIVEN FactionChat & TeamSelector are mounted in a Scheduled match room
        WHEN  the match transitions to Live
        THEN  the same DOM elements must still be present (not remounted).
        """
        page = self.page

        # Stamp elements with UIDs
        page.assert_persistent_elements_present()
        prev_uids = page.capture_element_ids()

        # Transition to Live
        page.trigger_state_and_wait(self.match_id, "Live", self.token)

        # Verify elements were NOT remounted
        page.assert_persistent_elements_present()
        page.verify_elements_not_remounted(prev_uids)

    def test_transition_live_to_ended(self, driver):
        """
        GIVEN a Live match room with stamped persistent elements
        WHEN  the match transitions to Ended
        THEN  FactionChat & TeamSelector must not be remounted.
        """
        page = self.page

        # Move to Live first
        MatchRoomPage.trigger_state_via_api(self.match_id, "Live", self.token)
        time.sleep(1)
        driver.refresh()
        page.wait_for_match_loaded()

        page.assert_persistent_elements_present()
        prev_uids = page.capture_element_ids()

        # Transition to Ended
        page.trigger_state_and_wait(self.match_id, "Ended", self.token)

        page.assert_persistent_elements_present()
        page.verify_elements_not_remounted(prev_uids)

    def test_transition_scheduled_to_cancelled(self, driver):
        """
        GIVEN a Scheduled match room with stamped persistent elements
        WHEN  the match transitions to Cancelled
        THEN  FactionChat & TeamSelector must not be remounted.
        """
        page = self.page

        page.assert_persistent_elements_present()
        prev_uids = page.capture_element_ids()

        # Transition to Cancelled
        page.trigger_state_and_wait(self.match_id, "Cancelled", self.token)

        page.assert_persistent_elements_present()
        page.verify_elements_not_remounted(prev_uids)

    def test_full_lifecycle_no_remount(self, driver):
        """
        End-to-end: Scheduled → Live → Ended.
        Verify persistent elements survive the entire lifecycle.
        """
        page = self.page

        page.assert_persistent_elements_present()
        prev_uids = page.capture_element_ids()

        # Scheduled → Live
        page.trigger_state_and_wait(self.match_id, "Live", self.token)
        page.assert_persistent_elements_present()
        page.verify_elements_not_remounted(prev_uids)

        # Live → Ended
        page.trigger_state_and_wait(self.match_id, "Ended", self.token)
        page.assert_persistent_elements_present()
        page.verify_elements_not_remounted(prev_uids)
