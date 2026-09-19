"""
match_room_page.py – Page-Object for the Match Room view.

The MatchRoomView (/matches/:matchId) renders:
  • Left column – swaps between ScheduledView / StreamContainer / EndedView / CancelledView
  • Right column – *always mounted*:
      - TeamSelector   (placeholder div, class includes "arena-card")
      - FactionChat    (placeholder div, class includes "arena-card")
      - BattleBar      (placeholder div)

This page object focuses on:
  1. Verifying persistent nested UI elements are in the DOM.
  2. Checking they are NOT remounted across state transitions
     (Scheduled → Live → Ended → Cancelled) by capturing and comparing
     element identity via JavaScript object identity.
  3. Triggering state transitions via the backend PATCH /matches/:id/status API.
"""

import os
import requests
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.remote.webelement import WebElement
from .base_page import BasePage


# The Stream Service API base used for direct state-transition calls.
STREAM_API_BASE = os.getenv("ARENA_STREAM_API_URL", "http://localhost:5167/api")


class MatchRoomPage(BasePage):
    """Encapsulates locators and actions for the Match Room view."""

    # ── Locators ──────────────────────────────────────────────────────────
    # TeamSelector placeholder
    TEAM_SELECTOR = (By.XPATH,
        "//div[contains(text(),'TeamSelector Placeholder')]"
    )

    # FactionChat placeholder
    FACTION_CHAT = (By.XPATH,
        "//div[contains(text(),'FactionChat Placeholder')]"
    )

    # Status badge (Badge component renders a <span> with status text)
    STATUS_BADGE = (By.XPATH,
        "//span[contains(@class,'uppercase') and ("
        "text()='Scheduled' or text()='Live' or text()='Ended' or text()='Cancelled'"
        ")]"
    )

    # Match overview heading
    MATCH_HEADING = (By.XPATH, "//h1[contains(text(),'Match Overview')]")

    # ScheduledView (countdown container)
    SCHEDULED_VIEW = (By.XPATH,
        "//div[contains(text(),'Stream starts in')]/.."
    )

    # Loading spinner
    LOADING_SPINNER = (By.CSS_SELECTOR, "div.animate-spin")

    # ── Navigation ────────────────────────────────────────────────────────

    def navigate_to_match(self, match_id: int) -> None:
        """Navigate to a specific match room."""
        self.navigate_to(f"/matches/{match_id}")

    def wait_for_match_loaded(self) -> None:
        """Wait until the match room has finished loading."""
        WebDriverWait(self.driver, self.timeout).until(
            EC.presence_of_element_located(self.MATCH_HEADING)
        )

    # ── Persistent UI Checks ─────────────────────────────────────────────

    def is_team_selector_present(self) -> bool:
        return len(self.find_all(self.TEAM_SELECTOR)) > 0

    def is_faction_chat_present(self) -> bool:
        return len(self.find_all(self.FACTION_CHAT)) > 0

    def assert_persistent_elements_present(self) -> None:
        """Assert both FactionChat and TeamSelector are in the DOM."""
        assert self.is_team_selector_present(), \
            "TeamSelector is missing from the DOM"
        assert self.is_faction_chat_present(), \
            "FactionChat is missing from the DOM"

    def capture_element_ids(self) -> dict[str, str]:
        """
        Capture a fingerprint for each persistent element using a JS-injected
        attribute.  Selenium's internal element IDs change across find calls,
        so we stamp a custom `data-arena-uid` attribute with a unique value.
        
        Returns a dict like:
          { "TeamSelector": "uid-1", "FactionChat": "uid-2" }
        """
        uids: dict[str, str] = {}
        for name, locator in [("TeamSelector", self.TEAM_SELECTOR),
                               ("FactionChat", self.FACTION_CHAT)]:
            el = self.find(locator)
            # If we already stamped it, read the existing value
            existing = el.get_attribute("data-arena-uid")
            if existing:
                uids[name] = existing
            else:
                import uuid
                uid = f"uid-{uuid.uuid4().hex[:8]}"
                self.driver.execute_script(
                    "arguments[0].setAttribute('data-arena-uid', arguments[1])",
                    el, uid
                )
                uids[name] = uid
        return uids

    def verify_elements_not_remounted(self, prev_uids: dict[str, str]) -> None:
        """
        After a state transition, re-locate the persistent elements and verify
        that the custom `data-arena-uid` attribute we stamped earlier is still
        present.  If React unmounted and re-created the DOM node, the attribute
        will be gone.
        """
        for name, locator in [("TeamSelector", self.TEAM_SELECTOR),
                               ("FactionChat", self.FACTION_CHAT)]:
            el = self.find(locator)
            current_uid = el.get_attribute("data-arena-uid")
            assert current_uid == prev_uids[name], (
                f"{name} was remounted! "
                f"Expected data-arena-uid='{prev_uids[name]}', "
                f"got '{current_uid}'"
            )

    # ── State Transition Helpers ─────────────────────────────────────────

    @staticmethod
    def trigger_state_via_api(match_id: int, new_status: str, auth_token: str | None = None) -> None:
        """
        Call PATCH /api/matches/{id}/status to transition the match to
        *new_status* ('Live', 'Ended', 'Cancelled').
        
        Requires a valid Organizer JWT token.  Pass via the `auth_token`
        parameter or the ARENA_ORGANIZER_TOKEN env-var.
        """
        token = auth_token or os.getenv("ARENA_ORGANIZER_TOKEN", "")
        url = f"{STREAM_API_BASE}/matches/{match_id}/status"
        headers = {
            "Content-Type": "application/json",
        }
        if token:
            headers["Authorization"] = f"Bearer {token}"

        payload = {
            "status": new_status,
            "forceOverride": True,
        }
        resp = requests.patch(url, json=payload, headers=headers, timeout=10)
        resp.raise_for_status()

    def trigger_state_and_wait(self, match_id: int, new_status: str,
                                auth_token: str | None = None) -> None:
        """
        Transition the match via the API, then wait for the badge in the UI
        to reflect the new status (the MatchRoomView polls every 15s, but
        we force a page refresh to speed things up).
        """
        self.trigger_state_via_api(match_id, new_status, auth_token)

        # Force the polling hook to re-fetch immediately
        self.driver.refresh()
        self.wait_for_match_loaded()
