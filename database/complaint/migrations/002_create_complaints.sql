USE Hostel_Complaint_System;

-- =========================================================
-- Complaints (HMS-52)
-- One row per complaint submitted by a student.
--
-- Lifecycle: OPEN -> IN_PROGRESS -> RESOLVED
-- HMS-53 only ever creates OPEN rows. HMS-54 (staff triage)
-- assigns complaints and changes the status.
--
-- NOTE: the category list below is a proposal. Confirm it with
-- the BA before the Sprint review and, if it changes, update
-- chk_complaints_category and ComplaintCategories.cs together.
-- =========================================================

CREATE TABLE complaints
(
    complaint_id         BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,

    -- External IdentityService references.
    -- student_user_id is users.user_id (the JWT "sub" claim).
    -- student_username is a snapshot of the JWT "unique_name"
    -- claim, so this database never has to query IdentityService.
    student_user_id      BIGINT UNSIGNED NOT NULL,
    student_username     VARCHAR(50) NOT NULL,

    category             VARCHAR(30) NOT NULL,
    description          VARCHAR(1000) NOT NULL,

    status               VARCHAR(20) NOT NULL
                         DEFAULT 'OPEN',

    -- External IdentityService reference (users.user_id) of the
    -- staff member handling the complaint. NULL = not assigned.
    assigned_to_user_id  BIGINT UNSIGNED NULL,
    assigned_at          DATETIME NULL,

    resolved_at          DATETIME NULL,

    created_at           DATETIME NOT NULL
                         DEFAULT CURRENT_TIMESTAMP,

    updated_at           DATETIME NOT NULL
                         DEFAULT CURRENT_TIMESTAMP
                         ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT pk_complaints
        PRIMARY KEY (complaint_id),

    CONSTRAINT chk_complaints_status
        CHECK (status IN ('OPEN', 'IN_PROGRESS', 'RESOLVED')),

    CONSTRAINT chk_complaints_category
        CHECK
        (
            category IN
            (
                'MAINTENANCE',
                'ELECTRICAL',
                'PLUMBING',
                'CLEANLINESS',
                'SECURITY',
                'FOOD',
                'NOISE',
                'OTHER'
            )
        ),

    -- A description cannot be blank.
    CONSTRAINT chk_complaints_required_text
        CHECK (CHAR_LENGTH(TRIM(description)) > 0),

    -- assigned_to_user_id and assigned_at are set together.
    CONSTRAINT chk_complaints_assignment
        CHECK
        (
            (assigned_to_user_id IS NULL AND assigned_at IS NULL)
            OR
            (assigned_to_user_id IS NOT NULL AND assigned_at IS NOT NULL)
        ),

    -- resolved_at is set only while the status is RESOLVED.
    CONSTRAINT chk_complaints_resolved
        CHECK
        (
            (status = 'RESOLVED' AND resolved_at IS NOT NULL)
            OR
            (status <> 'RESOLVED' AND resolved_at IS NULL)
        ),

    INDEX ix_complaints_student
        (student_user_id, created_at),

    INDEX ix_complaints_status
        (status),

    INDEX ix_complaints_category
        (category),

    INDEX ix_complaints_assigned_to
        (assigned_to_user_id)
) ENGINE = InnoDB;
