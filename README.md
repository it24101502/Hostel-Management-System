# Hostel Management System — setup and testing

This README documents the [`integration/se3112-testing`](https://github.com/it24101502/Hostel-Management-System/tree/integration/se3112-testing) branch of the Hostel Management System (HMS). The application has a React frontend, five ASP.NET services (Identity, Accommodation, Leave, Complaint, and Notice), MySQL, and Kafka. Run the commands below from the **repository root** unless a step says otherwise.

## 1. Project setup

### Required tools

| Tool | Used for |
| --- | --- |
| Git | Clone and select the testing branch |
| Docker with Docker Compose | MySQL, Kafka, and the backend services |
| .NET SDK 10 | Build and run service tests |
| Node.js 22 and npm | React frontend and its build check |
| Python 3.13 and pip | Selenium UI tests |
| Microsoft Edge | Local Selenium runs (Chrome is used in CI) |
| Java 17 and Apache JMeter 5.6.3 | Load and performance tests |
| PowerShell | The local combined and quality scripts |

The load scripts are Bash scripts and also require `curl` and `python3`. On Windows, use WSL or a Bash environment for those scripts.

### Get the code and configure it

```powershell
git clone https://github.com/it24101502/Hostel-Management-System.git
cd Hostel-Management-System
git switch integration/se3112-testing
Copy-Item .env.example .env
```

Edit `.env` and set `MYSQL_ROOT_PASSWORD` and `JWT_KEY`; the JWT key must be at least 32 characters. Keep `.env` and real credentials out of version control. `SONAR_TOKEN` is needed only when running the optional Sonar analysis locally.

Start the database, Kafka, and five APIs:

```powershell
docker compose up -d --build mysql kafka kafka-init identity-api accommodation-api leave-api complaint-api notice-api
docker compose ps
```

The APIs use local ports **8080–8084**, and MySQL is exposed on **3307**. Docker initializes a new MySQL volume with the migrations mounted from `database/`. Existing volumes do not rerun those initialization scripts; use a suitable migration procedure when updating an existing database.

For the local React frontend, open a second terminal:

```powershell
cd frontend
npm ci --ignore-scripts
$env:VITE_API_BASE_URL = 'http://localhost:8080'
$env:VITE_ACCOMMODATION_API_BASE_URL = 'http://localhost:8081'
$env:VITE_LEAVE_API_BASE_URL = 'http://localhost:8082'
$env:VITE_COMPLAINT_API_BASE_URL = 'http://localhost:8083'
$env:VITE_NOTICE_API_BASE_URL = 'http://localhost:8084'
npm run dev -- --host 0.0.0.0 --port 5173 --strictPort
```

Open <http://localhost:5173>. The `frontend` service in `compose.yml` is configured with remote API build URLs, so use the local Vite command above for local UI testing unless you change those build arguments.

## 2. Tool setup for testing

Install the .NET test dependencies through the normal restore operation:

```powershell
dotnet restore backend/HostelManagement.sln
```

For Selenium, install the pinned Python packages from the repository root:

```powershell
py -m pip install -r tests/ui_e2e/requirements.txt
```

Install Edge locally. Selenium Manager obtains the matching driver; its first run may need internet access. For load tests, install Java 17 and JMeter 5.6.3, and ensure `jmeter` is on `PATH`. The local quality script installs `dotnet-reportgenerator-globaltool` and `dotnet-sonarscanner` if missing. To run Sonar analysis, provide a valid `SONAR_TOKEN` and a reachable SonarQube server; without the token, the script generates a coverage report only.

## 3. Testing pipeline

The main GitHub Actions workflow is [`.github/workflows/testing-pipeline.yml`](https://github.com/it24101502/Hostel-Management-System/blob/integration/se3112-testing/.github/workflows/testing-pipeline.yml). It runs on pushes and pull requests targeting `integration/se3112-testing` or `main`, and can be started manually from **Actions → Testing Pipeline → Run workflow**.

| Stage | What it runs | Result |
| --- | --- | --- |
| Unit tests and mocking | `dotnet test` on `backend/HostelManagement.sln` with XPlat coverage | `unit-test-results` artifact |
| Code quality and metrics | Build, tests, OpenCover coverage, optional SonarCloud analysis | `coverage-report` artifact and run summary |
| Load and performance | Docker stack, seeded test accounts, JMeter **smoke** profile, quality gate | `load-report-smoke` artifact and run summary |
| Unified report | Collects the four job results | Actions run summary |

The separate [Backend CI workflow](https://github.com/it24101502/Hostel-Management-System/blob/integration/se3112-testing/.github/workflows/backend-ci.yml) also builds services, checks database migrations, runs the Selenium UI suite against disposable services, and builds the frontend and Docker images. The Selenium suite is **not** part of `testing-pipeline.yml`.

### Combined local command

From the repository root in PowerShell:

```powershell
.\scripts\run-all.ps1 -SkipMutation
```

This runs the .NET tests, quality/coverage script, and a direct JMeter run. Start and seed the Docker stack before the JMeter stage. The script's mutation command references `testing/mutation/stryker-config.json`, which is absent from this branch; use `-SkipMutation` until a real mutation configuration is added. You can also use `-SkipUnit`, `-SkipQuality`, or `-SkipLoad` to omit a stage. The combined script does not run Selenium.

## 4. Run each test or check separately

### .NET unit and service tests

```powershell
dotnet test backend/HostelManagement.sln -c Release --collect:"XPlat Code Coverage" --results-directory TestResults
```

This covers the five service test projects in `tests/`. To run one project, for example:

```powershell
dotnet test tests/LeaveService.Tests/LeaveService.Tests.csproj -c Release
```

Some repository tests need MySQL and are conditional on database availability. Start the local database and supply any test-specific connection configuration before expecting those integration tests to run.

### Code quality and coverage

```powershell
.\testing\quality\run-quality-local.ps1
```

The script builds and tests the solution, writes an HTML and JSON coverage report to `testing/quality/coverage-report/`, prints line and branch coverage, and opens the HTML report. Set `SONAR_TOKEN` (and, if needed, `SONAR_HOST_URL`) to add local Sonar analysis. The GitHub Actions quality job uses SonarCloud when its secret is present.

### Frontend build

```powershell
cd frontend
npm ci --ignore-scripts
npm run build
```

This is a build check. The frontend `package.json` does not define a unit-test script. Return to the repository root before the remaining commands.

### Selenium UI / end-to-end tests

Use a **test database** with the local frontend, IdentityService, AccommodationService, NoticeService, Kafka, and migrations running. Prepare an active Warden account, an active Student account with a hostel block claim, and two active blocks. Test 11 needs access to the test MySQL Docker container. The [UI test README](https://github.com/it24101502/Hostel-Management-System/blob/integration/se3112-testing/tests/ui_e2e/README.md) describes all fixtures and limits.

```powershell
$env:E2E_HEADLESS = '1'
py tests/ui_e2e/run_tests.py
```

The runner prompts for Warden and Student credentials if their environment variables are not set. For unattended execution, set `E2E_USERNAME`, `E2E_PASSWORD`, `E2E_STUDENT_USERNAME`, and `E2E_STUDENT_PASSWORD`. Set `E2E_BROWSER=chrome` to use Chrome instead of local Edge. Results go to `tests/ui_e2e/artifacts/report.json`; failed tests capture screenshots in the same folder. Use `E2E_HEADLESS='0'` for a visible browser.

### JMeter load and performance tests

Start all five APIs first and seed **test-only** accounts as described in the [load testing README](https://github.com/it24101502/Hostel-Management-System/blob/integration/se3112-testing/testing/load/README.md). In a Bash shell from the repository root:

```bash
docker exec -i hostel-mysql sh -c 'mysql -uroot -p"$MYSQL_ROOT_PASSWORD"' < testing/load/seed.sql
./testing/load/run-load.sh smoke
./testing/load/run-load.sh load
./testing/load/run-load.sh stress
./testing/load/run-load.sh spike
```

Run one profile at a time. `smoke` is the CI check; `load`, `stress`, and `spike` exercise heavier traffic. For repeatable load runs, use `RESET_DB=1 ./testing/load/run-load.sh load` to remove prior **load-test** leave rows. The script creates a JMeter HTML dashboard and raw results under `testing/load/report/`, then fails its gate when an endpoint's p95 exceeds **3000 ms** or functional errors exceed **1%**. The separate `stress-steps.sh` script searches for a breaking point.

## 5. Team responsibilities

The project README identifies the four members' primary product areas. The testing workflow and test READMEs identify the testing assignments below. The team confirmed that Sandil is De Silva and Imansa is Premarathna.

| Member / identifier | Product responsibility | Testing responsibility documented in this branch |
| --- | --- | --- |
| Suwasthikka S — IT24101502 | Daily operations: complaints and schedules | Code quality and metrics (workflow owner: Suwasthikka) |
| Peiris M P V P — IT24100245 | Leave and movement | JMeter load and performance testing (load README names Prashasthi, IT24100245) |
| De Silva D S P S N (Sandil) — IT24102190 | Rooms and allocation | Unit tests and mocking |
| Premarathna P A I B (Imansa) — IT24101844 | Users and fees | Selenium NoticeService UI/E2E testing (UI README); |
