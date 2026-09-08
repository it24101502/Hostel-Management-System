namespace IdentityService.Exceptions;

public class DuplicateUserException : Exception
{
    public DuplicateUserException()
        : base("A user with this username or email already exists.")
    {
    }
}