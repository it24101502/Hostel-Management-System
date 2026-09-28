using LeaveService.Models;

namespace LeaveService.Repositories;

public interface ILeaveRequestRepository
{
    /// <summary>
    /// Saves a new PENDING leave request, its SUBMIT audit row and
    /// its staff notification in one database transaction.
    /// </summary>
    Task<LeaveRequest> CreateWithNotificationAsync(
        NewLeaveRequest request,
        NewLeaveNotification notification,
        DateTime occurredAtUtc);

    Task<LeaveRequest?> GetByIdAsync(
        ulong leaveRequestId);

    Task<IReadOnlyList<LeaveRequest>> GetByStudentAsync(
        ulong studentUserId);

    /// <summary>
    /// Returns every request that matches the filter, newest first.
    /// <paramref name="today"/> decides which departed students
    /// count as overdue.
    /// </summary>
    Task<IReadOnlyList<LeaveRequest>> GetFilteredAsync(
        LeaveRequestFilter filter,
        DateOnly today);

    /// <summary>
    /// Counts requests by status for the given departure-date range.
    /// </summary>
    Task<LeaveStatusTotals> GetStatusTotalsAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        DateOnly today);

    // The methods below change a request's status. Each one only
    // succeeds when the request is still in the expected status
    // and returns false otherwise, so two people acting on the
    // same request can never both succeed. The status change and
    // its audit row are saved in one transaction.

    /// <summary>PENDING to APPROVED or REJECTED.</summary>
    Task<bool> TryDecideAsync(
        ulong leaveRequestId,
        string newStatus,
        ulong actorUserId,
        string actorRole,
        string reason,
        DateTime occurredAtUtc);

    /// <summary>APPROVED to DEPARTED.</summary>
    Task<bool> TryRecordDepartureAsync(
        ulong leaveRequestId,
        ulong actorUserId,
        string actorRole,
        DateTime occurredAtUtc);

    /// <summary>DEPARTED to CLOSED.</summary>
    Task<bool> TryRecordReturnAsync(
        ulong leaveRequestId,
        ulong actorUserId,
        string actorRole,
        DateTime occurredAtUtc);

    /// <summary>
    /// Departed students past their expected return date that
    /// staff have not been alerted about yet.
    /// </summary>
    Task<IReadOnlyList<LeaveRequest>> GetOverdueNotAlertedAsync(
        DateOnly today);

    /// <summary>
    /// Marks the request as alerted, records the FLAG_OVERDUE audit
    /// row and saves the staff notification in one transaction.
    /// Returns false when the request was already alerted or is no
    /// longer departed.
    /// </summary>
    Task<bool> TryFlagOverdueAsync(
        ulong leaveRequestId,
        NewLeaveNotification notification,
        DateTime occurredAtUtc);
}
