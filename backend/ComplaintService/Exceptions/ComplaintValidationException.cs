namespace ComplaintService.Exceptions;

/// <summary>
/// Thrown when a complaint is missing information or contains
/// invalid values. Errors are grouped by field name.
/// </summary>
public class ComplaintValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ComplaintValidationException(
        IReadOnlyDictionary<string, string[]> errors)
        : base("The complaint contains missing or invalid information.")
    {
        Errors = errors;
    }
}
