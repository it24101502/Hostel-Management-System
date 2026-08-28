namespace IdentityService.Models;

public class OverdueFeeJobResult
{
    public DateOnly ProcessingDate { get; set; }

    public int InvoicesMarkedOverdue { get; set; }

    public int RemindersSent { get; set; }

    public int RemindersFailed { get; set; }
}