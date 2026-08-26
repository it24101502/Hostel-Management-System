USE Hostel_Management_System;

CREATE TABLE login_audit_logs
(
    audit_log_id     BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    user_id          BIGINT UNSIGNED NULL,
    identifier       VARCHAR(255) NOT NULL,
    outcome          VARCHAR(20) NOT NULL,
    attempted_at     DATETIME(6) NOT NULL,

    CONSTRAINT pk_login_audit_logs
        PRIMARY KEY (audit_log_id),

    CONSTRAINT fk_login_audit_logs_users
        FOREIGN KEY (user_id)
        REFERENCES users(user_id)
        ON UPDATE CASCADE
        ON DELETE SET NULL,

    CONSTRAINT chk_login_audit_logs_outcome
        CHECK (outcome IN ('SUCCESS', 'FAILURE')),

    INDEX ix_login_audit_logs_attempted_at
        (attempted_at),

    INDEX ix_login_audit_logs_user_id
        (user_id),

    INDEX ix_login_audit_logs_identifier
        (identifier)
) ENGINE = InnoDB;