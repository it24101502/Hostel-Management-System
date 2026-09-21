USE Hostel_Leave_System;

-- =========================================================
-- Leave requests (HMS-39)
-- One row per leave request submitted by a student.
--
-- The full request lifecycle is defined here:
--   PENDING -> APPROVED -> DEPARTED -> CLOSED
--   PENDING -> REJECTED
-- HMS-5 only ever creates PENDING rows. The other statuses
-- are used by HMS-6 (approve/reject, departure and return).
-- =========================================================

CREATE TABLE leave_requests
(
    leave_request_id        BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,

    -- External IdentityService references.
    -- student_user_id is users.user_id (the JWT "sub" claim).
    -- student_username is a snapshot of the JWT "unique_name"
    -- claim, so this database never has to query IdentityService.
    student_user_id         BIGINT UNSIGNED NOT NULL,
    student_username        VARCHAR(50) NOT NULL,

    departure_date          DATE NOT NULL,
    expected_return_date    DATE NOT NULL,
    reason                  VARCHAR(500) NOT NULL,

    -- Companion or guardian travelling with / receiving the student.
    companion_name          VARCHAR(200) NOT NULL,
    companion_relationship  VARCHAR(100) NOT NULL,
    companion_phone         VARCHAR(20) NOT NULL,

    status                  VARCHAR(20) NOT NULL
                            DEFAULT 'PENDING',

    created_at              DATETIME NOT NULL
                            DEFAULT CURRENT_TIMESTAMP,

    updated_at              DATETIME NOT NULL
                            DEFAULT CURRENT_TIMESTAMP
                            ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT pk_leave_requests
        PRIMARY KEY (leave_request_id),

    -- HMS-42: the return date cannot be earlier than departure.
    CONSTRAINT chk_leave_requests_dates
        CHECK (expected_return_date >= departure_date),

    CONSTRAINT chk_leave_requests_status
        CHECK
        (
            status IN
            (
                'PENDING',
                'APPROVED',
                'REJECTED',
                'DEPARTED',
                'CLOSED'
            )
        ),

    -- HMS-41: required text fields cannot be blank.
    CONSTRAINT chk_leave_requests_required_text
        CHECK
        (
            CHAR_LENGTH(TRIM(reason)) > 0
            AND CHAR_LENGTH(TRIM(companion_name)) > 0
            AND CHAR_LENGTH(TRIM(companion_relationship)) > 0
            AND CHAR_LENGTH(TRIM(companion_phone)) > 0
        ),

    INDEX ix_leave_requests_student
        (student_user_id, created_at),

    INDEX ix_leave_requests_status
        (status),

    INDEX ix_leave_requests_expected_return
        (expected_return_date)
) ENGINE = InnoDB;
