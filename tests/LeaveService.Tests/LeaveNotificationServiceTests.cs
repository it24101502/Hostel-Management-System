using LeaveService.Models;
using LeaveService.Services;
using LeaveService.Tests.TestDoubles;

namespace LeaveService.Tests;

public class LeaveNotificationServiceTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetStaffNotificationsAsync_ForwardsUnreadOnlyFlag(
        bool unreadOnly)
    {
        var repository = new FakeLeaveNotificationRepository();
        var service = CreateService(repository);

        await service.GetStaffNotificationsAsync(unreadOnly);

        Assert.Equal(unreadOnly, repository.LastUnreadOnly);
    }

    [Fact]
    public async Task GetStaffNotificationsAsync_ReturnsOnlyUnreadWhenRequested()
    {
        var repository = new FakeLeaveNotificationRepository();
        repository.Notifications.Add(CreateNotification(1, isRead: false));
        repository.Notifications.Add(CreateNotification(2, isRead: true));

        var service = CreateService(repository);

        var unread = await service.GetStaffNotificationsAsync(true);
        var all = await service.GetStaffNotificationsAsync(false);

        Assert.Equal((ulong)1, Assert.Single(unread).NotificationId);
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task GetStaffNotificationsAsync_MapsNotificationFields()
    {
        var repository = new FakeLeaveNotificationRepository();
        repository.Notifications.Add(CreateNotification(5, isRead: false));

        var service = CreateService(repository);

        var result = Assert.Single(
            await service.GetStaffNotificationsAsync(false));

        Assert.Equal((ulong)5, result.NotificationId);
        Assert.Equal((ulong)9, result.LeaveRequestId);
        Assert.Equal(LeaveNotificationTypes.LeaveSubmitted, result.NotificationType);
        Assert.Equal("student7 submitted a leave request.", result.Message);
        Assert.False(result.IsRead);
    }

    [Fact]
    public async Task MarkReadAsync_ForExistingNotification_ReturnsTrueAndRecordsReader()
    {
        var repository = new FakeLeaveNotificationRepository();
        repository.Notifications.Add(CreateNotification(5, isRead: false));

        var service = CreateService(repository);

        bool exists = await service.MarkReadAsync(5, staffUserId: 20);

        Assert.True(exists);
        Assert.True(repository.Notifications[0].IsRead);
        Assert.Equal((ulong)20, repository.LastReadByUserId);
        Assert.Equal(LeaveTestData.Now.UtcDateTime, repository.LastReadAtUtc);
    }

    [Fact]
    public async Task MarkReadAsync_ForMissingNotification_ReturnsFalse()
    {
        var service = CreateService(new FakeLeaveNotificationRepository());

        bool exists = await service.MarkReadAsync(404, staffUserId: 20);

        Assert.False(exists);
    }

    private static LeaveNotificationService CreateService(
        FakeLeaveNotificationRepository repository)
    {
        return new LeaveNotificationService(
            repository,
            new FixedTimeProvider(LeaveTestData.Now));
    }

    private static LeaveNotification CreateNotification(
        ulong id,
        bool isRead)
    {
        return new LeaveNotification
        {
            NotificationId = id,
            LeaveRequestId = 9,
            Audience = "STAFF",
            NotificationType = LeaveNotificationTypes.LeaveSubmitted,
            Message = "student7 submitted a leave request.",
            IsRead = isRead,
            CreatedAt = LeaveTestData.Now.UtcDateTime
        };
    }
}
