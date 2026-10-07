namespace NoticeService.Models;

public class NoticeAuditLog
{
    public ulong AuditLogId { get; set; }
    public ulong NoticeId { get; set; }
    public ulong? ActorUserId { get; set; }
    public string ActorRole { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
}