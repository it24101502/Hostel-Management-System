"""
HMS-64: Student sees only active, block-relevant notices/schedules (Selenium, pytest).

Run (file name does not start with test_, so pass it explicitly):
    pip install selenium pytest requests
    pytest hms64_selenium.py -v

Required env vars:
  HMS_STUDENT_USER / HMS_STUDENT_PASS   student ALLOCATED to a hostel block
  HMS_STAFF_USER   / HMS_STAFF_PASS     admin or warden (creates/deletes test notices)
  HMS_OTHER_BLOCK_ID                    ACTIVE hostel block id different from the student's block
Optional:
  HMS_NOBLOCK_USER / HMS_NOBLOCK_PASS   student with no block
  HMS_BASE_URL (http://localhost:5173)  HMS_AUTH_API (http://localhost:8080)
  HMS_NOTICE_API (http://localhost:8084)  HEADLESS (1; set 0 to watch the browser)
  EVIDENCE_DIR (selenium_evidence)      screenshots are saved here after every test
"""
import base64
import json
import os
import uuid
from datetime import date, timedelta

import pytest
import requests
from selenium import webdriver
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.common.by import By
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.support.ui import WebDriverWait

BASE_URL = os.getenv("HMS_BASE_URL", "http://localhost:5173")
AUTH_API = os.getenv("HMS_AUTH_API", "http://localhost:8080")
NOTICE_API = os.getenv("HMS_NOTICE_API", "http://localhost:8084")
EVIDENCE_DIR = os.getenv("EVIDENCE_DIR", "selenium_evidence")
WAIT = 15


def _env(name, required=True):
    value = os.getenv(name)
    if required and not value:
        pytest.skip(f"Set {name} to run this test")
    return value


def api_login(user, password):
    r = requests.post(f"{AUTH_API}/api/auth/login",
                      json={"identifier": user, "password": password}, timeout=15)
    r.raise_for_status()
    return r.json()["accessToken"]


def block_id_from_token(token):
    payload = token.split(".")[1]
    payload += "=" * (-len(payload) % 4)
    claims = json.loads(base64.urlsafe_b64decode(payload))
    return claims.get("hostel_block_id")


# ---------------------------------------------------------------- browser
@pytest.fixture
def driver(request):
    opts = Options()
    if os.getenv("HEADLESS", "1") == "1":
        opts.add_argument("--headless=new")
    opts.add_argument("--window-size=1366,900")
    opts.add_argument("--no-sandbox")
    opts.add_argument("--disable-dev-shm-usage")
    drv = webdriver.Chrome(options=opts)   # Selenium Manager downloads chromedriver
    yield drv
    # evidence screenshot for the DoD ("manually verified in the test environment")
    try:
        os.makedirs(EVIDENCE_DIR, exist_ok=True)
        drv.save_screenshot(os.path.join(EVIDENCE_DIR, f"hms64_{request.node.name}.png"))
    except Exception:
        pass
    drv.quit()


# ------------------------------------------------------------ test data
@pytest.fixture(scope="session")
def student_creds():
    return _env("HMS_STUDENT_USER"), _env("HMS_STUDENT_PASS")


@pytest.fixture(scope="session")
def student_block_id(student_creds):
    block = block_id_from_token(api_login(*student_creds))
    if block is None:
        pytest.skip("HMS_STUDENT_USER has no hostel block; use an allocated student")
    return int(block)


