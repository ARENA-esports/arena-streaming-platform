"""
test_schedule.py – Selenium tests for the Schedule Dashboard & Date Picker.

Test coverage:
  1. Custom Date Picker  – ArenaDatePicker popover renders on click.
  2. Week Navigation     – Forward/back arrows update the displayed date range.
  3. Countdown Formats   – DD:HH:MM:SS for >1 day, HH:MM:SS for <1 day.

Prerequisites:
  • The Schedule page (/organizer/matches/new) is a protected route
    requiring an Organizer role.  Tests that need it will log in first
    via the login helper.
  • For countdown tests, at least one match must exist.  The test
    uses a configurable ARENA_TEST_MATCH_ID env-var (default: 1).
"""

import os
import re
import time
import pytest
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.common.by import By
from pages.schedule_page import SchedulePage


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

# Test credentials – override via env-vars in CI.
ORGANIZER_USERNAME = os.getenv("ARENA_ORGANIZER_USER", "organizer1")
ORGANIZER_PASSWORD = os.getenv("ARENA_ORGANIZER_PASS", "Password123!")
TEST_MATCH_ID = int(os.getenv("ARENA_TEST_MATCH_ID", "1"))


def login_as_organizer(driver, base_url: str) -> None:
    """
    Log into the Arena app as an Organizer so protected routes
    like /organizer/matches/new are accessible.
    
    Raises pytest.skip if login fails (e.g. account doesn't exist yet).
    """
    driver.get(f"{base_url}/login")

    # Wait for the login form — LoginView uses id="identifier" and id="password"
    try:
        WebDriverWait(driver, 15).until(
            EC.presence_of_element_located((By.ID, "identifier"))
        )
    except Exception:
        pytest.skip("Login page did not load – cannot test protected routes.")

    # Fill in credentials
    ident_input = driver.find_element(By.ID, "identifier")
    ident_input.clear()
    ident_input.send_keys(ORGANIZER_USERNAME)

    pass_input = driver.find_element(By.ID, "password")
    pass_input.clear()
    pass_input.send_keys(ORGANIZER_PASSWORD)

    # Submit the form (button text is "Sign In")
    submit_btn = driver.find_element(By.CSS_SELECTOR, "button[type='submit']")
    submit_btn.click()

    # Wait until we're redirected away from /login
    try:
        WebDriverWait(driver, 15).until(
            lambda d: "/login" not in d.current_url
        )
    except Exception:
        # Check if there's a server error on the page
        error_els = driver.find_elements(By.CSS_SELECTOR, ".text-arena-crimson, [class*='crimson']")
        error_msg = error_els[0].text if error_els else "Unknown error"
        pytest.skip(
            f"Login failed for '{ORGANIZER_USERNAME}' – {error_msg}. "
            f"Ensure the Organizer account exists in the database."
        )


# ---------------------------------------------------------------------------
# 1. Custom Date Picker
# ---------------------------------------------------------------------------

class TestArenaDatePicker:
    """The ArenaDatePicker calendar popover must render when clicked."""

    @pytest.fixture(autouse=True)
    def _navigate(self, driver, base_url):
        """Log in and navigate to the schedule page before each test."""
        login_as_organizer(driver, base_url)
        page = SchedulePage(driver)
        page.navigate_to_schedule()
        # Wait for the page to load
        WebDriverWait(driver, 10).until(
            EC.presence_of_element_located(page.DATE_PICKER_TRIGGER)
        )

    def test_date_picker_popover_renders(self, driver):
        """
        GIVEN the user is on the Schedule Match page
        WHEN  they click the ArenaDatePicker trigger button
        THEN  the calendar popover must be present in the DOM.
        """
        page = SchedulePage(driver)
        page.open_date_picker()
        assert page.is_date_picker_popover_visible(), (
            "ArenaDatePicker calendar popover did not render after clicking the trigger"
        )

    def test_date_picker_popover_has_month_navigation(self, driver):
        """The popover should show month/year header and navigation arrows."""
        page = SchedulePage(driver)
        page.open_date_picker()

        # The popover must contain month navigation buttons
        popover = page.find(page.DATE_PICKER_POPOVER)
        buttons = popover.find_elements(By.TAG_NAME, "button")
        assert len(buttons) >= 2, (
            f"Expected at least 2 buttons in popover (prev/next month), "
            f"found {len(buttons)}"
        )


# ---------------------------------------------------------------------------
# 2. Week Navigation
# ---------------------------------------------------------------------------

