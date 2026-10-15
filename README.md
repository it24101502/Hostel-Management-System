# `integration/se3112-testing` Branch — Hostel Management System

This branch holds the **SE3112 Advanced Software Engineering** testing pipeline for the Hostel Management System (HMS) microservices. It layers four testing disciplines on top of the existing HMS code and runs them together in a single GitHub Actions workflow.

It is an integration branch for testing work, not for feature development. Merge feature changes through `main`; merge tested pipeline changes here from the individual `feature/*` testing branches.

## Pipeline Overview

The orchestrating workflow is `.github/workflows/testing-pipeline.yml`. It triggers on manual dispatch, on pushes to this branch, and on pull requests into this branch or `main`.

| Layer | Owner | Workflow | Local tooling |
| --- | --- | --- | --- |
| Unit testing & mocking | Sandil | `test-unit.yml` | xUnit, Moq, NSubstitute, Coverlet |
| Code quality & metrics | Suwasthikka | `test-quality.yml` | SonarCloud, ReportGenerator, Coverlet |
| Mutation testing | Imansa | `test-mutation.yml` | Stryker.NET |
| Load & performance | Prashasthi | `test-load.yml` | Apache JMeter |

Execution order:

1. **Unit** and **Quality** start in parallel.
2. **Mutation** and **Load** start after Unit succeeds.
3. A **Unified report** job always runs last and writes a results table to the run summary.

> **Status:** Unit and Quality are implemented. `test-mutation.yml` and `test-load.yml` are currently placeholders and are replaced as those layers are completed.

## Layers

### 1. Unit testing & mocking

Runs every test project in `backend/HostelManagement.sln` in Release mode with code coverage.

- Test projects: `tests/IdentityService.Tests`, `AccommodationService.Tests`, `LeaveService.Tests`, `ComplaintService.Tests`, `NoticeService.Tests`
- Test doubles: hand-written fakes (repositories, event publishers, `FixedTimeProvider` for simulated dates), NSubstitute (Complaint) and Moq (Notice)
- MySQL-backed repository tests are tagged `Category=MySql` and run only when `NOTICE_TEST_CONNECTION` is set
- Output: `unit-test-results` artifact (coverage files)

```bash
dotnet test backend/HostelManagement.sln -c Release --collect:"XPlat Code Coverage" --results-directory TestResults
```

### 2. Code quality & metrics

Builds the solution, runs tests with coverage, then publishes results to SonarCloud and a coverage report.

- Coverage settings: `testing/quality/coverlet.runsettings` (OpenCover + Cobertura)
- Excluded from coverage: `Program.cs`, `Repositories/**`, `Kafka*` publishers, `BackgroundServices/**`
- SonarCloud steps run only when the `SONAR_TOKEN` secret is available; otherwise only the coverage report is produced
- Output: `coverage-report` artifact (HTML) and a coverage table in the run summary

Run locally (Windows PowerShell):

```powershell
./testing/quality/run-quality-local.ps1
```

### 3. Mutation testing

Uses Stryker.NET to check that the tests actually catch injected faults. Configuration lives in `testing/mutation/stryker-config.json`.

```powershell
cd tests/LeaveService.Tests
dotnet stryker --config-file ../../testing/mutation/stryker-config.json
```

### 4. Load & performance

Uses JMeter to load-test critical endpoints, mainly leave request submission against the ≤ 3 second response target (NFR-01). The test plan lives in `testing/load/leave-load.jmx`; raw results (`*.jtl`) and reports under `testing/load/report/` are git-ignored.

```powershell
jmeter -n -t testing/load/leave-load.jmx -l testing/load/results.jtl -e -o testing/load/report
```

## Running Everything Locally

`scripts/run-all.ps1` runs all four stages and prints a pass/fail summary. Each stage can be skipped.

```powershell
./scripts/run-all.ps1
./scripts/run-all.ps1 -SkipMutation -SkipLoad
```

Prerequisites: .NET 10 SDK, Java 17 (Sonar scanner), `dotnet-reportgenerator-globaltool`, `dotnet-sonarscanner`, `dotnet-stryker`, and JMeter on your `PATH`.

## Repository Layout (testing-related)

```
.github/workflows/
  testing-pipeline.yml    # orchestrator
  test-unit.yml
  test-quality.yml
  test-mutation.yml
  test-load.yml
  backend-ci.yml          # existing build/test/migrate/deploy pipeline
backend/                  # IdentityService, AccommodationService, LeaveService,
                          # ComplaintService, NoticeService
tests/                    # xUnit test projects, one per service
testing/
  quality/                # Coverlet settings + local quality script
  mutation/               # Stryker config
  load/                   # JMeter plan and reports
scripts/run-all.ps1       # local runner for all stages
database/                 # migrations and CI sample data
```

## Secrets and Configuration

| Name | Used by | Purpose |
| --- | --- | --- |
| `SONAR_TOKEN` | Quality | SonarCloud authentication |
| `NOTICE_TEST_CONNECTION` | Unit (optional) | Enables MySQL repository tests |

Never commit secrets. Local `.env` files are git-ignored; only the `*.example` templates are tracked.

## Workflow for Contributors

1. Create a branch from `integration/se3112-testing` (for example `feature/mutation-testing`).
2. Make your changes and run the relevant stage locally.
3. Open a pull request into `integration/se3112-testing` and confirm the **Testing Pipeline** run is green.
4. When the full pipeline is stable, merge into `main` for the final submission.

## Deliverables Checklist

- [ ] Unit tests and mocks passing in CI
- [ ] Coverage and SonarCloud quality results available
- [ ] Mutation score recorded
- [ ] Load test results documented against the 3-second target
- [ ] Unified pipeline run green
- [ ] Live demo and viva prepared

## Team

| Member | Responsibility |
| --- | --- |
| Sandil | Unit testing & mocking |
| Prashasthi | Load & performance |
| Suwasthikka | Code quality & metrics |
| Imansa | Mutation testing |

## Related

- `main`: stable feature code
- `deploy`: deployment configuration and release pipeline