-- ============================================================
-- HMS 52: Manual complaints database tests
--
-- Record of previously used QA queries.
-- Historical complaint IDs: 4, 5, 6 and 7.
-- Fixed IDs must be checked before any re-execution.
-- Negative-test statements must be executed separately.
-- Do not execute this entire file in one operation.
--
-- Schema creation is in the original migration files:
-- 001_create_complaint_database.sql
-- 002_create_complaints.sql
-- This file contains verification and data-testing queries.
-- ============================================================


-- ============================================================
-- ENVIRONMENT VERIFICATION
-- ============================================================

SELECT VERSION(), @@port;

SHOW DATABASES;


-- ============================================================
-- HMS52-T01: Verify database and table existence
-- Successful migration execution output is separate evidence.
-- ============================================================

USE Hostel_Complaint_System;

SHOW TABLES;

SHOW DATABASES LIKE 'Hostel_Complaint_System';

SHOW TABLES FROM Hostel_Complaint_System LIKE 'complaints';


-- ============================================================
-- HMS52-T02: Verify required schema fields
-- ============================================================

SHOW COLUMNS FROM complaints;


-- ============================================================
-- HMS52-T03: Insert a valid complaint and check defaults
-- ============================================================

USE Hostel_Complaint_System;

START TRANSACTION;

INSERT INTO complaints (
    student_user_id,
    student_username,
    category,
    description
)
VALUES (
    900001,
    'qa_hms52',
    'PLUMBING',
    'HMS52 QA test: leaking tap'
);

SET @qa_complaint_id = LAST_INSERT_ID();

SELECT
    complaint_id,
    category,
    description,
    status,
    assigned_to_user_id,
    assigned_at,
    resolved_at
FROM complaints
WHERE complaint_id = @qa_complaint_id;


-- ============================================================
-- HMS52-T04: Assign a complaint and set IN_PROGRESS
-- Pasted SELECT typo categoryS corrected to category.
-- ============================================================

USE Hostel_Complaint_System;

UPDATE complaints
SET assigned_to_user_id = 900002,
    assigned_at = NOW(),
    status = 'IN_PROGRESS'
WHERE complaint_id = 4;

SELECT
    complaint_id,
    category,
    status,
    assigned_to_user_id,
    assigned_at,
    resolved_at
FROM complaints
WHERE complaint_id = 4;


-- ============================================================
-- HMS52-T05: Set a complaint to RESOLVED
-- ============================================================

UPDATE complaints
SET status = 'RESOLVED',
    resolved_at = NOW()
WHERE complaint_id = 4;

SELECT
    complaint_id,
    status,
    assigned_to_user_id,
    assigned_at,
    resolved_at
FROM complaints
WHERE complaint_id = 4;


-- ============================================================
-- HMS52-T06: Attempt to save an invalid status
-- Expected: Error 3819; original status remains.
-- ============================================================

UPDATE complaints
SET status = 'INVALID_STATUS'
WHERE complaint_id = 4;

SELECT complaint_id, status, resolved_at
FROM complaints
WHERE complaint_id = 4;


-- ============================================================
-- HMS52-T07: Attempt to save an invalid category
-- Expected: Error 3819; original category remains.
-- ============================================================

UPDATE complaints
SET category = 'INVALID_CATEGORY'
WHERE complaint_id = 4;

SELECT complaint_id, category, status
FROM complaints
WHERE complaint_id = 4;


-- ============================================================
-- HMS52-T08: Attempt to save a spaces-only description
-- Expected: Error 3819; original description remains.
-- ============================================================

UPDATE complaints
SET description = '   '
WHERE complaint_id = 4;

SELECT complaint_id, description
FROM complaints
WHERE complaint_id = 4;


-- ============================================================
-- HMS52-T09: Verify assignment consistency
-- Case: Assignee exists without assignment date.
-- Expected: Error 3819.
-- ============================================================

