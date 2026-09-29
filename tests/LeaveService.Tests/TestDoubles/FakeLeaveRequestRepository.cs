using LeaveService.Models;
using LeaveService.Repositories;

namespace LeaveService.Tests.TestDoubles;

/// <summary>
/// An in-memory stand-in for the MySQL repository. The Try...
/// methods copy the real behaviour: they only succeed when the
/// request is still in the expected status.
/// </summary>
internal sealed class FakeLeaveRequestRepository : ILeaveRequestRepository
{
    private ulong _nextId = 1;

    public sealed record AuditEntry(
        ulong LeaveRequestId,
        string Action,
        ulong? ActorUserId,
        string ActorRole,
        string FromStatus,
        string ToStatus,
        string? Remarks,
        DateTime OccurredAtUtc);

    public List<LeaveRequest> Requests { get; } = new();

    public List<AuditEntry> AuditEntries { get; } = new();

    public List<NewLeaveNotification> OverdueNotifications { get; } = new();

    public int CreateCallCount { get; private set; }

    public NewLeaveRequest? LastNewRequest { get; private set; }

    public NewLeaveNotification? LastNotification { get; private set; }

    public DateTime? LastOccurredAtUtc { get; private set; }

    public LeaveRequestFilter? LastFilter { get; private set; }

    public (DateOnly? From, DateOnly? To)? LastTotalsRange { get; private set; }

    public Exception? CreateException { get; set; }

    // Simulates another staff member changing the request between
    // the service's read and its update.
    public bool ForceTransitionConflict { get; set; }

    public HashSet<ulong> FailFlaggingFor { get; } = new();

