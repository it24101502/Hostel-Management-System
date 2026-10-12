SELECT
    COUNT(*) AS total,
    SUM(status = 'OPEN') AS open,
    SUM(status = 'IN_PROGRESS') AS in_progress,
    SUM(status = 'RESOLVED') AS resolved
FROM Hostel_Complaint_System.complaints;

SELECT category, COUNT(*) AS complaint_count
FROM Hostel_Complaint_System.complaints
GROUP BY category
ORDER BY category;

SELECT complaint_id, category, description, status, assigned_to_user_id
FROM Hostel_Complaint_System.complaints
ORDER BY complaint_id DESC;

SELECT complaint_id, category, status
FROM Hostel_Complaint_System.complaints
WHERE category = 'PLUMBING'
ORDER BY complaint_id DESC;

SELECT complaint_id, category, status
FROM Hostel_Complaint_System.complaints
WHERE status = 'IN_PROGRESS'
ORDER BY complaint_id DESC;

SELECT complaint_id, category, status
FROM Hostel_Complaint_System.complaints
WHERE category = 'PLUMBING'
  AND status = 'IN_PROGRESS'
ORDER BY complaint_id DESC;

SELECT complaint_id, category, status
FROM Hostel_Complaint_System.complaints
WHERE category = 'PLUMBING'
  AND status = 'RESOLVED';