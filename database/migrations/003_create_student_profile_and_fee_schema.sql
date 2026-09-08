USE Hostel_Management_System;

-- =========================================================
-- 1. Student Profiles
-- One student profile belongs to one user account.
-- =========================================================

CREATE TABLE student_profiles
(
    student_profile_id             BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    user_id                        BIGINT UNSIGNED NOT NULL,

    registration_number            VARCHAR(50) NOT NULL,
    normalized_registration_number VARCHAR(50) NOT NULL,

    date_of_birth                  DATE NULL,
    gender                         VARCHAR(20) NULL,

    address_line_1                 VARCHAR(255) NULL,
    address_line_2                 VARCHAR(255) NULL,
    city                           VARCHAR(100) NULL,
    district                       VARCHAR(100) NULL,
    postal_code                    VARCHAR(20) NULL,

    programme_name                 VARCHAR(150) NULL,
    faculty_name                   VARCHAR(150) NULL,
    academic_year                  INT UNSIGNED NULL,

    profile_photo_url              VARCHAR(500) NULL,

    created_at                     DATETIME NOT NULL
                                   DEFAULT CURRENT_TIMESTAMP,

    updated_at                     DATETIME NOT NULL
                                   DEFAULT CURRENT_TIMESTAMP
                                   ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT pk_student_profiles
        PRIMARY KEY (student_profile_id),

    CONSTRAINT uq_student_profiles_user
        UNIQUE (user_id),

    CONSTRAINT uq_student_profiles_registration
        UNIQUE (registration_number),

    CONSTRAINT uq_student_profiles_normalized_registration
        UNIQUE (normalized_registration_number),

    CONSTRAINT fk_student_profiles_users
        FOREIGN KEY (user_id)
        REFERENCES users(user_id)
        ON UPDATE CASCADE
        ON DELETE CASCADE,

    CONSTRAINT chk_student_profiles_academic_year
        CHECK
        (
            academic_year IS NULL
            OR academic_year BETWEEN 1 AND 10
        ),

    CONSTRAINT chk_student_profiles_gender
        CHECK
        (
            gender IS NULL
            OR gender IN
            (
                'MALE',
                'FEMALE',
                'OTHER',
                'PREFER_NOT_TO_SAY'
            )
        )
) ENGINE = InnoDB;


-- =========================================================
-- 2. Guardian and Emergency Contacts
-- A student can have multiple contacts.
-- =========================================================

CREATE TABLE guardian_contacts
(
    guardian_contact_id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    student_profile_id  BIGINT UNSIGNED NOT NULL,

    contact_type        VARCHAR(20) NOT NULL,
    full_name           VARCHAR(200) NOT NULL,
    relationship        VARCHAR(100) NOT NULL,

    phone_number        VARCHAR(20) NOT NULL,
    alternate_phone     VARCHAR(20) NULL,
    email               VARCHAR(255) NULL,
    address             VARCHAR(500) NULL,

    is_primary          BOOLEAN NOT NULL DEFAULT FALSE,
    is_active           BOOLEAN NOT NULL DEFAULT TRUE,

    created_at          DATETIME NOT NULL
                        DEFAULT CURRENT_TIMESTAMP,

    updated_at          DATETIME NOT NULL
                        DEFAULT CURRENT_TIMESTAMP
                        ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT pk_guardian_contacts
        PRIMARY KEY (guardian_contact_id),

    CONSTRAINT fk_guardian_contacts_student
        FOREIGN KEY (student_profile_id)
        REFERENCES student_profiles(student_profile_id)
        ON UPDATE CASCADE
        ON DELETE CASCADE,

    CONSTRAINT chk_guardian_contacts_type
        CHECK
        (
            contact_type IN
            (
                'GUARDIAN',
                'EMERGENCY'
            )
        ),

    INDEX ix_guardian_contacts_student
        (student_profile_id),

    INDEX ix_guardian_contacts_phone
        (phone_number)
) ENGINE = InnoDB;


