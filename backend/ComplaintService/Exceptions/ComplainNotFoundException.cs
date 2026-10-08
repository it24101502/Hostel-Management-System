namespace ComplaintService.Exceptions;

public class ComplaintNotFoundException : Exception
{
    public ComplaintNotFoundException()
        : base("The complaint was not found.") { }
}