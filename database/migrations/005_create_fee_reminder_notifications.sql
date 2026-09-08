USE Hostel_Management_System;

CREATE TABLE fee_reminder_notifications
(
    reminder_id         BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    invoice_id          BIGINT UNSIGNED NOT NULL,
    student_profile_id  BIGINT UNSIGNED NOT NULL,
    recipient_user_id   BIGINT UNSIGNED NOT NULL,

    reminder_type       VARCHAR(30) NOT NULL
                        DEFAULT 'OVERDUE_FEE',

    message             VARCHAR(500) NOT NULL,

    notification_status VARCHAR(20) NOT NULL
                        DEFAULT 'PENDING',

    triggered_at        DATETIME NOT NULL
                        DEFAULT CURRENT_TIMESTAMP,

    sent_at             DATETIME NULL,
    failure_reason      VARCHAR(500) NULL,

    created_at          DATETIME NOT NULL
                        DEFAULT CURRENT_TIMESTAMP,

    updated_at          DATETIME NOT NULL
                        DEFAULT CURRENT_TIMESTAMP
                        ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT pk_fee_reminder_notifications
        PRIMARY KEY (reminder_id),

    CONSTRAINT uq_fee_reminder_invoice
        UNIQUE (invoice_id),

    CONSTRAINT fk_fee_reminder_invoice
        FOREIGN KEY (invoice_id)
        REFERENCES fee_invoices(invoice_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT fk_fee_reminder_student
        FOREIGN KEY (student_profile_id)
        REFERENCES student_profiles(student_profile_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT fk_fee_reminder_user
        FOREIGN KEY (recipient_user_id)
        REFERENCES users(user_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT chk_fee_reminder_type
        CHECK
        (
            reminder_type = 'OVERDUE_FEE'
        ),

    CONSTRAINT chk_fee_reminder_status
        CHECK
        (
            notification_status IN
            (
                'PENDING',
                'SENT',
                'FAILED'
            )
        ),

    INDEX ix_fee_reminder_status
        (notification_status),

    INDEX ix_fee_reminder_recipient
        (recipient_user_id),

    INDEX ix_fee_reminder_triggered_at
        (triggered_at)
) ENGINE = InnoDB;