namespace IdentityService.Exceptions;

public sealed class AccountLockedException : Exception
{
    public AccountLockedException()
        : base(
            "The account is temporarily locked. Please try again later.")
    {
    }
}