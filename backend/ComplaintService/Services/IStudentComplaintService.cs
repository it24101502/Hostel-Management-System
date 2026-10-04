using ComplaintService.DTOs;

namespace ComplaintService.Services;

public interface IStudentComplaintService
{
    Task<ComplaintResponse> SubmitAsync(
        SubmitComplaintRequest request,
        ulong studentUserId,
        string studentUsername);

    Task<IReadOnlyList<ComplaintResponse>> GetMyComplaintsAsync(
        ulong studentUserId);

    /// <summary>
    /// Returns null when the complaint does not exist or belongs
    /// to another student.
    /// </summary>
    Task<ComplaintResponse?> GetMyComplaintAsync(
        ulong complaintId,
        ulong studentUserId);

    Task<IReadOnlyList<StudentNotificationResponse>> GetNotificationsByStudentIdAsync(
        ulong studentId);
}