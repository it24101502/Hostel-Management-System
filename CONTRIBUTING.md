# Contributing to HMS

This document describes how to branch, commit, and open pull requests on this repository. It applies to all four services (IdentityService, AccommodationService, frontend, database migrations).

## 1. Before you start

- Make sure you can build and run the solution locally (see the root `README.md` for the tech stack, and `compose.yml` to bring up MySQL, Kafka, and both backend services).
- Pick up a Jira ticket (an `HMS-##` ID) before you start work. Every branch, commit, and PR should trace back to one.
- If your change touches the database, add a new numbered migration file under `database/migrations/` or `database/accommodation/migrations/` rather than editing an existing one.

## 2. Branching

- `main` is stable, release-ready code. Never commit directly to it.
- `deploy` holds deployment configuration and the pipeline for staging/production. Only sprint QA branches merge into it once verified.
- `Sprint-N-QA-Testing` (e.g. `Sprint-3-QA-Testing`) is the integration branch for that sprint's QA verification. CI runs on every push and PR against it.
- All feature work happens on a `feature/HMS-<id>-<short-description>` branch, cut from the current sprint's QA branch:

  ```
  git checkout Sprint-3-QA-Testing
  git pull
  git checkout -b feature/HMS-40-leave-request-submission-api
  ```

- Keep branches scoped to one Jira ticket. If a task grows into two unrelated changes, split it into two branches/PRs.
- Delete your feature branch after it's merged.

## 3. Commits

- Write commits in the imperative mood, one logical change per commit: `Add leave request submission endpoint`, not `added stuff`.
- Prefix the subject line with the Jira ID when practical: `HMS-40: add leave request submission endpoint`.
- Keep commits small enough to review — a schema migration, a repository change, a service change, and a test can be separate commits even within the same PR.
- Never commit secrets, connection strings, or `.env` files (see `.gitignore` / `.env.example`). Use `.env.example` as the template for any new required variable.
- Run the relevant tests locally before committing:

  ```
  dotnet test backend/HostelManagement.sln
  ```

## 4. Pull requests

- Open the PR against the current sprint's QA branch (e.g. `Sprint-3-QA-Testing`), not against `main` or `deploy`.
- Title the PR with the Jira ID and a short summary: `HMS-46: Approve/reject leave with mandatory decision reason`.
- In the description, include:
  - What changed and why (link the Jira ticket).
  - Which acceptance criteria from the ticket are covered.
  - Any new environment variables, migrations, or manual test steps.
- A PR must have passing CI (`Backend CI` workflow: build, unit/integration tests, frontend build, Docker image builds) before it can be merged.
- At least one other team member reviews and approves before merging. Use "Squash and merge" to keep the QA branch history readable.
- If your change adds or modifies a database migration, note the migration file name in the PR description so reviewers can check it runs cleanly on a fresh database.

## 5. Sprint boundary handover

Per the role-rotation plan in the initial presentation, roles (BA / Developer / QA / DevOps) rotate every sprint. At each sprint boundary:

- Merge the outgoing `Sprint-N-QA-Testing` branch into `deploy` once all Sprint N acceptance criteria pass CI.
- Cut `Sprint-(N+1)-QA-Testing` from `deploy`.
- Hand over open risks, in-progress branches, and next actions to the incoming role owner (see README's role-rotation table).

## 6. Code style

- Match the existing formatting in the file you're editing (this codebase favours one parameter/argument per line in C# for readability — follow that convention in new code).
- Add or update unit tests alongside any service or repository change; integration/E2E/load tests belong in `tests/` and are wired into the CI workflow under `.github/workflows/`.
