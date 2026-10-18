\# NoticeService UI / E2E Testing



Owner: P. A. I. B. Premarathna — IT24101844

Module: SE3112 Advanced Software Engineering

Area: UI / E2E Testing



\## Tools



Python 3.13, Selenium 4.51.0 and unittest.

Local runs use Microsoft Edge. CI is configured to use Chrome.



\## Scope



Three automated tests cover:



1\. Warden login and required-field validation.

2\. Notice creation, retrieval, editing and deletion.

3\. Schedule creation, retrieval, editing and deletion.



CRUD tests reload the page to verify persisted changes.

Each CRUD test uses a unique title and deletes its record on success.

A failed CRUD test may leave its uniquely named test record behind.



The tests use the real React frontend, APIs and MySQL database.

Student visibility, block filtering and expiry behavior are outside

the current test scope.



\## Local prerequisites



Start these services before running:



\- React frontend: http://localhost:5173

\- IdentityService: http://localhost:8080

\- AccommodationService: http://localhost:8081

\- NoticeService: http://localhost:8084

\- MySQL with the identity, accommodation and notice migrations applied



The frontend must use the local API addresses.

IdentityService and NoticeService must share compatible JWT settings.

Use an active Warden test account.



\## Install



Run from the repository root:



```powershell

py -m pip install -r tests/ui\_e2e/requirements.txt

```



Microsoft Edge must be installed. Selenium Manager resolves its driver;

initial resolution may require internet access.



\## Run with a visible browser



```powershell

$env:E2E\_HEADLESS = "0"

py tests/ui\_e2e/run\_tests.py

```



Enter the Warden username and password when prompted.



\## Run headlessly



```powershell

$env:E2E\_HEADLESS = "1"

py tests/ui\_e2e/run\_tests.py

```



For unattended runs, supply E2E\_USERNAME and E2E\_PASSWORD through the

environment. Do not commit real credentials.



Set E2E\_BROWSER=chrome to use Chrome; Edge is the default.



\## Reports and exit codes



\- JSON report: tests/ui\_e2e/artifacts/report.json

\- Failure screenshots: tests/ui\_e2e/artifacts/

\- Validation screenshot: notice-validation.png

\- Exit code 0: successful run

\- Exit code 1: test failure or error



Failure/error entries can include subtest failures, so they are not

necessarily counts of distinct test methods.



\## CI



The ui-e2e job in .github/workflows/backend-ci.yml is configured to:



1\. Start disposable MySQL and backend services using Docker Compose.

2\. Create a Warden account in the disposable database.

3\. Start the frontend with local API addresses.

4\. Run Selenium tests headlessly.

5\. Upload reports, screenshots and service logs.

6\. Remove the disposable services and volumes.



CI credentials must be provided through environment variables.

Missing credentials cause an immediate error instead of an input prompt.



\## Verification status



Local visible-browser and headless runs: 3 tests passed.

Missing-CI-credentials check: failed immediately with exit code 1,

as expected.

GitHub Actions execution: pending verification.

Failure screenshot capture: implemented, not yet verified with a

deliberate browser-test failure.

