USE Hostel_Leave_System;

-- =========================================================
-- Leave audit log (NFR-03)
-- Every state-changing action on a leave request adds a row.
-- Rows are only ever inserted, never updated or deleted.
--
-- actor_user_id is NULL when the system itself acts
-- (for example the overdue-return job), in which case
-- actor_role is 'SYSTEM'.
-- =========================================================

CREATE TABLE leave_request_audit_logs
(
    audit_log_id      BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    leave_request_id  BIGINT UNSIGNED NOT NULL,

    -- External IdentityService reference (users.user_id).
    actor_user_id     BIGINT UNSIGNED NULL,
    actor_role        VARCHAR(20) NOT NULL,

    action            VARCHAR(30) NOT NULL,
    from_status       VARCHAR(20) NULL,
    to_status         VARCHAR(20) NOT NULL,
    remarks           VARCHAR(500) NULL,

    occurred_at       DATETIME(6) NOT NULL
                      DEFAULT CURRENT_TIMESTAMP(6),

    CONSTRAINT pk_leave_request_audit_logs
        PRIMARY KEY (audit_log_id),

    CONSTRAINT fk_leave_audit_leave_request
        FOREIGN KEY (leave_request_id)
        REFERENCES leave_requests(leave_request_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT chk_leave_audit_action
        CHECK
        (
            action IN
            (
                'SUBMIT',
                'APPROVE',
                'REJECT',
                'DEPART',
                'RETURN',
                'FLAG_OVERDUE'
            )
        ),

    CONSTRAINT chk_leave_audit_actor_role
        CHECK
        (
            actor_role IN
            (
                'STUDENT',
                'WARDEN',
                'HOSTEL_MASTER',
                'ADMIN',
                'SYSTEM'
            )
        ),

    INDEX ix_leave_audit_leave_request
        (leave_request_id),

    INDEX ix_leave_audit_actor
        (actor_user_id),

    INDEX ix_leave_audit_occurred
        (occurred_at)
) ENGINE = InnoDB;
