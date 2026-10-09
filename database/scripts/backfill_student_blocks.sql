-- Run once on the existing MySQL instance. If IdentityService's
-- hostel_blocks already holds test rows (for example 'CI-A'), delete them
-- first, otherwise their code/name can clash with the mirrored blocks.
INSERT INTO Hostel_Management_System.hostel_blocks
    (block_id, block_code, block_name, is_active)
SELECT block_id, block_code, block_name, is_active
FROM Hostel_Accommodation_System.hostel_blocks
ON DUPLICATE KEY UPDATE
    block_code = VALUES(block_code),
    block_name = VALUES(block_name),
    is_active  = VALUES(is_active);

UPDATE Hostel_Management_System.student_profiles AS sp
JOIN Hostel_Accommodation_System.student_room_allocations AS a
    ON a.student_profile_id = sp.student_profile_id
JOIN Hostel_Accommodation_System.hostel_rooms AS r
    ON r.room_id = a.room_id
SET sp.hostel_block_id = r.block_id;