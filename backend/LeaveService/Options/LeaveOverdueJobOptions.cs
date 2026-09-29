namespace LeaveService.Options;

public class LeaveOverdueJobOptions
{
    public const string SectionName = "LeaveOverdueJob";

    public bool Enabled { get; set; } = true;

    public int IntervalMinutes { get; set; } = 60;
}
