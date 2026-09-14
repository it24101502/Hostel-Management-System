CREATE DATABASE IF NOT EXISTS Hostel_Accommodation_System
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE Hostel_Accommodation_System;

CREATE TABLE hostel_blocks
(
    block_id    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    block_code  VARCHAR(20) NOT NULL,
    block_name  VARCHAR(100) NOT NULL,
    is_active   BOOLEAN NOT NULL DEFAULT TRUE,

    created_at  DATETIME NOT NULL
                DEFAULT CURRENT_TIMESTAMP,

    updated_at  DATETIME NOT NULL
                DEFAULT CURRENT_TIMESTAMP
                ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT pk_hostel_blocks
        PRIMARY KEY (block_id),

    CONSTRAINT uq_hostel_blocks_code
        UNIQUE (block_code),

    CONSTRAINT uq_hostel_blocks_name
        UNIQUE (block_name)
) ENGINE = InnoDB;