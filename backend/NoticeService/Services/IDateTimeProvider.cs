namespace NoticeService.Services;

public interface IDateTimeProvider
{
    DateOnly Today { get; }
    DateTime UtcNow { get; }
}

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
    public DateTime UtcNow => DateTime.UtcNow;
}