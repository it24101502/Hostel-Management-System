namespace LeaveService.Models;

/// <summary>
/// Audit actions. These values match chk_leave_audit_action
/// in the database.
/// </summary>
public static class LeaveAuditActions
{
    public const string Submit = "SUBMIT";

    public const string Approve = "APPROVE";

    public const string Reject = "REJECT";

    public const string Depart = "DEPART";

    public const string Return = "RETURN";

    public const string FlagOverdue = "FLAG_OVERDUE";
}
