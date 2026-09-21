using LeaveService.Events;
using LeaveService.Models;
using LeaveService.Tests.TestDoubles;

namespace LeaveService.Tests;

public class LeaveRequestSubmissionTests
{
    [Fact]
    public async Task SubmitAsync_WithValidRequest_ReturnsPendingRequest()
    {
        var repository = new FakeLeaveRequestRepository();
        var publisher = new FakeLeaveEventPublisher();
        var service = LeaveTestData.CreateRequestService(
            repository,
            publisher);

        var result = await service.SubmitAsync(
            LeaveTestData.ValidRequest(),
            studentUserId: 7,
            studentUsername: "student7");

        Assert.Equal(LeaveRequestStatuses.Pending, result.Status);
        Assert.Equal((ulong)7, result.StudentUserId);
        Assert.Equal("student7", result.StudentUsername);
        Assert.Equal(LeaveTestData.Today.AddDays(3), result.DepartureDate);
        Assert.Equal(LeaveTestData.Today.AddDays(5), result.ExpectedReturnDate);
        Assert.Equal(1, repository.CreateCallCount);
    }

    [Fact]
    public async Task SubmitAsync_WithValidRequest_TrimsAndSavesEveryField()
    {
        var repository = new FakeLeaveRequestRepository();
        var service = LeaveTestData.CreateRequestService(
            repository,
            new FakeLeaveEventPublisher());

        var request = LeaveTestData.ValidRequest();
        request.Reason = "  Family wedding  ";
        request.CompanionName = "  Nimal Perera ";
        request.CompanionRelationship = " Father ";
        request.CompanionPhone = " 0771234567 ";

        await service.SubmitAsync(request, 7, "student7");

        NewLeaveRequest saved = repository.LastNewRequest!;

        Assert.Equal((ulong)7, saved.StudentUserId);
        Assert.Equal("student7", saved.StudentUsername);
        Assert.Equal("Family wedding", saved.Reason);
        Assert.Equal("Nimal Perera", saved.CompanionName);
        Assert.Equal("Father", saved.CompanionRelationship);
        Assert.Equal("0771234567", saved.CompanionPhone);
    }

    [Fact]
    public async Task SubmitAsync_WithValidRequest_CreatesStaffNotification()
    {
        var repository = new FakeLeaveRequestRepository();
        var service = LeaveTestData.CreateRequestService(
            repository,
            new FakeLeaveEventPublisher());

        await service.SubmitAsync(
            LeaveTestData.ValidRequest(),
            7,
            "student7");

        NewLeaveNotification notification = repository.LastNotification!;

        Assert.Equal(
            LeaveNotificationTypes.LeaveSubmitted,
            notification.NotificationType);

        Assert.Contains("student7", notification.Message);
        Assert.Contains("2026-09-23", notification.Message);
        Assert.Contains("2026-09-25", notification.Message);
        Assert.Equal(
            LeaveTestData.Now.UtcDateTime,
            repository.LastOccurredAtUtc);
    }

    [Fact]
    public async Task SubmitAsync_WithValidRequest_PublishesSubmittedEvent()
    {
        var repository = new FakeLeaveRequestRepository();
        var publisher = new FakeLeaveEventPublisher();
        var service = LeaveTestData.CreateRequestService(
            repository,
            publisher);

        var result = await service.SubmitAsync(
            LeaveTestData.ValidRequest(),
            7,
            "student7");

        LeaveEvent published = Assert.Single(publisher.PublishedEvents);

        Assert.Equal(LeaveEventTypes.RequestSubmitted, published.EventType);
        Assert.Equal(result.LeaveRequestId, published.LeaveRequestId);
        Assert.Equal((ulong)7, published.StudentUserId);
        Assert.Equal(LeaveRequestStatuses.Pending, published.Status);
        Assert.Equal(LeaveTestData.Now, published.OccurredAtUtc);
    }

    [Fact]
    public async Task SubmitAsync_WhenEventPublisherFails_StillReturnsSavedRequest()
    {
        var repository = new FakeLeaveRequestRepository();
        var publisher = new FakeLeaveEventPublisher
        {
            ExceptionToThrow = new InvalidOperationException("Kafka is down.")
        };
        var service = LeaveTestData.CreateRequestService(
            repository,
            publisher);

        var result = await service.SubmitAsync(
            LeaveTestData.ValidRequest(),
            7,
            "student7");

        Assert.Equal(LeaveRequestStatuses.Pending, result.Status);
        Assert.Equal(1, repository.CreateCallCount);
    }

    [Fact]
    public async Task SubmitAsync_WhenRepositoryFails_DoesNotPublishEvent()
    {
        var repository = new FakeLeaveRequestRepository
        {
            CreateException = new InvalidOperationException("Database is down.")
        };
        var publisher = new FakeLeaveEventPublisher();
        var service = LeaveTestData.CreateRequestService(
            repository,
            publisher);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SubmitAsync(
                LeaveTestData.ValidRequest(),
                7,
                "student7"));

        Assert.Empty(publisher.PublishedEvents);
    }

    [Fact]
    public async Task SubmitAsync_ReturnOnSameDayAsDeparture_IsAccepted()
    {
        var repository = new FakeLeaveRequestRepository();
        var service = LeaveTestData.CreateRequestService(
            repository,
            new FakeLeaveEventPublisher());

        var request = LeaveTestData.ValidRequest();
        request.ExpectedReturnDate = request.DepartureDate;

        var result = await service.SubmitAsync(request, 7, "student7");

        Assert.Equal(result.DepartureDate, result.ExpectedReturnDate);
    }

    [Fact]
    public async Task SubmitAsync_DepartureToday_IsAccepted()
    {
        var repository = new FakeLeaveRequestRepository();
        var service = LeaveTestData.CreateRequestService(
            repository,
            new FakeLeaveEventPublisher());

        var request = LeaveTestData.ValidRequest();
        request.DepartureDate = LeaveTestData.Today;
        request.ExpectedReturnDate = LeaveTestData.Today.AddDays(1);

        var result = await service.SubmitAsync(request, 7, "student7");

        Assert.Equal(LeaveTestData.Today, result.DepartureDate);
    }
}
