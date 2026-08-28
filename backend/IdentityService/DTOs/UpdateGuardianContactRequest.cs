using System.ComponentModel.DataAnnotations;

namespace IdentityService.DTOs;

public class UpdateGuardianContactRequest
{
    [Required]
    [RegularExpression(
        "^(GUARDIAN|EMERGENCY)$",
        ErrorMessage = "Contact type must be GUARDIAN or EMERGENCY.")]
    public string ContactType { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Relationship { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? AlternatePhone { get; set; }

    [EmailAddress]
    [MaxLength(255)]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    public bool IsPrimary { get; set; }

    public bool IsActive { get; set; } = true;
}