namespace LeaveService.Models;

/// <summary>
/// The lifecycle of a leave request.
///   PENDING -> APPROVED -> DEPARTED -> CLOSED
///   PENDING -> REJECTED
/// These values match chk_leave_requests_status in the database.
/// </summary>
public static class LeaveRequestStatuses
{
    public const string Pending = "PENDING";

    public const string Approved = "APPROVED";

    public const string Rejected = "REJECTED";

    public const string Departed = "DEPARTED";

    public const string Closed = "CLOSED";
}
