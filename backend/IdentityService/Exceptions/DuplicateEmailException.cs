namespace IdentityService.Exceptions;

public class DuplicateEmailException : Exception
{
    public DuplicateEmailException()
        : base("An account with this email address already exists.")
    {
    }
}