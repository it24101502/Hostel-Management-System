using LeaveService.DTOs;
using LeaveService.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace LeaveService.Tests.TestDoubles;

internal static class LeaveTestData
{
    /// <summary>
    /// The simulated "current time" used by the tests.
    /// </summary>
    public static readonly DateTimeOffset Now =
        new(2026, 9, 20, 10, 0, 0, TimeSpan.Zero);

    public static readonly DateOnly Today = new(2026, 9, 20);

    public static SubmitLeaveRequest ValidRequest()
    {
        return new SubmitLeaveRequest
        {
            DepartureDate = Today.AddDays(3),
            ExpectedReturnDate = Today.AddDays(5),
            Reason = "Family wedding",
            CompanionName = "Nimal Perera",
            CompanionRelationship = "Father",
            CompanionPhone = "0771234567"
        };
    }

    public static LeaveRequestService CreateRequestService(
        FakeLeaveRequestRepository repository,
        FakeLeaveEventPublisher publisher)
    {
        return new LeaveRequestService(
            repository,
            publisher,
            new FixedTimeProvider(Now),
            NullLogger<LeaveRequestService>.Instance);
    }
}
