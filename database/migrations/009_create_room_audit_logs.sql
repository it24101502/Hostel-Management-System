USE Hostel_Management_System;

CREATE TABLE room_audit_logs
(
    room_audit_log_id    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    administrator_user_id BIGINT UNSIGNED NULL,

    room_id              BIGINT UNSIGNED NOT NULL,
    action               VARCHAR(20) NOT NULL,

    block_id             BIGINT UNSIGNED NOT NULL,
    floor_number         SMALLINT UNSIGNED NOT NULL,
    room_number          VARCHAR(20) NOT NULL,
    bed_capacity         SMALLINT UNSIGNED NOT NULL,

    occurred_at          DATETIME(6) NOT NULL
                         DEFAULT CURRENT_TIMESTAMP(6),

    CONSTRAINT pk_room_audit_logs
        PRIMARY KEY (room_audit_log_id),

    CONSTRAINT fk_room_audit_logs_administrator
        FOREIGN KEY (administrator_user_id)
        REFERENCES users(user_id)
        ON UPDATE CASCADE
        ON DELETE SET NULL,

    CONSTRAINT chk_room_audit_logs_action
        CHECK (action IN ('CREATE', 'UPDATE', 'DELETE')),

    INDEX ix_room_audit_logs_room
        (room_id),

    INDEX ix_room_audit_logs_administrator
        (administrator_user_id),

    INDEX ix_room_audit_logs_occurred
        (occurred_at)
) ENGINE = InnoDB;