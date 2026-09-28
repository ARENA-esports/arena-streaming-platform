"""
conftest.py – Shared Selenium fixtures for Arena UI tests.

Provides:
  - driver:           A Chrome WebDriver (headless by default) that navigates
                      to BASE_URL before each test and quits after.
  - base_url:         The root URL of the running Vite dev-server.
  - screenshot_on_failure:  Auto-captures a screenshot when a test fails.
"""

import os
import pytest
from selenium import webdriver
from selenium.webdriver.chrome.service import Service as ChromeService
from selenium.webdriver.chrome.options import Options

# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------
BASE_URL = os.getenv("ARENA_BASE_URL", "http://localhost:5173")
SCREENSHOTS_DIR = os.path.join(os.path.dirname(__file__), "screenshots")


# ---------------------------------------------------------------------------
# Fixtures
# ---------------------------------------------------------------------------

@pytest.fixture(scope="session")
def base_url():
    """The root URL the Vite dev-server is running on."""
    return BASE_URL


@pytest.fixture(scope="function")
def driver(base_url):
    """
    Yields a Chrome WebDriver instance.
    - Headless by default (set ARENA_HEADLESS=0 to see the browser).
    - Window size fixed to 1920x1080 for consistent CSS computed values.
    - Navigates to `base_url` before yielding.
    - Quits after the test function completes.
    """
    options = Options()

    headless = os.getenv("ARENA_HEADLESS", "1") != "0"
    if headless:
        options.add_argument("--headless=new")

    options.add_argument("--disable-gpu")
    options.add_argument("--window-size=1920,1080")
    options.add_argument("--no-sandbox")
    options.add_argument("--disable-dev-shm-usage")

    drv = webdriver.Chrome(options=options)
    drv.implicitly_wait(5)
    drv.get(base_url)

    yield drv

    drv.quit()


@pytest.fixture(autouse=True)
def screenshot_on_failure(request, driver):
    """Automatically save a screenshot when a test fails."""
    yield
    rep = getattr(request.node, "rep_call", None)
    if rep and rep.failed:
        os.makedirs(SCREENSHOTS_DIR, exist_ok=True)
        path = os.path.join(SCREENSHOTS_DIR, f"{request.node.name}.png")
        driver.save_screenshot(path)
        print(f"\n📸 Screenshot saved: {path}")


@pytest.hookimpl(tryfirst=True, hookwrapper=True)
def pytest_runtest_makereport(item, call):
    """Attach test outcome to the request.node so the fixture above can read it."""
    import pluggy
    outcome = yield
    rep = outcome.get_result()
    setattr(item, f"rep_{rep.when}", rep)
