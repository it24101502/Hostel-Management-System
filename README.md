Hostel Management System - Deployment

This branch contains the deployment-ready configuration for the Hostel Management System. It should receive only reviewed changes that have passed the required CI checks on the main branch.

Deployment Architecture

The application uses:

React.js frontend

ASP.NET Web API microservices

An API gateway

One isolated MySQL schema per microservice

Apache Kafka for asynchronous domain events

Docker containers

GitHub Actions for CI/CD

Azure App Service or another Docker-capable host

Deployment Flow

flowchart TD
    PR[Reviewed code on main] --> CI[Build and automated tests]
    CI --> IMG[Build container images]
    IMG --> REG[Push images to registry]
    REG --> DEP[Deploy from deploy branch]
    DEP --> HC[Health checks]
    HC --> LIVE[Release available]

Deployment Prerequisites

Docker Engine with Docker Compose support

Access to the chosen container registry

Access to the Azure or Docker hosting environment

Production MySQL databases or schemas

A reachable Kafka cluster

HTTPS certificates and domain configuration

GitHub repository secrets configured for the workflow

Required Configuration

Use deployment environment variables or the hosting platform's secret store. Variable names may differ between services, but the deployment normally requires values equivalent to:

ASPNETCORE_ENVIRONMENT=Production

MYSQL_HOST=<production-mysql-host>
MYSQL_PORT=3306
MYSQL_DATABASE=<service-specific-database>
MYSQL_USER=<production-user>
MYSQL_PASSWORD=<production-password>

JWT_SECRET=<strong-production-secret>
JWT_ISSUER=<token-issuer>
JWT_AUDIENCE=<token-audience>

KAFKA_BOOTSTRAP_SERVERS=<kafka-host:port>

FRONTEND_API_BASE_URL=<public-api-gateway-url>

Never store real secrets in this README, source files, Docker images, or committed .env files.

Recommended GitHub Secrets

Configure only the secrets needed by your actual workflow. Common examples are:

Secret

Purpose

REGISTRY_USERNAME

Container registry account

REGISTRY_PASSWORD

Container registry token or password

AZURE_CREDENTIALS

Azure deployment authentication, if used

AZURE_APP_NAME

Target application name, if required by the workflow

PRODUCTION_DB_PASSWORD

Production database credential

JWT_SECRET

JWT signing secret

Prefer workload identity or short-lived credentials where supported.

Local Deployment Check

Before promoting a release, validate the deployment configuration locally:

docker compose config
docker compose build
docker compose up -d
docker compose ps

Inspect the logs when a container is unhealthy:

docker compose logs <service-name>

Stop the local environment after validation:

docker compose down

Replace <service-name> with the service defined in your Compose file.

Production Deployment

Confirm all required pull requests are merged into main.

Confirm the CI workflow has passed all build and test jobs.

Create or update a release version tag.

Merge or promote the approved main commit into deploy.

Allow the deployment workflow to build immutable container images.

Deploy the images using the version tag or commit SHA, not latest alone.

Apply database changes using the team's approved migration scripts.

Run service health checks and a short smoke test.

Record the deployed version and deployment result.

Health Checks

After deployment, verify:

The frontend loads over HTTPS.

The API gateway responds successfully.

Authentication issues a valid JWT for correct credentials.

Role-based authorization blocks unauthorized requests.

Each microservice reports a healthy status.

Each service can access its own MySQL schema.

Kafka producers and consumers can exchange events.

A student can view active notices and schedules.

Logs and metrics are available to the configured monitoring platform.

If health endpoints are implemented, test each one using its real deployed URL:

curl --fail https://<host>/<health-endpoint>

Smoke Test Checklist

Student login works

Administrator login works

Warden/Master login works

Student can view their profile

Room occupancy data loads

Leave request submission works

Warden can approve or reject a leave request

Complaint submission and status update work

Fee status loads correctly

Notices and schedules are visible

No secrets appear in browser responses or logs

Monitoring

Key services should expose logs and metrics to a monitoring platform such as Azure Application Insights or Prometheus/Grafana. Monitor at least:

HTTP error rate and response time

Service and container health

Authentication failures and account lockouts

MySQL connection failures

Kafka producer/consumer failures and lag

CPU, memory, and storage usage

Overdue leave and fee notification failures

Rollback

If the deployment fails:

Stop the release workflow or prevent further traffic from reaching the failed version.

Redeploy the last known healthy image tag or commit SHA.

Restore database data only through an approved and tested recovery procedure.

Confirm health checks and critical smoke tests.

Record the incident, cause, affected version, and corrective action.

Avoid deleting production data or running destructive database commands during rollback unless the recovery plan explicitly requires and authorizes them.

Branch Protection Recommendations

Protect the deploy branch with:

Pull requests required before merging

At least one reviewer approval

Required CI status checks

Blocked force pushes

Blocked direct deletion

Restricted deployment permissions

Environment approval for production, if supported

Release Record

Record each production release using a GitHub Release or deployment log:

Field

Example

Version

v1.0.0

Source commit

Full commit SHA

Deployment date

ISO date and time

Environment

Staging or Production

Deployed by

Team member or workflow

Result

Successful, Rolled Back, or Failed

Important Rule

The deploy branch must represent a reproducible deployment state. Do not commit generated secrets, local-only configuration, debug files, or unreviewed source changes to this branch.
