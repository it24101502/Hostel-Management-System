USE Hostel_Management_System;

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


ALTER TABLE student_profiles
    ADD COLUMN hostel_block_id BIGINT UNSIGNED NULL
        AFTER academic_year,

    ADD CONSTRAINT fk_student_profiles_hostel_block
        FOREIGN KEY (hostel_block_id)
        REFERENCES hostel_blocks(block_id)
        ON UPDATE CASCADE
        ON DELETE SET NULL,

    ADD INDEX ix_student_profiles_hostel_block
        (hostel_block_id);