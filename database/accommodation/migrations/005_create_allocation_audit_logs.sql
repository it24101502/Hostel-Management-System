USE Hostel_Accommodation_System;

CREATE TABLE student_room_allocation_audit_logs
(
    allocation_audit_log_id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    allocation_id           BIGINT UNSIGNED NOT NULL,

    administrator_user_id   BIGINT UNSIGNED NOT NULL,
    student_profile_id      BIGINT UNSIGNED NOT NULL,

    action                  VARCHAR(20) NOT NULL,
    from_room_id            BIGINT UNSIGNED NULL,
    to_room_id              BIGINT UNSIGNED NOT NULL,

    occurred_at             DATETIME(6) NOT NULL
                            DEFAULT CURRENT_TIMESTAMP(6),

    CONSTRAINT pk_student_room_allocation_audit_logs
        PRIMARY KEY (allocation_audit_log_id),

    CONSTRAINT chk_student_room_allocation_audit_action
        CHECK (action IN ('ALLOCATE', 'TRANSFER')),

    INDEX ix_allocation_audit_student
        (student_profile_id),

    INDEX ix_allocation_audit_to_room
        (to_room_id),

    INDEX ix_allocation_audit_administrator
        (administrator_user_id),

    INDEX ix_allocation_audit_occurred
        (occurred_at)
) ENGINE = InnoDB;