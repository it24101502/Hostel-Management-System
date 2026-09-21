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

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
