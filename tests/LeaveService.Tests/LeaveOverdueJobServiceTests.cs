using LeaveService.Events;
using LeaveService.Models;
using LeaveService.Tests.TestDoubles;

namespace LeaveService.Tests;

/// <summary>
/// The overdue job reads today's date from the injected clock, so
/// these tests move the clock instead of waiting for real days.
/// </summary>
public class LeaveOverdueJobServiceTests
{
    // The student's expected return date in most tests.
    private static readonly DateOnly ExpectedReturn = new(2026, 9, 12);

    [Fact]
    public async Task RunOnceAsync_BeforeTheExpectedReturnDate_DoesNotFlag()
    {
        var (repository, _) = await RunAsync(
            clock: new DateTimeOffset(2026, 9, 11, 10, 0, 0, TimeSpan.Zero));

        Assert.Null(repository.Requests[0].OverdueAlertedAt);
        Assert.Empty(repository.OverdueNotifications);
    }

    [Fact]
    public async Task RunOnceAsync_OnTheExpectedReturnDate_DoesNotFlag()
    {
        var (repository, result) = await RunAsync(
            clock: new DateTimeOffset(2026, 9, 12, 23, 59, 0, TimeSpan.Zero));

        Assert.Equal(0, result.RequestsFlagged);
        Assert.Empty(repository.OverdueNotifications);
    }

    [Fact]
    public async Task RunOnceAsync_TheDayAfterTheExpectedReturnDate_FlagsAndNotifiesStaff()
    {
        var (repository, result) = await RunAsync(
            clock: new DateTimeOffset(2026, 9, 13, 0, 5, 0, TimeSpan.Zero));

        Assert.Equal(1, result.RequestsFlagged);
        Assert.Equal(new DateOnly(2026, 9, 13), result.ProcessingDate);

        var notification = Assert.Single(repository.OverdueNotifications);

        Assert.Equal(
            LeaveNotificationTypes.ReturnOverdue,
            notification.NotificationType);
        Assert.Contains("student7", notification.Message);
        Assert.Contains("2026-09-12", notification.Message);
    }

    [Fact]
    public async Task RunOnceAsync_RecordsSystemAuditEntryAndPublishesEvent()
    {
        var repository = new FakeLeaveRequestRepository();
        var request = repository.Seed(
            7,
            LeaveRequestStatuses.Departed,
            "student7",
            expectedReturnDate: ExpectedReturn);

        var publisher = new FakeLeaveEventPublisher();
        var service = LeaveTestData.CreateOverdueJobService(
            repository,
            publisher,
            new DateTimeOffset(2026, 9, 14, 6, 0, 0, TimeSpan.Zero));

        await service.RunOnceAsync();

        var audit = Assert.Single(repository.AuditEntries);

        Assert.Equal(LeaveAuditActions.FlagOverdue, audit.Action);
        Assert.Null(audit.ActorUserId);
        Assert.Equal(LeaveRoles.System, audit.ActorRole);

        LeaveEvent published = Assert.Single(publisher.PublishedEvents);

        Assert.Equal(LeaveEventTypes.ReturnOverdue, published.EventType);
        Assert.Equal(request.LeaveRequestId, published.LeaveRequestId);
        Assert.Null(published.ActorUserId);
    }

    [Fact]
    public async Task RunOnceAsync_WhenRunAgain_AlertsOnlyOnce()
    {
        var repository = new FakeLeaveRequestRepository();
        repository.Seed(
            7,
            LeaveRequestStatuses.Departed,
            "student7",
            expectedReturnDate: ExpectedReturn);

        var service = LeaveTestData.CreateOverdueJobService(
            repository,
            new FakeLeaveEventPublisher(),
            new DateTimeOffset(2026, 9, 14, 6, 0, 0, TimeSpan.Zero));

        var first = await service.RunOnceAsync();
        var second = await service.RunOnceAsync();

        Assert.Equal(1, first.RequestsFlagged);
        Assert.Equal(0, second.RequestsFlagged);
        Assert.Single(repository.OverdueNotifications);
    }

    [Theory]
    [InlineData(LeaveRequestStatuses.Pending)]
    [InlineData(LeaveRequestStatuses.Approved)]
    [InlineData(LeaveRequestStatuses.Rejected)]
    [InlineData(LeaveRequestStatuses.Closed)]
    public async Task RunOnceAsync_IgnoresRequestsThatAreNotDeparted(
        string status)
    {
        var repository = new FakeLeaveRequestRepository();
        repository.Seed(
            7,
            status,
            expectedReturnDate: ExpectedReturn);

        var service = LeaveTestData.CreateOverdueJobService(
            repository,
            new FakeLeaveEventPublisher(),
            new DateTimeOffset(2026, 9, 20, 6, 0, 0, TimeSpan.Zero));

        var result = await service.RunOnceAsync();

        Assert.Equal(0, result.RequestsFlagged);
        Assert.Empty(repository.OverdueNotifications);
    }