-- =========================================================
-- 3. Fee Invoices
-- Each invoice belongs to one student profile.
-- =========================================================

CREATE TABLE fee_invoices
(
    invoice_id         BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    student_profile_id BIGINT UNSIGNED NOT NULL,

    invoice_number     VARCHAR(50) NOT NULL,
    fee_type           VARCHAR(100) NOT NULL,
    description        VARCHAR(500) NULL,

    total_amount       DECIMAL(12, 2) NOT NULL,
    paid_amount        DECIMAL(12, 2) NOT NULL DEFAULT 0.00,

    issued_at          DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    due_date           DATE NOT NULL,

    status             VARCHAR(30) NOT NULL DEFAULT 'PENDING',

    created_at         DATETIME NOT NULL
                       DEFAULT CURRENT_TIMESTAMP,

    updated_at         DATETIME NOT NULL
                       DEFAULT CURRENT_TIMESTAMP
                       ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT pk_fee_invoices
        PRIMARY KEY (invoice_id),

    CONSTRAINT uq_fee_invoices_number
        UNIQUE (invoice_number),

    CONSTRAINT fk_fee_invoices_student
        FOREIGN KEY (student_profile_id)
        REFERENCES student_profiles(student_profile_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT chk_fee_invoices_total
        CHECK (total_amount > 0),

    CONSTRAINT chk_fee_invoices_paid
        CHECK
        (
            paid_amount >= 0
            AND paid_amount <= total_amount
        ),

    CONSTRAINT chk_fee_invoices_status
        CHECK
        (
            status IN
            (
                'PENDING',
                'PARTIALLY_PAID',
                'PAID',
                'OVERDUE',
                'CANCELLED'
            )
        ),

    INDEX ix_fee_invoices_student
        (student_profile_id),

    INDEX ix_fee_invoices_status
        (status),

    INDEX ix_fee_invoices_due_date
        (due_date)
) ENGINE = InnoDB;


-- =========================================================
-- 4. Fee Payments
-- An invoice can have multiple payment records.
-- =========================================================

CREATE TABLE fee_payments
(
    payment_id         BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    invoice_id         BIGINT UNSIGNED NOT NULL,

    payment_reference  VARCHAR(100) NOT NULL,
    amount             DECIMAL(12, 2) NOT NULL,

    payment_method     VARCHAR(30) NOT NULL,
    payment_status     VARCHAR(30) NOT NULL DEFAULT 'COMPLETED',

    paid_at            DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    recorded_by_user_id BIGINT UNSIGNED NULL,

    notes               VARCHAR(500) NULL,

    created_at          DATETIME NOT NULL
                        DEFAULT CURRENT_TIMESTAMP,

    updated_at          DATETIME NOT NULL
                        DEFAULT CURRENT_TIMESTAMP
                        ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT pk_fee_payments
        PRIMARY KEY (payment_id),

    CONSTRAINT uq_fee_payments_reference
        UNIQUE (payment_reference),

    CONSTRAINT fk_fee_payments_invoice
        FOREIGN KEY (invoice_id)
        REFERENCES fee_invoices(invoice_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT fk_fee_payments_recorded_by
        FOREIGN KEY (recorded_by_user_id)
        REFERENCES users(user_id)
        ON UPDATE CASCADE
        ON DELETE SET NULL,

    CONSTRAINT chk_fee_payments_amount
        CHECK (amount > 0),

    CONSTRAINT chk_fee_payments_method
        CHECK
        (
            payment_method IN
            (
                'CASH',
                'CARD',
                'BANK_TRANSFER',
                'ONLINE'
            )
        ),

    CONSTRAINT chk_fee_payments_status
        CHECK
        (
            payment_status IN
            (
                'PENDING',
                'COMPLETED',
                'FAILED',
                'REFUNDED'
            )
        ),

    INDEX ix_fee_payments_invoice
        (invoice_id),

    INDEX ix_fee_payments_status
        (payment_status),

    INDEX ix_fee_payments_paid_at
        (paid_at)
) ENGINE = InnoDB;