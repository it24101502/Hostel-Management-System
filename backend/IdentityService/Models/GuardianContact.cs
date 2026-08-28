namespace IdentityService.Models;

public class GuardianContact
{
    public ulong GuardianContactId { get; set; }

    public ulong StudentProfileId { get; set; }

    public string ContactType { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Relationship { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string? AlternatePhone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public bool IsPrimary { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}