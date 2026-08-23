# `deploy` Branch — Hostel Management System

This branch holds deployment configuration and pipeline definitions for the HMS microservices. It is not meant for feature development — merge stable, tested code here from `main` when preparing a release.

## Purpose

- Docker build/compose definitions for each microservice (Auth, Rooms, Leave & Movement, Complaints, Fees, Notices)
- Azure deployment configuration
- GitHub Actions workflows for CI/CD (build, test, deploy)
- Environment configuration templates (`.env.example`) for staging/production

## Architecture Notes

The system follows a microservice architecture per NFR-04: each service is independently deployable, so a failure in one service (e.g., Notices) should not cause a full-system outage. Services communicate over HTTPS/TLS (NFR-02).

| Service | Responsibility |
| --- | --- |
| Auth Service | Login, JWT issuance/validation, role-based access, audit logging |
| User/Fee Service | Student & staff profiles, guardian contacts, fee invoices & payments |
| Room Service | Room CRUD, allocation, transfer, occupancy status |
| Leave & Movement Service | Leave requests, approvals, departure/return tracking |
| Complaint Service | Complaint submission, triage, status tracking |
| Notice Service | Notices & schedules, block filtering, auto-archive |

## Deployment Targets

- **Container platform:** Docker
- **Cloud:** Azure
- **Database:** MySQL (managed instance or containerized, per environment)

## CI/CD Pipeline (GitHub Actions)

The pipeline has been functional since Sprint 1 and runs on every PR and merge:

1. Restore dependencies / build each service
2. Run unit and integration tests
3. Run Selenium end-to-end tests (leave lifecycle, etc.)
4. Run JMeter load tests against critical endpoints (e.g. leave submission, ≤ 3s target per NFR-01)
5. Build and push Docker images
6. Deploy to the target environment (staging on merge to `deploy`, production on tagged release)

## Before Deploying

- [ ] All Sprint deliverables merged and passing CI on `main`
- [ ] Security tests passed (auth, RBAC, profile/fee access control)
- [ ] Environment variables and secrets configured (not committed)
- [ ] Database migrations verified against a fresh schema
- [ ] Monitoring/logging endpoints wired up (NFR-10 — logs/metrics for tracing)

## Rollback

Each microservice can be rolled back independently by redeploying its previous container image/tag, without affecting the other services.
