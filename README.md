# `Sprint-2-QA-Testing` Branch — Hostel Management System

This branch contains QA verification work for **Sprint 2 (Room Management)**. It does not contain feature code — it documents and stores the test plan, test cases, test scripts/collections, and evidence used to verify the Sprint 2 deliverables before they are merged toward `main`/`deploy`.

Feature development for Sprint 2 happens on the developer's feature branches; this branch tracks QA sign-off against that work.

## Sprint 2 Scope Under Test

Sprint 2 covers **Room Management**, made up of two epics:

| Epic | Description | Jira Range |
| --- | --- | --- |
| HMS-3 | Manage rooms, beds and capacity | HMS-28 – HMS-32 |
| HMS-4 | Allocate or transfer students | HMS-33 – HMS-38 |

### HMS-3 — Manage rooms, beds and capacity (HMS-28 to HMS-32)

| ID | Item |
| --- | --- |
| HMS-28 | Design room/bed DB schema |
| HMS-29 | Implement room CRUD API |
| HMS-30 | Implement capacity validation (reject invalid/negative values) |
| HMS-31 | Implement delete-guard for occupied rooms |
| HMS-32 | Build room management UI |

### HMS-4 — Allocate or transfer students (HMS-33 to HMS-38)

| ID | Item |
| --- | --- |
| HMS-33 | Implement student allocation API |
| HMS-34 | Implement capacity-enforcement check on allocation |
| HMS-35 | Implement transfer-between-rooms logic |
| HMS-36 | Implement auto-update of occupancy status (Available/Full) |
| HMS-37 | Live occupancy report filterable by block/floor |
| HMS-38 | Build allocation/transfer UI |

## Testing Approach

This branch verifies each item above with the following test types:

- **API / CRUD tests** — Postman collection + automated assertions covering room create, read, update, delete, and the HMS-30/HMS-31 validation and delete-guard rules
- **UI / E2E tests** — Selenium (NUnit) scripts covering room management (HMS-32) and allocation/transfer (HMS-38) user flows, including over-capacity error handling
- **Performance tests** — JMeter plans targeting the room/allocation endpoints against the NFR-01 target (≤ 3 second response time under expected load)
- **Coverage** — `dotnet test` with Coverlet against the room and allocation services
- **Dynamic report test** — verification of the HMS-37 live occupancy report against seeded sample data, filtered by block and floor
- **Event/integration test** — verification that a room allocation/transfer publishes the expected domain event on the Kafka message bus and that it is consumed correctly (per the SRS event-driven microservice architecture)

## Repository Structure

```
qa/sprint2/
├── test-plan.md              # Full test case list mapped to HMS-28–38 acceptance criteria
├── postman/                  # Postman collection(s) for room + allocation API tests
├── selenium/                 # NUnit + Selenium UI test project
├── jmeter/                   # .jmx test plans and exported HTML reports
├── coverage/                 # Coverage reports (dotnet test / Coverlet output)
├── kafka-events/             # Notes/scripts verifying allocation event publish & consume
├── standup-log/              # Daily QA stand-up entries for Sprint 2
└── test-summary.md           # Final pass/fail summary, bugs found, and sign-off notes
```

## Environment

| Tool | Purpose |
| --- | --- |
| Postman / Newman | API request collections and CI-runnable assertions |
| Selenium WebDriver (NUnit) | UI/E2E test automation |
| Apache JMeter | Load/performance testing |
| Coverlet | Code coverage collection for `dotnet test` |
| Docker (local Kafka + Zookeeper) | Verifying async domain events without shared infra |
| MySQL | Seeding sample room/block/allocation data for report tests |

## How to Run

```powershell
# API tests
newman run qa/sprint2/postman/HMS-Sprint2.postman_collection.json

# UI tests
dotnet test qa/sprint2/selenium/HMS.UITests.csproj

# Unit/integration tests with coverage
dotnet test backend/HostelManagement.sln --collect:"XPlat Code Coverage"

# Performance test (JMeter, non-GUI mode)
jmeter -n -t qa/sprint2/jmeter/room-allocation-load.jmx -l qa/sprint2/jmeter/results.jtl -e -o qa/sprint2/jmeter/report
```

## Sprint 2 QA Status

- [ ] HMS-28–32 (rooms) test cases written
- [ ] HMS-33–38 (allocation/transfer) test cases written
- [ ] API/CRUD tests passing
- [ ] UI/Selenium tests passing
- [ ] JMeter results meet NFR-01 (≤ 3s)
- [ ] Coverage report generated
- [ ] HMS-37 occupancy report verified against sample data
- [ ] Kafka allocation event verified end-to-end
- [ ] Bugs logged in Jira with severity/priority
- [ ] Daily stand-up log complete for the sprint
- [ ] Test summary compiled and PR opened toward `main`/`deploy`

## Known Issues / Blockers

_Update as testing progresses — e.g. endpoints not yet available, Kafka producer not wired up, environment instability._

## Owner

QA Engineer — Sprint 2: _Suwasthikka S (IT24101502)_

## AI Usage Disclosure

Per the assignment brief, AI was used only for research and planning support (structuring this QA branch, test plan format, and testing approach) — not to generate or complete project or test code.