USE Hostel_Accommodation_System;

ALTER TABLE student_room_allocation_audit_logs
    MODIFY to_room_id BIGINT UNSIGNED NULL;

ALTER TABLE student_room_allocation_audit_logs
    DROP CHECK chk_student_room_allocation_audit_action;

ALTER TABLE student_room_allocation_audit_logs
    ADD CONSTRAINT chk_student_room_allocation_audit_action
        CHECK
        (
            action IN
            (
                'ALLOCATE',
                'TRANSFER',
                'RELEASE'
            )
        );