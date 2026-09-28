namespace LeaveService.Models;

public class LeaveNotification
{
    public ulong NotificationId { get; set; }

    public ulong LeaveRequestId { get; set; }

    public string Audience { get; set; } = string.Empty;

    public string NotificationType { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public ulong? ReadByUserId { get; set; }

    public DateTime? ReadAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
