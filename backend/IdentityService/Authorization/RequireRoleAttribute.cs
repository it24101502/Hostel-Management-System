namespace IdentityService.Authorization;

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = true)]
public sealed class RequireRoleAttribute : Attribute
{
    public IReadOnlyCollection<string> AllowedRoles { get; }

    public RequireRoleAttribute(params string[] allowedRoles)
    {
        if (allowedRoles is null || allowedRoles.Length == 0)
        {
            throw new ArgumentException(
                "At least one allowed role is required.",
                nameof(allowedRoles));
        }

        AllowedRoles = allowedRoles;
    }
}