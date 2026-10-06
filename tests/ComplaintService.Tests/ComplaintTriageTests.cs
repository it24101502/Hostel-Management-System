using ComplaintService.DTOs;
using ComplaintService.Exceptions;
using ComplaintService.Models;
using ComplaintService.Services;
using ComplaintService.Tests.TestDoubles;
using NSubstitute;
using Xunit;

namespace ComplaintService.Tests;

public class ComplaintTriageTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 6, 10, 0, 0, TimeSpan.Zero);

    private static StaffComplaintService CreateService(
        FakeComplaintRepository repository,
        INotificationPublisher? notificationPublisher = null)
    {
        var publisher = notificationPublisher ?? Substitute.For<INotificationPublisher>();
        return new StaffComplaintService(repository, new FixedTimeProvider(Now), publisher);
    }

    [Fact]
    public async Task Assign_WithoutAssignee_AssignsToTheActingStaffMember()
    {
        var repository = new FakeComplaintRepository();
        var complaint = repository.Seed(7);

        var result = await CreateService(repository).AssignAsync(
            complaint.ComplaintId, new AssignComplaintRequest(),
            actorUserId: 20, actorRole: ComplaintRoles.Warden);

        Assert.Equal((ulong)20, result.AssignedToUserId);
        Assert.Equal(Now.UtcDateTime, result.AssignedAt);
    }

    [Fact]
    public async Task Assign_ToAnotherStaffMember_RecordsAuditEntry()
    {
        var repository = new FakeComplaintRepository();
        var complaint = repository.Seed(7);

        var result = await CreateService(repository).AssignAsync(
            complaint.ComplaintId,
            new AssignComplaintRequest { AssignedToUserId = 31 },
            20, ComplaintRoles.Warden);

        Assert.Equal((ulong)31, result.AssignedToUserId);

        var audit = Assert.Single(repository.AuditEntries);
        Assert.Equal(ComplaintAuditActions.Assign, audit.Action);
        Assert.Equal((ulong)20, audit.ActorUserId);
    }

    [Fact]
    public async Task Assign_MissingComplaint_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<ComplaintNotFoundException>(
            () => CreateService(new FakeComplaintRepository()).AssignAsync(
                404, new AssignComplaintRequest(), 20,
                ComplaintRoles.Warden));
    }

    [Fact]
    public async Task ChangeStatus_OpenToInProgress_Succeeds()
    {
        var repository = new FakeComplaintRepository();
        var complaint = repository.Seed(7);

        var result = await CreateService(repository).ChangeStatusAsync(
            complaint.ComplaintId,
            new UpdateComplaintStatusRequest { Status = "in_progress" },
            20, ComplaintRoles.Warden);

        Assert.Equal(ComplaintStatuses.InProgress, result.Status);
        Assert.Null(result.ResolvedAt);
    }

    [Fact]
    public async Task ChangeStatus_ToResolved_SetsResolvedAtAndAudits()
    {
        var repository = new FakeComplaintRepository();
        var complaint = repository.Seed(7, ComplaintStatuses.InProgress);

        var result = await CreateService(repository).ChangeStatusAsync(
            complaint.ComplaintId,
            new UpdateComplaintStatusRequest
            {
                Status = "RESOLVED",
                Remarks = "Tap replaced."
            },
            20, ComplaintRoles.HostelMaster);

        Assert.Equal(ComplaintStatuses.Resolved, result.Status);
        Assert.Equal(Now.UtcDateTime, result.ResolvedAt);

        var audit = Assert.Single(repository.AuditEntries);
        Assert.Equal(ComplaintStatuses.InProgress, audit.FromStatus);
        Assert.Equal(ComplaintStatuses.Resolved, audit.ToStatus);
        Assert.Equal("Tap replaced.", audit.Remarks);
    }

    [Fact]
    public async Task ChangeStatus_ReopeningResolved_ClearsResolvedAt()
    {
        var repository = new FakeComplaintRepository();
        var complaint = repository.Seed(7, ComplaintStatuses.Resolved);

        var result = await CreateService(repository).ChangeStatusAsync(
            complaint.ComplaintId,
            new UpdateComplaintStatusRequest { Status = "OPEN" },
            20, ComplaintRoles.Warden);

        Assert.Equal(ComplaintStatuses.Open, result.Status);
        Assert.Null(result.ResolvedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("DONE")]
    public async Task ChangeStatus_WithInvalidStatus_IsRejected(string? status)
    {
        var repository = new FakeComplaintRepository();
        var complaint = repository.Seed(7);

        var exception = await Assert.ThrowsAsync<ComplaintValidationException>(
            () => CreateService(repository).ChangeStatusAsync(
                complaint.ComplaintId,
                new UpdateComplaintStatusRequest { Status = status },
                20, ComplaintRoles.Warden));

        Assert.True(exception.Errors.ContainsKey("status"));
        Assert.Empty(repository.AuditEntries);
    }

    [Fact]
    public async Task ChangeStatus_ToTheSameStatus_IsRefused()
    {
        var repository = new FakeComplaintRepository();
        var complaint = repository.Seed(7);

        await Assert.ThrowsAsync<InvalidComplaintStatusException>(
            () => CreateService(repository).ChangeStatusAsync(
                complaint.ComplaintId,
                new UpdateComplaintStatusRequest { Status = "OPEN" },
                20, ComplaintRoles.Warden));
    }

    [Fact]
    public async Task ChangeStatus_WhenSomeoneElseChangedItFirst_IsRefused()
    {
        var repository = new FakeComplaintRepository
        {
            ForceStatusConflict = true
        };
        var complaint = repository.Seed(7);

        await Assert.ThrowsAsync<InvalidComplaintStatusException>(
            () => CreateService(repository).ChangeStatusAsync(
                complaint.ComplaintId,
                new UpdateComplaintStatusRequest { Status = "IN_PROGRESS" },
                20, ComplaintRoles.Warden));
    }

    [Fact]
    public async Task GetComplaints_FiltersByStatusAndCategory()
    {
        var repository = new FakeComplaintRepository();
        repository.Seed(1, ComplaintStatuses.Open, "PLUMBING");
        repository.Seed(2, ComplaintStatuses.Resolved, "PLUMBING");
        repository.Seed(3, ComplaintStatuses.Open, "NOISE");

        var result = await CreateService(repository)
            .GetComplaintsAsync("open", "plumbing");

        Assert.Equal((ulong)1, Assert.Single(result).StudentUserId);
    }

    [Fact]
    public async Task GetComplaints_WithUnknownStatus_IsRejected()
    {
        await Assert.ThrowsAsync<ComplaintValidationException>(
            () => CreateService(new FakeComplaintRepository())
                .GetComplaintsAsync("nonsense", null));
    }

    [Fact]
    public async Task ChangeStatus_WhenPublisherFails_StillReturnsUpdatedComplaint()
    {
        var repository = new FakeComplaintRepository();
        var complaint = repository.Seed(7);

        var publisher = Substitute.For<INotificationPublisher>();
        publisher.PublishStatusChangeNotificationAsync(
                Arg.Any<ComplaintStatusChangedEvent>())
            .Returns(Task.FromException(new InvalidOperationException("Kafka is down.")));

        var result = await CreateService(repository, publisher).ChangeStatusAsync(
            complaint.ComplaintId,
            new UpdateComplaintStatusRequest { Status = "IN_PROGRESS" },
            20, ComplaintRoles.Warden);

        Assert.Equal(ComplaintStatuses.InProgress, result.Status);
    }
}