namespace NoticeService.Models;

/// <summary>
/// Notice types. These values match chk_notices_type
/// in the database.
/// </summary>
public static class NoticeTypes
{
    public const string Notice = "NOTICE";
    public const string Schedule = "SCHEDULE";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Notice,
        Schedule
    };
}