UPDATE complaints
SET assigned_at = NULL
WHERE complaint_id = 4;

SELECT complaint_id, assigned_to_user_id, assigned_at
FROM complaints
WHERE complaint_id = 4;


-- ============================================================
-- HMS52-T10: Verify resolution-date consistency
-- Case: RESOLVED complaint without resolution date.
-- Expected: Error 3819.
-- ============================================================

UPDATE complaints
SET resolved_at = NULL
WHERE complaint_id = 4;

SELECT complaint_id, status, resolved_at
FROM complaints
WHERE complaint_id = 4;


-- ============================================================
-- HMS52-T13: Verify transaction rollback
-- Positioned here to preserve the transaction sequence.
-- Expected: Zero rows after rollback.
-- ============================================================

ROLLBACK;

SELECT complaint_id, student_username, description, status
FROM complaints
WHERE complaint_id = 4;


-- ============================================================
-- HMS52-T14: Verify committed data retrieval
-- ============================================================

USE Hostel_Complaint_System;

START TRANSACTION;

INSERT INTO complaints (
    student_user_id,
    student_username,
    category,
    description
)
VALUES (
    900001,
    'qa_hms52_persist',
    'PLUMBING',
    'HMS52 persistence test'
);

SET @persist_id = LAST_INSERT_ID();

COMMIT;

SELECT complaint_id, student_username, category, description, status
FROM complaints
WHERE complaint_id = @persist_id;

SELECT complaint_id, student_username, category, description, status
FROM Hostel_Complaint_System.complaints
WHERE student_username = 'qa_hms52_persist'
  AND description = 'HMS52 persistence test';

SELECT complaint_id, student_username, category, description, status
FROM Hostel_Complaint_System.complaints
WHERE complaint_id = 5;


-- ============================================================
-- HMS52-T15: Clean up committed test data
-- ============================================================

START TRANSACTION;

DELETE FROM Hostel_Complaint_System.complaints
WHERE complaint_id = 5
  AND student_username = 'qa_hms52_persist'
  AND description = 'HMS52 persistence test';

COMMIT;

SELECT complaint_id
FROM Hostel_Complaint_System.complaints
WHERE complaint_id = 5;


-- ============================================================
-- CONFIGURATION VERIFICATION
-- ============================================================

USE Hostel_Complaint_System;

SELECT @@SESSION.sql_mode;


-- ============================================================
-- HMS52-T11: Description-length boundaries
-- First execution: complaint ID 6.
-- ============================================================

START TRANSACTION;

INSERT INTO complaints (
    student_user_id,
    student_username,
    category,
    description
)
VALUES (
    900001,
    'qa_hms52_boundary',
    'PLUMBING',
    REPEAT('A', 1000)
);

SET @boundary_id = LAST_INSERT_ID();

SELECT
    complaint_id,
    CHAR_LENGTH(description) AS description_length,
    status
FROM complaints
WHERE complaint_id = @boundary_id;

-- Expected: Error 1406.

UPDATE complaints
SET description = REPEAT('B', 1001)
WHERE complaint_id = @boundary_id;

SELECT
    complaint_id,
    CHAR_LENGTH(description) AS description_length,
    LEFT(description, 5) AS description_start
FROM complaints
WHERE complaint_id = @boundary_id;


-- ============================================================
-- HMS52-T12: Required fields reject NULL
-- First execution: complaint ID 6.
-- Expected: Error 1048 for each UPDATE.
-- ============================================================

UPDATE complaints
SET category = NULL
WHERE complaint_id = 6;

UPDATE complaints
SET description = NULL
WHERE complaint_id = 6;

UPDATE complaints
SET status = NULL
WHERE complaint_id = 6;

SELECT
    complaint_id,
    category,
    CHAR_LENGTH(description) AS description_length,
    status
FROM complaints
WHERE complaint_id = 6;


