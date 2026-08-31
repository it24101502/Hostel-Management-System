# `deploy` Branch — Hostel Management System

This branch contains QA-approved, deployment-ready releases of the Hostel Management System. Feature development must not be performed directly on this branch.

## Sprint 1 Release

Sprint 1 provides the foundation for Identity, Users, Student Profiles, and Fees.

### Included Features

* Secure login with JWT authentication
* Role-based access control
* Account lockout after repeated failed login attempts
* Login audit logging
* Admin user management
* Student profile management
* Guardian and emergency contact management
* Fee invoices, payments, overdue detection, reminders, and reports
* React frontend
* ASP.NET backend
* MySQL database migrations

## Container Architecture

The Sprint 1 system uses Docker Compose with three services:

| Service      | Technology      | Local Port |
| ------------ | --------------- | ---------- |
| Frontend     | React and Nginx | 5173       |
| Identity API | ASP.NET 10      | 8080       |
| Database     | MySQL 8.4       | 3307       |

## Local Deployment

Create the local environment file:

```powershell
Copy-Item .env.example .env
```

Replace the example values in `.env` with secure local values. Never commit the real `.env` file.

Build and start the system:

```powershell
docker compose up --detach --build
docker compose ps
```

Open the frontend at:

```text
http://localhost:5173
```

Stop the system without deleting database data:

```powershell
docker compose down
```

## CI Pipeline

GitHub Actions currently performs:

1. MySQL startup and migration validation
2. .NET dependency restoration
3. Backend Release build
4. Backend unit and integration tests
5. Frontend dependency installation
6. Frontend production build
7. Docker Compose configuration validation
8. Backend Docker image build
9. Frontend Docker image build

Automatic container-registry publishing and Azure deployment are planned deployment-stage tasks and are not yet enabled.

## QA Verification

Sprint 1 has been approved by QA with evidence covering:

* Docker infrastructure
* Unit tests and code coverage
* Selenium login testing
* JMeter tests with 20 and 50 users

Evidence is available in:

```text
qa-sprint1-evidence/
```

GitHub Actions also passed on the QA-approved commit.

## Deployment Status

* [x] HMS-1 and HMS-2 integrated
* [x] Backend tests passing
* [x] Frontend production build passing
* [x] Database migrations verified
* [x] Docker Compose environment verified
* [x] QA evidence uploaded
* [x] QA approval received
* [ ] Cloud staging resources configured
* [ ] Deployment secrets configured in GitHub
* [ ] HTTPS and public health monitoring configured
* [ ] Docker images published to a container registry

## Security

* Secrets must be provided through environment variables.
* The real `.env` file must never be committed.
* Production JWT and database credentials must be stored using GitHub or cloud-platform secrets.
* HTTPS/TLS must be enabled in the staging and production environments.

## Rollback

Until automated cloud deployment is configured, rollback is performed by redeploying the previous verified Git commit or Docker image tag.
