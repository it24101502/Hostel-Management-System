namespace IdentityService.DTOs;

public class FeeStatusReportRow
{
    public ulong InvoiceId { get; set; }

    public string InvoiceNumber { get; set; } =
        string.Empty;

    public ulong StudentProfileId { get; set; }

    public string RegistrationNumber { get; set; } =
        string.Empty;

    public string StudentName { get; set; } =
        string.Empty;

    public string Email { get; set; } =
        string.Empty;

    public ulong? BlockId { get; set; }

    public string? BlockCode { get; set; }

    public string? BlockName { get; set; }

    public string FeeType { get; set; } =
        string.Empty;

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal OutstandingAmount { get; set; }

    public DateTime IssuedAt { get; set; }

    public DateOnly DueDate { get; set; }

    public string Status { get; set; } =
        string.Empty;
}