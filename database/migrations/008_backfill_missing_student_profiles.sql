INSERT INTO student_profiles
(
    user_id,
    registration_number,
    normalized_registration_number
)
SELECT
    u.user_id,
    CONCAT('TEMP-', u.user_id),
    CONCAT('TEMP-', u.user_id)
FROM users AS u
INNER JOIN roles AS r
    ON r.role_id = u.role_id
LEFT JOIN student_profiles AS sp
    ON sp.user_id = u.user_id
WHERE r.role_name = 'STUDENT'
  AND u.is_active = TRUE
  AND sp.student_profile_id IS NULL;