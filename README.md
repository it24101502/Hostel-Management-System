# `feature/HMS-1-secure-login-role-based-access` — Hostel Management System

Feature branch for **HMS-1: Secure login and role-based access**.

- **Priority:** Must
- **Owner:** Member 1
- **Module:** Authentication
- **Related FRs:** FR-01–FR-05
- **User story:** US01
- **Sprint:** 1 (Foundation)

## Description

As a registered user (Student, Warden, or Administrator), I want to log in securely with my credentials and be granted access only to the features permitted for my role, so that hostel data stays protected and each user only sees what they are authorized to see.

## Acceptance Criteria

- [ ] A user can log in using a valid email/username and password
- [ ] An invalid email/username or password shows a clear error without revealing which field was wrong
- [ ] After 5 consecutive failed attempts, the account is locked for a defined cooling-off period
- [ ] On successful login, the system issues a signed session token (JWT)
- [ ] The logged-in user's role (Student, Warden, Administrator) determines which menus and actions are visible
- [ ] Every login attempt (success or failure) is logged with a timestamp

## Definition of Done

- [ ] Login API and UI implemented and code-reviewed
- [ ] Passwords stored as salted hashes; all traffic over HTTPS
- [ ] Role-based access control enforced on both frontend routes and backend endpoints
- [ ] Unit and integration tests covering valid login, invalid login, lockout, and role restriction pass in CI
- [ ] Authentication attempts visible in the audit log
- [ ] Feature demoed and accepted by the product owner

## Sub-tasks in this Branch

| ID | Task | Priority | Points |
| --- | --- | --- | --- |
| HMS-9 | Design auth DB schema (users, roles, sessions) — incl. guardian/emergency contact fields | High | 3 |
| HMS-10 | Implement login API (email/username + password) | Highest | 5 |
| HMS-11 | Implement role-based access middleware | Highest | 5 |
| HMS-12 | Implement account lockout after 5 failed attempts | Medium | 3 |
| HMS-13 | Implement JWT issuance and validation | Highest | 5 |
| HMS-14 | Implement authentication audit logging | Medium | 3 |
| HMS-15 | Build login UI (React) | High | 5 |
| — | Unit tests — auth service | Medium | 3 |
| — | Integration tests — login → protected route access by role | Medium | 3 |

## Implementation Notes

- **Stack:** ASP.NET + ADO.NET (Auth microservice), MySQL (users/roles/sessions schema), React (login UI)
- Login endpoint must return a **generic** error for bad credentials — do not indicate whether the email or password was wrong (FR-01)
- RBAC middleware must reject unauthorized role requests with `403` and must be applied to **all** protected routes, not just the frontend
- Lockout duration should be configurable, not hardcoded
- JWT validation must reject both expired and tampered tokens
- Every login attempt (success/failure) needs a persisted log entry with user ID, outcome, and timestamp, queryable by an admin

## Related NFRs

- NFR-02 (Security): role-based access, salted password hashes, HTTPS/TLS
- NFR-03 (Data Integrity/Audit): all state-changing operations logged
- NFR-09: server-side validation in addition to client-side

## Testing

- Unit tests: valid login, invalid login, lockout trigger, lockout expiry
- Integration tests: Student cannot access Admin-only endpoints; each role can access its permitted endpoints
