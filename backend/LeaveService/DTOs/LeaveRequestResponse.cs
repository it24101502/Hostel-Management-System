namespace LeaveService.DTOs;

public class LeaveRequestResponse
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

    public string? DecisionReason { get; set; }

    public DateTime? DecidedAt { get; set; }

    public DateTime? ActualDepartureAt { get; set; }

    public DateTime? ActualReturnAt { get; set; }

    // True when the student has departed and is past the
    // expected return date without a recorded return.
    public bool IsOverdue { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
