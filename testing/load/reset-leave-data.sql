-- =========================================================
-- SE3112 Load testing: clean up rows created by a load run
--
-- Every run inserts many leave requests for loadtest_student.
-- The "list my requests" and "staff report" endpoints return
-- ALL rows (no paging), so leftover rows make the next run
-- slower and the results unfair to compare. Run this between
-- runs so every profile starts from the same data.
--
-- Child rows are deleted first because the foreign keys use
-- ON DELETE RESTRICT.
-- =========================================================

USE Hostel_Leave_System;

DELETE n
FROM leave_notifications AS n
INNER JOIN leave_requests AS r
    ON r.leave_request_id = n.leave_request_id
WHERE r.student_username = 'loadtest_student';

DELETE a
FROM leave_request_audit_logs AS a
INNER JOIN leave_requests AS r
    ON r.leave_request_id = a.leave_request_id
WHERE r.student_username = 'loadtest_student';

DELETE FROM leave_requests
WHERE student_username = 'loadtest_student';

SELECT COUNT(*) AS remaining_loadtest_rows
FROM leave_requests
WHERE student_username = 'loadtest_student';
