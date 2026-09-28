# HMS-5 — Submit a complete leave request

**Branch:** `feature/HMS-5-Submit-a-complete-leave-request`
**Epic:** HMS-5 | **Priority:** Must | **Module:** Leave & Movement | **Owner:** Member 3
**Related FRs:** FR-15–FR-18, FR-22
**Sprint:** Sprint 3 — Leave & Movement

## Description

As a **Student**, I want to submit a leave request with my departure date, return date, reason and companion/guardian details, and track its status, so that I can obtain permission to go home and know where my request stands.

## Scope of this branch

This branch implements the student-facing leave request submission flow end to end: schema, API, validation, notification on submit, status tracking, and the submission UI.

## Acceptance Criteria

- Departure and expected-return dates are required to submit a request
- A reason must be provided
- Companion or guardian details are captured when required
- A request is rejected if the return date is earlier than the departure date
- A request missing any required field (dates, reason, or companion/guardian info) is rejected
- A valid request is saved with status `Pending` and the assigned warden is notified
- The student can view the real-time status of their submitted request(s)

## Definition of Done

- [ ] Leave request form and submission API implemented with full server-side validation
- [ ] `Pending`-status workflow and warden notification implemented
- [ ] Status tracking view available to the student
- [ ] Unit + integration tests covering valid submission, missing-field rejection, and invalid-date-range rejection pass in CI
- [ ] Request submissions recorded in the audit log
- [ ] Feature demoed and accepted by the product owner

## Related sub-tasks (JIRA)

| ID | Task | Priority | Points |
| --- | --- | --- | --- |
| HMS-39 | Design leave request DB schema (status enum: Pending/Approved/Rejected/Closed) | High | 3 |
| HMS-40 | Implement leave request submission API | Highest | 5 |
| HMS-41 | Implement required-field validation (dates, reason, companion/guardian) | Highest | 2 |
| HMS-42 | Implement date-range validation (return ≥ departure) | High | 2 |
| HMS-43 | Implement Pending status + warden notification on submit | High | 5 |
| HMS-44 | Implement student-facing request status tracking | Medium | 3 |
| HMS-45 | Build leave request submission UI | High | 5 |
| — | Unit + integration tests — submission, validation, status tracking | Medium | 5 |

## Dependencies

Depends on Authentication (HMS-1) and Room Management (HMS-3/HMS-4) modules being available, per the sprint plan (leave & movement is core student-facing workflow layered on Auth + Room data).

## Notes

Business rule: a request cannot be approved when required dates, reason, or companion/guardian information is missing (enforced at submission, checked again at approval in HMS-6).

