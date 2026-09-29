using LeaveService.DTOs;
using LeaveService.Events;
using LeaveService.Exceptions;
using LeaveService.Models;
using LeaveService.Tests.TestDoubles;

namespace LeaveService.Tests;

public class LeaveMovementTests
{
    [Fact]
    public async Task RecordDepartureAsync_ForApprovedRequest_SetsDepartedAndRecordsTime()
    {
        var repository = new FakeLeaveRequestRepository();
        var request = repository.Seed(7, LeaveRequestStatuses.Approved);
        var service = LeaveTestData.CreateReviewService(
            repository,
            new FakeLeaveEventPublisher());

        var result = await service.RecordDepartureAsync(
            request.LeaveRequestId,
            20,
            LeaveRoles.Warden);

        Assert.Equal(LeaveRequestStatuses.Departed, result.Status);
        Assert.Equal(LeaveTestData.Now.UtcDateTime, result.ActualDepartureAt);
        Assert.Null(result.ActualReturnAt);
    }

    [Fact]
    public async Task RecordDepartureAsync_RecordsAuditEntryAndEvent()
    {
        var repository = new FakeLeaveRequestRepository();
        var request = repository.Seed(7, LeaveRequestStatuses.Approved);
        var publisher = new FakeLeaveEventPublisher();
        var service = LeaveTestData.CreateReviewService(repository, publisher);

        await service.RecordDepartureAsync(
            request.LeaveRequestId,
            21,
            LeaveRoles.HostelMaster);

        var audit = Assert.Single(repository.AuditEntries);

        Assert.Equal(LeaveAuditActions.Depart, audit.Action);
        Assert.Equal((ulong)21, audit.ActorUserId);
        Assert.Equal(LeaveRoles.HostelMaster, audit.ActorRole);
        Assert.Equal(LeaveRequestStatuses.Approved, audit.FromStatus);
        Assert.Equal(LeaveRequestStatuses.Departed, audit.ToStatus);

        LeaveEvent published = Assert.Single(publisher.PublishedEvents);

        Assert.Equal(LeaveEventTypes.StudentDeparted, published.EventType);
    }

    [Theory]
    [InlineData(LeaveRequestStatuses.Pending)]
    [InlineData(LeaveRequestStatuses.Rejected)]
    [InlineData(LeaveRequestStatuses.Departed)]
    [InlineData(LeaveRequestStatuses.Closed)]
    public async Task RecordDepartureAsync_ForARequestThatIsNotApproved_IsRefused(
        string status)
    {
        var repository = new FakeLeaveRequestRepository();
        var request = repository.Seed(7, status);
        var service = LeaveTestData.CreateReviewService(
            repository,
            new FakeLeaveEventPublisher());

        var exception = await Assert.ThrowsAsync<InvalidLeaveStatusException>(
            () => service.RecordDepartureAsync(
                request.LeaveRequestId,
                20,
                LeaveRoles.Warden));

        Assert.Contains("approved", exception.Message);
        Assert.Equal(status, repository.Requests[0].Status);
        Assert.Empty(repository.AuditEntries);
    }

    [Fact]
    public async Task RecordReturnAsync_ForDepartedStudent_ClosesRequestAndRecordsTime()
    {
        var repository = new FakeLeaveRequestRepository();
        var request = repository.Seed(7, LeaveRequestStatuses.Departed);
        var publisher = new FakeLeaveEventPublisher();
        var service = LeaveTestData.CreateReviewService(repository, publisher);

        var result = await service.RecordReturnAsync(
            request.LeaveRequestId,
            20,
            LeaveRoles.Warden);

        Assert.Equal(LeaveRequestStatuses.Closed, result.Status);
        Assert.Equal(LeaveTestData.Now.UtcDateTime, result.ActualReturnAt);

        var audit = Assert.Single(repository.AuditEntries);

        Assert.Equal(LeaveAuditActions.Return, audit.Action);
        Assert.Equal(LeaveRequestStatuses.Closed, audit.ToStatus);
        Assert.Equal(
            LeaveEventTypes.StudentReturned,
            Assert.Single(publisher.PublishedEvents).EventType);
    }

