import os
import getpass
import unittest
from pathlib import Path
from datetime import date, timedelta
from uuid import uuid4

from selenium import webdriver
from selenium.webdriver.edge.service import Service
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import Select, WebDriverWait
from selenium.webdriver.support import expected_conditions as EC


BASE_URL = "http://localhost:5173"
DRIVER_PATH = os.environ.get(
    "QA_EDGE_DRIVER",
    r"C:\Users\ASUS\OneDrive\Desktop\Y3S1\Case Study Project"
    r"\Assignment\msedgedriver.exe"
)

USERNAME = input("Warden username [TestWarden01]: ").strip() or "TestWarden01"
PASSWORD = getpass.getpass("Warden password: ")


class HMS63Tests(unittest.TestCase):
    def setUp(self):
        self.driver = webdriver.Edge(service=Service(DRIVER_PATH))
        self.addCleanup(self.driver.quit)
        self.driver.maximize_window()
        self.wait = WebDriverWait(self.driver, 20)
        self.folder = Path("qa_testing/sprint_4/hms63_evidence")
        self.folder.mkdir(parents=True, exist_ok=True)

        self.driver.get(BASE_URL)
        self.wait.until(
            EC.visibility_of_element_located((By.ID, "identifier"))
        ).send_keys(USERNAME)
        self.driver.find_element(By.ID, "password").send_keys(PASSWORD)
        self.button("Sign in")

        self.wait.until(lambda d: d.execute_script(
            "return Boolean(sessionStorage.getItem('accessToken')) "
            "&& sessionStorage.getItem('userRole') === 'WARDEN';"
        ))

        self.driver.get(BASE_URL + "/warden/notices")
        self.ready()

    def ready(self):
        self.wait.until(
            EC.visibility_of_element_located((By.ID, "noticeSearch"))
        )
        self.wait.until(
            EC.invisibility_of_element_located(
                (By.CSS_SELECTOR, ".loading-spinner")
            )
        )
        errors = self.driver.find_elements(
            By.CSS_SELECTOR, ".message.error"
        )
        self.assertFalse(
            any(e.is_displayed() for e in errors),
            "Notices could not be loaded."
        )

    def click(self, element):
        self.driver.execute_script(
            "arguments[0].scrollIntoView({block:'center'});", element
        )
        element.click()

    def button(self, text):
        element = self.wait.until(EC.element_to_be_clickable((
            By.XPATH, f"//button[normalize-space(.)='{text}']"
        )))
        self.click(element)

    def field(self, field_id, value):
        element = self.wait.until(
            EC.visibility_of_element_located((By.ID, field_id))
        )
        element.clear()
        element.send_keys(value)

    def expiry(self, value):
        element = self.driver.find_element(By.ID, "noticeExpiry")
        self.driver.execute_script("""
            const setter = Object.getOwnPropertyDescriptor(
                HTMLInputElement.prototype, 'value'
            ).set;
            setter.call(arguments[0], arguments[1]);
            arguments[0].dispatchEvent(
                new Event('input', {bubbles: true})
            );
            arguments[0].dispatchEvent(
                new Event('change', {bubbles: true})
            );
        """, element, value)
        self.assertEqual(element.get_attribute("value"), value)

    def row_locator(self, title):
        return (
            By.XPATH,
            f"//table//tbody/tr[.//strong[normalize-space(.)='{title}']]"
        )

    def row(self, title):
        return self.wait.until(
            EC.visibility_of_element_located(self.row_locator(title))
        )

    def saved(self, message):
        self.wait.until(EC.text_to_be_present_in_element(
            (By.CSS_SELECTOR, ".message.success"), message
        ))
        self.wait.until(EC.invisibility_of_element_located(
            (By.ID, "noticeTitle")
        ))

    def screenshot(self, name):
        self.driver.save_screenshot(str(self.folder / name))

    def crud(self, notice_type):
        title = f"HMS63-Selenium-{notice_type}-{uuid4().hex[:8]}"
        updated_title = title + "-Updated"
        original_content = "QA: Water maintenance on Monday."
        updated_content = "QA: Water maintenance moved to Tuesday."
        first_expiry = (date.today() + timedelta(days=7)).isoformat()
        new_expiry = (date.today() + timedelta(days=10)).isoformat()

        # CREATE through the UI
        self.button("+ New notice")
        self.field("noticeTitle", title)
        Select(self.driver.find_element(
            By.ID, "noticeType"
        )).select_by_value(notice_type)
        Select(self.driver.find_element(
            By.ID, "noticeBlock"
        )).select_by_value("")
        self.expiry(first_expiry)
        self.field("noticeContent", original_content)
        self.button("Publish")
        self.saved("The notice was published.")

        self.driver.refresh()
        self.ready()
        row = self.row(title)
        self.assertIn(original_content, row.text)
        self.assertEqual(
            row.find_element(By.CSS_SELECTOR, ".notice-type")
            .text.upper(), notice_type
        )
        self.assertIn("All blocks", row.text)
        self.screenshot(f"{notice_type}_01_created.png")
        print(f"\nPASS: {notice_type} created and persisted.")

        # EDIT through the UI
        self.click(row.find_element(
            By.XPATH, ".//button[normalize-space(.)='Edit']"
        ))
        self.field("noticeTitle", updated_title)
        self.field("noticeContent", updated_content)
        self.expiry(new_expiry)
        self.button("Save changes")
        self.saved("The notice was updated.")

        self.driver.refresh()
        self.ready()
        row = self.row(updated_title)
        self.assertIn(updated_content, row.text)
        self.assertEqual(
            row.find_element(By.CSS_SELECTOR, ".notice-type")
            .text.upper(), notice_type
        )

        # Verify the saved expiry date by reopening the edit form
        self.click(row.find_element(
            By.XPATH, ".//button[normalize-space(.)='Edit']"
        ))
        expiry_field = self.wait.until(
            EC.visibility_of_element_located((By.ID, "noticeExpiry"))
        )
        self.assertEqual(
            expiry_field.get_attribute("value"), new_expiry
        )
        self.screenshot(f"{notice_type}_02_updated.png")
        self.button("Close")
        print(f"PASS: {notice_type} edited and persisted.")

        # DELETE only the item created by this test
        self.click(self.row(updated_title).find_element(
            By.XPATH, ".//button[normalize-space(.)='Delete']"
        ))
        confirm = self.wait.until(EC.element_to_be_clickable((
            By.CSS_SELECTOR,
            "[role='alertdialog'] .danger-button"
        )))
        self.click(confirm)
        self.wait.until(EC.text_to_be_present_in_element(
            (By.CSS_SELECTOR, ".message.success"),
            f'"{updated_title}" was deleted.'
        ))

        self.driver.refresh()
        self.ready()
        self.assertEqual(
            len(self.driver.find_elements(
                *self.row_locator(updated_title)
            )), 0
        )
        self.screenshot(f"{notice_type}_03_deleted.png")
        print(f"PASS: {notice_type} deleted and absent after refresh.")

    def test_01_notice_crud(self):
        self.crud("NOTICE")

    def test_02_schedule_crud(self):
        self.crud("SCHEDULE")

    def tearDown(self):
        self.screenshot(f"{self._testMethodName}_final.png")


if __name__ == "__main__":
    unittest.main(verbosity=2)