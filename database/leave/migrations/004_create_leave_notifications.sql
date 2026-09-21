USE Hostel_Leave_System;

-- =========================================================
-- Leave notifications (HMS-43)
-- In-app notifications raised by the leave service.
--
-- audience = 'STAFF' is a shared inbox for wardens and hostel
-- masters. IdentityService does not record which warden is
-- responsible for which student, so a notification is sent to
-- the staff group rather than to one named warden.
-- =========================================================

CREATE TABLE leave_notifications
(
    notification_id    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    leave_request_id   BIGINT UNSIGNED NOT NULL,

    audience           VARCHAR(20) NOT NULL
                       DEFAULT 'STAFF',

    notification_type  VARCHAR(30) NOT NULL,
    message            VARCHAR(500) NOT NULL,

    is_read            BOOLEAN NOT NULL DEFAULT FALSE,

    -- External IdentityService reference (users.user_id).
    read_by_user_id    BIGINT UNSIGNED NULL,
    read_at            DATETIME(6) NULL,

    created_at         DATETIME(6) NOT NULL
                       DEFAULT CURRENT_TIMESTAMP(6),

    CONSTRAINT pk_leave_notifications
        PRIMARY KEY (notification_id),

    CONSTRAINT fk_leave_notifications_leave_request
        FOREIGN KEY (leave_request_id)
        REFERENCES leave_requests(leave_request_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT chk_leave_notifications_audience
        CHECK (audience IN ('STAFF')),

    CONSTRAINT chk_leave_notifications_type
        CHECK
        (
            notification_type IN
            (
                'LEAVE_SUBMITTED',
                'RETURN_OVERDUE'
            )
        ),

    INDEX ix_leave_notifications_inbox
        (audience, is_read, created_at),

    INDEX ix_leave_notifications_leave_request
        (leave_request_id)
) ENGINE = InnoDB;
