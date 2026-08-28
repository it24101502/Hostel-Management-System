using System.ComponentModel.DataAnnotations;

namespace IdentityService.DTOs;

public class CreateFeeInvoiceRequest
{
    [Required(
        ErrorMessage =
            "Student profile ID is required.")]
    public ulong? StudentProfileId { get; set; }

    [Required(
        ErrorMessage =
            "Fee type is required.")]
    [StringLength(
        100,
        MinimumLength = 2,
        ErrorMessage =
            "Fee type must contain between 2 and 100 characters.")]
    public string FeeType { get; set; }
        = string.Empty;

    [StringLength(
        500,
        ErrorMessage =
            "Description must not exceed 500 characters.")]
    public string? Description { get; set; }

    [Required(
        ErrorMessage =
            "Amount is required.")]
    [Range(
        typeof(decimal),
        "0.01",
        "9999999999.99",
        ErrorMessage =
            "Amount must be greater than zero.")]
    public decimal? Amount { get; set; }

    [Required(
        ErrorMessage =
            "Due date is required.")]
    public DateOnly? DueDate { get; set; }
}