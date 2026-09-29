using LeaveService.DTOs;
using LeaveService.Exceptions;
using LeaveService.Services;
using LeaveService.Tests.TestDoubles;

namespace LeaveService.Tests;

public class LeaveRequestValidationTests
{
    [Theory]
    [InlineData("reason", null)]
    [InlineData("reason", "")]
    [InlineData("reason", "   ")]
    [InlineData("companionName", null)]
    [InlineData("companionName", "")]
    [InlineData("companionName", "   ")]
    [InlineData("companionRelationship", null)]
    [InlineData("companionRelationship", "")]
    [InlineData("companionRelationship", "   ")]
    [InlineData("companionPhone", null)]
    [InlineData("companionPhone", "")]
    [InlineData("companionPhone", "   ")]
    public async Task SubmitAsync_WithMissingRequiredText_IsRejected(
        string fieldName,
        string? value)
    {
        var request = LeaveTestData.ValidRequest();
        SetTextField(request, fieldName, value);

        var exception = await SubmitExpectingValidationErrorAsync(request);

        Assert.True(exception.Errors.ContainsKey(fieldName));
        Assert.Single(exception.Errors);
    }

    [Fact]
    public async Task SubmitAsync_WithMissingDepartureDate_IsRejected()
    {
        var request = LeaveTestData.ValidRequest();
        request.DepartureDate = null;

        var exception = await SubmitExpectingValidationErrorAsync(request);

        Assert.True(exception.Errors.ContainsKey("departureDate"));
    }

    [Fact]
    public async Task SubmitAsync_WithMissingExpectedReturnDate_IsRejected()
    {
        var request = LeaveTestData.ValidRequest();
        request.ExpectedReturnDate = null;

        var exception = await SubmitExpectingValidationErrorAsync(request);

        Assert.True(exception.Errors.ContainsKey("expectedReturnDate"));
    }

    [Fact]
    public async Task SubmitAsync_WithEmptyRequest_ReportsEveryRequiredField()
    {
        var exception = await SubmitExpectingValidationErrorAsync(
            new SubmitLeaveRequest());

        Assert.Equal(6, exception.Errors.Count);
        Assert.True(exception.Errors.ContainsKey("departureDate"));
        Assert.True(exception.Errors.ContainsKey("expectedReturnDate"));
        Assert.True(exception.Errors.ContainsKey("reason"));
        Assert.True(exception.Errors.ContainsKey("companionName"));
        Assert.True(exception.Errors.ContainsKey("companionRelationship"));
        Assert.True(exception.Errors.ContainsKey("companionPhone"));
    }

    [Fact]
    public async Task SubmitAsync_WithReturnDateEarlierThanDeparture_IsRejected()
    {
        var request = LeaveTestData.ValidRequest();
        request.DepartureDate = LeaveTestData.Today.AddDays(5);
        request.ExpectedReturnDate = LeaveTestData.Today.AddDays(4);

        var exception = await SubmitExpectingValidationErrorAsync(request);

        Assert.Contains(
            "earlier than the departure date",
            exception.Errors["expectedReturnDate"][0]);
    }

    [Fact]
    public async Task SubmitAsync_WithDepartureDateInThePast_IsRejected()
    {
        var request = LeaveTestData.ValidRequest();
        request.DepartureDate = LeaveTestData.Today.AddDays(-1);
        request.ExpectedReturnDate = LeaveTestData.Today.AddDays(2);

        var exception = await SubmitExpectingValidationErrorAsync(request);

        Assert.Contains(
            "in the past",
            exception.Errors["departureDate"][0]);
    }

    [Fact]
    public async Task SubmitAsync_WithReasonAtMaximumLength_IsAccepted()
    {
        var repository = new FakeLeaveRequestRepository();
        var service = LeaveTestData.CreateRequestService(
            repository,
            new FakeLeaveEventPublisher());

        var request = LeaveTestData.ValidRequest();
        request.Reason = new string('a', LeaveRequestService.MaxReasonLength);

        var result = await service.SubmitAsync(request, 7, "student7");

        Assert.Equal(LeaveRequestService.MaxReasonLength, result.Reason.Length);
    }

    [Fact]
    public async Task SubmitAsync_WithReasonOverMaximumLength_IsRejected()
    {
        var request = LeaveTestData.ValidRequest();
        request.Reason =
            new string('a', LeaveRequestService.MaxReasonLength + 1);

        var exception = await SubmitExpectingValidationErrorAsync(request);

        Assert.True(exception.Errors.ContainsKey("reason"));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12345")]
    [InlineData("077-ABC-1234")]
    [InlineData("1234567890123456")]
    [InlineData("+94 77 123 4567 ext 12345")]
    public async Task SubmitAsync_WithInvalidPhoneNumber_IsRejected(
        string phone)
    {
        var request = LeaveTestData.ValidRequest();
        request.CompanionPhone = phone;

        var exception = await SubmitExpectingValidationErrorAsync(request);

        Assert.True(exception.Errors.ContainsKey("companionPhone"));
    }

    [Theory]
    [InlineData("0771234567")]
    [InlineData("+94 77 123 4567")]
    [InlineData("(011) 234-5678")]
    public async Task SubmitAsync_WithValidPhoneNumber_IsAccepted(
        string phone)
    {
        var repository = new FakeLeaveRequestRepository();
        var service = LeaveTestData.CreateRequestService(
            repository,
            new FakeLeaveEventPublisher());

        var request = LeaveTestData.ValidRequest();
        request.CompanionPhone = phone;

        var result = await service.SubmitAsync(request, 7, "student7");

        Assert.Equal(phone, result.CompanionPhone);
    }

    [Fact]
    public async Task SubmitAsync_WhenValidationFails_DoesNotSaveNotifyOrPublish()
    {
        var repository = new FakeLeaveRequestRepository();
        var publisher = new FakeLeaveEventPublisher();
        var service = LeaveTestData.CreateRequestService(
            repository,
            publisher);

        await Assert.ThrowsAsync<LeaveRequestValidationException>(
            () => service.SubmitAsync(
                new SubmitLeaveRequest(),
                7,
                "student7"));

        Assert.Equal(0, repository.CreateCallCount);
        Assert.Null(repository.LastNotification);
        Assert.Empty(publisher.PublishedEvents);
    }

    private static async Task<LeaveRequestValidationException>
        SubmitExpectingValidationErrorAsync(SubmitLeaveRequest request)
    {
        var service = LeaveTestData.CreateRequestService(
            new FakeLeaveRequestRepository(),
            new FakeLeaveEventPublisher());

        return await Assert.ThrowsAsync<LeaveRequestValidationException>(
            () => service.SubmitAsync(request, 7, "student7"));
    }

    private static void SetTextField(
        SubmitLeaveRequest request,
        string fieldName,
        string? value)
    {
        switch (fieldName)
        {
            case "reason":
                request.Reason = value;
                break;
            case "companionName":
                request.CompanionName = value;
                break;
            case "companionRelationship":
                request.CompanionRelationship = value;
                break;
            case "companionPhone":
                request.CompanionPhone = value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(fieldName));
        }
    }
}
