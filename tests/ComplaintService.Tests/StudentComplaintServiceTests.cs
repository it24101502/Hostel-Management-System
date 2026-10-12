using ComplaintService.DTOs;
using ComplaintService.Exceptions;
using ComplaintService.Models;
using ComplaintService.Repositories;
using ComplaintService.Services;
using NSubstitute;
using Xunit;

namespace ComplaintService.Tests;

public class StudentComplaintServiceTests
{
    private readonly IComplaintRepository _mockRepo = Substitute.For<IComplaintRepository>();
    private readonly TimeProvider _mockTimeProvider = System.TimeProvider.System;

    [Fact]
    public async Task SubmitAsync_ValidRequest_CreatesAndReturnsComplaint()
    {
        // Arrange
        var service = new StudentComplaintService(_mockRepo, _mockTimeProvider);
        ulong studentUserId = 500;
        string studentUsername = "johndoe";

        var request = new SubmitComplaintRequest
        {
            Category = ComplaintCategories.All.First(),
            Description = "Leaking pipe in room 204."
        };

        var createdComplaint = new Complaint
        {
            ComplaintId = 1,
            StudentUserId = studentUserId,
            StudentUsername = studentUsername,
            Category = request.Category.ToUpperInvariant(),
            Description = request.Description,
            Status = "OPEN",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockRepo.CreateAsync(Arg.Any<NewComplaint>(), Arg.Any<DateTime>())
                 .Returns(createdComplaint);

        // Act
        var result = await service.SubmitAsync(request, studentUserId, studentUsername);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1U, result.ComplaintId);
        Assert.Equal(studentUserId, result.StudentUserId);
        Assert.Equal("OPEN", result.Status);
    }

    [Fact]
    public async Task SubmitAsync_EmptyDescription_ThrowsComplaintValidationException()
    {
        // Arrange
        var service = new StudentComplaintService(_mockRepo, _mockTimeProvider);
        var request = new SubmitComplaintRequest
        {
            Category = ComplaintCategories.All.First(),
            Description = ""
        };

        // Act & Assert
        await Assert.ThrowsAsync<ComplaintValidationException>(() =>
            service.SubmitAsync(request, 500, "johndoe"));
    }

    [Fact]
    public async Task GetMyComplaintAsync_OtherStudentComplaint_ReturnsNull()
    {
        // Arrange
        var service = new StudentComplaintService(_mockRepo, _mockTimeProvider);
        ulong complaintId = 10;
        ulong requestingStudentId = 500;
        ulong ownerStudentId = 999;

        var existingComplaint = new Complaint
        {
            ComplaintId = complaintId,
            StudentUserId = ownerStudentId,
            Category = "MAINTENANCE",
            Description = "Issue description",
            Status = "OPEN"
        };

        _mockRepo.GetByIdAsync(complaintId).Returns(existingComplaint);

        // Act
        var result = await service.GetMyComplaintAsync(complaintId, requestingStudentId);

        // Assert
        Assert.Null(result);
    }
}