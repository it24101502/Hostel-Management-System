namespace LeaveService.Exceptions;

/// <summary>
/// Thrown when an action is not allowed in the request's current
/// status, for example approving a request that was already
/// rejected. The controller reports it as 409 Conflict.
/// </summary>
public class InvalidLeaveStatusException : Exception
{
    public InvalidLeaveStatusException(string message)
        : base(message)
    {
    }
}
