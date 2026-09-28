using LeaveService.DTOs;
using LeaveService.Events;
using LeaveService.Exceptions;
using LeaveService.Models;
using LeaveService.Repositories;

namespace LeaveService.Services;

public class LeaveReviewService : ILeaveReviewService
{
    public const int MaxDecisionReasonLength = 500;

    private static readonly string[] ValidStatuses =
    {
        LeaveRequestStatuses.Pending,
        LeaveRequestStatuses.Approved,
        LeaveRequestStatuses.Rejected,
        LeaveRequestStatuses.Departed,
        LeaveRequestStatuses.Closed
    };

    private readonly ILeaveRequestRepository _repository;
    private readonly ILeaveEventPublisher _eventPublisher;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<LeaveReviewService> _logger;

    public LeaveReviewService(
        ILeaveRequestRepository repository,
        ILeaveEventPublisher eventPublisher,
        TimeProvider timeProvider,
        ILogger<LeaveReviewService> logger)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<LeaveReportResponse> GetReportAsync(
        LeaveReportFilter filter)
    {
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        DateOnly today = DateOnly.FromDateTime(utcNow.UtcDateTime);

        LeaveRequestFilter validFilter = ValidateFilter(filter);

        var requests = await _repository.GetFilteredAsync(
            validFilter,
            today);

        LeaveStatusTotals totals =
            await _repository.GetStatusTotalsAsync(
                validFilter.FromDate,
                validFilter.ToDate,
                today);

        return new LeaveReportResponse
        {
            GeneratedAtUtc = utcNow,
            Totals = new LeaveReportTotals
            {
                Total = totals.Total,
                Pending = totals.Pending,
                Approved = totals.Approved,
                Rejected = totals.Rejected,
                Departed = totals.Departed,
                Closed = totals.Closed,
                Overdue = totals.Overdue
            },
            Requests = requests
                .Select(request =>
                    LeaveRequestMapper.ToResponse(request, today))
                .ToList()
        };
    }

    public async Task<LeaveRequestResponse> DecideAsync(
        ulong leaveRequestId,
        DecideLeaveRequest request,
        ulong actorUserId,
        string actorRole)
    {
        var errors = new Dictionary<string, string[]>();

        string decision =
            request.Decision?.Trim().ToUpperInvariant() ?? string.Empty;

        if (decision != LeaveDecisions.Approve &&
            decision != LeaveDecisions.Reject)
        {
            errors["decision"] = new[]
            {
                "Decision must be APPROVE or REJECT."
            };
        }

        string reason = request.Reason?.Trim() ?? string.Empty;

        if (reason.Length == 0)
        {
            errors["reason"] = new[]
            {
                "A reason for the decision is required."
            };
        }
        else if (reason.Length > MaxDecisionReasonLength)
        {
            errors["reason"] = new[]
            {
                $"Reason cannot exceed {MaxDecisionReasonLength} characters."
            };
        }

        if (errors.Count > 0)
        {
            throw new LeaveRequestValidationException(errors);
        }

        LeaveRequest existing = await GetExistingAsync(leaveRequestId);

        if (existing.Status != LeaveRequestStatuses.Pending)
        {
            throw new InvalidLeaveStatusException(
                "Only pending leave requests can be approved or rejected. " +
                $"This request is {DescribeStatus(existing.Status)}.");
        }

        bool approve = decision == LeaveDecisions.Approve;

        string newStatus = approve
            ? LeaveRequestStatuses.Approved
            : LeaveRequestStatuses.Rejected;

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        bool applied = await _repository.TryDecideAsync(
            leaveRequestId,
            newStatus,
            actorUserId,
            actorRole,
            reason,
            utcNow.UtcDateTime);

        return await CompleteAsync(
            leaveRequestId,
            applied,
            approve
                ? LeaveEventTypes.RequestApproved
                : LeaveEventTypes.RequestRejected,
            actorUserId,
            utcNow);
    }

