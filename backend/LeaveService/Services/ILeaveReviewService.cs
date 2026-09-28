using LeaveService.DTOs;

namespace LeaveService.Services;

public interface ILeaveReviewService
{
    /// <summary>
    /// The leave and movement report: counts by status plus the
    /// requests that match the filters.
    /// </summary>
    Task<LeaveReportResponse> GetReportAsync(
        LeaveReportFilter filter);

    /// <summary>PENDING to APPROVED or REJECTED, with a reason.</summary>
    Task<LeaveRequestResponse> DecideAsync(
        ulong leaveRequestId,
        DecideLeaveRequest request,
        ulong actorUserId,
        string actorRole);

    /// <summary>APPROVED to DEPARTED.</summary>
    Task<LeaveRequestResponse> RecordDepartureAsync(
        ulong leaveRequestId,
        ulong actorUserId,
        string actorRole);

    /// <summary>DEPARTED to CLOSED.</summary>
    Task<LeaveRequestResponse> RecordReturnAsync(
        ulong leaveRequestId,
        ulong actorUserId,
        string actorRole);
}
