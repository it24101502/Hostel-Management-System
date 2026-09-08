using System.ComponentModel.DataAnnotations;

namespace IdentityService.DTOs;

public class UpdateStudentProfileRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string RegistrationNumber { get; set; } =
        string.Empty;

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(20)]
    public string? Gender { get; set; }

    [MaxLength(255)]
    public string? AddressLine1 { get; set; }

    [MaxLength(255)]
    public string? AddressLine2 { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? District { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(150)]
    public string? ProgrammeName { get; set; }

    [MaxLength(150)]
    public string? FacultyName { get; set; }

    [Range(1, 10)]
    public uint? AcademicYear { get; set; }

    [MaxLength(500)]
    public string? ProfilePhotoUrl { get; set; }
}