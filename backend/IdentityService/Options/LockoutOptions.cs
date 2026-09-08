namespace IdentityService.Options;

public sealed class LockoutOptions
{
    public const string SectionName = "Authentication:Lockout";

    public int DurationMinutes { get; set; } = 15;
}