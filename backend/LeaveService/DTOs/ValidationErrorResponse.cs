namespace LeaveService.DTOs;

/// <summary>
/// A validation failure. Errors are grouped by the camelCase
/// name of the field so the frontend can show each message
/// beside the matching input.
/// </summary>
public class ValidationErrorResponse : ErrorResponse
{
    public IReadOnlyDictionary<string, string[]> Errors { get; set; } =
        new Dictionary<string, string[]>();
}