    [Theory]
    [InlineData(LeaveRequestStatuses.Pending)]
    [InlineData(LeaveRequestStatuses.Approved)]
    [InlineData(LeaveRequestStatuses.Rejected)]
    [InlineData(LeaveRequestStatuses.Closed)]
    public async Task RecordReturnAsync_ForAStudentWhoHasNotDeparted_IsRefused(
        string status)
    {
        var repository = new FakeLeaveRequestRepository();
        var request = repository.Seed(7, status);
        var service = LeaveTestData.CreateReviewService(
            repository,
            new FakeLeaveEventPublisher());

        var exception = await Assert.ThrowsAsync<InvalidLeaveStatusException>(
            () => service.RecordReturnAsync(
                request.LeaveRequestId,
                20,
                LeaveRoles.Warden));

        Assert.Contains("departed", exception.Message);
        Assert.Equal(status, repository.Requests[0].Status);
    }

    [Fact]
    public async Task MovementActions_ForAMissingRequest_ThrowNotFound()
    {
        var service = LeaveTestData.CreateReviewService(
            new FakeLeaveRequestRepository(),
            new FakeLeaveEventPublisher());

        await Assert.ThrowsAsync<LeaveRequestNotFoundException>(
            () => service.RecordDepartureAsync(404, 20, LeaveRoles.Warden));

        await Assert.ThrowsAsync<LeaveRequestNotFoundException>(
            () => service.RecordReturnAsync(404, 20, LeaveRoles.Warden));
    }

    [Fact]
    public async Task MovementActions_WhenAnotherStaffMemberActedFirst_AreRefused()
    {
        var repository = new FakeLeaveRequestRepository();
        var approved = repository.Seed(7, LeaveRequestStatuses.Approved);
        var departed = repository.Seed(8, LeaveRequestStatuses.Departed);
        repository.ForceTransitionConflict = true;

        var service = LeaveTestData.CreateReviewService(
            repository,
            new FakeLeaveEventPublisher());

        await Assert.ThrowsAsync<InvalidLeaveStatusException>(
            () => service.RecordDepartureAsync(
                approved.LeaveRequestId,
                20,
                LeaveRoles.Warden));

        await Assert.ThrowsAsync<InvalidLeaveStatusException>(
            () => service.RecordReturnAsync(
                departed.LeaveRequestId,
                20,
                LeaveRoles.Warden));
    }

    [Fact]
    public async Task FullLifecycle_PendingToClosed_IsAuditedInOrder()
    {
        var repository = new FakeLeaveRequestRepository();
        var request = repository.Seed(7);
        var publisher = new FakeLeaveEventPublisher();
        var service = LeaveTestData.CreateReviewService(repository, publisher);
        ulong id = request.LeaveRequestId;

        await service.DecideAsync(
            id,
            new DecideLeaveRequest { Decision = "APPROVE", Reason = "OK" },
            20,
            LeaveRoles.Warden);

        await service.RecordDepartureAsync(id, 20, LeaveRoles.Warden);
        var closed = await service.RecordReturnAsync(id, 21, LeaveRoles.HostelMaster);

        Assert.Equal(LeaveRequestStatuses.Closed, closed.Status);

        Assert.Equal(
            new[]
            {
                LeaveAuditActions.Approve,
                LeaveAuditActions.Depart,
                LeaveAuditActions.Return
            },
            repository.AuditEntries.Select(entry => entry.Action).ToArray());

        Assert.Equal(
            new[]
            {
                LeaveEventTypes.RequestApproved,
                LeaveEventTypes.StudentDeparted,
                LeaveEventTypes.StudentReturned
            },
            publisher.PublishedEvents.Select(e => e.EventType).ToArray());
    }

    [Fact]
    public async Task ClosedRequest_CannotBeReopenedOrDecidedAgain()
    {
        var repository = new FakeLeaveRequestRepository();
        var request = repository.Seed(7, LeaveRequestStatuses.Closed);
        var service = LeaveTestData.CreateReviewService(
            repository,
            new FakeLeaveEventPublisher());

        await Assert.ThrowsAsync<InvalidLeaveStatusException>(
            () => service.RecordReturnAsync(
                request.LeaveRequestId,
                20,
                LeaveRoles.Warden));

        await Assert.ThrowsAsync<InvalidLeaveStatusException>(
            () => service.DecideAsync(
                request.LeaveRequestId,
                new DecideLeaveRequest { Decision = "APPROVE", Reason = "OK" },
                20,
                LeaveRoles.Warden));
    }
}
