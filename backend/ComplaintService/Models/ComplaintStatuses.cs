namespace ComplaintService.Models;

/// <summary>
/// The lifecycle of a complaint: OPEN -> IN_PROGRESS -> RESOLVED.
/// These values match chk_complaints_status in the database.
/// </summary>
public static class ComplaintStatuses
{
    public const string Open = "OPEN";

    public const string InProgress = "IN_PROGRESS";

    public const string Resolved = "RESOLVED";
}
