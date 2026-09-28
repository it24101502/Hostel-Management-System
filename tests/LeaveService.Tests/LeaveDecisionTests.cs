using LeaveService.DTOs;
using LeaveService.Events;
using LeaveService.Exceptions;
using LeaveService.Models;
using LeaveService.Services;
using LeaveService.Tests.TestDoubles;

namespace LeaveService.Tests;

public class LeaveDecisionTests
{
    [Fact]
    public async Task DecideAsync_Approve_MovesPendingRequestToApprovedWithReason()
    {
        var (repository, publisher, service, request) = Arrange();

        var result = await service.DecideAsync(
            request.LeaveRequestId,
            new DecideLeaveRequest
            {
                Decision = "APPROVE",
                Reason = "Guardian confirmed by phone."
            },
            actorUserId: 20,
            actorRole: LeaveRoles.Warden);

        Assert.Equal(LeaveRequestStatuses.Approved, result.Status);
        Assert.Equal("Guardian confirmed by phone.", result.DecisionReason);
        Assert.Equal(LeaveTestData.Now.UtcDateTime, result.DecidedAt);
        Assert.Equal(LeaveRequestStatuses.Approved, repository.Requests[0].Status);
        Assert.Equal((ulong)20, repository.Requests[0].DecidedByUserId);
    }

    [Fact]
    public async Task DecideAsync_Reject_MovesPendingRequestToRejectedWithReason()
    {
        var (_, _, service, request) = Arrange();

        var result = await service.DecideAsync(
            request.LeaveRequestId,
            new DecideLeaveRequest
            {
                Decision = "REJECT",
                Reason = "Exam week."
            },
            20,
            LeaveRoles.Warden);

        Assert.Equal(LeaveRequestStatuses.Rejected, result.Status);
        Assert.Equal("Exam week.", result.DecisionReason);
    }

    [Theory]
    [InlineData("approve")]
    [InlineData("  Approve  ")]
    public async Task DecideAsync_DecisionIsNotCaseOrSpaceSensitive(
        string decision)
    {
        var (_, _, service, request) = Arrange();

        var result = await service.DecideAsync(
            request.LeaveRequestId,
            new DecideLeaveRequest { Decision = decision, Reason = "OK" },
            20,
            LeaveRoles.Warden);

        Assert.Equal(LeaveRequestStatuses.Approved, result.Status);
    }

    [Fact]
    public async Task DecideAsync_RecordsDecisionInAuditLog()
    {
        var (repository, _, service, request) = Arrange();

        await service.DecideAsync(
            request.LeaveRequestId,
            new DecideLeaveRequest { Decision = "REJECT", Reason = "Exam week." },
            actorUserId: 21,
            actorRole: LeaveRoles.HostelMaster);

        var audit = Assert.Single(repository.AuditEntries);

        Assert.Equal(LeaveAuditActions.Reject, audit.Action);
        Assert.Equal((ulong)21, audit.ActorUserId);
        Assert.Equal(LeaveRoles.HostelMaster, audit.ActorRole);
        Assert.Equal(LeaveRequestStatuses.Pending, audit.FromStatus);
        Assert.Equal(LeaveRequestStatuses.Rejected, audit.ToStatus);
        Assert.Equal("Exam week.", audit.Remarks);
    }

