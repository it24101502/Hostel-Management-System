USE Hostel_Leave_System;

-- =========================================================
-- Leave decision and movement columns (HMS-6)
--
-- Adds what a leave request needs after submission:
--   * the warden's decision and the reason for it
--   * the recorded departure and return
--   * the moment the overdue-return alert was raised
--
-- Two CHECK constraints make the database itself refuse any
-- row whose status disagrees with its decision/movement data:
--   PENDING   -> nothing decided, nothing recorded
--   APPROVED  -> decided, nothing recorded yet
--   REJECTED  -> decided, nothing recorded
--   DEPARTED  -> decided, departure recorded
--   CLOSED    -> decided, departure and return recorded
-- =========================================================

ALTER TABLE leave_requests
    -- External IdentityService reference (users.user_id).
    ADD COLUMN decided_by_user_id
        BIGINT UNSIGNED NULL
        AFTER status,

    ADD COLUMN decision_reason
        VARCHAR(500) NULL
        AFTER decided_by_user_id,

    ADD COLUMN decided_at
        DATETIME(6) NULL
        AFTER decision_reason,

    -- External IdentityService reference (users.user_id).
    ADD COLUMN departure_recorded_by_user_id
        BIGINT UNSIGNED NULL
        AFTER decided_at,

    ADD COLUMN actual_departure_at
        DATETIME(6) NULL
        AFTER departure_recorded_by_user_id,

    -- External IdentityService reference (users.user_id).
    ADD COLUMN return_recorded_by_user_id
        BIGINT UNSIGNED NULL
        AFTER actual_departure_at,

    ADD COLUMN actual_return_at
        DATETIME(6) NULL
        AFTER return_recorded_by_user_id,

    -- Set once when the overdue-return job alerts the staff,
    -- so the same overdue student is never alerted twice.
    ADD COLUMN overdue_alerted_at
        DATETIME(6) NULL
        AFTER actual_return_at,

    ADD CONSTRAINT chk_leave_requests_decision
        CHECK
        (
            (
                status = 'PENDING'
                AND decided_by_user_id IS NULL
                AND decision_reason IS NULL
                AND decided_at IS NULL
            )
            OR
            (
                status <> 'PENDING'
                AND decided_by_user_id IS NOT NULL
                AND decided_at IS NOT NULL
                AND decision_reason IS NOT NULL
                AND CHAR_LENGTH(TRIM(decision_reason)) > 0
            )
        ),

    ADD CONSTRAINT chk_leave_requests_movement
        CHECK
        (
            (
                status IN ('PENDING', 'APPROVED', 'REJECTED')
                AND actual_departure_at IS NULL
                AND actual_return_at IS NULL
            )
            OR
            (
                status = 'DEPARTED'
                AND actual_departure_at IS NOT NULL
                AND actual_return_at IS NULL
            )
            OR
            (
                status = 'CLOSED'
                AND actual_departure_at IS NOT NULL
                AND actual_return_at IS NOT NULL
                AND actual_return_at >= actual_departure_at
            )
        ),

    ADD INDEX ix_leave_requests_departure_date
        (departure_date);
