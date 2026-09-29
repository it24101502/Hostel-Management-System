namespace LeaveService.DTOs;

/// <summary>
/// The body of PUT /api/staff/leave-requests/{id}/decision.
/// </summary>
public class DecideLeaveRequest
{
    // "APPROVE" or "REJECT".
    public string? Decision { get; set; }

    public string? Reason { get; set; }
}
