using System.ComponentModel.DataAnnotations;

namespace IdentityService.DTOs;

public class RecordFeePaymentRequest
{
    [Required(
        ErrorMessage =
            "Payment amount is required.")]
    [Range(
        typeof(decimal),
        "0.01",
        "9999999999.99",
        ErrorMessage =
            "Payment amount must be greater than zero.")]
    public decimal? Amount { get; set; }

    [Required(
        ErrorMessage =
            "Payment method is required.")]
    [StringLength(
        30,
        ErrorMessage =
            "Payment method must not exceed 30 characters.")]
    public string PaymentMethod { get; set; }
        = string.Empty;

    [StringLength(
        500,
        ErrorMessage =
            "Notes must not exceed 500 characters.")]
    public string? Notes { get; set; }
}