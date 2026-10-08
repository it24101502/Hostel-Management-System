using ComplaintService.DTOs;
using ComplaintService.Exceptions;
using ComplaintService.Models;
using ComplaintService.Repositories;
using ComplaintService.Services;
using NSubstitute;
using Xunit;

namespace ComplaintService.Tests;

public class ComplaintManagerTests
{
    [Fact]
    public async Task UpdateStatus_WhenStatusChanges_TriggersNotification()
    {
        // Arrange
        var mockRepo = Substitute.For<IComplaintRepository>();
        var mockPublisher = Substitute.For<INotificationPublisher>();
        var mockTimeProvider = TimeProvider.System;

        ulong complaintId = 101;
        ulong actorUserId = 1;
        string actorRole = "STAFF";

        var existingComplaint = new Complaint
        {
            ComplaintId = complaintId,
            StudentUserId = 500,
            Status = "OPEN"
        };

        mockRepo.GetByIdAsync(complaintId).Returns(existingComplaint);
        mockRepo.TryChangeStatusAsync(
            complaintId,
            "OPEN",
            "RESOLVED",
            actorUserId,
            actorRole,
            Arg.Any<string?>(),
            Arg.Any<DateTime>())
            .Returns(true);

        var service = new StaffComplaintService(mockRepo, mockTimeProvider, mockPublisher);
        var request = new UpdateComplaintStatusRequest { Status = "RESOLVED" };

        // Act
        await service.ChangeStatusAsync(complaintId, request, actorUserId, actorRole);

        // Assert
        await mockPublisher.Received(1).PublishStatusChangeNotificationAsync(
            Arg.Is<ComplaintStatusChangedEvent>(e => e.NewStatus == "RESOLVED")
        );
    }

    [Fact]
    public async Task UpdateStatus_WhenStatusIsSame_DoesNotTriggerNotification()
    {
        // Arrange
        var mockRepo = Substitute.For<IComplaintRepository>();
        var mockPublisher = Substitute.For<INotificationPublisher>();
        var mockTimeProvider = TimeProvider.System;

        ulong complaintId = 101;
        ulong actorUserId = 1;
        string actorRole = "STAFF";

        var existingComplaint = new Complaint
        {
            ComplaintId = complaintId,
            StudentUserId = 500,
            Status = "OPEN"
        };

        mockRepo.GetByIdAsync(complaintId).Returns(existingComplaint);

        var service = new StaffComplaintService(mockRepo, mockTimeProvider, mockPublisher);
        var request = new UpdateComplaintStatusRequest { Status = "OPEN" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidComplaintStatusException>(() =>
            service.ChangeStatusAsync(complaintId, request, actorUserId, actorRole));

        await mockPublisher.DidNotReceive().PublishStatusChangeNotificationAsync(
            Arg.Any<ComplaintStatusChangedEvent>()
        );
    }

    [Fact]
    public async Task GetComplaints_FilterByStatusAndCategory_ReturnsMatchingComplaints()
    {
        // Arrange
        var mockRepo = Substitute.For<IComplaintRepository>();
        var mockPublisher = Substitute.For<INotificationPublisher>();
        var mockTimeProvider = TimeProvider.System;

        var expectedComplaints = new List<Complaint>
        {
            new Complaint
            {
                ComplaintId = 101,
                StudentUserId = 500,
                Category = "PLUMBING",
                Status = "OPEN"
            }
        };

        mockRepo.GetFilteredAsync("OPEN", "PLUMBING").Returns(expectedComplaints);

        var service = new StaffComplaintService(mockRepo, mockTimeProvider, mockPublisher);

        // Act
        var result = await service.GetComplaintsAsync("open", "plumbing");

        // Assert
        Assert.Single(result);
        Assert.Equal("PLUMBING", result[0].Category);
        Assert.Equal("OPEN", result[0].Status);
        await mockRepo.Received(1).GetFilteredAsync("OPEN", "PLUMBING");
    }

    [Fact]
    public async Task GetComplaints_WithoutFilters_ReturnsAllComplaints()
    {
        // Arrange
        var mockRepo = Substitute.For<IComplaintRepository>();
        var mockPublisher = Substitute.For<INotificationPublisher>();
        var mockTimeProvider = TimeProvider.System;

        var expectedComplaints = new List<Complaint>
        {
            new Complaint { ComplaintId = 101, Category = "PLUMBING", Status = "OPEN" },
            new Complaint { ComplaintId = 102, Category = "ELECTRICAL", Status = "IN_PROGRESS" }
        };

        mockRepo.GetFilteredAsync(null, null).Returns(expectedComplaints);

        var service = new StaffComplaintService(mockRepo, mockTimeProvider, mockPublisher);

        // Act
        var result = await service.GetComplaintsAsync(null, null);

        // Assert
        Assert.Equal(2, result.Count);
        await mockRepo.Received(1).GetFilteredAsync(null, null);
    }
}