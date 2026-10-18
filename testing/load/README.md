# Load and Performance Testing (SE3112)

**Owner:** Prashasthi (IT24100245) · **Tool:** Apache JMeter 5.6.3 · **Target:** NFR-01, every API response within 3 seconds

This folder load-tests all five HMS microservices together, the way they run in production (Docker Compose: MySQL, Kafka, 5 ASP.NET services).

## What is tested

| Thread group | Virtual users | Endpoints (service → port) |
|---|---|---|
| setUp (runs once first) | 1 | `POST /api/auth/login` for the student and the admin; tokens are shared with every other group |
| Students | `student_users` | `POST /api/student/leave-requests` (Leave 8082) → `GET /api/student/leave-requests/{id}` → `GET /api/student/leave-requests` → `GET /api/student/complaints` (Complaint 8083) |
| Staff | `staff_users` | `GET /api/admin/rooms` (Accommodation 8081), `GET /api/notices` (Notice 8084), `GET /api/staff/leave-requests/report` (Leave 8082), `GET /api/admin/users` (Identity 8080) |
| Logins | `login_users` | `POST /api/auth/login` (Identity 8080), which is CPU-heavy because of BCrypt |

Every request checks two things: the **status code** (201 for create, 200 for reads) and the **duration** (≤ `sla_ms`, 3000 ms).

## Load profiles (`profiles/*.properties`)

| Profile | Students / Staff / Logins | Ramp-up | Duration | Purpose |
|---|---|---|---|---|
| smoke | 5 / 1 / 1 | 5 s | 30 s | CI check on every pipeline run |
| load | 50 / 5 / 5 | 30 s | 120 s | Normal busy-hour traffic |
| stress | 200 / 10 / 20 | 60 s | 180 s | Far beyond normal, to find what breaks first |
| spike | 150 / 5 / 10 | 1 s | 60 s | Sudden burst, then checking it recovers |

`stress-steps.sh` runs 25 → 50 → 100 → 200 → 400 students (60 s each) to find the **breaking point** where p95 crosses 3 s.

## Setup (once)

```bash
# 1. Tools (Mac)
brew install openjdk@17 jmeter

# 2. Start the stack from the repo root
cp .env.example .env          # set MYSQL_ROOT_PASSWORD and a JWT_KEY of 32+ chars
docker compose up -d --build mysql kafka kafka-init identity-api accommodation-api leave-api complaint-api notice-api

# 3. Create the test accounts (loadtest_student / loadtest_admin, password LoadTest@2026)
docker exec -i hostel-mysql sh -c 'mysql -uroot -p"$MYSQL_ROOT_PASSWORD"' < testing/load/seed.sql
```

## Run

```bash
./testing/load/run-load.sh smoke              # quick check
RESET_DB=1 ./testing/load/run-load.sh load    # clean leave rows first, then run
./testing/load/stress-steps.sh                # breaking-point search
open testing/load/report/load/index.html      # JMeter HTML dashboard
```

Plain JMeter command (what the script runs):

```bash
jmeter -n -t testing/load/leave-load.jmx -q testing/load/profiles/load.properties \
       -l testing/load/report/load.jtl -e -o testing/load/report/load
```

Any setting can be overridden with `-J`, e.g. `-Jstudent_users=80 -Jhost=20.212.39.185`.

`scripts/run-all.ps1` runs the same plan without a profile file (`jmeter -n -t testing/load/leave-load.jmx ...`), so it uses the built-in defaults: 20 students, 3 staff, 2 logins, 60 s. The stack must be running and `seed.sql` applied first.

## Quality gate

`summarize.py` reads the dashboard's `statistics.json` and the raw `.jtl` file. It prints per-endpoint avg/p90/p95/p99/throughput and splits failures into:

- **functional errors**: wrong status code, timeout, or refused connection
- **SLA misses**: correct response, but slower than 3 s

The gate **fails** (exit code 1, red CI job) if any endpoint has p95 > 3000 ms or functional errors > 1 %.

## CI (`.github/workflows/test-load.yml`)

`testing-pipeline.yml` calls this after the unit tests pass, using the **smoke** profile. It starts the stack in Docker on the runner, seeds the accounts, installs JMeter (cached), runs the gate, writes the results table to the run summary and uploads the HTML dashboard as the `load-report-smoke` artifact. Heavier profiles can be started by hand: **Actions → Load and Performance Testing → Run workflow**.

## Files

| File | What it is |
|---|---|
| `leave-load.jmx` | The JMeter test plan (open in the JMeter GUI to view or edit) |
| `profiles/*.properties` | The four load profiles |
| `seed.sql` | Test-only student and admin accounts |
| `reset-leave-data.sql` | Deletes leave rows created by earlier runs |
| `run-load.sh` | Runs one profile, then the dashboard and gate |
| `report/` (generated) | Dashboards, `.jtl` files and summaries. Git-ignored by the root `.gitignore`, never committed |
| `stress-steps.sh` | Step-stress, which finds the breaking point |
| `summarize.py` | Summary table and quality gate |
