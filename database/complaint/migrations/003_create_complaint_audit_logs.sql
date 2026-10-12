USE Hostel_Complaint_System;

-- =========================================================
-- Complaint audit log
-- Every state-changing action on a complaint adds a row.
-- Rows are only ever inserted, never updated or deleted.
-- actor_user_id is NULL only if the system itself acts.
-- =========================================================

CREATE TABLE complaint_audit_logs
(
    audit_log_id   BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    complaint_id   BIGINT UNSIGNED NOT NULL,

    -- External IdentityService reference (users.user_id).
    actor_user_id  BIGINT UNSIGNED NULL,
    actor_role     VARCHAR(20) NOT NULL,

    action         VARCHAR(30) NOT NULL,
    from_status    VARCHAR(20) NULL,
    to_status      VARCHAR(20) NOT NULL,
    remarks        VARCHAR(500) NULL,

    occurred_at    DATETIME(6) NOT NULL
                   DEFAULT CURRENT_TIMESTAMP(6),

    CONSTRAINT pk_complaint_audit_logs
        PRIMARY KEY (audit_log_id),

    CONSTRAINT fk_complaint_audit_complaint
        FOREIGN KEY (complaint_id)
        REFERENCES complaints(complaint_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT chk_complaint_audit_action
        CHECK (action IN ('SUBMIT', 'ASSIGN', 'STATUS_CHANGE')),

    CONSTRAINT chk_complaint_audit_actor_role
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

    INDEX ix_complaint_audit_complaint
        (complaint_id),

    INDEX ix_complaint_audit_actor
        (actor_user_id),

    INDEX ix_complaint_audit_occurred
        (occurred_at)
) ENGINE = InnoDB;
