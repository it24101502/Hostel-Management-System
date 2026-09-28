namespace LeaveService.Tests.TestDoubles;

/// <summary>
/// A clock that always returns the time it was given, so tests
/// can simulate any date without waiting.
/// </summary>
internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _utcNow;

    public FixedTimeProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return _utcNow;
    }
}
