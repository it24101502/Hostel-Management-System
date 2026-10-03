namespace ComplaintService.Models;

public record ComplaintStatusChangedEvent
{
    public Guid ComplaintId { get; init; }
    public string StudentId { get; init; } = string.Empty;
    public string OldStatus { get; init; } = string.Empty;
    public string NewStatus { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}