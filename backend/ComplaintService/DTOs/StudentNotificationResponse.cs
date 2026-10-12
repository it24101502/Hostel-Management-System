namespace ComplaintService.DTOs;

public class StudentNotificationResponse
{
    public string NotificationId { get; set; } = string.Empty;
    public ulong ComplaintId { get; set; }
    public ulong StudentId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}