USE Hostel_Accommodation_System;

CREATE TABLE hostel_rooms
(
    room_id       BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    block_id      BIGINT UNSIGNED NOT NULL,

    floor_number  SMALLINT UNSIGNED NOT NULL,
    room_number   VARCHAR(20) NOT NULL,
    bed_capacity  SMALLINT UNSIGNED NOT NULL,

    is_active     BOOLEAN NOT NULL DEFAULT TRUE,

    created_at    DATETIME NOT NULL
                  DEFAULT CURRENT_TIMESTAMP,

    updated_at    DATETIME NOT NULL
                  DEFAULT CURRENT_TIMESTAMP
                  ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT pk_hostel_rooms
        PRIMARY KEY (room_id),

    CONSTRAINT uq_hostel_rooms_location
        UNIQUE (block_id, floor_number, room_number),

    CONSTRAINT fk_hostel_rooms_block
        FOREIGN KEY (block_id)
        REFERENCES hostel_blocks(block_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT chk_hostel_rooms_bed_capacity
        CHECK (bed_capacity > 0),

    INDEX ix_hostel_rooms_block_floor
        (block_id, floor_number),

    INDEX ix_hostel_rooms_active
        (is_active)
) ENGINE = InnoDB;