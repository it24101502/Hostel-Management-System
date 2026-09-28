namespace LeaveService.Models;

/// <summary>
/// Notification types. These values match
/// chk_leave_notifications_type in the database.
/// </summary>
public static class LeaveNotificationTypes
{
    public const string LeaveSubmitted = "LEAVE_SUBMITTED";

    public const string ReturnOverdue = "RETURN_OVERDUE";
}
