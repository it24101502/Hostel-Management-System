namespace IdentityService.Exceptions;

public class RestrictedProfileFieldException : Exception
{
    public IReadOnlyCollection<string> RestrictedFields
    {
        get;
    }

    public RestrictedProfileFieldException(
        IEnumerable<string> restrictedFields)
        : base(
            "The request contains fields that students are not permitted to update: " +
            string.Join(", ", restrictedFields) +
            ".")
    {
        RestrictedFields =
            restrictedFields.ToArray();
    }
}