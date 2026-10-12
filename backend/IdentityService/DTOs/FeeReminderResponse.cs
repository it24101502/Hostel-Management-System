namespace IdentityService.DTOs;

public class FeeReminderResponse
{
    public ulong ReminderId { get; set; }

    public ulong InvoiceId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public string FeeType { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal OutstandingAmount { get; set; }

    public DateOnly DueDate { get; set; }

    public string Message { get; set; } = string.Empty;

    public DateTime TriggeredAt { get; set; }

    public DateTime? SentAt { get; set; }
}