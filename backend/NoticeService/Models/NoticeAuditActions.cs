namespace NoticeService.Models;

/// <summary>
/// Audit actions. These values match chk_notice_audit_action
/// in the database.
/// </summary>
public static class NoticeAuditActions
{
    public const string Publish = "PUBLISH";

    public const string Update = "UPDATE";

    public const string Delete = "DELETE";

    public const string Archive = "ARCHIVE";
}
