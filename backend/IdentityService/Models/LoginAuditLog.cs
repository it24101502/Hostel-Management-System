namespace IdentityService.Models;

public sealed class LoginAuditLog
{
    public ulong AuditLogId { get; set; }

    public ulong? UserId { get; set; }

    public string Identifier { get; set; } = string.Empty;

    public string Outcome { get; set; } = string.Empty;

    public DateTime AttemptedAt { get; set; }
}