HMS-2: Register and Maintain Student Profiles and Fees

Branch: feature/HMS-2-register-and-maintain-student-profiles

This branch implements administration of student and staff accounts, permitted student self-service profile updates, and the student fee lifecycle for the Hostel Management System.

User Story

As an Administrator, I want to create, view, update and deactivate student and staff accounts, and manage each student's fee records, so that user information and financial status are accurate and centrally maintained.

Feature Scope

Create, view, update, and deactivate Student and Warden/Master accounts.

Allow students to view their own profile.

Allow students to update only permitted profile fields.

Enforce unique email addresses.

Enforce unique student registration numbers.

Store and edit guardian or emergency contact information.

Generate fee invoices with an amount and due date.

Record fee payments.

Maintain Unpaid, Paid, and Overdue fee states.

Automatically mark unpaid invoices as overdue after their due dates.

Trigger reminders for newly overdue fees.

Produce a downloadable fee report per student or hostel block.

Record profile and fee changes in the audit log.

Authorization Matrix

Action

Student

Warden / Master

Administrator

View own permitted profile

Yes

As defined by policy

Yes

Update own permitted profile fields

Yes

As defined by policy

Yes

View another user's complete profile

No

Only if explicitly authorized

Yes

Create or deactivate accounts

No

No

Yes

Manage guardian/emergency contacts

Own permitted data only

Only if explicitly authorized

Yes

Generate fee invoices

No

No

Yes

Record fee payments

No

No

Yes

Download administrative fee reports

No

No

Yes

The backend must enforce these permissions even if the frontend hides restricted controls.

Acceptance Criteria

An Administrator can create, view, update, and deactivate a user account.

A student can view and update only the permitted fields of their own profile.

Email addresses are unique during creation and update.

Student registration numbers are unique during creation and update.

Guardian or emergency contact information can be recorded and edited.

An Administrator can generate a fee invoice with an amount and due date.

Payments can be recorded and fee status becomes Unpaid, Paid, or Overdue as appropriate.

Unpaid fees become overdue after the due date and trigger a reminder.

A downloadable fee report can be generated per student or hostel block.

Account Lifecycle

Accounts should be deactivated rather than permanently deleted when historical fee, audit, allocation, complaint, or leave records depend on them.

stateDiagram-v2
    [*] --> Active: Account created
    Active --> Active: Profile updated
    Active --> Inactive: Administrator deactivates
    Inactive --> Active: Administrator reactivates

Deactivated users must not be able to authenticate unless the product owner approves a different documented policy.

Fee Lifecycle

stateDiagram-v2
    [*] --> Unpaid: Invoice generated
    Unpaid --> Paid: Valid payment recorded
    Unpaid --> Overdue: Due date passes
    Overdue --> Paid: Valid payment recorded

Recommended status rules:

A new invoice starts as Unpaid.

An invoice becomes Paid only after the required payment is successfully recorded.

An unpaid invoice becomes Overdue when its due date has passed.

The overdue job should be safe to run more than once without creating duplicate state changes or reminders.

All monetary calculations should use a fixed-precision decimal type, not floating-point arithmetic.

Dates and scheduled jobs should follow one documented timezone policy.

Data Validation

User Profiles

Email is required, normalized, and unique.

Student registration number is required for students and unique.

Role must be one of the supported system roles.

Required names and contact fields cannot be blank.

Account status must use an allowed value.

Students can update only fields explicitly allowed by the profile policy.

Administrative fields such as role, registration number, and account status must not be writable through student self-service requests unless explicitly approved.

Guardian or Emergency Contact

Store the contact's name, relationship, and at least one valid contact method.

Validate phone and email formats where supplied.

Do not expose another student's contact information to unauthorized users.

Fee Records

Amount must be greater than zero.

Due date is required and must follow the agreed business rule.

Student account must exist and be eligible for invoicing.

Payment amount and payment date must be valid.

Duplicate payment submissions should be prevented or safely handled.

Status changes must be calculated on the server.

All important rules must be enforced server-side even when the React UI performs the same validation.

Suggested API Coverage

Adjust routes to match the project's API conventions.

Method

Example Route

Purpose

Access

POST

/api/users

Create a user account

Administrator

GET

/api/users

Search or list users

Administrator

GET

/api/users/{id}

View a user account

Administrator or authorized owner

PUT/PATCH

/api/users/{id}

Update a user account

Administrator or permitted owner fields

PATCH

/api/users/{id}/status

Deactivate or reactivate an account

Administrator

GET

/api/profile/me

View the authenticated user's profile

Authenticated user

PATCH

/api/profile/me

Update permitted self-service fields

Authenticated user

POST

/api/fees/invoices

Generate an invoice

Administrator

GET

/api/fees/students/{studentId}