    public async Task<LeaveRequestResponse> RecordDepartureAsync(
        ulong leaveRequestId,
        ulong actorUserId,
        string actorRole)
    {
        LeaveRequest existing = await GetExistingAsync(leaveRequestId);

        if (existing.Status != LeaveRequestStatuses.Approved)
        {
            throw new InvalidLeaveStatusException(
                "Departure can only be recorded for approved leave requests. " +
                $"This request is {DescribeStatus(existing.Status)}.");
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        bool applied = await _repository.TryRecordDepartureAsync(
            leaveRequestId,
            actorUserId,
            actorRole,
            utcNow.UtcDateTime);

        return await CompleteAsync(
            leaveRequestId,
            applied,
            LeaveEventTypes.StudentDeparted,
            actorUserId,
            utcNow);
    }

    public async Task<LeaveRequestResponse> RecordReturnAsync(
        ulong leaveRequestId,
        ulong actorUserId,
        string actorRole)
    {
        LeaveRequest existing = await GetExistingAsync(leaveRequestId);

        if (existing.Status != LeaveRequestStatuses.Departed)
        {
            throw new InvalidLeaveStatusException(
                "Return can only be recorded for students who have departed. " +
                $"This request is {DescribeStatus(existing.Status)}.");
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        bool applied = await _repository.TryRecordReturnAsync(
            leaveRequestId,
            actorUserId,
            actorRole,
            utcNow.UtcDateTime);

        return await CompleteAsync(
            leaveRequestId,
            applied,
            LeaveEventTypes.StudentReturned,
            actorUserId,
            utcNow);
    }

    // Shared ending of every status change: fail if the database
    // refused it, otherwise reload, publish the event and respond.
    private async Task<LeaveRequestResponse> CompleteAsync(
        ulong leaveRequestId,
        bool applied,
        string eventType,
        ulong actorUserId,
        DateTimeOffset utcNow)
    {
        if (!applied)
        {
            // Someone else changed the request between our read and
            // our update. The database refused the second change.
            throw new InvalidLeaveStatusException(
                "This leave request was just updated by someone else. " +
                "Refresh the list and try again.");
        }

        LeaveRequest updated = await GetExistingAsync(leaveRequestId);

        await _eventPublisher.PublishSafelyAsync(
            new LeaveEvent(
                Guid.NewGuid(),
                eventType,
                updated.LeaveRequestId,
                updated.StudentUserId,
                updated.Status,
                actorUserId,
                utcNow),
            _logger);

        return LeaveRequestMapper.ToResponse(
            updated,
            DateOnly.FromDateTime(utcNow.UtcDateTime));
    }

    private async Task<LeaveRequest> GetExistingAsync(
        ulong leaveRequestId)
    {
        return await _repository.GetByIdAsync(leaveRequestId)
            ?? throw new LeaveRequestNotFoundException();
    }

    private static LeaveRequestFilter ValidateFilter(
        LeaveReportFilter filter)
    {
        var errors = new Dictionary<string, string[]>();

        string? status = string.IsNullOrWhiteSpace(filter.Status)
            ? null
            : filter.Status.Trim().ToUpperInvariant();

        if (status is not null && !ValidStatuses.Contains(status))
        {
            errors["status"] = new[]
            {
                "Status must be PENDING, APPROVED, REJECTED, DEPARTED or CLOSED."
            };
        }

        if (filter.FromDate is not null &&
            filter.ToDate is not null &&
            filter.ToDate.Value < filter.FromDate.Value)
        {
            errors["toDate"] = new[]
            {
                "The end date cannot be earlier than the start date."
            };
        }

        if (errors.Count > 0)
        {
            throw new LeaveRequestValidationException(errors);
        }

        return new LeaveRequestFilter
        {
            Status = status,
            FromDate = filter.FromDate,
            ToDate = filter.ToDate,
            OverdueOnly = filter.OverdueOnly
        };
    }

    private static string DescribeStatus(string status)
    {
        return status.ToLowerInvariant();
    }
}
