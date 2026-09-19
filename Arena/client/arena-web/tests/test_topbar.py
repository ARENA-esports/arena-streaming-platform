"""
test_topbar.py – Selenium tests for TopBar & Global Search.

Test coverage:
  1. Geometry Check     – TopBar height is exactly 56px.
  2. Input Styling      – Search input and header buttons are 34px tall
                          with 14px border-radius.
  3. Search Routing     – Typing a team ID and pressing Enter navigates
                          to /matches?teamId={query}.
"""

import time
import pytest
from selenium.webdriver.support.ui import WebDriverWait
from pages.topbar_page import TopBarPage


class TestTopBarGeometry:
    """1. TopBar height must be exactly 56px and must not compress."""

    def test_topbar_height_is_56px(self, driver):
        """
        GIVEN the home page is loaded
        WHEN  we inspect the <header class="topbar"> element
        THEN  its computed height must be exactly 56px.
        """
        page = TopBarPage(driver)
        height = page.get_topbar_height()
        assert height == "56px", (
            f"TopBar height mismatch: expected '56px', got '{height}'"
        )


class TestTopBarInputStyling:
    """2. Global search input and header action buttons must have
       height=34px and border-radius=14px."""

    def test_search_input_height(self, driver):
        """Search input computed height must be 34px."""
        page = TopBarPage(driver)
        height = page.get_search_input_height()
        assert height == "34px", (
            f"Search input height mismatch: expected '34px', got '{height}'"
        )

    def test_search_input_border_radius(self, driver):
        """Search input computed border-radius must be 14px."""
        page = TopBarPage(driver)
        br = page.get_search_input_border_radius()
        assert br == "14px", (
            f"Search input border-radius mismatch: expected '14px', got '{br}'"
        )

    def test_header_buttons_styling(self, driver):
        """
        Every visible header action button (btn-ghost, btn-solid) must have
        height=34px and border-radius=14px.
        
        On the logged-out home page the buttons are:
          • "Log In"  (btn-ghost)
          • "Sign Up" (btn-solid)
        """
        page = TopBarPage(driver)
        buttons = page.get_header_button_styles()

        # There should be at least one button visible
        assert len(buttons) > 0, "No header action buttons found"

        for btn in buttons:
            assert btn["height"] == "34px", (
                f"Button '{btn['label']}' height mismatch: "
                f"expected '34px', got '{btn['height']}'"
            )
            assert btn["border-radius"] == "14px", (
                f"Button '{btn['label']}' border-radius mismatch: "
                f"expected '14px', got '{btn['border-radius']}'"
            )


class TestGlobalSearchRouting:
    """3. Typing a team name into the TopBar search and pressing Enter
       must navigate to /matches?teamId={query}."""

    def test_search_navigates_to_matches_with_team_id(self, driver):
        """
        GIVEN the user is on the home page
        WHEN  they type '42' into the global search input and press Enter
        THEN  the URL must contain '/matches?teamId=42'
        """
        page = TopBarPage(driver)
        search_term = "42"
        page.search_for(search_term)

        # Wait for the navigation to complete
        WebDriverWait(driver, 10).until(
            lambda d: "/matches" in d.current_url
        )

        current = driver.current_url
        assert f"/matches?teamId={search_term}" in current, (
            f"Expected URL to contain '/matches?teamId={search_term}', "
            f"got '{current}'"
        )

    def test_search_with_text_query(self, driver):
        """
        GIVEN the user is on the home page
        WHEN  they type 'The Dragons' into the global search and press Enter
        THEN  the URL must contain '/matches?teamId=The%20Dragons'
               (or the URL-decoded equivalent)
        """
        page = TopBarPage(driver)
        search_term = "The Dragons"
        page.search_for(search_term)

        WebDriverWait(driver, 10).until(
            lambda d: "/matches" in d.current_url
        )

        current = driver.current_url
        # The URL will have the space encoded as %20 or +
        assert "/matches?teamId=" in current, (
            f"Expected URL to contain '/matches?teamId=', got '{current}'"
        )
        # Check the decoded form
        from urllib.parse import unquote
        decoded = unquote(current)
        assert "The Dragons" in decoded, (
            f"Expected decoded URL to contain 'The Dragons', "
            f"got '{decoded}'"
        )
