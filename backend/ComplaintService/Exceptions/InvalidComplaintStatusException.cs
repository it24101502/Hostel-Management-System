namespace ComplaintService.Exceptions;

public class InvalidComplaintStatusException : Exception
{
    public InvalidComplaintStatusException(string message)
        : base(message) { }
}