@pytest.fixture(scope="session")
def seeded_notices(student_block_id):
    """Creates notices through the staff API, yields their titles, deletes them afterwards."""
    other_block = int(_env("HMS_OTHER_BLOCK_ID"))
    assert other_block != student_block_id, "HMS_OTHER_BLOCK_ID must differ from the student's block"

    staff_token = api_login(_env("HMS_STAFF_USER"), _env("HMS_STAFF_PASS"))
    headers = {"Authorization": f"Bearer {staff_token}"}
    run = uuid.uuid4().hex[:6]
    expiry = (date.today() + timedelta(days=5)).isoformat()

    specs = {
        "general_notice":  ("NOTICE",   None),
        "general_sched":   ("SCHEDULE", None),
        "own_notice":      ("NOTICE",   student_block_id),
        "own_sched":       ("SCHEDULE", student_block_id),
        "other_notice":    ("NOTICE",   other_block),
        "other_sched":     ("SCHEDULE", other_block),
    }
    titles, ids = {}, []
    for key, (ntype, block) in specs.items():
        title = f"SEL-{run} {key}"
        r = requests.post(f"{NOTICE_API}/api/notices", headers=headers, timeout=15, json={
            "title": title, "content": f"Selenium test content {key}",
            "noticeType": ntype, "hostelBlockId": block, "expiryDate": expiry})
        r.raise_for_status()
        titles[key] = title
        ids.append(r.json()["noticeId"])

    yield titles

    for nid in ids:
        requests.delete(f"{NOTICE_API}/api/notices/{nid}", headers=headers, timeout=15)


# ------------------------------------------------------------------ helpers
def login(driver, user, password):
    driver.get(BASE_URL)
    w = WebDriverWait(driver, WAIT)
    w.until(EC.visibility_of_element_located((By.ID, "identifier"))).send_keys(user)
    driver.find_element(By.ID, "password").send_keys(password)
    driver.find_element(By.CSS_SELECTOR, "form button[type='submit']").click()
    w.until(lambda d: d.current_url.rstrip("/") != BASE_URL.rstrip("/"))


def open_notices(driver):
    driver.get(f"{BASE_URL}/student/notices")
    w = WebDriverWait(driver, WAIT)
    w.until(EC.presence_of_element_located((By.CSS_SELECTOR, ".notice-banner")))
    # wait until the loading spinner is gone
    w.until(EC.invisibility_of_element_located((By.CSS_SELECTOR, ".loading-spinner")))


def card_titles(driver):
    return [e.text for e in driver.find_elements(By.CSS_SELECTOR, ".notice-card h3")]


def click_tab(driver, label):
    for btn in driver.find_elements(By.CSS_SELECTOR, ".notice-tabs button[role='tab']"):
        if btn.text.strip() == label:
            btn.click()
            return btn
    raise AssertionError(f"Tab '{label}' not found")


def card_for(driver, title):
    cards = driver.find_elements(By.CSS_SELECTOR, ".notice-card")
    for c in cards:
        if c.find_element(By.TAG_NAME, "h3").text == title:
            return c
    raise AssertionError(f"No card titled '{title}'")


@pytest.fixture
def student_page(driver, student_creds, seeded_notices):
    login(driver, *student_creds)
    open_notices(driver)
    return driver


# -------------------------------------------------------------------- tests
def test_page_layout(student_page):
    d = student_page
    assert d.find_element(By.TAG_NAME, "h1").text == "Notices and schedules"
    assert d.find_element(By.XPATH, "//h2[normalize-space()='Active notices']").is_displayed()
    tabs = [b.text for b in d.find_elements(By.CSS_SELECTOR, ".notice-tabs button")]
    assert tabs == ["All", "Notices", "Schedules"]
    assert "Showing notices for your hostel block" in d.find_element(
        By.CSS_SELECTOR, ".notice-banner").text


def test_general_notices_visible_with_all_blocks_label(student_page, seeded_notices):
    titles = card_titles(student_page)
    for key in ("general_notice", "general_sched"):
        assert seeded_notices[key] in titles
    card = card_for(student_page, seeded_notices["general_notice"])
    assert card.find_element(By.CSS_SELECTOR, ".notice-audience").text == "All blocks"


def test_own_block_notices_visible_with_your_block_label(student_page, seeded_notices):
    titles = card_titles(student_page)
    for key in ("own_notice", "own_sched"):
        assert seeded_notices[key] in titles
    card = card_for(student_page, seeded_notices["own_sched"])
    assert card.find_element(By.CSS_SELECTOR, ".notice-audience").text == "Your block"


