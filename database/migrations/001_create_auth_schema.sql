CREATE DATABASE Hostel_Management_System
	CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;
USE Hostel_Management_System;

-- =========================================================
-- 1. Roles
-- =========================================================

CREATE TABLE roles
(
    role_id       BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    role_name     VARCHAR(50) NOT NULL,
    description   VARCHAR(255) NULL,
    is_active     BOOLEAN NOT NULL DEFAULT TRUE,
    created_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                                 ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT pk_roles PRIMARY KEY (role_id),
    CONSTRAINT uq_roles_role_name UNIQUE (role_name)
) ENGINE = InnoDB;

INSERT INTO roles (role_name, description)
VALUES
    ('ADMIN', 'Manages the entire system'),
    ('WARDEN', 'Manages hostel activities and operations'),
    ('HOSTEL_MASTER', 'Supervises students and permissions'),
    ('STUDENT', 'Uses student hostel services');

-- =========================================================
-- 2. Users
-- =========================================================

CREATE TABLE users
(
    user_id                  BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    role_id                  BIGINT UNSIGNED NOT NULL,

    username                 VARCHAR(50) NOT NULL,
    normalized_username      VARCHAR(50) NOT NULL,

    email                    VARCHAR(255) NOT NULL,
    normalized_email         VARCHAR(255) NOT NULL,

    password_hash            VARCHAR(255) NOT NULL,

    first_name               VARCHAR(100) NOT NULL,
    last_name                VARCHAR(100) NOT NULL,

    phone_number             VARCHAR(20) NULL,

    failed_login_attempts    INT UNSIGNED NOT NULL DEFAULT 0,
    is_locked                BOOLEAN NOT NULL DEFAULT FALSE,
    lockout_end_at           DATETIME NULL,

    is_active                BOOLEAN NOT NULL DEFAULT TRUE,
    is_email_verified        BOOLEAN NOT NULL DEFAULT FALSE,

    last_login_at            DATETIME NULL,
    password_changed_at      DATETIME NULL,

    created_at               DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at               DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                                         ON UPDATE CURRENT_TIMESTAMP,
    deleted_at               DATETIME NULL,

    CONSTRAINT pk_users PRIMARY KEY (user_id),

    CONSTRAINT uq_users_username
        UNIQUE (username),

    CONSTRAINT uq_users_normalized_username
        UNIQUE (normalized_username),

    CONSTRAINT uq_users_email
        UNIQUE (email),

    CONSTRAINT uq_users_normalized_email
        UNIQUE (normalized_email),

    CONSTRAINT fk_users_roles
        FOREIGN KEY (role_id)
        REFERENCES roles(role_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT chk_users_failed_attempts
        CHECK (failed_login_attempts >= 0)
) ENGINE = InnoDB;

-- =========================================================
-- 3. User Sessions / Token Tracking Table
-- =========================================================

CREATE TABLE user_sessions
(
    session_id          BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    user_id             BIGINT UNSIGNED NOT NULL,

    refresh_token_hash  VARCHAR(255) NOT NULL,

    issued_at           DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expires_at          DATETIME NOT NULL,

    is_revoked          BOOLEAN NOT NULL DEFAULT FALSE,
    revoked_at          DATETIME NULL,

    ip_address          VARCHAR(45) NULL,
    user_agent          VARCHAR(500) NULL,

    created_at          DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT pk_user_sessions PRIMARY KEY (session_id),

    CONSTRAINT uq_user_sessions_token_hash
        UNIQUE (refresh_token_hash),

    CONSTRAINT fk_user_sessions_users
        FOREIGN KEY (user_id)
        REFERENCES users(user_id)
        ON UPDATE CASCADE
        ON DELETE CASCADE,

    CONSTRAINT chk_user_sessions_expiry
        CHECK (expires_at > issued_at)
) ENGINE = InnoDB;

SHOW TABLES;
SELECT * FROM roles;
SELECT * FROM users;
SELECT * FROM user_sessions;
