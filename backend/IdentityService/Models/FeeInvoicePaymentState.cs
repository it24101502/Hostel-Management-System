namespace IdentityService.Models;

public class FeeInvoicePaymentState
{
    public ulong InvoiceId { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public DateOnly DueDate { get; set; }

    public string Status { get; set; }
        = string.Empty;

    public decimal OutstandingAmount =>
        TotalAmount - PaidAmount;
}