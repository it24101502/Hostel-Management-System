namespace LeaveService.DTOs;

public class LeaveNotificationResponse
{
    public ulong NotificationId { get; set; }

    public ulong LeaveRequestId { get; set; }

    public string NotificationType { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }
}