def test_other_block_notices_not_visible(student_page, seeded_notices):
    titles = card_titles(student_page)
    assert seeded_notices["other_notice"] not in titles
    assert seeded_notices["other_sched"] not in titles
    assert seeded_notices["other_notice"] not in student_page.page_source


def test_expired_notice_not_visible(student_page):
    """
    Expired notices cannot be created through the API (expiry must not be in the past),
    so this uses the seeded row from database/tests/notice_sample_data.sql
    ('CI expired general'). Skipped if that row is not loaded.
    """
    staff = api_login(_env("HMS_STAFF_USER"), _env("HMS_STAFF_PASS"))
    rows = requests.get(f"{NOTICE_API}/api/notices?includeArchived=true",
                        headers={"Authorization": f"Bearer {staff}"}, timeout=15).json()
    if not any(r["title"] == "CI expired general" for r in rows):
        pytest.skip("Load database/tests/notice_sample_data.sql to run this test")
    assert "CI expired general" not in card_titles(student_page)


def test_tabs_filter_by_type(student_page, seeded_notices):
    d = student_page
    t = seeded_notices

    btn = click_tab(d, "Notices")
    assert btn.get_attribute("aria-selected") == "true"
    titles = card_titles(d)
    assert t["general_notice"] in titles and t["own_notice"] in titles
    assert t["general_sched"] not in titles and t["own_sched"] not in titles
    assert all(c.text == "Notice" for c in d.find_elements(By.CSS_SELECTOR, ".notice-type"))

    click_tab(d, "Schedules")
    titles = card_titles(d)
    assert t["general_sched"] in titles and t["own_sched"] in titles
    assert t["general_notice"] not in titles and t["own_notice"] not in titles
    assert all(c.text == "Schedule" for c in d.find_elements(By.CSS_SELECTOR, ".notice-type"))

    click_tab(d, "All")
    titles = card_titles(d)
    for key in ("general_notice", "general_sched", "own_notice", "own_sched"):
        assert t[key] in titles


def test_item_count_matches_cards(student_page):
    summary = student_page.find_element(
        By.CSS_SELECTOR, ".admin-users-toolbar div:first-child span").text
    shown = int(summary.split()[0])
    assert shown == len(student_page.find_elements(By.CSS_SELECTOR, ".notice-card"))


def test_card_shows_title_content_and_validity(student_page, seeded_notices):
    card = card_for(student_page, seeded_notices["own_notice"])
    assert "Selenium test content own_notice" in card.find_element(By.TAG_NAME, "p").text
    meta = card.find_element(By.TAG_NAME, "small").text
    assert "Posted" in meta and "valid until" in meta


def test_student_without_block_sees_only_general(driver, seeded_notices):
    user, pwd = _env("HMS_NOBLOCK_USER"), _env("HMS_NOBLOCK_PASS")
    login(driver, user, pwd)
    open_notices(driver)
    assert "not allocated to a hostel block" in driver.find_element(
        By.CSS_SELECTOR, ".notice-banner").text
    titles = card_titles(driver)
    assert seeded_notices["general_notice"] in titles
    for key in ("own_notice", "own_sched", "other_notice", "other_sched"):
        assert seeded_notices[key] not in titles


def test_unauthenticated_user_redirected_to_login(driver):
    driver.get(BASE_URL)                      # same origin, so we can clear storage
    driver.execute_script("window.sessionStorage.clear();")
    driver.get(f"{BASE_URL}/student/notices")
    WebDriverWait(driver, WAIT).until(EC.visibility_of_element_located((By.ID, "identifier")))
    assert "/student/notices" not in driver.current_url


def test_staff_cannot_open_student_notices_page(driver):
    login(driver, _env("HMS_STAFF_USER"), _env("HMS_STAFF_PASS"))
    driver.get(f"{BASE_URL}/student/notices")
    WebDriverWait(driver, WAIT).until(EC.visibility_of_element_located((By.ID, "identifier")))
    assert not driver.find_elements(By.CSS_SELECTOR, ".notice-card")