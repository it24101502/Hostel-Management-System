namespace LeaveService.Exceptions;

/// <summary>
/// Thrown when a leave request is missing information or
/// contains invalid values. Errors are grouped by field name.
/// </summary>
public class LeaveRequestValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public LeaveRequestValidationException(
        IReadOnlyDictionary<string, string[]> errors)
        : base("The leave request contains missing or invalid information.")
    {
        Errors = errors;
    }
}
