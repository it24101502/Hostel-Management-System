from datetime import datetime
from pathlib import Path
import unittest

from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import Select, WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.edge.service import Service

class HMS57Tests(unittest.TestCase):
    def setUp(self):
        self.driver = webdriver.Edge(
            service=Service(
                r"C:\Users\ASUS\OneDrive\Desktop\Y3S1\Case Study Project\Assignment\msedgedriver.exe"
            )
        )
        self.driver.maximize_window()
        self.wait = WebDriverWait(self.driver, 20)
        self.evidence = Path("qa_testing/sprint_4/selenium_evidence")
        self.evidence.mkdir(parents=True, exist_ok=True)

        self.driver.get("http://localhost:5173")
        self.wait.until(
            EC.visibility_of_element_located((By.ID, "identifier"))
        ).send_keys("TestStudent01")
        self.driver.find_element(By.ID, "password").send_keys(
            "TestStudent@01"
        )
        self.click_button("Sign in")
        self.wait.until(
            lambda driver: driver.find_elements(
                By.XPATH, "//button[contains(., 'Sign out')]"
            )
        )

        self.driver.get("http://localhost:5173/student/complaints")
        self.click_button("+ New complaint")
        self.wait.until(
            EC.visibility_of_element_located((By.ID, "complaintCategory"))
        )

    def click_button(self, text):
        button = self.wait.until(
            EC.element_to_be_clickable(
                (By.XPATH, f"//button[normalize-space(.)='{text}']")
            )
        )
        self.driver.execute_script(
            "arguments[0].scrollIntoView({block:'center'});", button
        )
        button.click()

    def test_01_empty_description(self):
        Select(
            self.driver.find_element(By.ID, "complaintCategory")
        ).select_by_value("PLUMBING")

        self.click_button("Submit complaint")
        error = self.wait.until(
            EC.visibility_of_element_located(
                (By.XPATH, "//*[normalize-space(.)='Description is required.']")
            )
        )
        self.assertTrue(error.is_displayed())
        self.assertEqual(
            self.driver.find_element(
                By.ID, "complaintDescription"
            ).get_attribute("aria-invalid"),
            "true",
        )
        self.driver.save_screenshot(
            str(self.evidence / "T02_empty_description.png")
        )

    def test_02_valid_submission(self):
        marker = "HMS57-Selenium-" + datetime.now().strftime("%Y%m%d-%H%M%S-%f")
        description = marker + ": Bathroom tap is leaking."

        Select(
            self.driver.find_element(By.ID, "complaintCategory")
        ).select_by_value("PLUMBING")
        self.driver.find_element(
            By.ID, "complaintDescription"
        ).send_keys(description)
        self.click_button("Submit complaint")

        success = self.wait.until(
            EC.visibility_of_element_located(
                (By.CSS_SELECTOR, ".message.success")
            )
        )
        self.assertIn("Your complaint was submitted.", success.text)
        self.driver.save_screenshot(
            str(self.evidence / "T01_success.png")
        )

        row = self.wait.until(
            EC.visibility_of_element_located(
                (By.XPATH, f"//tr[contains(., '{marker}')]")
            )
        )
        self.assertIn("Plumbing", row.text)
        self.assertIn("OPEN", row.text.upper())
        self.driver.execute_script(
            "arguments[0].scrollIntoView({block:'center'});", row
        )
        self.driver.save_screenshot(
            str(self.evidence / "T01_history.png")
        )
        print("\nCreated test complaint:", description)

    def tearDown(self):
        self.driver.quit()


if __name__ == "__main__":
    unittest.main(verbosity=2)