namespace LeaveService.Models;

/// <summary>
/// A validated leave request that is ready to be saved.
/// </summary>
public class NewLeaveRequest
{
    public ulong StudentUserId { get; set; }

    public string StudentUsername { get; set; } = string.Empty;

    public DateOnly DepartureDate { get; set; }

    public DateOnly ExpectedReturnDate { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string CompanionName { get; set; } = string.Empty;

    public string CompanionRelationship { get; set; } = string.Empty;

    public string CompanionPhone { get; set; } = string.Empty;
}
