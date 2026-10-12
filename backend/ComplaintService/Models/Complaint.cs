namespace ComplaintService.Models;

public class Complaint
{
    public ulong ComplaintId { get; set; }

    public ulong StudentUserId { get; set; }

    public string StudentUsername { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public ulong? AssignedToUserId { get; set; }

    public DateTime? AssignedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
