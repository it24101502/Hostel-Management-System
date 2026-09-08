namespace IdentityService.Models;

public class FeePayment
{
    public ulong PaymentId { get; set; }

    public ulong InvoiceId { get; set; }

    public string PaymentReference { get; set; }
        = string.Empty;

    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; }
        = string.Empty;

    public string PaymentStatus { get; set; }
        = string.Empty;

    public DateTime PaidAt { get; set; }

    public ulong? RecordedByUserId { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}