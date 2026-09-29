namespace LeaveService.DTOs;

/// <summary>
/// The query string of GET /api/staff/leave-requests/report.
/// </summary>
public class LeaveReportFilter
{
    public string? Status { get; set; }

    public DateOnly? FromDate { get; set; }

    public DateOnly? ToDate { get; set; }

    public bool OverdueOnly { get; set; }
}
