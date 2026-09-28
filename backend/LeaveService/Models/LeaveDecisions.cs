namespace LeaveService.Models;

/// <summary>
/// The two decisions a warden can make on a pending request.
/// </summary>
public static class LeaveDecisions
{
    public const string Approve = "APPROVE";

    public const string Reject = "REJECT";
}
