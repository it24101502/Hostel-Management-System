namespace LeaveService.DTOs;

public class LeaveReportTotals
{
    public int Total { get; set; }

    public int Pending { get; set; }

    public int Approved { get; set; }

    public int Rejected { get; set; }

    public int Departed { get; set; }

    public int Closed { get; set; }

    public int Overdue { get; set; }
}