    [Theory]
    [InlineData("APPROVE", LeaveEventTypes.RequestApproved)]
    [InlineData("REJECT", LeaveEventTypes.RequestRejected)]
    public async Task DecideAsync_PublishesMatchingEvent(
        string decision,
        string expectedEventType)
    {
        var (_, publisher, service, request) = Arrange();

        await service.DecideAsync(
            request.LeaveRequestId,
            new DecideLeaveRequest { Decision = decision, Reason = "Reason" },
            20,
            LeaveRoles.Warden);

        LeaveEvent published = Assert.Single(publisher.PublishedEvents);

        Assert.Equal(expectedEventType, published.EventType);
        Assert.Equal(request.LeaveRequestId, published.LeaveRequestId);
        Assert.Equal((ulong)7, published.StudentUserId);
        Assert.Equal((ulong)20, published.ActorUserId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DecideAsync_WithoutAReason_IsRejectedAndChangesNothing(
        string? reason)
    {
        var (repository, publisher, service, request) = Arrange();

        var exception = await Assert.ThrowsAsync<LeaveRequestValidationException>(
            () => service.DecideAsync(
                request.LeaveRequestId,
                new DecideLeaveRequest { Decision = "APPROVE", Reason = reason },
                20,
                LeaveRoles.Warden));

        Assert.True(exception.Errors.ContainsKey("reason"));
        Assert.Equal(LeaveRequestStatuses.Pending, repository.Requests[0].Status);
        Assert.Empty(repository.AuditEntries);
        Assert.Empty(publisher.PublishedEvents);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("MAYBE")]
    public async Task DecideAsync_WithInvalidDecision_IsRejected(
        string? decision)
    {
        var (repository, _, service, request) = Arrange();

        var exception = await Assert.ThrowsAsync<LeaveRequestValidationException>(
            () => service.DecideAsync(
                request.LeaveRequestId,
                new DecideLeaveRequest { Decision = decision, Reason = "Reason" },
                20,
                LeaveRoles.Warden));

        Assert.True(exception.Errors.ContainsKey("decision"));
        Assert.Equal(LeaveRequestStatuses.Pending, repository.Requests[0].Status);
    }

    [Fact]
    public async Task DecideAsync_ReasonAtMaximumLength_IsAccepted()
    {
        var (_, _, service, request) = Arrange();

        var result = await service.DecideAsync(
            request.LeaveRequestId,
            new DecideLeaveRequest
            {
                Decision = "APPROVE",
                Reason = new string('a', LeaveReviewService.MaxDecisionReasonLength)
            },
            20,
            LeaveRoles.Warden);

        Assert.Equal(
            LeaveReviewService.MaxDecisionReasonLength,
            result.DecisionReason!.Length);
    }

    [Fact]
    public async Task DecideAsync_ReasonOverMaximumLength_IsRejected()
    {
        var (_, _, service, request) = Arrange();

        var exception = await Assert.ThrowsAsync<LeaveRequestValidationException>(
            () => service.DecideAsync(
                request.LeaveRequestId,
                new DecideLeaveRequest
                {
                    Decision = "APPROVE",
                    Reason = new string(
                        'a',
                        LeaveReviewService.MaxDecisionReasonLength + 1)
                },
                20,
                LeaveRoles.Warden));

        Assert.True(exception.Errors.ContainsKey("reason"));
    }

    [Theory]
    [InlineData(LeaveRequestStatuses.Approved)]
    [InlineData(LeaveRequestStatuses.Rejected)]
    [InlineData(LeaveRequestStatuses.Departed)]
    [InlineData(LeaveRequestStatuses.Closed)]
    public async Task DecideAsync_ForARequestThatIsNotPending_IsRefused(
        string status)
    {
        var repository = new FakeLeaveRequestRepository();
        var request = repository.Seed(7, status);
        var publisher = new FakeLeaveEventPublisher();
        var service = LeaveTestData.CreateReviewService(repository, publisher);

        var exception = await Assert.ThrowsAsync<InvalidLeaveStatusException>(
            () => service.DecideAsync(
                request.LeaveRequestId,
                new DecideLeaveRequest { Decision = "REJECT", Reason = "Changed my mind" },
                20,
                LeaveRoles.Warden));

        Assert.Contains("Only pending", exception.Message);
        Assert.Equal(status, repository.Requests[0].Status);
        Assert.Empty(repository.AuditEntries);
        Assert.Empty(publisher.PublishedEvents);
    }

    [Fact]
    public async Task DecideAsync_ForAMissingRequest_ThrowsNotFound()
    {
        var (_, _, service, _) = Arrange();

        await Assert.ThrowsAsync<LeaveRequestNotFoundException>(
            () => service.DecideAsync(
                404,
                new DecideLeaveRequest { Decision = "APPROVE", Reason = "OK" },
                20,
                LeaveRoles.Warden));
    }

    [Fact]
    public async Task DecideAsync_WhenAnotherStaffMemberDecidedFirst_IsRefused()
    {
        var (repository, publisher, service, request) = Arrange();
        repository.ForceTransitionConflict = true;

        var exception = await Assert.ThrowsAsync<InvalidLeaveStatusException>(
            () => service.DecideAsync(
                request.LeaveRequestId,
                new DecideLeaveRequest { Decision = "APPROVE", Reason = "OK" },
                20,
                LeaveRoles.Warden));

        Assert.Contains("updated by someone else", exception.Message);
        Assert.Empty(publisher.PublishedEvents);
    }

    [Fact]
    public async Task DecideAsync_WhenEventPublisherFails_StillReturnsTheDecision()
    {
        var (_, publisher, service, request) = Arrange();
        publisher.ExceptionToThrow = new InvalidOperationException("Kafka is down.");

        var result = await service.DecideAsync(
            request.LeaveRequestId,
            new DecideLeaveRequest { Decision = "APPROVE", Reason = "OK" },
            20,
            LeaveRoles.Warden);

        Assert.Equal(LeaveRequestStatuses.Approved, result.Status);
    }

    private static (
        FakeLeaveRequestRepository Repository,
        FakeLeaveEventPublisher Publisher,
        LeaveReviewService Service,
        LeaveRequest Request) Arrange()
    {
        var repository = new FakeLeaveRequestRepository();
        var request = repository.Seed(studentUserId: 7);
        var publisher = new FakeLeaveEventPublisher();
        var service = LeaveTestData.CreateReviewService(repository, publisher);

        return (repository, publisher, service, request);
    }
}