    public LeaveRequest Seed(
        ulong studentUserId,
        string status = LeaveRequestStatuses.Pending,
        string studentUsername = "student",
        DateOnly? departureDate = null,
        DateOnly? expectedReturnDate = null)
    {
        DateTime seededAt = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

        var request = new LeaveRequest
        {
            LeaveRequestId = _nextId++,
            StudentUserId = studentUserId,
            StudentUsername = studentUsername,
            DepartureDate = departureDate ?? new DateOnly(2026, 10, 1),
            ExpectedReturnDate = expectedReturnDate ?? new DateOnly(2026, 10, 3),
            Reason = "Family visit",
            CompanionName = "Nimal Perera",
            CompanionRelationship = "Father",
            CompanionPhone = "0771234567",
            Status = status,
            CreatedAt = seededAt,
            UpdatedAt = seededAt
        };

        // Give the request the data its status implies, like the
        // database constraints require.
        if (status != LeaveRequestStatuses.Pending)
        {
            request.DecidedByUserId = 20;
            request.DecisionReason = "Seeded decision";
            request.DecidedAt = seededAt;
        }

        if (status == LeaveRequestStatuses.Departed ||
            status == LeaveRequestStatuses.Closed)
        {
            request.ActualDepartureAt = seededAt;
        }

        if (status == LeaveRequestStatuses.Closed)
        {
            request.ActualReturnAt = seededAt.AddDays(1);
        }

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

    public Task<IReadOnlyList<LeaveRequest>> GetFilteredAsync(
        LeaveRequestFilter filter,
        DateOnly today)
    {
        LastFilter = filter;

        IReadOnlyList<LeaveRequest> result = Requests
            .Where(request =>
                (filter.Status is null || request.Status == filter.Status) &&
                (filter.FromDate is null || request.DepartureDate >= filter.FromDate) &&
                (filter.ToDate is null || request.DepartureDate <= filter.ToDate) &&
                (!filter.OverdueOnly || IsOverdue(request, today)))
            .OrderByDescending(request => request.LeaveRequestId)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<LeaveStatusTotals> GetStatusTotalsAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        DateOnly today)
    {
        LastTotalsRange = (fromDate, toDate);

        var inRange = Requests
            .Where(request =>
                (fromDate is null || request.DepartureDate >= fromDate) &&
                (toDate is null || request.DepartureDate <= toDate))
            .ToList();

        return Task.FromResult(new LeaveStatusTotals
        {
            Total = inRange.Count,
            Pending = inRange.Count(r => r.Status == LeaveRequestStatuses.Pending),
            Approved = inRange.Count(r => r.Status == LeaveRequestStatuses.Approved),
            Rejected = inRange.Count(r => r.Status == LeaveRequestStatuses.Rejected),
            Departed = inRange.Count(r => r.Status == LeaveRequestStatuses.Departed),
            Closed = inRange.Count(r => r.Status == LeaveRequestStatuses.Closed),
            Overdue = inRange.Count(r => IsOverdue(r, today))
        });
    }

    public Task<bool> TryDecideAsync(
        ulong leaveRequestId,
        string newStatus,
        ulong actorUserId,
        string actorRole,
        string reason,
        DateTime occurredAtUtc)
    {
        LeaveRequest? request = Find(leaveRequestId);

        if (ForceTransitionConflict ||
            request is null ||
            request.Status != LeaveRequestStatuses.Pending)
        {
            return Task.FromResult(false);
        }

        request.Status = newStatus;
        request.DecidedByUserId = actorUserId;
        request.DecisionReason = reason;
        request.DecidedAt = occurredAtUtc;

        AuditEntries.Add(new AuditEntry(
            leaveRequestId,
            newStatus == LeaveRequestStatuses.Approved
                ? LeaveAuditActions.Approve
                : LeaveAuditActions.Reject,
            actorUserId,
            actorRole,
            LeaveRequestStatuses.Pending,
            newStatus,
            reason,
            occurredAtUtc));

        return Task.FromResult(true);
    }

    public Task<bool> TryRecordDepartureAsync(
        ulong leaveRequestId,
        ulong actorUserId,
        string actorRole,
        DateTime occurredAtUtc)
    {
        LeaveRequest? request = Find(leaveRequestId);

        if (ForceTransitionConflict ||
            request is null ||
            request.Status != LeaveRequestStatuses.Approved)
        {
            return Task.FromResult(false);
        }

        request.Status = LeaveRequestStatuses.Departed;
        request.DepartureRecordedByUserId = actorUserId;
        request.ActualDepartureAt = occurredAtUtc;

        AuditEntries.Add(new AuditEntry(
            leaveRequestId,
            LeaveAuditActions.Depart,
            actorUserId,
            actorRole,
            LeaveRequestStatuses.Approved,
            LeaveRequestStatuses.Departed,
            null,
            occurredAtUtc));

        return Task.FromResult(true);
    }

    public Task<bool> TryRecordReturnAsync(
        ulong leaveRequestId,
        ulong actorUserId,
        string actorRole,
        DateTime occurredAtUtc)
    {
        LeaveRequest? request = Find(leaveRequestId);

        if (ForceTransitionConflict ||
            request is null ||
            request.Status != LeaveRequestStatuses.Departed)
        {
            return Task.FromResult(false);
        }

        request.Status = LeaveRequestStatuses.Closed;
        request.ReturnRecordedByUserId = actorUserId;
        request.ActualReturnAt = occurredAtUtc;

        AuditEntries.Add(new AuditEntry(
            leaveRequestId,
            LeaveAuditActions.Return,
            actorUserId,
            actorRole,
            LeaveRequestStatuses.Departed,
            LeaveRequestStatuses.Closed,
            null,
            occurredAtUtc));

        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<LeaveRequest>> GetOverdueNotAlertedAsync(
        DateOnly today)
    {
        IReadOnlyList<LeaveRequest> result = Requests
            .Where(request =>
                IsOverdue(request, today) &&
                request.OverdueAlertedAt is null)
            .OrderBy(request => request.ExpectedReturnDate)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<bool> TryFlagOverdueAsync(
        ulong leaveRequestId,
        NewLeaveNotification notification,
        DateTime occurredAtUtc)
    {
        if (FailFlaggingFor.Contains(leaveRequestId))
        {
            throw new InvalidOperationException("Simulated database failure.");
        }

        LeaveRequest? request = Find(leaveRequestId);

        if (request is null ||
            request.Status != LeaveRequestStatuses.Departed ||
            request.OverdueAlertedAt is not null)
        {
            return Task.FromResult(false);
        }

        request.OverdueAlertedAt = occurredAtUtc;
        OverdueNotifications.Add(notification);

        AuditEntries.Add(new AuditEntry(
            leaveRequestId,
            LeaveAuditActions.FlagOverdue,
            null,
            LeaveRoles.System,
            LeaveRequestStatuses.Departed,
            LeaveRequestStatuses.Departed,
            null,
            occurredAtUtc));

        return Task.FromResult(true);
    }

    private LeaveRequest? Find(ulong leaveRequestId)
    {
        return Requests.FirstOrDefault(
            request => request.LeaveRequestId == leaveRequestId);
    }

    private static bool IsOverdue(LeaveRequest request, DateOnly today)
    {
        return request.Status == LeaveRequestStatuses.Departed &&
               request.ExpectedReturnDate < today;
    }
}
