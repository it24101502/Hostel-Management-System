# Hostel Management System — setup and testing

This README documents the [`integration/se3112-testing`](https://github.com/it24101502/Hostel-Management-System/tree/integration/se3112-testing) branch of the Hostel Management System (HMS). The application has a React frontend, five ASP.NET services (Identity, Accommodation, Leave, Complaint, and Notice), MySQL, and Kafka. Run the commands below from the **repository root** unless a step says otherwise.

The testing work for SE3112 uses **four testing areas**, one per team member: Unit Testing & Mocking, Code Quality & Metrics, Load & Performance, and UI/E2E Testing (Selenium). Mutation testing was not selected.

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

The stack runs **one MySQL container (`hostel-mysql`) with one database per service**. The APIs use local ports **8080–8084**, and MySQL is exposed on **3307**. Docker initializes a new MySQL volume with the migrations mounted from `database/`. Existing volumes do not rerun those initialization scripts; use a suitable migration procedure when updating an existing database.

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

| Area | Tools | Install / configure |
| --- | --- | --- |
| Unit Testing & Mocking | xUnit, NSubstitute, Moq, hand-written fakes, coverlet (XPlat Code Coverage) | `dotnet restore backend/HostelManagement.sln` (NuGet restores everything) |
| Code Quality & Metrics | OpenCover coverage, ReportGenerator, SonarQube/SonarCloud | The quality script installs `dotnet-reportgenerator-globaltool` and `dotnet-sonarscanner` if missing; set `SONAR_TOKEN` for Sonar |
| Load & Performance | Apache JMeter 5.6.3, Java 17 | Put `jmeter` on `PATH`; seed test accounts with `testing/load/seed.sql` |
| UI / E2E | Selenium (Python), Edge locally / Chrome in CI | `py -m pip install -r tests/ui_e2e/requirements.txt` (Selenium Manager fetches the matching driver on first run, so it may need internet) |

```powershell
dotnet restore backend/HostelManagement.sln
py -m pip install -r tests/ui_e2e/requirements.txt
```

To run Sonar analysis locally, provide a valid `SONAR_TOKEN` and a reachable SonarQube server; without the token, the quality script generates a coverage report only.

## 3. Testing pipeline

The main GitHub Actions workflow is [`.github/workflows/testing-pipeline.yml`](https://github.com/it24101502/Hostel-Management-System/blob/integration/se3112-testing/.github/workflows/testing-pipeline.yml). It runs on pushes and pull requests targeting `integration/se3112-testing` or `main`, and can be started manually from **Actions → Testing Pipeline → Run workflow**. Each stage lives in its own reusable workflow file.

| Stage | Workflow file | What it runs | Result |
| --- | --- | --- | --- |
| Unit tests and mocking | `test-unit.yml` | `dotnet test` on `backend/HostelManagement.sln` with XPlat coverage | `unit-test-results` artifact |
| Code quality and metrics | `test-quality.yml` | Build, tests, OpenCover coverage, SonarCloud analysis when `SONAR_TOKEN` is set | `coverage-report` artifact and run summary |
| Load and performance | `test-load.yml` | Docker stack, seeded test accounts, JMeter **smoke** profile, quality gate | `load-report-smoke` artifact and run summary |
| UI / E2E (Selenium) | `test-ui.yml` | Disposable stack, seeded accounts, Chrome headless | `selenium-ui-e2e-evidence` artifact |
| Unified report | `testing-pipeline.yml` (summary job) | Collects the four job results, one row per testing area and owner | Actions run summary |

The separate [Backend CI workflow](https://github.com/it24101502/Hostel-Management-System/blob/integration/se3112-testing/.github/workflows/backend-ci.yml) also builds the services, checks database migrations, runs the Selenium UI suite against disposable services, and builds the frontend and Docker images.

### Combined local command

From the repository root in PowerShell:

```powershell
.\scripts\run-all.ps1
```

This runs three of the four layers locally: the .NET unit tests, the quality/coverage script, and a direct JMeter run. **UI/E2E is run separately** (see section 4), because it needs the frontend, seeded accounts and a browser. Start and seed the Docker stack before the JMeter stage. Use `-SkipUnit`, `-SkipQuality`, or `-SkipLoad` to omit a stage.

> The script still contains a leftover mutation stage that points to `testing/mutation/stryker-config.json`, which does not exist on this branch. Mutation testing is not part of this project, so always pass `-SkipMutation` (or delete that stage from the script).

## 4. Run each test or check separately

### .NET unit and service tests (Unit Testing & Mocking)

```powershell
dotnet test backend/HostelManagement.sln -c Release --collect:"XPlat Code Coverage" --results-directory TestResults
```

This covers the five service test projects in `tests/`. To run one project, for example:

```powershell
dotnet test tests/LeaveService.Tests/LeaveService.Tests.csproj -c Release
```

What the suite uses:

- **xUnit** as the test framework.
- **Parameterised tests** with `[Theory]` and `[InlineData]` to run one test body over multiple input cases.
- **Test doubles** to isolate each service from its dependencies: **NSubstitute** and **Moq** mocks, plus hand-written fakes in the Leave and Accommodation tests.
- **Coverage** collected with XPlat Code Coverage on every run.

Some repository tests need MySQL and are conditional on database availability. Start the local database and supply any test-specific connection configuration before expecting those integration tests to run.

### Code quality and coverage (Code Quality & Metrics)

```powershell
.\testing\quality\run-quality-local.ps1
```

The script builds and tests the solution, writes an HTML and JSON coverage report to `testing/quality/coverage-report/`, prints line and branch coverage, and opens the HTML report. Set `SONAR_TOKEN` (and, if needed, `SONAR_HOST_URL`) to add local Sonar analysis, which reports code smells, bugs, vulnerabilities and technical debt on the Sonar dashboard.

Notes on how the numbers are produced:

- The GitHub Actions quality job runs the Sonar step only when the `SONAR_TOKEN` secret is present. Without it, the step is skipped and only the coverage report is produced.
- Coverage excludes the Repositories, Kafka and BackgroundServices code, which depend on live infrastructure. The reported percentage therefore describes the testable service and domain logic, not the whole codebase.

### Frontend build

```powershell
cd frontend
npm ci --ignore-scripts
npm run build
```

This is a build check. The frontend `package.json` does not define a unit-test script. Return to the repository root before the remaining commands.

### Selenium UI / end-to-end tests (UI / E2E)

Use a **test database** with the local frontend, IdentityService, AccommodationService, NoticeService, Kafka, and migrations running (the Docker command and Vite server from section 1).

Prepare these fixtures in the test database before running:

- An active **Warden** account.
- An active **Student** account with a hostel block claim.
- Two active hostel blocks.

CI seeds these automatically in `test-ui.yml`; locally you must create them yourself. The [UI test README](https://github.com/it24101502/Hostel-Management-System/blob/integration/se3112-testing/tests/ui_e2e/README.md) describes every fixture and limit.

Test 11 inspects MySQL through `docker exec`, and the suite looks for a container named `hostel-sprint4-mysql` by default. The compose stack in this README names it `hostel-mysql`, so set the variable below or test 11 will fail.

```powershell
$env:E2E_HEADLESS = '1'
$env:E2E_MYSQL_CONTAINER = 'hostel-mysql'
py tests/ui_e2e/run_tests.py
```

Environment variables:

| Variable | Purpose |
| --- | --- |
| `E2E_USERNAME`, `E2E_PASSWORD` | Warden credentials (prompted for if unset) |
| `E2E_STUDENT_USERNAME`, `E2E_STUDENT_PASSWORD` | Student credentials (prompted for if unset) |
| `E2E_MYSQL_CONTAINER` | MySQL container name for test 11 (`hostel-mysql` with this compose file) |
| `E2E_BROWSER` | Set to `chrome` to use Chrome instead of local Edge |
| `E2E_HEADLESS` | `1` for headless, `0` for a visible browser |

Results go to `tests/ui_e2e/artifacts/report.json`; failed tests capture screenshots in the same folder. For unattended execution, set all four credential variables.

### JMeter load and performance tests (Load & Performance)

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

Four testing areas were chosen: Unit Testing & Mocking, Code Quality & Metrics, Load & Performance, and UI/E2E Testing. Mutation testing was not selected.

| Member | Product responsibility | Testing area |
| --- | --- | --- |
| Suwasthikka S — IT24101502 | Daily operations (complaints, schedules) | Code Quality & Metrics (`testing/quality/`, `test-quality.yml`) |
| Peiris M P V P (Prashasthi) — IT24100245 | Leave and movement | Load & Performance (`testing/load/`, `test-load.yml`) |
| De Silva D S P S N (Sandil) — IT24102190 | Rooms and allocation | Unit Testing & Mocking (`tests/*`, `test-unit.yml`) |
| Premarathna P A I B (Imansa) — IT24101844 | Users and fees | UI / E2E with Selenium (`tests/ui_e2e/`, `test-ui.yml`) |
