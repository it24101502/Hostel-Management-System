# `feature/HMS-2-register-and-maintain-student-profiles` — Hostel Management System

Feature branch for **HMS-2: Register and maintain student profiles (incl. fees)**.

- **Priority:** Must
- **Owner:** Member 1
- **Module:** Student & User Management / Fees
- **Related FRs:** FR-06–FR-09, FR-27–FR-30
- **User story:** US02
- **Sprint:** 1 (Foundation)

## Description

As an Administrator, I want to create, view, update and deactivate student and staff accounts, and manage each student's fee records, so that user information and financial status are accurate and centrally maintained.

## Acceptance Criteria

- [ ] Administrator can create, view, update and deactivate a user account (CRUD)
- [ ] A student can view and update the permitted fields of their own profile
- [ ] Email address and student registration number are validated as unique on creation/update
- [ ] Guardian/emergency contact information can be recorded and edited for each student
- [ ] Administrator can generate a fee invoice with an amount and due date
- [ ] Fee payments can be recorded and the fee status updates to Unpaid, Paid, or Overdue
- [ ] Unpaid fees automatically move to Overdue after the due date and trigger a reminder
- [ ] A downloadable fee status report can be generated per student or per hostel block

## Definition of Done

- [ ] CRUD endpoints and UI for user profiles and fee records implemented
- [ ] Uniqueness validation enforced server-side (not just client-side)
- [ ] Overdue-status automation and reminder job implemented and tested
- [ ] Fee status report generates correctly and is downloadable
- [ ] Unit + integration tests for profile CRUD and fee lifecycle pass in CI
- [ ] Changes to profiles/fees appear in the audit log
- [ ] Reviewed, merged, and deployed to the test environment

## Sub-tasks in this Branch

| ID | Task | Priority | Points |
| --- | --- | --- | --- |
| HMS-17 | Design student profile & fee DB schema — incl. guardian/emergency contact fields | High | 3 |
| HMS-18 | Implement user CRUD API (Admin) | High | 5 |
| HMS-19 | Implement student self-profile view/update API | Medium | 3 |
| HMS-20 | Implement email & reg-number uniqueness validation | Medium | 2 |
| HMS-21 | Implement guardian/emergency contact fields | Low | 2 |
| HMS-22 | Implement fee invoice generation API | High | 5 |
| HMS-23 | Implement fee payment recording & status update | High | 5 |
| HMS-24 | Implement Overdue auto-flag + reminder job | Medium | 5 |
| HMS-25 | Fee status report (per student / per block, downloadable) — dynamic report deliverable | High | 8 |
| HMS-26 | Build Admin user-management UI | Medium | 5 |
| HMS-27 | Build student profile UI | Medium | 3 |
| — | Unit + security tests — profile & fee module | High | 5 |

## Implementation Notes

- **Stack:** ASP.NET + ADO.NET (User/Fee microservice), MySQL (profile/fee schema), React (Admin + student UIs)
- Deactivated users must not be able to log in — verify against the Auth service
- Students may only view/update **permitted** fields on their own profile; restricted fields must be rejected server-side, not just hidden in the UI
- Email and registration number uniqueness must be enforced at both the DB and API layer
- The Overdue auto-flag job is a scheduled job — test with simulated/mocked dates
- Fee status report must be filterable by student or hostel block and exportable (CSV/PDF)

## Related NFRs

- NFR-03: all profile/fee changes recorded in the audit log
- NFR-09: server-side validation for all form input
- NFR-01: fee report queries should stay within the 3-second response target

## Testing

- Unit tests: CRUD operations for profiles, fee invoice/payment lifecycle
- Security tests: students cannot access or edit other students' data; restricted fields cannot be modified by students
