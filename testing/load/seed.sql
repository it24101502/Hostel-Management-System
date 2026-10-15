-- =========================================================
-- SE3112 Load testing: test-only accounts
-- Owner: Prashasthi (Load & Performance)
--
-- The migrations create the four roles but no users, so the
-- load test needs its own accounts. This script is safe to run
-- more than once (it updates the rows if they already exist).
--
-- Password for BOTH accounts: LoadTest@2026
-- The hash below is a BCrypt hash (work factor 11, the same
-- default BCrypt.Net uses in AdminUserService). It is a
-- TEST-ONLY password: never reuse it on the Azure VM.
-- =========================================================

USE Hostel_Management_System;

INSERT INTO users
(
    role_id,
    username, normalized_username,
    email, normalized_email,
    password_hash,
    first_name, last_name,
    is_active, is_email_verified
)
SELECT
    r.role_id,
    'loadtest_student', 'LOADTEST_STUDENT',
    'loadtest.student@hms.test', 'LOADTEST.STUDENT@HMS.TEST',
    '$2a$11$JpGVEjPv/jgb7GCTmAXceOXvHq7QSrOI0uWbogqoHYYOUOjAl8fa.',
    'Load', 'Student',
    TRUE, TRUE
FROM roles AS r
WHERE r.role_name = 'STUDENT'
ON DUPLICATE KEY UPDATE
    password_hash         = VALUES(password_hash),
    is_active             = TRUE,
    failed_login_attempts = 0,
    is_locked             = FALSE,
    lockout_end_at        = NULL;

INSERT INTO users
(
    role_id,
    username, normalized_username,
    email, normalized_email,
    password_hash,
    first_name, last_name,
    is_active, is_email_verified
)
SELECT
    r.role_id,
    'loadtest_admin', 'LOADTEST_ADMIN',
    'loadtest.admin@hms.test', 'LOADTEST.ADMIN@HMS.TEST',
    '$2a$11$JpGVEjPv/jgb7GCTmAXceOXvHq7QSrOI0uWbogqoHYYOUOjAl8fa.',
    'Load', 'Admin',
    TRUE, TRUE
FROM roles AS r
WHERE r.role_name = 'ADMIN'
ON DUPLICATE KEY UPDATE
    password_hash         = VALUES(password_hash),
    is_active             = TRUE,
    failed_login_attempts = 0,
    is_locked             = FALSE,
    lockout_end_at        = NULL;

-- Show what was created so the run log proves the seed worked.
SELECT u.user_id, u.username, r.role_name, u.is_active, u.is_locked
FROM users AS u
INNER JOIN roles AS r ON r.role_id = u.role_id
WHERE u.username IN ('loadtest_student', 'loadtest_admin');
