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
}