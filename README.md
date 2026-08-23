# `Sprint-1-QA-Testing` Branch — Hostel Management System

QA verification branch for **Sprint 1: Foundation — Identity & Users**.

## Sprint 1 Goal

Stand up the CI/CD pipeline, database schemas, and secure authentication with role-based access, so every module has a working, deployable foundation to build on.

## Scope Under Test

This branch is for testing the epics and tasks delivered in Sprint 1:

- **HMS-1 — Secure login and role-based access** (US01, FR-01–FR-05)
- **HMS-2 — Register and maintain student profiles (incl. fees)** (US02, FR-06–FR-09, FR-27–FR-30)

### Underlying tasks in scope

| ID | Task | Priority |
| --- | --- | --- |
| HMS-9 | Design auth DB schema (users, roles, sessions) | High |
| HMS-10 | Implement login API (email/username + password) | Highest |
| HMS-11 | Implement role-based access middleware | Highest |
| HMS-12 | Implement account lockout after 5 failed attempts | Medium |
| HMS-13 | Implement JWT issuance and validation | Highest |
| HMS-14 | Implement authentication audit logging | Medium |
| HMS-15 | Build login UI (React) | High |
| HMS-17 | Design student profile & fee DB schema | High |
| HMS-18 | Implement user CRUD API (Admin) | High |
| HMS-19 | Implement student self-profile view/update API | Medium |
| HMS-20 | Implement email & reg-number uniqueness validation | Medium |
| HMS-21 | Implement guardian/emergency contact fields | Low |
| HMS-22 | Implement fee invoice generation API | High |
| HMS-23 | Implement fee payment recording & status update | High |
| HMS-24 | Implement Overdue auto-flag + reminder job | Medium |
| HMS-25 | Fee status report (per student / per block, downloadable) | High |
| HMS-26 | Build Admin user-management UI | Medium |
| HMS-27 | Build student profile UI | Medium |

## Test Checklist

### Authentication (HMS-1)
- [ ] Valid email/username + password logs in successfully
- [ ] Invalid credentials show a generic error (no field-specific hints)
- [ ] 5 consecutive failed attempts locks the account for the cooling-off period
- [ ] Successful login issues a signed JWT
- [ ] Role determines visible menus/actions (Student / Warden / Administrator)
- [ ] Every login attempt (success/failure) is logged with a timestamp
- [ ] Passwords stored as salted hashes; all traffic over HTTPS
- [ ] RBAC enforced on both frontend routes and backend endpoints

### Student Profiles & Fees (HMS-2)
- [ ] Admin can create, view, update, deactivate a user account
- [ ] Deactivated users cannot log in
- [ ] Student can view/update only permitted profile fields
- [ ] Duplicate email or registration number is rejected server-side
- [ ] Guardian/emergency contact info can be recorded and edited
- [ ] Admin can generate a fee invoice with amount and due date
- [ ] Recording a payment updates fee status (Unpaid / Paid / Overdue)
- [ ] Unpaid fees auto-flip to Overdue after due date and trigger a reminder
- [ ] Fee status report is downloadable, filterable by student or block

## Test Types Covered

- Unit tests — auth service, profile & fee module (CI-gated)
- Integration tests — login → protected route access by role
- Security tests — students cannot access/edit other students' data or restricted fields

## Definition of Done Reminders

- All acceptance criteria demoed and accepted by the product owner
- Unit + integration tests passing in CI
- Actions visible in the audit log
- Code reviewed and merged

## Reporting Bugs

Log defects against the relevant HMS-xx task ID in JIRA with reproduction steps, expected vs. actual result, and environment details.
