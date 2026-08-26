using System.ComponentModel.DataAnnotations;

namespace IdentityService.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "Identifier is required.")]
    public string Identifier { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;
}