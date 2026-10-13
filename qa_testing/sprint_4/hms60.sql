SELECT notice_id, title, content, notice_type,
       hostel_block_id, expiry_date, created_by_user_id
FROM Hostel_Notice_System.notices
WHERE notice_id = 3;

SELECT COUNT(*) AS remaining_rows
FROM Hostel_Notice_System.notices
WHERE notice_id = 3;

SELECT COUNT(*) AS remaining_rows
FROM Hostel_Notice_System.notices
WHERE notice_id = 4;