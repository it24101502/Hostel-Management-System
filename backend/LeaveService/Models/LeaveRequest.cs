namespace LeaveService.Models;

public class LeaveRequest
{
    public ulong LeaveRequestId { get; set; }

    public ulong StudentUserId { get; set; }

    public string StudentUsername { get; set; } = string.Empty;

    public DateOnly DepartureDate { get; set; }

    public DateOnly ExpectedReturnDate { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string CompanionName { get; set; } = string.Empty;

    public string CompanionRelationship { get; set; } = string.Empty;

    public string CompanionPhone { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public ulong? DecidedByUserId { get; set; }

    public string? DecisionReason { get; set; }

    public DateTime? DecidedAt { get; set; }

    public ulong? DepartureRecordedByUserId { get; set; }

    public DateTime? ActualDepartureAt { get; set; }

    public ulong? ReturnRecordedByUserId { get; set; }

    public DateTime? ActualReturnAt { get; set; }

    public DateTime? OverdueAlertedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
