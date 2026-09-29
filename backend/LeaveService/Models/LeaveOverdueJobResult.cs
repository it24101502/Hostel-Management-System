namespace LeaveService.Models;

public class LeaveOverdueJobResult
{
    public DateOnly ProcessingDate { get; set; }

    public int RequestsFlagged { get; set; }
}
