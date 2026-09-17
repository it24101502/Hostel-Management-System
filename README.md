# Sprint 2 QA Testing — Hostel Management System (HMS)

**Branch:** `Sprint-2-QA-Testing`
**Module:** SE3022 — Case Study Project (Year 3, Semester 1, 2026)
**QA Owner:** IT24101502 — Suwasthikka S
**Sprint Focus:** Room Management, Allocation/Transfer, UI, Performance, Coverage & Dynamic Reports

---

## 1. Purpose of This Branch

This branch contains the Sprint 2 QA verification work for the Accommodation module of the Hostel Management System — room CRUD, student allocation/transfer, live occupancy reporting, and the supporting automated tests, performance tests, and UI tests that back Sprint 2's deliverables.

## 2. Executive Summary

Sprint 2 QA covered Room Management and Allocation/Transfer functionality, including CRUD operations and role restrictions, Selenium UI testing, Apache JMeter performance testing, automated unit/integration test verification with code coverage, audit logging, CI/CD pipeline validation, and dynamic occupancy-report validation.

The Sprint 2 test specification defined **57 test cases**:

| Category | Cases |
| --- | --- |
| Room CRUD | 13 |
| Allocation/Transfer | 14 |
| UI / Selenium | 10 |
| Performance / JMeter | 4 |
| Coverage & Automation | 7 |
| Dynamic Reports | 6 |
| Kafka (deferred) | 5 |

All executable categories were tested with no defects identified. The Kafka category (5 cases) was recorded as **BLOCKED / DEFERRED** rather than failed, since Kafka integration was not implemented in Sprint 2.

## 3. Test Environment

| Component | Configuration |
| --- | --- |
| Accommodation API | `http://localhost:8081` |
| Identity Service (Auth) | `http://localhost:8080` (ADMIN Bearer token required) |
| Frontend | `http://localhost:5173` |
| API Tooling | Postman |
| UI Automation | Selenium |
| Performance | Apache JMeter |
| Automated Tests | xUnit / `dotnet test` |
| Coverage | Coverlet collector (Cobertura/HTML report) |
| CI/CD | GitHub Actions — Backend CI pipeline |
| Audit Verification | Database / admin endpoint spot-checks |

## 4. Scope Covered

- **Room Management** — create, read, update, delete; validation; duplicate-location prevention; persistence checks
- **Allocation/Transfer** — allocation with capacity checks, over-capacity rejection, transfer between rooms, release/unallocate, concurrency (last-bed race condition)
- **Authorization** — unauthenticated (401) and non-admin (403) access restrictions
- **UI (Selenium)** — forms, navigation, validation, filtering/search, allocation flow, session-expiry handling
- **Performance (JMeter)** — load testing on room listing, occupancy report, allocation, and transfer endpoints against the ≤ 3s response-time NFR
- **Automation & Coverage** — xUnit service test suites, coverage generation, audit log verification, CI pipeline validation
- **Dynamic Reporting** — live occupancy calculations, block/floor filters, status logic, UI/API consistency

## 5. Key Results

### 5.1 Room CRUD
Positive and negative create/read/update/delete scenarios all behaved as expected: `201` on valid creation, `400` on zero capacity or invalid block, `409` on duplicate location, `404` on missing room, `204` on successful delete, `409` on deleting an occupied room, and correct `401`/`403` access restrictions.

### 5.2 Allocation & Transfer
Allocation and transfer enforced capacity limits correctly (`201`/`409` as appropriate), rejected duplicate allocations and invalid rooms, and the concurrency test for the **last available bed** produced exactly one `201` and one `409` across two simultaneous requests — confirming no double booking.

### 5.3 Performance (JMeter)

| Test | Load | Samples | Threshold |
| --- | --- | --- | --- |
| List rooms | 20 threads × 5 loops | 100 | Avg + P90 ≤ 3000 ms; all `200` |
| Live occupancy | 50 threads × 5 loops | 250 | 0% errors; avg ≤ 3000 ms |
| Allocate | 20 threads × 1 loop | 20 | All `201`; no 5xx; ≤ 3000 ms |
| Transfer | 10 concurrent threads | 10 | All `200`; avg ≤ 3000 ms |

> Raw JMeter Aggregate/Summary metrics (actual averages, P90, throughput) are retained in the QA evidence screenshots rather than restated here.

### 5.4 Automation, Coverage & CI
`RoomServiceTests`, `RoomAllocationServiceTests`, and `HostelBlockServiceTests` all passed. Coverage was generated via Coverlet/Cobertura; the exact minimum coverage threshold is still to be formally agreed with the Dev/BA. Room and allocation audit logs (`CREATE`/`UPDATE`/`DELETE`, `ALLOCATE`/`TRANSFER`/`RELEASE`) were verified against the database. The GitHub Actions backend CI pipeline ran migrations, `dotnet test`, frontend build, and Docker image builds successfully.

### 5.5 Dynamic Reports (Live Occupancy)
Verified with no filters, block filter, block + floor filter, before/after allocation-change consistency, correct `AVAILABLE`/`FULL`/`INACTIVE` status logic, and UI-to-API consistency on the occupancy dashboard.

### 5.6 UI Testing (Selenium + Manual)
Covered room/block form validation, allocation and transfer flows, the occupied-room deletion error path, block/floor filtering and room search, and session-expiry redirect behaviour. A manual smoke pass over login and core navigation was also performed.

## 6. Defects

**No defects were identified during Sprint 2 testing.** All executed test cases passed; no bug logging or defect-severity tracking was required this sprint.

## 7. Deferred Item — Kafka Event Testing

Kafka producer/consumer integration was not implemented in Sprint 2, so the 5 Kafka test cases are recorded as **BLOCKED**, not failed:

| Test | Purpose | Sprint 3+ Action |
| --- | --- | --- |
| TC-KAFKA-01 | Producer publishes event | Implement producer; execute test |
| TC-KAFKA-02 | Consumer processes event | Implement consumer; verify DB side effect |
| TC-KAFKA-03 | Malformed message handling | Test invalid JSON + recovery/DLQ |
| TC-KAFKA-04 | Consumer restart/offset resumption | Test restart, offset resume, duplicate prevention |

## 8. Retrospective — Sprint 3 Actions

| Finding | Sprint 3 Improvement | Owner |
| --- | --- | --- |
| No agreed coverage threshold | Agree and document minimum coverage % before the Sprint 3 CI gate | QA + Dev/BA |
| Evidence not consistently tied to test IDs | Name screenshots/results by test-case ID and link to Jira | QA Owner |
| JMeter results need raw metrics archived | Archive Aggregate/Summary reports (avg, P90, error %, throughput) | QA / Performance Owner |
| QA findings not yet tracked as backlog items | Create/assign Sprint 3 Jira items for open QA findings | Scrum Team |

## 9. Full Report

The complete QA report — including the detailed test-case tables and the evidence screenshot appendix — is available in `IT24101502_Group_05_Sprint_2_QA_Report.pdf` in this branch.

## 10. Overall Assessment

Sprint 2 QA provides broad coverage of the implemented Room Management and Allocation/Transfer functionality across positive, negative, persistence, authorization, UI, performance, automation, reporting, and CI dimensions. The only deferred area is Kafka, planned for an upcoming sprint. Final sign-off should reference the actual measured JMeter metrics, coverage percentage, CI run, and any Jira items raised from this retrospective.