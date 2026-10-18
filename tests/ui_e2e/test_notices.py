import getpass
import unittest
import os

from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from datetime import date, timedelta
from uuid import uuid4
from selenium.webdriver.support.ui import Select


BASE_URL = "http://localhost:5173"


class NoticeUITests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.username = os.getenv("E2E_USERNAME")
        cls.password = os.getenv("E2E_PASSWORD")

        if os.getenv("CI") and not (cls.username and cls.password):
            raise RuntimeError(
                "CI requires E2E_USERNAME and E2E_PASSWORD."
            )

        if not cls.username:
            cls.username = input("Warden username: ").strip()

        if not cls.password:
            cls.password = getpass.getpass("Warden password: ")

    def setUp(self):
        options = webdriver.EdgeOptions()

        if os.getenv("E2E_HEADLESS") == "1":
            options.add_argument("--headless=new")

    def setUp(self):
        browser = os.getenv("E2E_BROWSER", "edge")

        options = (
            webdriver.ChromeOptions()
            if browser == "chrome"
            else webdriver.EdgeOptions()
        )

        if os.getenv("E2E_HEADLESS") == "1":
            options.add_argument("--headless=new")

        options.add_argument("--window-size=1440,1000")

        self.driver = (
            webdriver.Chrome(options=options)
            if browser == "chrome"
            else webdriver.Edge(options=options)
        )

        self.addCleanup(self.driver.quit)
        self.wait = WebDriverWait(self.driver, 20)

        self.addCleanup(self.driver.quit)
        self.wait = WebDriverWait(self.driver, 20)

    def click(self, xpath):
        self.wait.until(
            EC.element_to_be_clickable((By.XPATH, xpath))
        ).click()

    def test_01_login_and_required_field_validation(self):
        driver = self.driver
        driver.get(BASE_URL)

        self.wait.until(
            EC.visibility_of_element_located((By.ID, "identifier"))
        ).send_keys(self.username)

        driver.find_element(By.ID, "password").send_keys(self.password)
        self.click("//form//button[@type='submit']")

        self.wait.until(
            lambda d: bool(
                d.execute_script(
                    "return sessionStorage.getItem('accessToken')"
                )
            )
        )

        role = driver.execute_script(
            "return sessionStorage.getItem('userRole')"
        )
        self.assertEqual(role, "WARDEN")
        print("\nPASS: Warden login")

        driver.get(BASE_URL + "/warden/notices")

        self.click("//button[contains(., 'New notice')]")

        self.wait.until(
            EC.visibility_of_element_located((By.ID, "noticeTitle"))
        )
        print("PASS: Notices page and publish form opened")

        self.click("//form//button[@type='submit']")

        for message in (
            "Title is required.",
            "Content is required.",
            "Expiry date is required.",
        ):
            with self.subTest(validation=message):
                self.wait.until(
                    EC.visibility_of_element_located(
                        (By.XPATH, f"//p[text()='{message}']")
                    )
                )

        print("PASS: Empty form shows all required-field errors")
        driver.save_screenshot("notice-validation.png")

    def login_and_open_notices(self):
        driver = self.driver
        driver.get(BASE_URL)

        self.wait.until(
            EC.visibility_of_element_located((By.ID, "identifier"))
        ).send_keys(self.username)

        driver.find_element(By.ID, "password").send_keys(self.password)
        self.click("//form//button[@type='submit']")

        self.wait.until(
            lambda d: bool(
                d.execute_script(
                    "return sessionStorage.getItem('accessToken')"
                )
            )
        )

        driver.get(BASE_URL + "/warden/notices")
        self.wait.until(
            EC.element_to_be_clickable(
                (By.XPATH, "//button[contains(., 'New notice')]")
            )
        )

    def fill_field(self, field_id, value):
        field = self.wait.until(
            EC.visibility_of_element_located((By.ID, field_id))
        )
        field.clear()
        field.send_keys(value)

    def find_row(self, title):
        return (
            By.XPATH,
            f"//tbody/tr[.//strong[text()='{title}']]",
        )

    def run_crud(self, notice_type):
        driver = self.driver
        self.login_and_open_notices()

        title = f"E2E-{notice_type}-{uuid4().hex[:10]}"
        updated_title = title + "-updated"
        content = "Automated UI test content."
        expiry = (date.today() + timedelta(days=7)).isoformat()

        self.click("//button[contains(., 'New notice')]")

        self.fill_field("noticeTitle", title)
        self.fill_field("noticeContent", content)

        Select(driver.find_element(By.ID, "noticeType")).select_by_value(
            notice_type
        )

        Select(driver.find_element(By.ID, "noticeBlock")).select_by_value("")

        # Use the native setter so React receives the date change.
        driver.execute_script(
            """
            const field = arguments[0];
            const setter = Object.getOwnPropertyDescriptor(
                HTMLInputElement.prototype, 'value'
            ).set;
            setter.call(field, arguments[1]);
            field.dispatchEvent(new Event('input', {bubbles: true}));
            field.dispatchEvent(new Event('change', {bubbles: true}));
            """,
            driver.find_element(By.ID, "noticeExpiry"),
            expiry,
        )

        self.click("//form//button[@type='submit']")

        row = self.wait.until(
            EC.visibility_of_element_located(self.find_row(title))
        )
        self.assertIn(content, row.text)
        print(f"\nPASS: {notice_type} created")

        driver.refresh()

        self.wait.until(
            EC.visibility_of_element_located(self.find_row(title))
        )
        print(f"PASS: {notice_type} persisted after reload")

        row = driver.find_element(*self.find_row(title))
        row.find_element(By.XPATH, ".//button[text()='Edit']").click()

        self.fill_field("noticeTitle", updated_title)
        self.fill_field("noticeContent", "Updated automated UI test content.")
        self.click("//form//button[@type='submit']")

        row = self.wait.until(
            EC.visibility_of_element_located(self.find_row(updated_title))
        )
        self.assertIn("Updated automated UI test content.", row.text)

        driver.refresh()
        row = self.wait.until(
            EC.visibility_of_element_located(self.find_row(updated_title))
        )
        self.assertIn("Updated automated UI test content.", row.text)
        print(f"PASS: {notice_type} update persisted")

        row.find_element(By.XPATH, ".//button[text()='Delete']").click()

        self.click(
            "//section[@role='alertdialog']"
            "//button[normalize-space()='Delete notice']"
        )

        self.wait.until(
            EC.invisibility_of_element_located(self.find_row(updated_title))
        )

        driver.refresh()

        # Wait for the refreshed list to finish loading before checking absence.
        self.wait.until(
            EC.presence_of_element_located(
                (
                    By.XPATH,
                    "//h2[text()='Active notices']",
                )
            )
        )
        self.wait.until(
            EC.invisibility_of_element_located(
                (By.XPATH, "//p[text()='Loading notices...']")
            )
        )
        self.assertEqual(
            driver.find_elements(*self.find_row(updated_title)), []
        )
        print(f"PASS: {notice_type} deleted and absent after reload")

    def test_02_notice_crud(self):
        self.run_crud("NOTICE")

    def test_03_schedule_crud(self):
        self.run_crud("SCHEDULE")

if __name__ == "__main__":
    unittest.main(verbosity=2)