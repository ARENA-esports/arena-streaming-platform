"""
base_page.py – Abstract base for every Page-Object.

Centralises Selenium wait-logic, element location, and common user
actions so individual page objects stay thin.
"""

from selenium.webdriver.remote.webdriver import WebDriver
from selenium.webdriver.remote.webelement import WebElement
from selenium.webdriver.common.by import By
from selenium.webdriver.common.keys import Keys
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC


DEFAULT_TIMEOUT = 15  # seconds


class BasePage:
    """Thin wrapper around Selenium primitives used by all page objects."""

    def __init__(self, driver: WebDriver, timeout: int = DEFAULT_TIMEOUT):
        self.driver = driver
        self.timeout = timeout

    # ----- Waits & Lookups ------------------------------------------------

    def wait_for(self, locator: tuple[str, str], timeout: int | None = None) -> WebElement:
        """Wait until an element matching *locator* is present in the DOM."""
        t = timeout or self.timeout
        return WebDriverWait(self.driver, t).until(
            EC.presence_of_element_located(locator)
        )

    def wait_for_visible(self, locator: tuple[str, str], timeout: int | None = None) -> WebElement:
        """Wait until an element is both present *and* visible."""
        t = timeout or self.timeout
        return WebDriverWait(self.driver, t).until(
            EC.visibility_of_element_located(locator)
        )

    def wait_for_clickable(self, locator: tuple[str, str], timeout: int | None = None) -> WebElement:
        """Wait until an element is clickable."""
        t = timeout or self.timeout
        return WebDriverWait(self.driver, t).until(
            EC.element_to_be_clickable(locator)
        )

    def find(self, locator: tuple[str, str]) -> WebElement:
        """Shorthand for driver.find_element(*locator) with an implicit wait."""
        return self.wait_for(locator)

    def find_all(self, locator: tuple[str, str]) -> list[WebElement]:
        """Return *all* matching elements (may be empty)."""
        return self.driver.find_elements(*locator)

    # ----- Actions --------------------------------------------------------

    def click(self, locator: tuple[str, str]) -> None:
        self.wait_for_clickable(locator).click()

    def type_text(self, locator: tuple[str, str], text: str) -> None:
        """Clear the field and type *text* into it."""
        el = self.wait_for_clickable(locator)
        el.clear()
        el.send_keys(text)

    def press_enter(self, locator: tuple[str, str]) -> None:
        self.find(locator).send_keys(Keys.ENTER)

    # ----- CSS Assertions -------------------------------------------------

    def get_css(self, locator: tuple[str, str], prop: str) -> str:
        """Return the computed CSS property of the first matching element."""
        return self.find(locator).value_of_css_property(prop)

    def get_element_css(self, element: WebElement, prop: str) -> str:
        """Return a computed CSS property from an already-located element."""
        return element.value_of_css_property(prop)

    # ----- Navigation -----------------------------------------------------

    def navigate_to(self, path: str) -> None:
        """Navigate within the SPA by appending *path* to the current origin."""
        base = self.driver.execute_script("return window.location.origin")
        self.driver.get(f"{base}{path}")

    def current_url(self) -> str:
        return self.driver.current_url
