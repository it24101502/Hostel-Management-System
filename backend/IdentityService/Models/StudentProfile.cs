namespace IdentityService.Models;

public class StudentProfile
{
    public ulong StudentProfileId { get; set; }

    public ulong UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string RegistrationNumber { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? City { get; set; }

    public string? District { get; set; }

    public string? PostalCode { get; set; }

    public string? ProgrammeName { get; set; }

    public string? FacultyName { get; set; }

    public uint? AcademicYear { get; set; }

    public string? ProfilePhotoUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}