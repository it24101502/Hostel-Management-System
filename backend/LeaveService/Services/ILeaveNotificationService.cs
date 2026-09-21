using LeaveService.DTOs;

namespace LeaveService.Services;

public interface ILeaveNotificationService
{
    Task<IReadOnlyList<LeaveNotificationResponse>>
        GetStaffNotificationsAsync(bool unreadOnly);

    /// <summary>
    /// Returns false when the notification does not exist.
    /// </summary>
    Task<bool> MarkReadAsync(
        ulong notificationId,
        ulong staffUserId);
}