    [Fact]
    public async Task RunOnceAsync_UsesTheInjectedClock_NotTheRealDate()
    {
        var repository = new FakeLeaveRequestRepository();
        repository.Seed(
            7,
            LeaveRequestStatuses.Departed,
            "student7",
            expectedReturnDate: ExpectedReturn);

        var publisher = new FakeLeaveEventPublisher();

        var beforeService = LeaveTestData.CreateOverdueJobService(
            repository,
            publisher,
            new DateTimeOffset(2026, 9, 12, 12, 0, 0, TimeSpan.Zero));

        var afterService = LeaveTestData.CreateOverdueJobService(
            repository,
            publisher,
            new DateTimeOffset(2026, 9, 13, 12, 0, 0, TimeSpan.Zero));

        Assert.Equal(0, (await beforeService.RunOnceAsync()).RequestsFlagged);
        Assert.Equal(1, (await afterService.RunOnceAsync()).RequestsFlagged);
    }

    [Fact]
    public async Task RunOnceAsync_FlagsEveryOverdueStudent()
    {
        var repository = new FakeLeaveRequestRepository();
        repository.Seed(7, LeaveRequestStatuses.Departed, "a", expectedReturnDate: ExpectedReturn);
        repository.Seed(8, LeaveRequestStatuses.Departed, "b", expectedReturnDate: ExpectedReturn);
        repository.Seed(9, LeaveRequestStatuses.Departed, "c", expectedReturnDate: new DateOnly(2026, 9, 30));

        var service = LeaveTestData.CreateOverdueJobService(
            repository,
            new FakeLeaveEventPublisher(),
            new DateTimeOffset(2026, 9, 20, 6, 0, 0, TimeSpan.Zero));

        var result = await service.RunOnceAsync();

        Assert.Equal(2, result.RequestsFlagged);
    }

    [Fact]
    public async Task RunOnceAsync_WhenOneRequestFails_StillFlagsTheOthers()
    {
        var repository = new FakeLeaveRequestRepository();
        var broken = repository.Seed(7, LeaveRequestStatuses.Departed, "a", expectedReturnDate: ExpectedReturn);
        var healthy = repository.Seed(8, LeaveRequestStatuses.Departed, "b", expectedReturnDate: ExpectedReturn);
        repository.FailFlaggingFor.Add(broken.LeaveRequestId);

        var service = LeaveTestData.CreateOverdueJobService(
            repository,
            new FakeLeaveEventPublisher(),
            new DateTimeOffset(2026, 9, 20, 6, 0, 0, TimeSpan.Zero));

        var result = await service.RunOnceAsync();

        Assert.Equal(1, result.RequestsFlagged);
        Assert.NotNull(healthy.OverdueAlertedAt);
        Assert.Null(broken.OverdueAlertedAt);
    }

    [Fact]
    public async Task RunOnceAsync_WhenEventPublisherFails_StillFlagsTheRequest()
    {
        var repository = new FakeLeaveRequestRepository();
        repository.Seed(7, LeaveRequestStatuses.Departed, "a", expectedReturnDate: ExpectedReturn);

        var service = LeaveTestData.CreateOverdueJobService(
            repository,
            new FakeLeaveEventPublisher
            {
                ExceptionToThrow = new InvalidOperationException("Kafka is down.")
            },
            new DateTimeOffset(2026, 9, 20, 6, 0, 0, TimeSpan.Zero));

        var result = await service.RunOnceAsync();

        Assert.Equal(1, result.RequestsFlagged);
    }

    private static async Task<(FakeLeaveRequestRepository Repository, LeaveOverdueJobResult Result)>
        RunAsync(DateTimeOffset clock)
    {
        var repository = new FakeLeaveRequestRepository();
        repository.Seed(
            7,
            LeaveRequestStatuses.Departed,
            "student7",
            expectedReturnDate: ExpectedReturn);

        var service = LeaveTestData.CreateOverdueJobService(
            repository,
            new FakeLeaveEventPublisher(),
            clock);

        return (repository, await service.RunOnceAsync());
    }
}
