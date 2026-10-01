using ComplaintService.Models;
using ComplaintService.Repositories;

namespace ComplaintService.Tests.TestDoubles;

internal sealed class FakeComplaintRepository : IComplaintRepository
{
    private ulong _nextId = 1;

    public sealed record AuditEntry(
        ulong ComplaintId,
        string Action,
        ulong ActorUserId,
        string ActorRole,
        string FromStatus,
        string ToStatus,
        string? Remarks);

    public List<Complaint> Complaints { get; } = new();

    public List<AuditEntry> AuditEntries { get; } = new();

    public int CreateCallCount { get; private set; }

    public NewComplaint? LastNewComplaint { get; private set; }

    // Simulates someone else changing the status first.
    public bool ForceStatusConflict { get; set; }

    public Complaint Seed(
        ulong studentUserId,
        string status = ComplaintStatuses.Open,
        string category = "PLUMBING")
    {
        DateTime at = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

        var complaint = new Complaint
        {
            ComplaintId = _nextId++,
            StudentUserId = studentUserId,
            StudentUsername = "student" + studentUserId,
            Category = category,
            Description = "Seeded complaint",
            Status = status,
            ResolvedAt =
                status == ComplaintStatuses.Resolved ? at : null,
            CreatedAt = at,
            UpdatedAt = at
        };

        Complaints.Add(complaint);
        return complaint;
    }

    public Task<Complaint> CreateAsync(
        NewComplaint complaint, DateTime occurredAtUtc)
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
            Complaints.FirstOrDefault(c => c.ComplaintId == complaintId));
    }

    public Task<IReadOnlyList<Complaint>> GetByStudentAsync(
        ulong studentUserId)
    {
        IReadOnlyList<Complaint> result = Complaints
            .Where(c => c.StudentUserId == studentUserId)
            .OrderByDescending(c => c.ComplaintId)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<Complaint>> GetFilteredAsync(
        string? status, string? category)
    {
        IReadOnlyList<Complaint> result = Complaints
            .Where(c =>
                (status is null || c.Status == status) &&
                (category is null || c.Category == category))
            .OrderByDescending(c => c.ComplaintId)
            .ToList();

        return Task.FromResult(result);
    }

    public Task AssignAsync(
        ulong complaintId,
        ulong assigneeUserId,
        ulong actorUserId,
        string actorRole,
        DateTime occurredAtUtc)
    {
        Complaint c = Complaints.First(x => x.ComplaintId == complaintId);

        c.AssignedToUserId = assigneeUserId;
        c.AssignedAt = occurredAtUtc;

        AuditEntries.Add(new AuditEntry(
            complaintId, ComplaintAuditActions.Assign,
            actorUserId, actorRole, c.Status, c.Status, null));

        return Task.CompletedTask;
    }

    public Task<bool> TryChangeStatusAsync(
        ulong complaintId,
        string fromStatus,
        string toStatus,
        ulong actorUserId,
        string actorRole,
        string? remarks,
        DateTime occurredAtUtc)
    {
        Complaint? c =
            Complaints.FirstOrDefault(x => x.ComplaintId == complaintId);

        if (ForceStatusConflict || c is null || c.Status != fromStatus)
        {
            return Task.FromResult(false);
        }

        c.Status = toStatus;
        c.ResolvedAt =
            toStatus == ComplaintStatuses.Resolved ? occurredAtUtc : null;

        AuditEntries.Add(new AuditEntry(
            complaintId, ComplaintAuditActions.StatusChange,
            actorUserId, actorRole, fromStatus, toStatus, remarks));

        return Task.FromResult(true);
    }
}