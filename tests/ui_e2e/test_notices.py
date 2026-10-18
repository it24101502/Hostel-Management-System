import getpass
import unittest
import os
import json
import urllib.request
import urllib.error
import subprocess

from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from datetime import date, timedelta
from uuid import uuid4
from selenium.webdriver.support.ui import Select
from selenium.webdriver.common.keys import Keys

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
        field.click()
        field.send_keys(Keys.CONTROL, "a")
        field.send_keys(Keys.BACKSPACE)

        if value:
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

    def test_04_whitespace_validation(self):
        self.login_and_open_notices()
        self.click("//button[contains(., 'New notice')]")

        self.fill_field("noticeTitle", "   ")
        self.fill_field("noticeContent", "   ")
        self.click("//form//button[@type='submit']")

        for message in ("Title is required.", "Content is required."):
            with self.subTest(message=message):
                self.wait.until(
                    EC.visibility_of_element_located(
                        (By.XPATH, f"//p[text()='{message}']")
                    )
                )

    def test_05_past_expiry_validation(self):
        self.login_and_open_notices()
        self.click("//button[contains(., 'New notice')]")

        self.fill_field("noticeTitle", "E2E invalid expiry")
        self.fill_field("noticeContent", "This must not be published.")

        yesterday = (date.today() - timedelta(days=1)).isoformat()

        self.driver.execute_script(
            """
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
            """,
            self.driver.find_element(By.ID, "noticeExpiry"),
            yesterday,
        )

        self.click("//form//button[@type='submit']")

        self.wait.until(
            EC.visibility_of_element_located(
                (
                    By.XPATH,
                    "//p[text()='Expiry date cannot be in the past.']",
                )
            )
        )

    def test_06_input_length_limits(self):
        self.login_and_open_notices()
        self.click("//button[contains(., 'New notice')]")

        for field_id, limit in (
            ("noticeTitle", 200),
            ("noticeContent", 4000),
        ):
            with self.subTest(field=field_id):
                field = self.driver.find_element(By.ID, field_id)
                self.assertEqual(
                    field.get_attribute("maxlength"), str(limit)
                )

                # WebDriver types into the real browser input.
                field.send_keys("X" * limit)
                self.assertEqual(
                    len(field.get_attribute("value")), limit
                )

                field.send_keys("Y")
                self.assertEqual(
                    len(field.get_attribute("value")), limit
                )

    def test_07_logged_out_staff_page_access(self):
        self.driver.get(BASE_URL)
        self.driver.execute_script("sessionStorage.clear()")

        self.driver.get(BASE_URL + "/warden/notices")

        self.wait.until(
            EC.visibility_of_element_located((By.ID, "identifier"))
        )

        self.assertEqual(
            self.driver.find_elements(By.ID, "noticeTitle"), []
        )
        self.assertEqual(
            self.driver.find_elements(
                By.XPATH, "//button[contains(., 'New notice')]"
            ),
            [],
        )

    def test_08_student_login_and_notices_access(self):
        username = os.getenv("E2E_STUDENT_USERNAME")
        password = os.getenv("E2E_STUDENT_PASSWORD")

        if os.getenv("CI") and not (username and password):
            raise RuntimeError(
                "CI requires E2E_STUDENT_USERNAME and E2E_STUDENT_PASSWORD."
            )

        if not username:
            username = input("Student username: ").strip()

        if not password:
            password = getpass.getpass("Student password: ")

        self.driver.get(BASE_URL)

        self.wait.until(
            EC.visibility_of_element_located((By.ID, "identifier"))
        ).send_keys(username)

        self.driver.find_element(By.ID, "password").send_keys(password)
        self.click("//form//button[@type='submit']")

        self.wait.until(
            lambda d: d.execute_script(
                "return sessionStorage.getItem('userRole')"
            ) == "STUDENT"
        )

        block_id = self.driver.execute_script(
            """
            const token = sessionStorage.getItem('accessToken');
            let payload = token.split('.')[1]
                .replace(/-/g, '+').replace(/_/g, '/');
            payload = payload.padEnd(
                Math.ceil(payload.length / 4) * 4, '='
            );
            return JSON.parse(atob(payload)).hostel_block_id ?? null;
            """
        )

        print(f"\nStudent hostel block: {block_id}")
        self.assertIsNotNone(
            block_id,
            "Student needs a hostel block for block-filtering tests.",
        )
        self.assertGreater(int(block_id), 0)

        self.driver.get(BASE_URL + "/student/notices")

        self.wait.until(
            EC.visibility_of_element_located(
                (By.CSS_SELECTOR, "[role='tablist']")
            )
        )

        self.wait.until(
            EC.invisibility_of_element_located(
                (By.XPATH, "//p[text()='Loading notices...']")
            )
        )

        alerts = self.driver.find_elements(
            By.CSS_SELECTOR, "[role='alert']"
        )
        self.assertEqual(
            [a.text for a in alerts if a.is_displayed()], []
        )

        print("PASS: Student notices page loaded without API errors")

    def notice_api(self, method, path, token, payload=None):
        body = (
            json.dumps(payload).encode("utf-8")
            if payload is not None else None
        )
        request = urllib.request.Request(
            "http://localhost:8084/api/notices" + path,
            data=body,
            method=method,
            headers={
                "Authorization": "Bearer " + token,
                "Content-Type": "application/json",
            },
        )
        with urllib.request.urlopen(request, timeout=15) as response:
            raw = response.read()
            return json.loads(raw) if raw else None

    def test_09_student_visibility_and_type_tabs(self):
        # Obtain a real Warden token through browser login.
        self.login_and_open_notices()
        staff_token = self.driver.execute_script(
            "return sessionStorage.getItem('accessToken')"
        )

        # Obtain student credentials without saving them in source code.
        username = os.getenv("E2E_STUDENT_USERNAME")
        password = os.getenv("E2E_STUDENT_PASSWORD")

        if os.getenv("CI") and not (username and password):
            raise RuntimeError("CI student credentials are required.")

        if not username:
            username = input("Student username: ").strip()
        if not password:
            password = getpass.getpass("Student password: ")

        self.driver.execute_script("sessionStorage.clear()")
        self.driver.get(BASE_URL)

        self.wait.until(
            EC.visibility_of_element_located((By.ID, "identifier"))
        ).send_keys(username)
        self.driver.find_element(By.ID, "password").send_keys(password)
        self.click("//form//button[@type='submit']")

        self.wait.until(
            lambda d: d.execute_script(
                "return sessionStorage.getItem('userRole')"
            ) == "STUDENT"
        )

        own_block = self.driver.execute_script(
            """
            let p = sessionStorage.getItem('accessToken').split('.')[1]
                .replace(/-/g, '+').replace(/_/g, '/');
            p = p.padEnd(Math.ceil(p.length / 4) * 4, '=');
            return JSON.parse(atob(p)).hostel_block_id ?? null;
            """
        )
        self.assertIsNotNone(own_block)
        own_block = int(own_block)

        # Discover a different active block; do not hard-code its ID.
        request = urllib.request.Request(
            "http://localhost:8081/api/admin/blocks",
            headers={"Authorization": "Bearer " + staff_token},
        )
        with urllib.request.urlopen(request, timeout=15) as response:
            blocks = json.load(response)

        others = [
            int(block["blockId"]) for block in blocks
            if int(block["blockId"]) != own_block
        ]
        self.assertTrue(
            others, "A second active block is required for isolation testing."
        )

        prefix = "E2E-VIS-" + uuid4().hex[:8]
        titles = {
            "general": prefix + "-general",
            "own": prefix + "-own",
            "other": prefix + "-other",
            "schedule": prefix + "-schedule",
        }

        fixtures = [
            ("general", "NOTICE", None),
            ("own", "NOTICE", own_block),
            ("other", "NOTICE", others[0]),
            ("schedule", "SCHEDULE", None),
        ]

        for key, kind, block in fixtures:
            saved = self.notice_api(
                "POST", "", staff_token,
                {
                    "title": titles[key],
                    "content": "Student visibility test fixture.",
                    "noticeType": kind,
                    "hostelBlockId": block,
                    "expiryDate": (
                        date.today() + timedelta(days=7)
                    ).isoformat(),
                },
            )
            self.addCleanup(
                self.notice_api,
                "DELETE",
                "/" + str(saved["noticeId"]),
                staff_token,
            )

        self.driver.get(BASE_URL + "/student/notices")

        self.wait.until(
            EC.visibility_of_element_located(
                (By.CSS_SELECTOR, "[role='tablist']")
            )
        )
        self.wait.until(
            EC.invisibility_of_element_located(
                (By.XPATH, "//p[text()='Loading notices...']")
            )
        )

        def visible(title):
            return self.driver.find_elements(
                By.XPATH,
                f"//article[contains(@class,'notice-card')]"
                f"//h3[text()='{title}']",
            )

        for key in ("general", "own", "schedule"):
            with self.subTest(visible=key):
                self.assertEqual(len(visible(titles[key])), 1)

        self.assertEqual(visible(titles["other"]), [])
        print("\nPASS: General and own-block items visible; other block hidden")

        self.click("//button[@role='tab' and text()='Notices']")
        self.assertEqual(len(visible(titles["general"])), 1)
        self.assertEqual(len(visible(titles["own"])), 1)
        self.assertEqual(visible(titles["schedule"]), [])

        self.click("//button[@role='tab' and text()='Schedules']")
        self.assertEqual(len(visible(titles["schedule"])), 1)
        self.assertEqual(visible(titles["general"]), [])
        self.assertEqual(visible(titles["own"]), [])

        self.click("//button[@role='tab' and text()='All']")
        self.assertEqual(len(visible(titles["general"])), 1)
        self.assertEqual(len(visible(titles["schedule"])), 1)
        self.assertEqual(visible(titles["other"]), [])
        print("PASS: Notices, Schedules and All tabs filter correctly")

        student_token = self.driver.execute_script(
            "return sessionStorage.getItem('accessToken')"
        )

        for method in ("GET", "POST"):
            with self.subTest(student_forbidden=method):
                payload = (
                    {
                        "title": "Must not be created",
                        "content": "Unauthorized request.",
                        "noticeType": "NOTICE",
                        "hostelBlockId": None,
                        "expiryDate": (
                            date.today() + timedelta(days=7)
                        ).isoformat(),
                    }
                    if method == "POST" else None
                )

                with self.assertRaises(urllib.error.HTTPError) as error:
                    self.notice_api(method, "", student_token, payload)

                self.assertEqual(error.exception.code, 403)

        with self.assertRaises(urllib.error.HTTPError) as error:
            self.notice_api("GET", "", "")

        self.assertEqual(error.exception.code, 401)
        print("PASS: Student staff-list/create access denied; anonymous denied")

    def test_10_search_title_content_and_no_match(self):
        self.login_and_open_notices()
        token = self.driver.execute_script(
            "return sessionStorage.getItem('accessToken')"
        )

        unique = uuid4().hex[:10]
        title = "E2E-SEARCH-" + unique
        content = "SearchContent-" + uuid4().hex[:10]

        saved = self.notice_api(
            "POST", "", token,
            {
                "title": title,
                "content": content,
                "noticeType": "NOTICE",
                "hostelBlockId": None,
                "expiryDate": (
                    date.today() + timedelta(days=7)
                ).isoformat(),
            },
        )
        self.addCleanup(
            self.notice_api,
            "DELETE",
            "/" + str(saved["noticeId"]),
            token,
        )

        self.driver.refresh()
        self.wait.until(
            EC.visibility_of_element_located(self.find_row(title))
        )

        for query in (title, content, title.lower()):
            with self.subTest(search=query):
                self.fill_field("noticeSearch", query)
                self.wait.until(
                    EC.visibility_of_element_located(self.find_row(title))
                )
                self.assertEqual(
                    len(self.driver.find_elements(
                        By.CSS_SELECTOR, "tbody tr"
                    )),
                    1,
                )

        self.fill_field("noticeSearch", "NO-MATCH-" + uuid4().hex)
        self.wait.until(
            EC.visibility_of_element_located(
                (By.XPATH, "//h3[text()='No notices found']")
            )
        )
        self.assertEqual(
            self.driver.find_elements(By.CSS_SELECTOR, "tbody tr"), []
        )

        self.fill_field("noticeSearch", "")
        self.wait.until(
            EC.visibility_of_element_located(self.find_row(title))
        )
        print("\nPASS: Title/content/case-insensitive search and no-match state")

    def test_11_expired_archived_visibility_and_toggle(self):
        self.login_and_open_notices()
        token = self.driver.execute_script(
            "return sessionStorage.getItem('accessToken')"
        )

        prefix = "E2E-ARCH-" + uuid4().hex[:8]
        records = {}

        for state in ("active", "expired", "archived"):
            saved = self.notice_api(
                "POST", "", token,
                {
                    "title": prefix + "-" + state,
                    "content": "Expiry and archive test fixture.",
                    "noticeType": "NOTICE",
                    "hostelBlockId": None,
                    "expiryDate": (
                        date.today() + timedelta(days=7)
                    ).isoformat(),
                },
            )
            records[state] = saved
            self.addCleanup(
                self.notice_api,
                "DELETE", "/" + str(saved["noticeId"]), token,
            )

        # Prepare historical states that cannot be created through the UI.
        expired_id = int(records["expired"]["noticeId"])
        archived_id = int(records["archived"]["noticeId"])
        container = os.getenv(
            "E2E_MYSQL_CONTAINER", "hostel-sprint4-mysql"
        )

        sql = f"""
        USE Hostel_Notice_System;
        UPDATE notices
        SET expiry_date = UTC_DATE() - INTERVAL 1 DAY
        WHERE notice_id = {expired_id};
        UPDATE notices
        SET is_archived = TRUE, archived_at = UTC_TIMESTAMP(6)
        WHERE notice_id = {archived_id};
        """

        setup = subprocess.run(
            [
                "docker", "exec", "-i", container, "sh", "-c",
                'MYSQL_PWD="$MYSQL_ROOT_PASSWORD" mysql -uroot',
            ],
            input=sql, text=True, capture_output=True,
        )
        self.assertEqual(setup.returncode, 0, setup.stderr)

        self.driver.refresh()
        self.wait.until(
            EC.visibility_of_element_located(
                self.find_row(prefix + "-active")
            )
        )
        self.assertEqual(
            self.driver.find_elements(
                *self.find_row(prefix + "-archived")
            ), []
        )

        self.click(
            "//label[contains(@class,'notice-toggle')]"
            "//input[@type='checkbox']"
        )
        archived_row = self.wait.until(
            EC.visibility_of_element_located(
                self.find_row(prefix + "-archived")
            )
        )
        self.assertEqual(
            archived_row.find_element(
                By.CSS_SELECTOR, ".notice-status"
            ).text.strip().upper(),
            "ARCHIVED",
        )
        self.assertFalse(
            archived_row.find_element(
                By.XPATH, ".//button[text()='Edit']"
            ).is_enabled()
        )

        self.click(
            "//label[contains(@class,'notice-toggle')]"
            "//input[@type='checkbox']"
        )
        self.wait.until(
            EC.invisibility_of_element_located(
                self.find_row(prefix + "-archived")
            )
        )
        print("\nPASS: Archived toggle and disabled archived Edit")

        username = os.getenv("E2E_STUDENT_USERNAME")
        password = os.getenv("E2E_STUDENT_PASSWORD")

        if os.getenv("CI") and not (username and password):
            raise RuntimeError("CI student credentials are required.")

        if not username:
            username = input("Student username: ").strip()
        if not password:
            password = getpass.getpass("Student password: ")

        self.driver.execute_script("sessionStorage.clear()")
        self.driver.get(BASE_URL)
        self.wait.until(
            EC.visibility_of_element_located((By.ID, "identifier"))
        ).send_keys(username)
        self.driver.find_element(By.ID, "password").send_keys(password)
        self.click("//form//button[@type='submit']")
        self.wait.until(
            lambda d: d.execute_script(
                "return sessionStorage.getItem('userRole')"
            ) == "STUDENT"
        )

        self.driver.get(BASE_URL + "/student/notices")
        self.wait.until(
            EC.visibility_of_element_located(
                (
                    By.XPATH,
                    f"//article//h3[text()='{prefix}-active']",
                )
            )
        )

        for state in ("expired", "archived"):
            with self.subTest(hidden=state):
                self.assertEqual(
                    self.driver.find_elements(
                        By.XPATH,
                        f"//article//h3[text()='{prefix}-{state}']",
                    ), []
                )

        print("PASS: Student sees active item; expired/archived items hidden")

if __name__ == "__main__":
    unittest.main(verbosity=2)