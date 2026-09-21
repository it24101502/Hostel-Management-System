using LeaveService.Models;
using LeaveService.Repositories;

namespace LeaveService.Tests.TestDoubles;

/// <summary>
/// An in-memory stand-in for the MySQL repository.
/// </summary>
internal sealed class FakeLeaveRequestRepository : ILeaveRequestRepository
{
    private ulong _nextId = 1;

    public List<LeaveRequest> Requests { get; } = new();

    public int CreateCallCount { get; private set; }

    public NewLeaveRequest? LastNewRequest { get; private set; }

    public NewLeaveNotification? LastNotification { get; private set; }

    public DateTime? LastOccurredAtUtc { get; private set; }

    public Exception? CreateException { get; set; }

    public LeaveRequest Seed(
        ulong studentUserId,
        string status = LeaveRequestStatuses.Pending,
        string studentUsername = "student")
    {
        var request = new LeaveRequest
        {
            LeaveRequestId = _nextId++,
            StudentUserId = studentUserId,
            StudentUsername = studentUsername,
            DepartureDate = new DateOnly(2026, 10, 1),
            ExpectedReturnDate = new DateOnly(2026, 10, 3),
            Reason = "Family visit",
            CompanionName = "Nimal Perera",
            CompanionRelationship = "Father",
            CompanionPhone = "0771234567",
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        Requests.Add(request);

        return request;
    }

    public Task<LeaveRequest> CreateWithNotificationAsync(
        NewLeaveRequest request,
        NewLeaveNotification notification,
        DateTime occurredAtUtc)
    {
        if (CreateException is not null)
        {
            throw CreateException;
        }

        CreateCallCount++;
        LastNewRequest = request;
        LastNotification = notification;
        LastOccurredAtUtc = occurredAtUtc;

        var created = new LeaveRequest
        {
            LeaveRequestId = _nextId++,
            StudentUserId = request.StudentUserId,
            StudentUsername = request.StudentUsername,
            DepartureDate = request.DepartureDate,
            ExpectedReturnDate = request.ExpectedReturnDate,
            Reason = request.Reason,
            CompanionName = request.CompanionName,
            CompanionRelationship = request.CompanionRelationship,
            CompanionPhone = request.CompanionPhone,
            Status = LeaveRequestStatuses.Pending,
            CreatedAt = occurredAtUtc,
            UpdatedAt = occurredAtUtc
        };

        Requests.Add(created);

        return Task.FromResult(created);
    }

    public Task<LeaveRequest?> GetByIdAsync(ulong leaveRequestId)
    {
        return Task.FromResult(
            Requests.FirstOrDefault(
                request => request.LeaveRequestId == leaveRequestId));
    }

    public Task<IReadOnlyList<LeaveRequest>> GetByStudentAsync(
        ulong studentUserId)
    {
        IReadOnlyList<LeaveRequest> result = Requests
            .Where(request => request.StudentUserId == studentUserId)
            .OrderByDescending(request => request.LeaveRequestId)
            .ToList();

        return Task.FromResult(result);
    }
}