View a student's fee records

Authorized user

POST

/api/fees/{feeId}/payments

Record a payment

Administrator

GET

/api/fees/reports

Generate/download a filtered report

Administrator

Do not return password hashes, authentication secrets, internal security fields, or unrelated personal information in profile responses.

Uniqueness Enforcement

Email and student registration number uniqueness must be protected at two levels:

Validate through the service before saving to provide a clear user-facing error.

Add database unique constraints to prevent race conditions and duplicate records.

Normalize values before comparison, and handle database constraint violations as safe conflict responses rather than unhandled server errors.

Suggested responses:

Situation

Status

Account or invoice created

201 Created

Successful read or update

200 OK

Invalid input

400 Bad Request

Missing/invalid authentication

401 Unauthorized

Insufficient role or ownership

403 Forbidden

Record not found

404 Not Found

Duplicate email/registration number

409 Conflict

Overdue Automation and Reminders

The scheduled process should:

Run at a documented interval.

Find invoices that remain Unpaid after their due date.

Update eligible invoices to Overdue in a safe transaction.

Publish or queue a reminder notification.

Record the status change and reminder event.

Avoid sending duplicate reminders for the same transition.

Log failures without exposing personal or financial details.

If Kafka is used for reminders, publish a documented domain event and ensure the consumer is retry-safe.

Fee Report Requirements

The report should support:

Filtering by individual student.

Filtering by hostel block.

Displaying invoice amount, due date, payment information, and current status.

A downloadable format approved by the team, such as CSV or PDF.

Clear generation date and applied filters.

Authorization checks before data is queried or returned.

Correct file content type and filename in the download response.

Audit Logging

Audit at least the following operations:

User account creation.

Profile updates, including the changed field names without sensitive values.

Account deactivation and reactivation.

Guardian/emergency contact updates.

Invoice creation.

Payment recording.

Automatic overdue status changes.

Fee reminder generation.

Fee report generation or download when required by policy.

Each entry should include the acting user or system process, action, target record, UTC timestamp, result, and correlation ID. Do not log plaintext passwords, tokens, full payment secrets, or unnecessary personal information.

Test Coverage

User and Profile Unit Tests

Creating a valid account succeeds.

Duplicate normalized email is rejected.

Duplicate student registration number is rejected.

Updating to another user's email/registration number is rejected.

Updating a record without changing its own unique values succeeds.

Student self-service updates accept permitted fields.

Student self-service updates reject administrative fields.

Deactivation changes account status without deleting history.

Fee Unit Tests

A valid invoice starts as Unpaid.

Zero or negative invoice amounts are rejected.

A valid payment changes the status correctly.

An unpaid invoice past its due date becomes Overdue.

A paid invoice is not marked overdue.

Re-running the overdue job does not duplicate transitions or reminders.

Report filters return only the requested student or hostel block.

Integration Tests

Administrator can complete account CRUD/deactivation operations.

Student can read their own profile.

Student cannot read or update another student's restricted data.

Database constraints prevent duplicate email and registration numbers.

Administrator can generate an invoice and record a payment.

Scheduled processing updates overdue invoices and triggers reminders.

Fee report downloads with correct filters and content type.

Profile and fee operations appear in the audit log.

Unauthorized roles receive 403 Forbidden on restricted endpoints.

UI Tests

Administrator can create and edit a user through the interface.

Deactivation requires a clear confirmation.

Duplicate values display useful validation messages.

Student sees only permitted profile fields as editable.

Administrator can generate an invoice and record payment.

Fee filters and report download work correctly.

Definition of Done

User profile and fee CRUD endpoints and UI are implemented.

Uniqueness validation is enforced on the server and database.

The overdue-status automation and reminder job are implemented and tested.

Fee reports generate correctly and are downloadable.

Profile CRUD and fee lifecycle unit/integration tests pass in CI.

Profile and fee changes appear in the audit log.

The implementation is reviewed, merged, and deployed to the test environment.

Running and Testing

Use the commands configured by the repository. Typical backend commands are:

dotnet restore
dotnet build
dotnet test

For the React application:

npm install
npm run dev
npm test

Run the repository's actual database, Kafka, background-job, and reporting dependencies before integration testing.

Branch Workflow

git checkout main
git pull origin main
git checkout -b feature/HMS-2-register-and-maintain-student-profiles

Before opening the pull request:

git status
git add .
git commit -m "feat(users-fees): implement profile and fee management"
git push -u origin feature/HMS-2-register-and-maintain-student-profiles

Open a pull request into main and merge only after review and all required CI checks pass.

Related Requirements

This branch addresses SRS requirements FR-06 through FR-09 and FR-27 through FR-30. It also contributes to server-side validation, auditability, security, reliability, and automated test requirements.
