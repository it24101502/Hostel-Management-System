# Hostel Management System (HMS)

**SE3022 – Case Study Project** | Year 3, Semester 1, 2026

A microservice-based web platform that connects students, wardens and administrators through one reliable workflow for authentication, room allocation, leave & movement, complaints, fees, and notices.

## Team

| Student ID | Name | Primary Module |
| --- | --- | --- |
| IT24101502 | Suwasthikka S | Daily operations (complaints / schedules) |
| IT24100245 | Peiris M P V P | Leave & movement |
| IT24102190 | De Silva D S P S N | Rooms & allocation |
| IT24101844 | Premarathna P A I B | Users & fees |

## The Problem

Manual hostel processes create avoidable risk: leave permission is hard to track, wardens can't see movements in real time, room/fee/complaint records are fragmented, and timetables and notices are easy to miss. One missing record can affect student safety.

## Proposed Solution

One platform connecting five core areas:

1. **Student & access** — profiles, roles and secure login
2. **Rooms** — room availability and allocation
3. **Leave & movement** — requests, approval, departure and return
4. **Daily operations** — schedules, complaints, fees and notices
5. **Reports** — live occupancy, leave, fee and complaint reports

## Roles & Scope

| Role | Capabilities |
| --- | --- |
| Student | Request leave, view room & fees, submit complaints, see schedules/notices |
| Warden / Master | Approve leave, record movements, manage schedules, monitor complaints |
| Administrator | Manage users, rooms, fees and notices; generate reports |

**In first release:** Authentication, Rooms, Leave, Movement, Timetables, Complaints, Fees, Notices, Reports
**Explicitly out of scope:** Biometric hardware, GPS tracking, native mobile apps, direct banking integration

## Tech Stack

- **Frontend:** React.js
- **Backend:** ASP.NET + ADO.NET (microservices)
- **Database:** MySQL
- **Infrastructure:** Docker, Azure
- **Source control / CI-CD:** GitHub, GitHub Actions (functional from Sprint 1)
- **Testing:** Unit & integration tests, Selenium (E2E), JMeter (load)

## Architecture

The system is built as independently deployable microservices (one per module — Auth, Rooms, Leave & Movement, Complaints, Fees, Notices) so that failure in one service does not cause a full-system outage (NFR-04).

## Non-Functional Highlights

- Requests complete within ≤ 3 seconds under expected load (NFR-01)
- Role-based access, salted password hashes, HTTPS/TLS everywhere (NFR-02)
- All state-changing operations recorded in an auditable activity log (NFR-03)
- 99% uptime target during the academic term (NFR-05)
- Server-side validation on all form input (NFR-09)

## Repository Branches

| Branch | Purpose |
| --- | --- |
| `main` | Stable, release-ready code |
| `deploy` | Deployment configuration and pipeline for staging/production |
| `Sprint-1-QA-Testing` | QA verification for Sprint 1 deliverables |
| `feature/HMS-1-...` | Feature branch — Secure login & role-based access |
| `feature/HMS-2-...` | Feature branch — Register & maintain student profiles |

## Sprint Plan

| Sprint | Theme |
| --- | --- |
| 1 | Foundation — Identity & Users |
| 2 | Room Management |
| 3 | Leave & Movement |
| 4 | Complaints, Fees, Notices & Reporting |

## Product Backlog (Must-priority core)

| ID | User Story | Priority | Owner |
| --- | --- | --- | --- |
| US01 | Secure login and role-based access | Must | Member 1 |
| US02 | Register and maintain student profiles | Must | Member 1 |
| US03 | Manage rooms, beds and capacity | Must | Member 2 |
| US04 | Allocate or transfer students | Must | Member 2 |
| US05 | Submit a complete leave request | Must | Member 3 |
| US06 | Approve or reject leave with a reason | Must | Member 3 |
| US07 | Submit and track complaints | Must | Member 4 |
| US08 | Publish schedules and notices | Should | Member 4 |

Full JIRA backlog: 16+ stories, acceptance criteria, priority, estimate, owner and sprint — see project documentation.

## AI Usage Disclosure

Per the assignment brief, direct use of AI to generate or complete project code is prohibited. AI tools were used only for research and planning support, with disclosure.
