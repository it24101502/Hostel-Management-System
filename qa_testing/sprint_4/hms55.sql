SHOW TABLES FROM Hostel_Complaint_System
LIKE 'complaint_notifications';

SELECT complaint_id, status, assigned_to_user_id, resolved_at
FROM Hostel_Complaint_System.complaints
WHERE complaint_id = 8;

SELECT complaint_id, status, assigned_to_user_id, resolved_at
FROM Hostel_Complaint_System.complaints
WHERE complaint_id = 8;

SELECT complaint_id, status, assigned_to_user_id, resolved_at
FROM Hostel_Complaint_System.complaints
WHERE complaint_id = 8;

SELECT COUNT(*) AS complaint_count
FROM Hostel_Complaint_System.complaints
WHERE complaint_id = 999999;

SELECT *
FROM Hostel_Complaint_System.complaint_audit_logs
WHERE complaint_id = 8;

SELECT COUNT(*) AS before_count
FROM Hostel_Complaint_System.complaint_notifications
WHERE complaint_id = 8;

SELECT COUNT(*) AS after_count
FROM Hostel_Complaint_System.complaint_notifications
WHERE complaint_id = 8;

SELECT notification_id, complaint_id, recipient_user_id,
       message, is_read, created_at
FROM Hostel_Complaint_System.complaint_notifications
WHERE complaint_id = 8
ORDER BY notification_id DESC
LIMIT 3;