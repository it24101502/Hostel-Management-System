namespace IdentityService.Exceptions;

public class RoleNotFoundException : Exception
{
    public RoleNotFoundException()
        : base("The selected role does not exist or is inactive.")
    {
    }
}