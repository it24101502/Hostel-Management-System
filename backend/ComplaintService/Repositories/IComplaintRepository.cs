using ComplaintService.Models;

namespace ComplaintService.Repositories;

public interface IComplaintRepository
{
    /// <summary>
    /// Saves a new OPEN complaint and its SUBMIT audit row in
    /// one database transaction.
    /// </summary>
    Task<Complaint> CreateAsync(
        NewComplaint complaint,
        DateTime occurredAtUtc);

    Task<Complaint?> GetByIdAsync(
        ulong complaintId);

    Task<IReadOnlyList<Complaint>> GetByStudentAsync(
        ulong studentUserId);
}
