SELECT user_id, username, role_id, is_active
FROM Hostel_Management_System.users
WHERE username = 'localadmin';

SELECT user_id, username, role_id, is_active
FROM Hostel_Management_System.users
ORDER BY user_id;

USE Hostel_Management_System;

INSERT INTO users
(
    role_id,
    username,
    normalized_username,
    email,
    normalized_email,
    password_hash,
    first_name,
    last_name,
    is_active,
    is_email_verified
)
SELECT
    role_id,
    'qa_admin',
    'QA_ADMIN',
    'qa.admin@example.com',
    'QA.ADMIN@EXAMPLE.COM',
    '$2b$12$6M7hV0ZyJu/v4hvbYlpKOe/ZVH6Vh2Er6Vywj3g4rpv.YcnBOXTWC',
    'QA',
    'Administrator',
    1,
    1
FROM roles
WHERE role_name = 'ADMIN'
AND NOT EXISTS (
    SELECT 1 FROM users WHERE username = 'qa_admin'
);

SELECT user_id, username, role_id, is_active
FROM users
WHERE username = 'qa_admin';

SELECT
    complaint_id,
    student_user_id,
    student_username,
    category,
    description,
    status,
    assigned_to_user_id,
    assigned_at,
    resolved_at,
    created_at,
    updated_at
FROM Hostel_Complaint_System.complaints
WHERE complaint_id = 8;

SELECT *
FROM Hostel_Complaint_System.complaint_audit_logs
WHERE complaint_id = 8;

SELECT complaint_id, category, description
FROM Hostel_Complaint_System.complaints
WHERE description = 'HMS53-T04: Complaint submitted without a category.';

SELECT complaint_id, category, description
FROM Hostel_Complaint_System.complaints
WHERE description LIKE 'HMS53-T05-%';

SELECT complaint_id, category, description
FROM Hostel_Complaint_System.complaints
WHERE description = 'HMS53-T06: Invalid category test.';

SELECT COUNT(*) AS student_complaint_count
FROM Hostel_Complaint_System.complaints
WHERE student_user_id = 2;

SELECT COUNT(*) AS student_complaint_count
FROM Hostel_Complaint_System.complaints
WHERE student_user_id = 2;

SELECT complaint_id, description,
       CHAR_LENGTH(description) AS description_length,
       status
FROM Hostel_Complaint_System.complaints
WHERE complaint_id = 9;

SELECT complaint_id,
       CHAR_LENGTH(description) AS description_length,
       status
FROM Hostel_Complaint_System.complaints
WHERE complaint_id = 10;

SELECT COUNT(*) AS student_complaint_count
FROM Hostel_Complaint_System.complaints
WHERE student_user_id = 2;

SELECT complaint_id, category,
       CONCAT('[', description, ']') AS description_with_markers,
       status
FROM Hostel_Complaint_System.complaints
WHERE complaint_id = 11;

SELECT complaint_id, category, description, status
FROM Hostel_Complaint_System.complaints
WHERE student_user_id = 2
  AND description LIKE 'HMS53-T12:%'
ORDER BY category;

SELECT complaint_id, description
FROM Hostel_Complaint_System.complaints
WHERE description = 'HMS53-T13: Submission without authentication.';

SELECT complaint_id, description
FROM Hostel_Complaint_System.complaints
WHERE description = 'HMS53-T14-A: Admin submission test.';

SELECT complaint_id, description
FROM Hostel_Complaint_System.complaints
WHERE description = 'HMS53-T14-B: Warden submission test.';

SELECT complaint_id, description
FROM Hostel_Complaint_System.complaints
WHERE description = 'HMS53-T15-A: Invalid token submission test.';

SELECT complaint_id, description
FROM Hostel_Complaint_System.complaints
WHERE description = 'HMS53-T15-B: Expired token submission test.';
