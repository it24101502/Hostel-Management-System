namespace IdentityService.Models;

public class LoginUser
{
    public ulong UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public uint FailedLoginAttempts { get; set; }

    public bool IsLocked { get; set; }

    public DateTime? LockoutEndAt { get; set; }

    public bool IsActive { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public bool IsRoleActive { get; set; }
}