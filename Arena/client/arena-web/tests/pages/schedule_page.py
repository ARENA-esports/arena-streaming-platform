"""
schedule_page.py – Page-Object for the Schedule Dashboard.

Components covered:
  • ScheduleHeader  (src/components/schedule/ScheduleHeader.tsx)
      – "Today" button, prev/next week arrows, date-range display
  • ArenaDatePicker  (src/components/common/ArenaDatePicker.tsx)
      – Trigger button, calendar popover dropdown
  • ScheduledView    (src/components/match/ScheduledView.tsx)
      – Countdown timer displayed in the match room

Routes:
  /organizer/matches/new  →  ScheduleMatchView (contains ScheduleHeader + ArenaDatePicker)
  /matches/:matchId       →  MatchRoomView (contains ScheduledView with countdown)
"""

from selenium.webdriver.common.by import By
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.support.ui import WebDriverWait
from .base_page import BasePage


class SchedulePage(BasePage):
    """Encapsulates locators and actions for the Schedule dashboard."""

    # ── ScheduleHeader locators ──────────────────────────────────────────
    # The ScheduleHeader renders inside ScheduleMatchView at /organizer/matches/new.
    # Structure:
    #   <div> …
    #     <button> Today </button>
    #     <div> <button> <ChevronLeft/> </button>  <button> <ChevronRight/> </button> </div>
    #     <button> <CalendarIcon/> <span> Sep 15, 2026 – Sep 21, 2026 </span> </button>
    #   </div>

    # "Today" pill button
    TODAY_BUTTON = (By.XPATH, "//button[normalize-space()='Today']")

    # Prev-week arrow (ChevronLeft is the only <svg> inside the first nav button)
    WEEK_PREV = (By.XPATH,
        "//button[normalize-space()='Today']/following-sibling::div//button[1]"
    )

    # Next-week arrow (second button in the same sibling div)
    WEEK_NEXT = (By.XPATH,
        "//button[normalize-space()='Today']/following-sibling::div//button[2]"
    )

    # Date-range display – the <button> that contains a <span> with the date range text.
    # It sits right after the chevron div and contains a CalendarIcon svg.
    DATE_RANGE_BUTTON = (By.XPATH,
        "//button[normalize-space()='Today']/following-sibling::button[contains(@class,'font-mono')]"
    )

    # ── ArenaDatePicker locators ─────────────────────────────────────────
    # The trigger button inside ArenaDatePicker
    DATE_PICKER_TRIGGER = (By.XPATH,
        "//label[contains(text(),'SCHEDULED START TIME')]/following-sibling::button"
    )

    # The popover calendar dropdown (renders conditionally when isOpen=true)
    DATE_PICKER_POPOVER = (By.CSS_SELECTOR,
        "div.absolute.top-full"
    )

    # "Apply Date & Time" button inside the popover
    DATE_PICKER_APPLY = (By.XPATH,
        "//button[normalize-space()='Apply Date & Time']"
    )

    # ── ScheduledView (countdown) locators ───────────────────────────────
    # The countdown timer inside ScheduledView
    # Structure: <div class="text-5xl md:text-7xl font-mono …"> DD:HH:MM:SS </div>
    COUNTDOWN_TIMER = (By.CSS_SELECTOR,
        "div.font-mono.font-bold.drop-shadow-\\[0_0_15px_rgba\\(0\\,184\\,252\\,0\\.4\\)\\]"
    )

    # Fallback: find by the "Stream starts in" label and then its next sibling
    COUNTDOWN_LABEL = (By.XPATH,
        "//div[contains(text(),'Stream starts in')]"
    )

    COUNTDOWN_VALUE = (By.XPATH,
        "//div[contains(text(),'Stream starts in')]/following-sibling::div[1]"
    )

    # ── Actions ───────────────────────────────────────────────────────────

    def navigate_to_schedule(self) -> None:
        """Navigate to the Organizer schedule-match page."""
        self.navigate_to("/organizer/matches/new")

    def navigate_to_match(self, match_id: int) -> None:
        """Navigate to a specific match room (for countdown tests)."""
        self.navigate_to(f"/matches/{match_id}")

    def click_today(self) -> None:
        self.click(self.TODAY_BUTTON)

    def click_week_prev(self) -> None:
        self.click(self.WEEK_PREV)

    def click_week_next(self) -> None:
        self.click(self.WEEK_NEXT)

    def get_date_range_text(self) -> str:
        """Return the visible date-range string, e.g. 'Sep 15, 2026 – Sep 21, 2026'."""
        el = self.find(self.DATE_RANGE_BUTTON)
        return el.text.strip()

    def open_date_picker(self) -> None:
        """Click the ArenaDatePicker trigger to open its calendar popover."""
        self.click(self.DATE_PICKER_TRIGGER)

    def is_date_picker_popover_visible(self) -> bool:
        """Return True if the calendar popover is present in the DOM."""
        try:
            WebDriverWait(self.driver, 5).until(
                EC.presence_of_element_located(self.DATE_PICKER_POPOVER)
            )
            return True
        except Exception:
            return False

    def get_countdown_text(self) -> str:
        """Return the raw text of the countdown timer (e.g. '02:14:30:15' or '05:22:10')."""
        try:
            el = self.find(self.COUNTDOWN_VALUE)
            return el.text.strip()
        except Exception:
            # fallback via CSS selector
            el = self.find(self.COUNTDOWN_TIMER)
            return el.text.strip()