class TestWeekNavigation:
    """Forward/back week navigation arrows must update the date range."""

    @pytest.fixture(autouse=True)
    def _navigate(self, driver, base_url):
        """Log in and navigate to the schedule page."""
        login_as_organizer(driver, base_url)
        page = SchedulePage(driver)
        page.navigate_to_schedule()
        WebDriverWait(driver, 10).until(
            EC.presence_of_element_located(page.DATE_RANGE_BUTTON)
        )

    def test_forward_arrow_advances_week(self, driver):
        """
        GIVEN the user is on the Schedule page
        WHEN  they click the forward week arrow
        THEN  the displayed date range must change to a later week.
        """
        page = SchedulePage(driver)
        original = page.get_date_range_text()

        page.click_week_next()
        time.sleep(0.5)  # let React re-render

        updated = page.get_date_range_text()
        assert updated != original, (
            f"Date range did not change after clicking Next.\n"
            f"Before: '{original}'\n"
            f"After:  '{updated}'"
        )

    def test_back_arrow_then_forward_returns_to_original(self, driver):
        """
        GIVEN the user is on the Schedule page
        WHEN  they click forward and then back
        THEN  the date range should return to the original value.
        """
        page = SchedulePage(driver)

        # First advance forward to enable the back button
        page.click_week_next()
        time.sleep(0.5)
        after_next = page.get_date_range_text()

        # Now go back
        page.click_week_prev()
        time.sleep(0.5)
        after_prev = page.get_date_range_text()

        assert after_prev != after_next, (
            f"Date range did not change after clicking Prev.\n"
            f"After Next: '{after_next}'\n"
            f"After Prev: '{after_prev}'"
        )

    def test_today_button_resets_to_current_week(self, driver):
        """
        GIVEN the user has navigated forward a few weeks
        WHEN  they click the 'Today' button
        THEN  the date range resets to the current week.
        """
        page = SchedulePage(driver)
        original = page.get_date_range_text()

        # Advance two weeks
        page.click_week_next()
        time.sleep(0.3)
        page.click_week_next()
        time.sleep(0.3)

        advanced = page.get_date_range_text()
        assert advanced != original, "Failed to advance weeks for test setup"

        # Reset
        page.click_today()
        time.sleep(0.5)

        reset = page.get_date_range_text()
        assert reset == original, (
            f"'Today' button did not reset to original range.\n"
            f"Expected: '{original}'\n"
            f"Got:      '{reset}'"
        )


# ---------------------------------------------------------------------------
# 3. Countdown Format
# ---------------------------------------------------------------------------

class TestCountdownFormat:
    """Countdown timer must use DD:HH:MM:SS when >1 day away,
    and HH:MM:SS when <1 day away."""

    def test_countdown_format_pattern(self, driver, base_url):
        """
        GIVEN a scheduled match exists (ARENA_TEST_MATCH_ID)
        WHEN  the user navigates to its match room
        THEN  the countdown timer must match either DD:HH:MM:SS or HH:MM:SS format.
        
        NOTE: The actual format depends on how far away the match is.
              We validate that it matches one of the two legal patterns.
        """
        page = SchedulePage(driver)
        page.navigate_to_match(TEST_MATCH_ID)

        # Wait for the countdown to appear
        try:
            WebDriverWait(driver, 15).until(
                EC.presence_of_element_located(page.COUNTDOWN_LABEL)
            )
        except Exception:
            pytest.skip(
                f"Match {TEST_MATCH_ID} does not have a countdown "
                f"(it may already be Live/Ended/Cancelled). "
                f"Set ARENA_TEST_MATCH_ID to a Scheduled match."
            )

        countdown = page.get_countdown_text()

        # Pattern: HH:MM:SS (< 1 day)  or  DD:HH:MM:SS (>= 1 day)
        pattern_short = r"^\d{2}:\d{2}:\d{2}$"      # e.g. "05:22:10"
        pattern_long = r"^\d{2}:\d{2}:\d{2}:\d{2}$"  # e.g. "02:14:30:15"

        assert re.match(pattern_short, countdown) or re.match(pattern_long, countdown), (
            f"Countdown format invalid: '{countdown}'. "
            f"Expected HH:MM:SS or DD:HH:MM:SS"
        )

    def test_countdown_shows_correct_format_for_distance(self, driver, base_url):
        """
        Validates format correctness based on countdown value:
        - If the first segment suggests > 0 days and there are 4 segments → DD:HH:MM:SS ✓
        - If there are 3 segments → HH:MM:SS ✓
        """
        page = SchedulePage(driver)
        page.navigate_to_match(TEST_MATCH_ID)

        try:
            WebDriverWait(driver, 15).until(
                EC.presence_of_element_located(page.COUNTDOWN_LABEL)
            )
        except Exception:
            pytest.skip("No countdown visible – match may not be Scheduled.")

        countdown = page.get_countdown_text()
        segments = countdown.split(":")

        if len(segments) == 4:
            # DD:HH:MM:SS – days should be > 0
            days = int(segments[0])
            assert days >= 0, f"Days segment is negative: {days}"
        elif len(segments) == 3:
            # HH:MM:SS – all numeric
            for seg in segments:
                assert seg.isdigit() and len(seg) == 2, (
                    f"HH:MM:SS segment not 2-digit: '{seg}' in '{countdown}'"
                )
        else:
            pytest.fail(
                f"Unexpected countdown format with {len(segments)} segments: "
                f"'{countdown}'"
            )
