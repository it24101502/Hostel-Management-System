using ComplaintService.Models;
using ComplaintService.Repositories;

namespace ComplaintService.Tests.TestDoubles;

/// <summary>
/// An in-memory stand-in for the MySQL repository.
/// </summary>
internal sealed class FakeComplaintRepository : IComplaintRepository
{
    private ulong _nextId = 1;

    public List<Complaint> Complaints { get; } = new();

    public int CreateCallCount { get; private set; }

    public NewComplaint? LastNewComplaint { get; private set; }

    public Task<Complaint> CreateAsync(
        NewComplaint complaint,
        DateTime occurredAtUtc)
    {
        CreateCallCount++;
        LastNewComplaint = complaint;

        var created = new Complaint
        {
            ComplaintId = _nextId++,
            StudentUserId = complaint.StudentUserId,
            StudentUsername = complaint.StudentUsername,
            Category = complaint.Category,
            Description = complaint.Description,
            Status = ComplaintStatuses.Open,
            CreatedAt = occurredAtUtc,
            UpdatedAt = occurredAtUtc
        };

        Complaints.Add(created);

        return Task.FromResult(created);
    }

    public Task<Complaint?> GetByIdAsync(ulong complaintId)
    {
        return Task.FromResult(
            Complaints.FirstOrDefault(
                complaint => complaint.ComplaintId == complaintId));
    }

    public Task<IReadOnlyList<Complaint>> GetByStudentAsync(
        ulong studentUserId)
    {
        IReadOnlyList<Complaint> result = Complaints
            .Where(complaint => complaint.StudentUserId == studentUserId)
            .OrderByDescending(complaint => complaint.ComplaintId)
            .ToList();

        return Task.FromResult(result);
    }
}
