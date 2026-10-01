USE Hostel_Notice_System;

-- =========================================================
-- Notices and schedules (HMS-59)
-- One row per notice or timetable schedule item.
--
-- hostel_block_id is an external reference to the hostel
-- block (AccommodationService hostel_blocks.block_id).
-- NULL means the notice is general and shown to every block.
--
-- A notice is archived (is_archived = TRUE) by the auto-archive
-- job (HMS-62) once its expiry_date has passed. Archived rows
-- are kept for the audit trail but hidden from the active list.
-- =========================================================

CREATE TABLE notices
(
    notice_id           BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,

    title               VARCHAR(200) NOT NULL,
    content             VARCHAR(4000) NOT NULL,

    notice_type         VARCHAR(20) NOT NULL
                        DEFAULT 'NOTICE',

    hostel_block_id     BIGINT UNSIGNED NULL,

    expiry_date         DATE NOT NULL,

    is_archived         BOOLEAN NOT NULL DEFAULT FALSE,
    archived_at         DATETIME(6) NULL,

    -- External IdentityService reference (users.user_id).
    created_by_user_id  BIGINT UNSIGNED NOT NULL,

    created_at          DATETIME NOT NULL
                        DEFAULT CURRENT_TIMESTAMP,

    updated_at          DATETIME NOT NULL
                        DEFAULT CURRENT_TIMESTAMP
                        ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT pk_notices
        PRIMARY KEY (notice_id),

    CONSTRAINT chk_notices_type
        CHECK (notice_type IN ('NOTICE', 'SCHEDULE')),

    CONSTRAINT chk_notices_required_text
        CHECK
        (
            CHAR_LENGTH(TRIM(title)) > 0
            AND CHAR_LENGTH(TRIM(content)) > 0
        ),

    -- archived_at is set only while the notice is archived.
    CONSTRAINT chk_notices_archive
        CHECK
        (
            (is_archived = TRUE AND archived_at IS NOT NULL)
            OR
            (is_archived = FALSE AND archived_at IS NULL)
        ),

    INDEX ix_notices_active
        (is_archived, expiry_date),

    INDEX ix_notices_block
        (hostel_block_id),

    INDEX ix_notices_created_by
        (created_by_user_id)
) ENGINE = InnoDB;
