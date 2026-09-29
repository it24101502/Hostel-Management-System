using LeaveService.DTOs;

namespace LeaveService.Services;

public interface ILeaveRequestService
{
    Task<LeaveRequestResponse> SubmitAsync(
        SubmitLeaveRequest request,
        ulong studentUserId,
        string studentUsername);

    Task<IReadOnlyList<LeaveRequestResponse>> GetMyRequestsAsync(
        ulong studentUserId);

    /// <summary>
    /// Returns null when the request does not exist or belongs
    /// to another student.
    /// </summary>
    Task<LeaveRequestResponse?> GetMyRequestAsync(
        ulong leaveRequestId,
        ulong studentUserId);
}
