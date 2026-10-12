USE Hostel_Notice_System;

-- Four notices:
--   expired general   -> must be archived
--   active general    -> visible to every block
--   active block 7    -> visible to block 7 only
--   active block 8    -> visible to block 8 only
INSERT INTO notices
    (title, content, notice_type, hostel_block_id, expiry_date, created_by_user_id)
VALUES
    ('CI expired general', 'x', 'NOTICE',   NULL, DATE_SUB(UTC_DATE(), INTERVAL 1 DAY), 1),
    ('CI active general',  'x', 'NOTICE',   NULL, DATE_ADD(UTC_DATE(), INTERVAL 5 DAY), 1),
    ('CI active block 7',  'x', 'SCHEDULE', 7,    DATE_ADD(UTC_DATE(), INTERVAL 5 DAY), 1),
    ('CI active block 8',  'x', 'SCHEDULE', 8,    DATE_ADD(UTC_DATE(), INTERVAL 5 DAY), 1);