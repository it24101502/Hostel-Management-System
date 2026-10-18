# NoticeService UI / E2E Testing

Owner: P. A. I. B. Premarathna — IT24101844  
Module: SE3112 Advanced Software Engineering  
Area: UI / E2E Testing

## Tools

- Python 3.13
- Selenium 4.51.0
- Python unittest
- Microsoft Edge for local runs
- Google Chrome for CI runs
- GitHub Actions and Docker Compose

## Scope

The suite uses the real React frontend, backend APIs and MySQL database.

| Test | Checks |
| --- | --- |
| 01 | Warden login and required-field validation |
| 02 | Notice creation, retrieval, editing, deletion and persistence |
| 03 | Schedule creation, retrieval, editing, deletion and persistence |
| 04 | Whitespace-only title and content validation |
| 05 | Past expiry date rejection |
| 06 | Browser title/content input-length limits |
| 07 | Logged-out access to the staff notices page |
| 08 | Student login, block claim and notices page access |
| 09 | General/own-block visibility, other-block isolation, type tabs and selected API permissions |
| 10 | Title/content search, case-insensitive matching, no-match state and clearing search |
| 11 | Archived toggle, disabled archived Edit and expired/archived student visibility |

CRUD tests reload the page to verify persisted changes.

Tests use unique titles. Tests 09–11 create fixtures through real APIs
and register cleanup actions. Test 11 prepares historical expiry/archive
states directly in the test database.

The original CRUD tests delete their records on success. An interrupted
or failed CRUD test may leave a uniquely named test record behind.

## Coverage limits

This suite does not establish 100% service or code coverage.

- Expired/archived visibility is tested; execution of the midnight
  archival job is not.
- Browser input-length limits are tested; complete server-side boundary
  validation is not.
- Permission checks cover anonymous staff-list access and Student
  staff-list/create access, rather than every role/endpoint combination.

## Local prerequisites

Run these services before executing tests:

| Component | Address |
| --- | --- |
| React frontend | http://localhost:5173 |
| IdentityService | http://localhost:8080 |
| AccommodationService | http://localhost:8081 |
| NoticeService | http://localhost:8084 |
| Local QA MySQL | localhost:3307 |

Apply the identity, accommodation and notice database migrations.
Kafka must also be available for services configured to use it.

Use:

- An active Warden test account.
- An active Student test account with a positive hostel_block_id claim.
- The student's active block and a second active block in AccommodationService.

The frontend must use local API addresses. The backend services must
share compatible JWT signing, issuer and audience settings.

Test 11 requires Docker access to the test MySQL container, which must
provide MYSQL_ROOT_PASSWORD in its environment. Use a test database.

## Install

Run from the repository root:

```powershell
py -m pip install -r tests/ui_e2e/requirements.txt
```

Install Microsoft Edge locally. Selenium Manager resolves the driver;
initial resolution may require internet access.

## Run with a visible browser

```powershell
$env:E2E_HEADLESS = "0"
py tests/ui_e2e/run_tests.py
```

Enter Warden and Student credentials when prompted.

## Run headlessly

```powershell
$env:E2E_HEADLESS = "1"
py tests/ui_e2e/run_tests.py
```

Headless mode runs the browser without displaying its window.

## Unattended execution

Provide these environment variables:

| Variable | Purpose |
| --- | --- |
| E2E_USERNAME | Warden username |
| E2E_PASSWORD | Warden password |
| E2E_STUDENT_USERNAME | Student username |
| E2E_STUDENT_PASSWORD | Student password |
| E2E_HEADLESS | Set to 1 for headless execution |
| E2E_BROWSER | chrome for Chrome; default is Edge |
| E2E_MYSQL_CONTAINER | Test MySQL container name |

The default MySQL container is hostel-sprint4-mysql.
CI uses hostel-mysql.

Do not commit real account credentials. Missing required credentials
cause CI execution to fail instead of waiting for interactive input.

## Reports and exit codes

- JSON report: tests/ui_e2e/artifacts/report.json
- Failure screenshots: tests/ui_e2e/artifacts/
- Validation screenshot: notice-validation.png
- Exit code 0: successful run
- Exit code 1: test failure or error

Use run_tests.py to generate reports and capture failure screenshots.
Direct unittest commands do not use the custom screenshot runner.

Failure/error entries can include subtest failures, so they are not
necessarily counts of distinct test methods.

## GitHub Actions

The ui-e2e job in .github/workflows/backend-ci.yml is configured to:

1. Build and start disposable MySQL and backend services.
2. Seed Warden and Student accounts and two active hostel blocks.
3. Start the frontend with local API addresses.
4. Wait for services to become available.
5. Run Selenium tests headlessly in Chrome.
6. Upload reports, screenshots and service logs.
7. Remove disposable services and volumes.

The package job depends on both migrate and ui-e2e succeeding.

## Verification status

Latest local full-suite result:

- Tests run: 11
- Failures: 0
- Errors: 0
- Skipped: 0
- Duration: 270.02 seconds
- Exit code: 0

The missing-CI-credentials check produced exit code 1 as expected.

The earlier three-test GitHub Actions run passed.
The expanded eleven-test CI run is pending verification.

Failure screenshot capture is implemented but has not been verified
with a deliberate browser-test failure.