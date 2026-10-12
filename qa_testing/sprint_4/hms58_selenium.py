from pathlib import Path
import unittest

from selenium import webdriver
from selenium.webdriver.edge.service import Service
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import Select, WebDriverWait
from selenium.webdriver.support import expected_conditions as EC


class HMS58Tests(unittest.TestCase):
    def setUp(self):
        self.driver = webdriver.Edge(service=Service(
            r"C:\Users\ASUS\OneDrive\Desktop\Y3S1\Case Study Project"
            r"\Assignment\msedgedriver.exe"
        ))
        self.addCleanup(self.driver.quit)
        self.driver.maximize_window()
        self.wait = WebDriverWait(self.driver, 20)
        self.folder = Path("qa_testing/sprint_4/hms58_evidence")
        self.folder.mkdir(parents=True, exist_ok=True)

    def click(self, element):
        self.driver.execute_script(
            "arguments[0].scrollIntoView({block:'center'});", element
        )
        element.click()

    def button(self, text):
        element = self.wait.until(EC.element_to_be_clickable(
            (By.XPATH, f"//button[normalize-space(.)='{text}']")
        ))
        self.click(element)

    def login(self, username, password):
        self.driver.get("http://localhost:5173")
        self.wait.until(EC.visibility_of_element_located(
            (By.ID, "identifier")
        )).send_keys(username)
        self.driver.find_element(By.ID, "password").send_keys(password)
        self.button("Sign in")
        self.wait.until(lambda d: d.find_elements(
            By.XPATH, "//button[normalize-space(.)='Sign out']"
        ))

    def row(self):
        return self.wait.until(EC.visibility_of_element_located((
            By.XPATH,
            "//tr[.//small[starts-with(normalize-space(.), '#21 ·')]]"
        )))

    def screenshot(self, name):
        self.driver.save_screenshot(str(self.folder / name))

    def test_01_staff_assign_and_status(self):
        self.login("TestWarden01", "TestWarden@01")
        self.driver.get("http://localhost:5173/warden/complaints")
        self.assertEqual(self.row().find_elements(
            By.TAG_NAME, "td"
        )[3].text.strip().upper(), "OPEN")
        self.screenshot("T01_staff_list.png")

        assign = self.row().find_element(
            By.XPATH,
            ".//button[normalize-space(.)='Assign' "
            "or normalize-space(.)='Reassign']"
        )
        self.click(assign)
        field = self.wait.until(EC.visibility_of_element_located(
            (By.ID, "complaintAssignee")
        ))
        field.clear()
        field.send_keys("3")
        assign_button = self.wait.until(EC.element_to_be_clickable((
            By.CSS_SELECTOR,
            "[role='dialog'][aria-labelledby='complaint-assign-title'] button[type='submit']"
        )))
        self.click(assign_button)
        self.wait.until(EC.invisibility_of_element_located(
            (By.ID, "complaintAssignee")
        ))
        self.wait.until(EC.text_to_be_present_in_element(
            (By.CSS_SELECTOR, ".message.success"),
            "Complaint #21 was assigned."
        ))

        self.driver.refresh()
        self.assertIn("User #3", self.row().text)
        self.screenshot("T02_assigned.png")

        for value, label in [
            ("IN_PROGRESS", "IN PROGRESS"),
            ("RESOLVED", "RESOLVED"),
            ("OPEN", "OPEN"),
        ]:
            self.click(self.row().find_element(
                By.XPATH, ".//button[normalize-space(.)='Change status']"
            ))
            dropdown = self.wait.until(
                EC.visibility_of_element_located(
                    (By.ID, "complaintNewStatus")
                )
            )
            Select(dropdown).select_by_value(value)
            self.driver.find_element(
                By.ID, "complaintRemarks"
            ).send_keys(f"HMS58-Selenium: Change to {value}.")
            self.button("Save status")
            self.wait.until(EC.invisibility_of_element_located(
                (By.ID, "complaintNewStatus")
            ))

            self.driver.refresh()
            self.wait.until(lambda d: " ".join(
                self.row().find_elements(By.TAG_NAME, "td")[3]
                .text.upper().split()
            ) == label)
            self.assertIn("User #3", self.row().text)
            self.screenshot(f"T03_{value}.png")

    def test_02_student_staff_page_blocked(self):
            self.login("TestStudent01", "TestStudent@01")
            self.driver.get("http://localhost:5173/warden/complaints")

            self.wait.until(
                lambda d: "/warden/complaints" not in d.current_url
            )
            self.wait.until(EC.visibility_of_element_located(
                (By.ID, "identifier")
            ))

            controls = self.driver.find_elements(
                By.XPATH,
                "//button[normalize-space(.)='Assign' "
                "or normalize-space(.)='Reassign' "
                "or normalize-space(.)='Change status']"
            )
            self.assertEqual(len(controls), 0)
            self.screenshot("T04_student_blocked.png")

    def tearDown(self):
        self.screenshot(f"{self._testMethodName}_final.png")


if __name__ == "__main__":
    unittest.main(verbosity=2)