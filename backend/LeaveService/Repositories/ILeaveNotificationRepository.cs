using LeaveService.Models;

namespace LeaveService.Repositories;

public interface ILeaveNotificationRepository
{
    /// <summary>
    /// Returns the newest staff notifications first.
    /// </summary>
    Task<IReadOnlyList<LeaveNotification>> GetForStaffAsync(
        bool unreadOnly);

    /// <summary>
    /// Marks a notification as read. Returns false when the
    /// notification does not exist. Marking an already-read
    /// notification is not an error.
    /// </summary>
    Task<bool> MarkReadAsync(
        ulong notificationId,
        ulong staffUserId,
        DateTime readAtUtc);
}
