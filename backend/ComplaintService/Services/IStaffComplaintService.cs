using ComplaintService.DTOs;

namespace ComplaintService.Services;

public interface IStaffComplaintService
{
    Task<IReadOnlyList<ComplaintResponse>> GetComplaintsAsync(
        string? status, string? category);

    Task<ComplaintResponse> AssignAsync(
        ulong complaintId,
        AssignComplaintRequest request,
        ulong actorUserId,
        string actorRole);

    Task<ComplaintResponse> ChangeStatusAsync(
        ulong complaintId,
        UpdateComplaintStatusRequest request,
        ulong actorUserId,
        string actorRole);

    Task<ComplaintReportResponse> GetReportAsync(string? status, string? category);

    Task<byte[]> GenerateReportCsvAsync(string? status, string? category);
}