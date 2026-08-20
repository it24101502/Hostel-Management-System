Hostel Management System

A web-based Hostel Management System (HMS) developed for the SE3022 Case Study Project at SLIIT. The platform connects students, wardens/masters, and administrators through a single system for accommodation, leave, complaints, fees, notices, and reporting.


Project Overview

The system follows an event-driven microservice architecture. A React single-page application communicates with ASP.NET Web API services through an API gateway. Apache Kafka is used for asynchronous events, and each microservice owns an isolated MySQL database.


Main Features

Secure login using JWT authentication

Role-based access for Students, Wardens/Masters, and Administrators

Student profile and user account management

Room and bed management

Student room allocation and transfer

Leave request submission and approval

Student departure and return tracking

Complaint submission and resolution tracking

Hostel fee invoicing and payment status management

Notice and timetable publishing

Live, filterable, and downloadable reports

Audit logging for state-changing operations

User Roles & main Capabilities

Student - View and update permitted profile details, view room and fee status, request leave, submit complaints, and view notices and schedules

Warden / Master - Approve or reject leave, record student movement, manage schedules, and monitor complaints

Administrator - Manage users rooms, allocations, fees, notices, and system reports

Technology Stcks

Frontend - React.js

Backend - ASP.NET Web API

Data Access - ADO.NET with direct SQL

Database - MySQL

Messaging - Apache Kafka

Architecture - Event-driven microservices

Authentication - JWT and role-based access control

Containers - Docker / Docker Compose

CI/CD - GitHub Actions

Deployment - Azure App Service or a Docker-capable host

Testing - Unit, integration, Selenium end-to-end, and JMeter load tests


High-Level Architecture

flowchart TD
    UI[React Web Application] --> GW[API Gateway]
    GW --> ID[Identity Service]
    GW --> AC[Accommodation Service]
    GW --> LA[Leave and Activity Service]
    GW --> HO[Hostel Operations Service]
    GW --> NT[Notification Service]
    ID <--> KF[Apache Kafka]
    AC <--> KF
    LA <--> KF
    HO <--> KF
    NT <--> KF

Each service must own and access only its assigned MySQL schema.


Getting Started
Prerequisites

Install the following tools:

Git

Node.js and npm

.NET SDK used by the project

MySQL Server

Docker Desktop

Clone the Repository

git clone <your-repository-url>

cd <repository-folder>

Replace the placeholders with your actual repository details.

Configure Environment Variables

Create local environment files from the example files included in the repository. Typical configuration values include:

MYSQL_HOST=localhost

MYSQL_PORT=3306

MYSQL_DATABASE=<service_database>

MYSQL_USER=<database_user>

MYSQL_PASSWORD=<database_password>

JWT_SECRET=<strong_secret>

KAFKA_BOOTSTRAP_SERVERS=localhost:9092

Do not commit passwords, JWT secrets, connection strings, or production credentials.

Run with Docker Compose

If the repository contains a root Compose file, run: docker compose up --build

To stop the containers: docker compose down

Run Without Docker
Start the backend services from their individual project directories: dotnet restore , dotnet run

Start the React frontend from its directory: npm install , npm run dev

Update these paths and commands if your repository uses different directory or script names.

Testing

Run .NET tests: dotnet test

Run frontend tests using the test script configured in the frontend package:

npm test

Before merging, confirm that relevant unit, integration, end-to-end, and load tests pass.

Branching Strategy

main - Stable, reviewed source code used as the integration baseline

deploy - Deployment-ready configuration and release state

feature/<name> - Development of an individual feature or user story

bugfix/<name> - Isolated defect correction

Recommended workflow:

Create a feature branch from main.

Implement and test the assigned feature.

Push the branch and open a pull request into main.

Merge only after review and successful CI checks.

Promote an approved release from main to deploy.

Security Requirements

Store passwords using a salted cryptographic hash.

Use HTTPS/TLS in deployed environments.

Validate all input on the server, even when client-side validation exists.

Enforce authorization on every protected endpoint.

Keep secrets in environment variables or a secure secret store.

Record authentication attempts and state-changing operations in audit logs.

Performance and Quality Targets

Normal user-facing requests should complete within 3 seconds under expected load.

The system targets 99% uptime during the academic term, excluding scheduled maintenance.

Microservices should be independently deployable and scalable.

Automated tests must pass in CI before changes are merged.

The interface should support the latest two versions of Chrome, Edge, and Firefox.


Team Members

IT24101502 - Suwasthikka S

IT24100245 - Peiris M P V P

IT24102190 - De Silva D S P S N

IT24101844 - Premarathna P A I B


Academic Information

Module: SE3022 - Case Study Project

Programme: BSc (Hons) in Computer Science

Year: 3

Semester: 1

Institution: SLIIT

License

This repository is an academic project. Add a license only if the team and module guidelines allow public reuse.
