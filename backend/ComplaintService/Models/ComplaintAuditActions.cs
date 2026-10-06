namespace ComplaintService.Models;

/// <summary>
/// Audit actions. These values match chk_complaint_audit_action
/// in the database.
/// </summary>
public static class ComplaintAuditActions
{
    public const string Submit = "SUBMIT";

    public const string Assign = "ASSIGN";

    public const string StatusChange = "STATUS_CHANGE";
}
