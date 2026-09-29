namespace LeaveService.DTOs;

/// <summary>
/// The body of POST /api/student/leave-requests.
/// Every property is nullable so a missing value can be reported
/// as a validation error instead of silently becoming a default.
/// </summary>
public class SubmitLeaveRequest
{
    public DateOnly? DepartureDate { get; set; }

    public DateOnly? ExpectedReturnDate { get; set; }

    public string? Reason { get; set; }

    public string? CompanionName { get; set; }

    public string? CompanionRelationship { get; set; }

    public string? CompanionPhone { get; set; }
}
