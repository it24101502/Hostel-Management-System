namespace LeaveService.Models;

/// <summary>
/// A validated set of report filters. Null means "no filter".
/// </summary>
public class LeaveRequestFilter
{
    public string? Status { get; set; }

    // Both dates filter on the departure date and are inclusive.
    public DateOnly? FromDate { get; set; }

    public DateOnly? ToDate { get; set; }

    // Only students who have departed and are past their
    // expected return date.
    public bool OverdueOnly { get; set; }
}
