namespace IdentityService.Exceptions;

public class FeeInvoiceValidationException
    : Exception
{
    public FeeInvoiceValidationException(
        string message)
        : base(message)
    {
    }
}