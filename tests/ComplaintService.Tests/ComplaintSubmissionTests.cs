using ComplaintService.DTOs;
using ComplaintService.Exceptions;
using ComplaintService.Models;
using ComplaintService.Services;
using ComplaintService.Tests.TestDoubles;

namespace ComplaintService.Tests;

public class ComplaintSubmissionTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 5, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SubmitAsync_WithValidComplaint_ReturnsOpenComplaint()
    {
        var repository = new FakeComplaintRepository();
        var service = CreateService(repository);

        var result = await service.SubmitAsync(
            new SubmitComplaintRequest
            {
                Category = "PLUMBING",
                Description = "The tap in room 101 is leaking."
            },
            studentUserId: 7,
            studentUsername: "student7");

        Assert.Equal(ComplaintStatuses.Open, result.Status);
        Assert.Equal((ulong)7, result.StudentUserId);
        Assert.Equal("student7", result.StudentUsername);
        Assert.Equal("PLUMBING", result.Category);
        Assert.Equal(Now.UtcDateTime, result.CreatedAt);
        Assert.Equal(1, repository.CreateCallCount);
    }

    [Fact]
    public async Task SubmitAsync_TrimsDescriptionAndNormalisesCategory()
    {
        var repository = new FakeComplaintRepository();
        var service = CreateService(repository);

        await service.SubmitAsync(
            new SubmitComplaintRequest
            {
                Category = "  plumbing ",
                Description = "  Leaking tap  "
            },
            7,
            "student7");

        Assert.Equal("PLUMBING", repository.LastNewComplaint!.Category);
        Assert.Equal("Leaking tap", repository.LastNewComplaint.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SubmitAsync_WithEmptyDescription_IsRejected(
        string? description)
    {
        var exception = await SubmitExpectingValidationErrorAsync(
            new SubmitComplaintRequest
            {
                Category = "PLUMBING",
                Description = description
            });

        Assert.True(exception.Errors.ContainsKey("description"));
        Assert.Single(exception.Errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("NOT_A_CATEGORY")]
    public async Task SubmitAsync_WithMissingOrInvalidCategory_IsRejected(
        string? category)
    {
        var exception = await SubmitExpectingValidationErrorAsync(
            new SubmitComplaintRequest
            {
                Category = category,
                Description = "Something is broken."
            });

        Assert.True(exception.Errors.ContainsKey("category"));
        Assert.Single(exception.Errors);
    }

    [Fact]
    public async Task SubmitAsync_WithDescriptionOverMaximumLength_IsRejected()
    {
        var exception = await SubmitExpectingValidationErrorAsync(
            new SubmitComplaintRequest
            {
                Category = "OTHER",
                Description = new string(
                    'a',
                    StudentComplaintService.MaxDescriptionLength + 1)
            });

        Assert.True(exception.Errors.ContainsKey("description"));
    }

    [Fact]
    public async Task SubmitAsync_WhenValidationFails_DoesNotSave()
    {
        var repository = new FakeComplaintRepository();
        var service = CreateService(repository);

        await Assert.ThrowsAsync<ComplaintValidationException>(
            () => service.SubmitAsync(
                new SubmitComplaintRequest(),
                7,
                "student7"));

        Assert.Equal(0, repository.CreateCallCount);
    }

    [Fact]
    public async Task GetMyComplaintAsync_ForAnotherStudentsComplaint_ReturnsNull()
    {
        var repository = new FakeComplaintRepository();
        var service = CreateService(repository);

        var other = await service.SubmitAsync(
            new SubmitComplaintRequest
            {
                Category = "NOISE",
                Description = "Loud music at night."
            },
            studentUserId: 8,
            studentUsername: "student8");

        var result = await service.GetMyComplaintAsync(other.ComplaintId, 7);

        Assert.Null(result);
    }

    private static StudentComplaintService CreateService(
        FakeComplaintRepository repository)
    {
        return new StudentComplaintService(
            repository,
            new FixedTimeProvider(Now));
    }

    private static async Task<ComplaintValidationException>
        SubmitExpectingValidationErrorAsync(SubmitComplaintRequest request)
    {
        var service = CreateService(new FakeComplaintRepository());

        return await Assert.ThrowsAsync<ComplaintValidationException>(
            () => service.SubmitAsync(request, 7, "student7"));
    }

    [Fact]
    public async Task MarkNotificationReadAsync_ForOwnNotification_ReturnsTrueAndRecordsIt()
    {
        var repository = new FakeComplaintRepository();
        var service = CreateService(repository);

        bool found = await service.MarkNotificationReadAsync(5, studentUserId: 7);

        Assert.True(found);
        Assert.Equal((5UL, 7UL), Assert.Single(repository.ReadMarks));
    }

    [Fact]
    public async Task MarkNotificationReadAsync_ForMissingOrForeignNotification_ReturnsFalse()
    {
        var repository = new FakeComplaintRepository { NotificationExists = false };
        var service = CreateService(repository);

        Assert.False(await service.MarkNotificationReadAsync(404, studentUserId: 7));
    }
}
