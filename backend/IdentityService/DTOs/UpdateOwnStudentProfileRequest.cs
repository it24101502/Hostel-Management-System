using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IdentityService.DTOs;

public class UpdateOwnStudentProfileRequest
{
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

    [MaxLength(500)]
    public string? ProfilePhotoUrl { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>
        AdditionalFields { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);
}