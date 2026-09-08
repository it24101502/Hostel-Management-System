USE Hostel_Management_System;

-- Create one hostel block.
INSERT INTO hostel_blocks
(
    block_code,
    block_name
)
VALUES
(
    'CI-A',
    'CI Test Block A'
);

SET @ci_block_id = LAST_INSERT_ID();


-- Create one student user.
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
    'ci_report_student',
    'CI_REPORT_STUDENT',
    'ci.report.student@example.com',
    'CI.REPORT.STUDENT@EXAMPLE.COM',
    'CI_TEST_PASSWORD_HASH',
    'CI',
    'Report Student',
    TRUE,
    TRUE
FROM roles
WHERE role_name = 'STUDENT';

SET @ci_user_id = LAST_INSERT_ID();


-- Create the student profile and assign the block.
INSERT INTO student_profiles
(
    user_id,
    registration_number,
    normalized_registration_number,
    academic_year,
    hostel_block_id
)
VALUES
(
    @ci_user_id,
    'CI-REPORT-001',
    'CI-REPORT-001',
    1,
    @ci_block_id
);

SET @ci_profile_id = LAST_INSERT_ID();


-- Future invoice: should appear as UNPAID.
INSERT INTO fee_invoices
(
    student_profile_id,
    invoice_number,
    fee_type,
    description,
    total_amount,
    paid_amount,
    issued_at,
    due_date,
    status
)
VALUES
(
    @ci_profile_id,
    'CI-REPORT-UNPAID',
    'HOSTEL_FEE',
    'CI unpaid invoice',
    10000.00,
    0.00,
    UTC_TIMESTAMP(),
    DATE_ADD(UTC_DATE(), INTERVAL 10 DAY),
    'UNPAID'
);


-- Fully paid invoice: should appear as PAID.
INSERT INTO fee_invoices
(
    student_profile_id,
    invoice_number,
    fee_type,
    description,
    total_amount,
    paid_amount,
    issued_at,
    due_date,
    status
)
VALUES
(
    @ci_profile_id,
    'CI-REPORT-PAID',
    'HOSTEL_FEE',
    'CI paid invoice',
    15000.00,
    15000.00,
    UTC_TIMESTAMP(),
    DATE_ADD(UTC_DATE(), INTERVAL 5 DAY),
    'PAID'
);


-- Past-due unpaid invoice:
-- the report must calculate this as OVERDUE.
INSERT INTO fee_invoices
(
    student_profile_id,
    invoice_number,
    fee_type,
    description,
    total_amount,
    paid_amount,
    issued_at,
    due_date,
    status
)
VALUES
(
    @ci_profile_id,
    'CI-REPORT-OVERDUE',
    'HOSTEL_FEE',
    'CI overdue invoice',
    5000.00,
    0.00,
    UTC_TIMESTAMP(),
    DATE_SUB(UTC_DATE(), INTERVAL 1 DAY),
    'UNPAID'
);