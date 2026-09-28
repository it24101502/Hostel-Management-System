# HMS-6 — Approve or reject leave with a reason

**Branch:** `feature/HMS-6-Approve-or-reject-leave-with-a-reason`
**Epic:** HMS-6 | **Priority:** Must | **Module:** Leave & Movement | **Owner:** Member 3
**Related FRs:** FR-19–FR-21
**Sprint:** Sprint 3 — Leave & Movement

## Description

As a **Warden**, I want to approve or reject a student's leave request with a recorded reason, and record actual departure and return, so that leave decisions and student movements are traceable and student safety is maintained.

## Scope of this branch

This branch implements the warden-facing decision and movement-tracking flow: approve/reject with a mandatory reason, departure/return recording, overdue-return alerting, and the warden review UI. It builds directly on the `Pending` requests created in HMS-5.

## Acceptance Criteria

- Warden can approve or reject a `Pending` leave request
- A decision reason is required and stored with the decision
- Authorized staff can record the actual departure time against an approved request
- Authorized staff can record the actual return time, which closes the request
- The system flags and alerts the warden when a student has not returned by the expected return date

## Definition of Done

- [ ] Approve/reject workflow implemented with mandatory decision-reason capture
- [ ] Departure/return recording implemented and linked to request status transitions (`Approved → Departed → Closed`)
- [ ] Overdue-return alert/flagging logic implemented and tested
- [ ] Unit + integration tests covering approval, rejection, departure/return recording, and overdue flagging pass in CI
- [ ] All decisions and movement records appear in the audit log
- [ ] Reviewed, merged, and deployed to the test environment

## Related sub-tasks (JIRA)

| ID | Task | Priority | Points |
| --- | --- | --- | --- |
| HMS-46 | Implement approve/reject API with mandatory decision reason | Highest | 5 |
| HMS-47 | Implement departure recording API | High | 3 |
| HMS-48 | Implement return recording API + auto-close | High | 3 |
| HMS-49 | Implement overdue-return detection & warden alert | High | 5 |
| HMS-50 | Leave & movement dynamic report | High | 8 |
| HMS-51 | Build warden review/decision UI | High | 8 |
| — | Selenium E2E — full leave lifecycle (request → approve → depart → return) | Medium | 8 |
| — | JMeter load test — leave submission endpoint (≤3s target, NFR-01) | Medium | 5 |

## Dependencies

Depends on **HMS-5** (leave request submission) for `Pending` requests to act on, and on Authentication (HMS-1) for warden role/session context.

## Notes

Status transition model: `Pending → Approved/Rejected → Departed → Closed`. Departure can only be recorded for an `Approved` request; return recording closes the request automatically.

