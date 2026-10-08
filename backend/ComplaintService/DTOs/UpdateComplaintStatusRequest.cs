namespace ComplaintService.DTOs;

public class UpdateComplaintStatusRequest
{
    // OPEN, IN_PROGRESS or RESOLVED.
    public string? Status { get; set; }

    public string? Remarks { get; set; }
}