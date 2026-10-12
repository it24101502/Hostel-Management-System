namespace ComplaintService.DTOs;

public class ComplaintReportResponse
{
    public DateTimeOffset GeneratedAtUtc { get; set; }

    // Counts for the selected category, ignoring the status filter,
    // so the summary always shows the full picture (same idea as the leave report).
    public ComplaintReportTotals Totals { get; set; } = new();

    public IReadOnlyList<ComplaintCategoryTotal> ByCategory { get; set; } =
        Array.Empty<ComplaintCategoryTotal>();

    // Complaints matching every filter.
    public IReadOnlyList<ComplaintResponse> Complaints { get; set; } =
        Array.Empty<ComplaintResponse>();
}

public class ComplaintReportTotals
{
    public int Total { get; set; }
    public int Open { get; set; }
    public int InProgress { get; set; }
    public int Resolved { get; set; }
}

public class ComplaintCategoryTotal
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
}