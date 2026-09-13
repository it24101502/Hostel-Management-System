## Sprint 2 DevOps QA Integration

This branch contains the DevOps CI updates prepared for the Sprint 2 QA environment.

### Purpose

- Run CI when code is pushed to the Sprint-2-QA-Testing branch.
- Run CI for pull requests targeting the Sprint-2-QA-Testing branch.
- Use MySQL 8.4 in CI to match the Docker Compose environment.
- Validate Sprint 2 changes before they are merged into the deploy branch.

This branch is based on Sprint-2-QA-Testing so that the DevOps changes can be reviewed without including unrelated commits from the main branch.