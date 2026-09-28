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
}
