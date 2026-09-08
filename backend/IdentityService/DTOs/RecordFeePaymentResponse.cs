namespace IdentityService.DTOs;

public class RecordFeePaymentResponse
{
    public ulong PaymentId { get; set; }

    public ulong InvoiceId { get; set; }

    public string PaymentReference { get; set; }
        = string.Empty;

    public decimal PaymentAmount { get; set; }

    public string PaymentMethod { get; set; }
        = string.Empty;

    public string PaymentStatus { get; set; }
        = string.Empty;

    public DateTime PaidAt { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal OutstandingAmount { get; set; }

    public string InvoiceStatus { get; set; }
        = string.Empty;
}