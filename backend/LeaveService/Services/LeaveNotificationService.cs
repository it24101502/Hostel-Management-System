using LeaveService.DTOs;
using LeaveService.Models;
using LeaveService.Repositories;

namespace LeaveService.Services;

public class LeaveNotificationService : ILeaveNotificationService
{
    private readonly ILeaveNotificationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public LeaveNotificationService(
        ILeaveNotificationRepository repository,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<LeaveNotificationResponse>>
        GetStaffNotificationsAsync(bool unreadOnly)
    {
        var notifications =
            await _repository.GetForStaffAsync(unreadOnly);

        return notifications.Select(MapResponse).ToList();
    }

    public Task<bool> MarkReadAsync(
        ulong notificationId,
        ulong staffUserId)
    {
        return _repository.MarkReadAsync(
            notificationId,
            staffUserId,
            _timeProvider.GetUtcNow().UtcDateTime);
    }

    private static LeaveNotificationResponse MapResponse(
        LeaveNotification notification)
    {
        return new LeaveNotificationResponse
        {
            NotificationId = notification.NotificationId,
            LeaveRequestId = notification.LeaveRequestId,
            NotificationType = notification.NotificationType,
            Message = notification.Message,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt
        };
    }
}
