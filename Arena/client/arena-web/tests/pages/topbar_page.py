"""
topbar_page.py – Page-Object for the Arena TopBar component.

The TopBar is rendered by <TopBar /> inside DashboardLayout.
HTML structure (from TopBar.tsx):
  <header class="topbar">
    <a class="brand">arena</a>
    <div class="search-wrap">
      <svg />            ← magnifying-glass icon
      <input type="text" placeholder="Search by Team ID..." />
    </div>
    <div class="topbar-right">
      ...  <a class="btn-ghost">  |  <a class="btn-solid">
      <button class="w-[34px] h-[34px] rounded-full …"> ← avatar/user button
    </div>
  </header>

CSS (from index.css):
  .topbar       → height: 56px; min-height: 56px; max-height: 56px
  .search-wrap input → height: 34px; border-radius: 14px
  .btn-ghost    → height: 34px; border-radius: 14px
  .btn-solid    → height: 34px; border-radius: 14px
"""

from selenium.webdriver.common.by import By
from selenium.webdriver.common.keys import Keys
from .base_page import BasePage


class TopBarPage(BasePage):
    """Encapsulates locators and actions for the top navigation bar."""

    # ── Locators ──────────────────────────────────────────────────────────
    TOPBAR = (By.CSS_SELECTOR, "header.topbar")
    SEARCH_INPUT = (By.CSS_SELECTOR, ".search-wrap input[type='text']")
    BTN_GHOST = (By.CSS_SELECTOR, ".topbar-right .btn-ghost")
    BTN_SOLID = (By.CSS_SELECTOR, ".topbar-right .btn-solid")
    AVATAR_BUTTON = (By.CSS_SELECTOR, ".topbar-right button.rounded-full")

    # ── Actions ───────────────────────────────────────────────────────────

    def search_for(self, query: str) -> None:
        """Type *query* into the global search input and press Enter."""
        el = self.wait_for_clickable(self.SEARCH_INPUT)
        el.clear()
        el.send_keys(query)
        el.send_keys(Keys.ENTER)

    # ── Geometry / Style Assertions ───────────────────────────────────────

    def get_topbar_height(self) -> str:
        """Return the computed height of the topbar (e.g. '56px')."""
        return self.get_css(self.TOPBAR, "height")

    def get_search_input_height(self) -> str:
        return self.get_css(self.SEARCH_INPUT, "height")

    def get_search_input_border_radius(self) -> str:
        return self.get_css(self.SEARCH_INPUT, "border-radius")

    def get_header_button_styles(self) -> list[dict[str, str]]:
        """
        Return height & border-radius for every header action button
        (btn-ghost, btn-solid, avatar circle).
        """
        results: list[dict[str, str]] = []
        # btn-ghost buttons (e.g. "Schedule", "Log In")
        for el in self.find_all(self.BTN_GHOST):
            results.append({
                "label": el.text.strip(),
                "height": self.get_element_css(el, "height"),
                "border-radius": self.get_element_css(el, "border-radius"),
            })
        # btn-solid buttons (e.g. "Sign Up")
        for el in self.find_all(self.BTN_SOLID):
            results.append({
                "label": el.text.strip(),
                "height": self.get_element_css(el, "height"),
                "border-radius": self.get_element_css(el, "border-radius"),
            })
        return results
