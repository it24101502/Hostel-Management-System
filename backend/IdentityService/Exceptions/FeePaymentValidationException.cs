namespace IdentityService.Exceptions;

public class FeePaymentValidationException
    : Exception
{
    public FeePaymentValidationException(
        string message)
        : base(message)
    {
    }
}