namespace ComplaintService.DTOs;

/// <summary>
/// The body of POST /api/student/complaints.
/// Every property is nullable so a missing value can be reported
/// as a validation error instead of silently becoming a default.
/// </summary>
public class SubmitComplaintRequest
{
    public string? Category { get; set; }

    public string? Description { get; set; }
}
