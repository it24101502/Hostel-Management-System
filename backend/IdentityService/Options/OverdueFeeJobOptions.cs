namespace IdentityService.Options;

public class OverdueFeeJobOptions
{
    public const string SectionName =
        "OverdueFeeJob";

    public bool Enabled { get; set; } = true;

    public int IntervalMinutes { get; set; } = 60;
}