-- ============================================================
-- HMS52-T09: Verify assignment consistency
-- Additional case: Assignment date without assignee.
-- Expected: Error 3819.
-- ============================================================

UPDATE complaints
SET assigned_at = NOW()
WHERE complaint_id = 6;


-- ============================================================
-- HMS52-T10: Verify resolution-date consistency
-- Additional case: OPEN complaint with resolution date.
-- Expected: Error 3819.
-- ============================================================

UPDATE complaints
SET resolved_at = NOW()
WHERE complaint_id = 6;

SELECT
    complaint_id,
    status,
    assigned_to_user_id,
    assigned_at,
    resolved_at
FROM complaints
WHERE complaint_id = 6;


-- ============================================================
-- HMS52-T13: Rollback boundary-test data
-- ============================================================

ROLLBACK;

SELECT complaint_id
FROM complaints
WHERE complaint_id = 6;


-- ============================================================
-- HMS52-T11: Description-length boundaries
-- Repeated execution for evidence: complaint ID 7.
-- ============================================================

USE Hostel_Complaint_System;

START TRANSACTION;

INSERT INTO complaints
    (student_user_id, student_username, category, description)
VALUES
    (900001, 'qa_hms52_boundary', 'PLUMBING', REPEAT('A', 1000));

SET @test_id = LAST_INSERT_ID();

SELECT complaint_id, category, status,
       CHAR_LENGTH(description) AS description_length
FROM complaints
WHERE complaint_id = @test_id;

-- Expected: Error 1406.

UPDATE complaints
SET description = REPEAT('B', 1001)
WHERE complaint_id = @test_id;

SELECT complaint_id,
       CHAR_LENGTH(description) AS description_length,
       LEFT(description, 5) AS description_sample
FROM complaints
WHERE complaint_id = @test_id;


-- ============================================================
-- HMS52-T12: Required fields reject NULL
-- Repeated execution for evidence: complaint ID 7.
-- Expected: Error 1048 for each UPDATE.
-- ============================================================

UPDATE complaints
SET category = NULL
WHERE complaint_id = @test_id;

UPDATE complaints
SET description = NULL
WHERE complaint_id = @test_id;

UPDATE complaints
SET status = NULL
WHERE complaint_id = @test_id;

SELECT complaint_id, category, status,
       CHAR_LENGTH(description) AS description_length
FROM complaints
WHERE complaint_id = @test_id;


-- ============================================================
-- HMS52-T13: Rollback repeated boundary-test data
-- ============================================================

ROLLBACK;

SELECT complaint_id
FROM complaints
WHERE complaint_id = @test_id;

-- ============================================================
-- HMS52-T14: Verify committed data retrieval
-- ============================================================

USE Hostel_Complaint_System;

START TRANSACTION;

INSERT INTO complaints (
    student_user_id,
    student_username,
    category,
    description
)
VALUES (
    900001,
    'qa_hms52_persist',
    'PLUMBING',
    'HMS52 persistence test'
);

SET @persist_id = LAST_INSERT_ID();

COMMIT;

SELECT complaint_id, student_username, category, description, status
FROM complaints
WHERE complaint_id = @persist_id;

SELECT complaint_id, student_username, category, description, status
FROM Hostel_Complaint_System.complaints
WHERE student_username = 'qa_hms52_persist'
  AND description = 'HMS52 persistence test';

SELECT complaint_id, student_username, category, description, status
FROM Hostel_Complaint_System.complaints
WHERE complaint_id = 5;


-- ============================================================
-- HMS52-T15: Clean up committed test data
-- ============================================================

START TRANSACTION;

DELETE FROM Hostel_Complaint_System.complaints
WHERE complaint_id = 5
  AND student_username = 'qa_hms52_persist'
  AND description = 'HMS52 persistence test';

COMMIT;

SELECT complaint_id
FROM Hostel_Complaint_System.complaints
WHERE complaint_id = 5;