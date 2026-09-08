namespace IdentityService.Exceptions;

public class DuplicateRegistrationNumberException : Exception
{
    public DuplicateRegistrationNumberException()
        : base("A student profile with this registration number already exists.")
    {
    }
}