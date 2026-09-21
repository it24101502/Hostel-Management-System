using LeaveService.Models;
using LeaveService.Repositories;

namespace LeaveService.Tests.TestDoubles;

internal sealed class FakeLeaveNotificationRepository
    : ILeaveNotificationRepository
{
    public List<LeaveNotification> Notifications { get; } = new();

    public bool? LastUnreadOnly { get; private set; }

    public ulong? LastReadByUserId { get; private set; }

    public DateTime? LastReadAtUtc { get; private set; }

    public Task<IReadOnlyList<LeaveNotification>> GetForStaffAsync(
        bool unreadOnly)
    {
        LastUnreadOnly = unreadOnly;

        IReadOnlyList<LeaveNotification> result = Notifications
            .Where(notification => !unreadOnly || !notification.IsRead)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<bool> MarkReadAsync(
        ulong notificationId,
        ulong staffUserId,
        DateTime readAtUtc)
    {
        LeaveNotification? notification = Notifications.FirstOrDefault(
            item => item.NotificationId == notificationId);

        if (notification is null)
        {
            return Task.FromResult(false);
        }

        notification.IsRead = true;
        notification.ReadByUserId = staffUserId;
        notification.ReadAt = readAtUtc;

        LastReadByUserId = staffUserId;
        LastReadAtUtc = readAtUtc;

        return Task.FromResult(true);
    }
}
