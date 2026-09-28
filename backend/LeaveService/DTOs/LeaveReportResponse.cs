namespace LeaveService.DTOs;

public class LeaveReportResponse
{
    public DateTimeOffset GeneratedAtUtc { get; set; }

    // Counts for every request in the selected date range,
    // regardless of the status filter, so the summary always
    // shows the full picture.
    public LeaveReportTotals Totals { get; set; } = new();

    // The requests that match every filter.
    public IReadOnlyList<LeaveRequestResponse> Requests { get; set; } =
        Array.Empty<LeaveRequestResponse>();
}
