USE Hostel_Notice_System;

-- =========================================================
-- Notice audit log
-- Records publish, update, delete and archive actions.
-- Rows are only ever inserted, never updated or deleted.
--
-- There is deliberately no foreign key to notices: a notice
-- can be deleted (HMS-60) and its audit history must remain.
-- actor_user_id is NULL when the auto-archive job acts,
-- in which case actor_role is 'SYSTEM'.
-- =========================================================

CREATE TABLE notice_audit_logs
(
    audit_log_id   BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    notice_id      BIGINT UNSIGNED NOT NULL,

    -- External IdentityService reference (users.user_id).
    actor_user_id  BIGINT UNSIGNED NULL,
    actor_role     VARCHAR(20) NOT NULL,

    action         VARCHAR(20) NOT NULL,
    title          VARCHAR(200) NOT NULL,

    occurred_at    DATETIME(6) NOT NULL
                   DEFAULT CURRENT_TIMESTAMP(6),

    CONSTRAINT pk_notice_audit_logs
        PRIMARY KEY (audit_log_id),

    CONSTRAINT chk_notice_audit_action
        CHECK (action IN ('PUBLISH', 'UPDATE', 'DELETE', 'ARCHIVE')),

    CONSTRAINT chk_notice_audit_actor_role
        CHECK
        (
            actor_role IN
            (
                'WARDEN',
                'HOSTEL_MASTER',
                'ADMIN',
                'SYSTEM'
            )
        ),

    INDEX ix_notice_audit_notice
        (notice_id),

    INDEX ix_notice_audit_occurred
        (occurred_at)
) ENGINE = InnoDB;
