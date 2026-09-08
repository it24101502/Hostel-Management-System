namespace IdentityService.DTOs;

public class FeeInvoiceResponse
{
    public ulong InvoiceId { get; set; }

    public ulong StudentProfileId { get; set; }

    public string InvoiceNumber { get; set; }
        = string.Empty;

    public string FeeType { get; set; }
        = string.Empty;

    public string? Description { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public DateTime IssuedAt { get; set; }

    public DateOnly DueDate { get; set; }

    public string Status { get; set; }
        = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}