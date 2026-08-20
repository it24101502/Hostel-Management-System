HMS-1: Secure Login and Role-Based Access

Branch: feature/HMS-1-secure-login-role-based-access

This branch implements secure authentication and role-based authorization for the Hostel Management System. Registered Students, Wardens/Masters, and Administrators can log in and access only the features permitted for their roles.

User Story

As a registered user (Student, Warden, or Administrator), I want to log in securely with my credentials and be granted access only to the features permitted for my role, so that hostel data stays protected and each user only sees what they are authorized to see.

Feature Scope

Authenticate users using an email address or username and password.

Return a generic error for invalid credentials.

Lock an account after five consecutive failed login attempts.

Unlock the account after the configured cooling-off period.

Issue a signed JWT after successful authentication.

Include the authenticated user's identity and role in the security context.

Restrict frontend routes, menus, and actions according to the user's role.

Enforce role restrictions again on protected backend endpoints.

Record every successful and failed login attempt with a timestamp.

Supported Roles

Role

Typical Access

Student

Own profile, room and fee status, leave requests, complaints, notices, and schedules

Warden / Master

Leave approval, movement tracking, schedules, and complaint monitoring

Administrator

User, room, allocation, fee, notice, and reporting administration

Frontend role checks improve the user experience, but they are not a security boundary. Every protected backend endpoint must independently validate the JWT and required role.

Acceptance Criteria

A user can log in using a valid email/username and password.

Invalid credentials produce a clear generic error without revealing which field was incorrect.

Five consecutive failed attempts lock the account for a defined cooling-off period.

A successful login issues a signed JWT.

The user's role determines the visible menus and permitted actions.

Every login attempt is recorded with a timestamp and success/failure result.

Expected Login Flow

flowchart TD
    A[Submit credentials] --> B{Account locked?}
    B -- Yes --> C[Return generic lockout response]
    B -- No --> D{Credentials valid?}
    D -- No --> E[Record failed attempt]
    E --> F{Five failures?}
    F -- Yes --> G[Apply temporary lockout]
    F -- No --> H[Return generic login error]
    D -- Yes --> I[Reset failed-attempt count]
    I --> J[Issue signed JWT]
    J --> K[Record successful attempt]
    K --> L[Open role-authorized interface]

API Behaviour

The authentication API should accept a request equivalent to:

{
  "identifier": "student@example.com",
  "password": "user-supplied-password"
}

A successful response should provide the signed token and only the user information required by the client:

{
  "accessToken": "<signed-jwt>",
  "expiresAt": "<UTC-timestamp>",
  "user": {
    "id": "<user-id>",
    "displayName": "<display-name>",
    "role": "Student"
  }
}

An invalid username, email, or password must return the same generic message, for example:

{
  "message": "Invalid username/email or password."
}

Do not return password hashes, internal lockout counters, signing keys, stack traces, or information confirming whether an account exists.

Suggested HTTP Status Codes

Situation

Status

Successful login

200 OK

Invalid or missing request data

400 Bad Request

Invalid credentials

401 Unauthorized

Valid user without the required role

403 Forbidden

Temporarily locked account

423 Locked or the team's documented alternative

Unexpected server failure

500 Internal Server Error with no sensitive details

Use one consistent lockout status and response contract across the UI, API, tests, and documentation.

JWT Requirements

Sign tokens using a strong secret or asymmetric private key stored outside source control.

Validate the signature, issuer, audience, and expiry on every protected request.

Include only necessary claims, such as user ID, username, and role.

Use a short, documented token lifetime.

Never place passwords, password hashes, or sensitive profile data inside the token.

Serve authentication and protected endpoints only over HTTPS outside local development.

Password and Lockout Security

Store passwords using a modern salted password-hashing algorithm.

Never store or log plaintext passwords.

Compare password hashes using the framework's secure password verification mechanism.

Increment the failed-attempt count only for rejected authentication attempts against an identified account.

Set a lockout expiry after the fifth consecutive failed attempt.

Reset the failed-attempt count after a successful login.

Compare lockout timestamps in UTC.

Keep the cooling-off duration configurable rather than hard-coded.

Example configuration names:

JWT_SECRET=<strong-signing-secret>
JWT_ISSUER=<expected-issuer>
JWT_AUDIENCE=<expected-audience>
JWT_EXPIRY_MINUTES=<token-lifetime>
AUTH_MAX_FAILED_ATTEMPTS=5
AUTH_LOCKOUT_MINUTES=<cooling-off-period>

Real secrets must be stored in environment variables, GitHub Secrets, or the deployment platform's secret store.

Role-Based Access Control

Authorization must be enforced at both application layers:

Frontend

Read the authenticated role from a trusted authentication state.

Protect role-specific routes.

Hide menus and actions the current role cannot use.

Clear authentication state during logout or token expiry.

Redirect unauthenticated users to the login page.

Display an access-denied page for authenticated but unauthorized users.

Backend

Require a valid JWT on protected endpoints.

Apply role policies to controllers or endpoints.

Return 401 when authentication is missing or invalid.

Return 403 when the user is authenticated but lacks permission.

Never rely only on hidden frontend buttons for protection.

Audit Logging

Each authentication log entry should contain:

Field

Description

Timestamp

UTC date and time of the attempt

User reference

User ID when safely resolvable

Submitted identifier

Masked or normalized value according to the logging policy

Result

Success, failure, or locked

Source

Appropriate request/IP metadata when allowed

Correlation ID

Identifier used to trace the request across services

Passwords, JWTs, secrets, and full sensitive request payloads must never be written to logs.

Test Coverage

Unit Tests

Valid password verification succeeds.

Invalid password verification fails.

Failed-attempt counter increments correctly.

The fifth consecutive failure applies lockout.

A successful login resets the failed-attempt counter.

Lockout expiry is evaluated correctly.

JWT contains the expected role and identity claims.

Expired or incorrectly signed tokens are rejected.

Integration Tests

Valid email and password return a signed JWT.

Valid username and password return a signed JWT.

Unknown user and incorrect password return the same generic error.

Five consecutive failures lock the account.

A locked account cannot log in before the cooling-off period ends.

A role-authorized endpoint is accessible.

A role-restricted endpoint returns 403 Forbidden.

Success and failure attempts appear in the audit log.

UI Tests

Login form validates required fields.

Invalid credentials display the generic error.

Student, Warden/Master, and Administrator menus differ correctly.

Protected routes redirect unauthenticated users.

Unauthorized routes show an access-denied result.

Logout removes the active authentication state.

Definition of Done

Login API and UI are implemented and code-reviewed.

Passwords are stored as salted hashes.

Deployed traffic uses HTTPS.

Role-based access is enforced on frontend routes and backend endpoints.

Valid login, invalid login, lockout, and role-restriction tests pass in CI.

Authentication attempts are visible in the audit log.

The feature is demonstrated and accepted by the product owner.

Running and Testing

Use the commands configured by the repository. Typical commands are:

dotnet restore
dotnet build
dotnet test

For the React application:

npm install
npm run dev
npm test

Run the repository's actual scripts if their names or locations differ.

Branch Workflow

git checkout main
git pull origin main
git checkout -b feature/HMS-1-secure-login-role-based-access

Before opening the pull request:

git status
git add .
git commit -m "feat(auth): implement secure login and role-based access"
git push -u origin feature/HMS-1-secure-login-role-based-access

Open a pull request into main and merge only after review and all required CI checks pass.

Related Requirements

This branch addresses SRS requirements FR-01 through FR-05 and contributes to the security, auditability, reliability, and maintainability requirements of the Hostel Management System.
