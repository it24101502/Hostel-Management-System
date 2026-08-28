namespace IdentityService.Models;

public class FeeReminderNotification
{
    public ulong ReminderId { get; set; }

    public ulong InvoiceId { get; set; }

    public ulong StudentProfileId { get; set; }

    public ulong RecipientUserId { get; set; }

    public string InvoiceNumber { get; set; }
        = string.Empty;

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public DateOnly DueDate { get; set; }

    public string Message { get; set; }
        = string.Empty;

    public string NotificationStatus { get; set; }
        = string.Empty;

    public DateTime TriggeredAt { get; set; }
}