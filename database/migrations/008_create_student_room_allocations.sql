USE Hostel_Management_System;

CREATE TABLE student_room_allocations
(
    allocation_id      BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    student_profile_id BIGINT UNSIGNED NOT NULL,
    room_id            BIGINT UNSIGNED NOT NULL,

    allocated_at       DATETIME NOT NULL
                       DEFAULT CURRENT_TIMESTAMP,

    updated_at         DATETIME NOT NULL
                       DEFAULT CURRENT_TIMESTAMP
                       ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT pk_student_room_allocations
        PRIMARY KEY (allocation_id),

    CONSTRAINT uq_student_room_allocations_student
        UNIQUE (student_profile_id),

    CONSTRAINT fk_student_room_allocations_student
        FOREIGN KEY (student_profile_id)
        REFERENCES student_profiles(student_profile_id)
        ON UPDATE CASCADE
        ON DELETE CASCADE,

    CONSTRAINT fk_student_room_allocations_room
        FOREIGN KEY (room_id)
        REFERENCES hostel_rooms(room_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    INDEX ix_student_room_allocations_room
        (room_id)
) ENGINE = InnoDB;