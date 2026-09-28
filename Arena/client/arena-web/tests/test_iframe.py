from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC

driver = webdriver.Chrome()
driver.maximize_window()

try:
    driver.get("http://localhost:5173")

    # Increased wait time to 30 seconds to account for slow local loads
    iframe = WebDriverWait(driver, 30).until(
        EC.presence_of_element_located((By.TAG_NAME, "iframe"))
    )

    src_url = iframe.get_attribute("src")
    print(f"Detected Iframe SRC: {src_url}")

    assert "parent=localhost" in src_url, "FAILED: parent=localhost not found in iframe src"
    print("PASS: parent=localhost is present in the dev environment.")

    driver.save_screenshot("104-dev-parent-param-selenium.png")

except Exception as e:
    # If it fails again, take a picture of what it's looking at
    driver.save_screenshot("selenium-error-state.png")
    print("Failed to find the iframe. Check 'selenium-error-state.png' to see what the browser was stuck on.")
    raise e

finally:
    driver.quit()