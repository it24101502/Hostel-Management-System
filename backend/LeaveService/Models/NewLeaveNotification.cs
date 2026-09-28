namespace LeaveService.Models;

/// <summary>
/// A staff notification that is saved together with the
/// leave request that caused it.
/// </summary>
public class NewLeaveNotification
{
    public string NotificationType { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
