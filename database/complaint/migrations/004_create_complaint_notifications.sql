USE Hostel_Complaint_System;

-- =========================================================
-- Complaint notifications (HMS-55)
-- In-app notification sent to the student who raised the
-- complaint whenever its status changes.
-- =========================================================

CREATE TABLE complaint_notifications
(
    notification_id    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    complaint_id       BIGINT UNSIGNED NOT NULL,

    -- External IdentityService reference (users.user_id).
    recipient_user_id  BIGINT UNSIGNED NOT NULL,

    message            VARCHAR(500) NOT NULL,

    is_read            BOOLEAN NOT NULL DEFAULT FALSE,
    read_at            DATETIME(6) NULL,

    created_at         DATETIME(6) NOT NULL
                       DEFAULT CURRENT_TIMESTAMP(6),

    CONSTRAINT pk_complaint_notifications
        PRIMARY KEY (notification_id),

    CONSTRAINT fk_complaint_notifications_complaint
        FOREIGN KEY (complaint_id)
        REFERENCES complaints(complaint_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    INDEX ix_complaint_notifications_inbox
        (recipient_user_id, is_read, created_at),

    INDEX ix_complaint_notifications_complaint
        (complaint_id)
) ENGINE = InnoDB;
