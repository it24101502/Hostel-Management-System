namespace NoticeService.Tests;

public sealed class RequiresMySqlFactAttribute : FactAttribute
{
    public const string VariableName = "NOTICE_TEST_CONNECTION";

    public RequiresMySqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable(VariableName)))
        {
            Skip = $"Set {VariableName} to run the MySQL repository tests.";
        }
    }
}