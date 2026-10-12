namespace IdentityService.Events;

public sealed record StudentAllocationChangedEvent(
    Guid EventId,
    ulong StudentProfileId,
    ulong? BlockId,
    string? BlockCode,
    string? BlockName,
    DateTimeOffset OccurredAtUtc);