namespace AccommodationService.Events;

/// BlockId is null when the student's allocation was released.
public sealed record StudentAllocationChangedEvent(
    Guid EventId,
    ulong StudentProfileId,
    ulong? BlockId,
    string? BlockCode,
    string? BlockName,
    DateTimeOffset OccurredAtUtc);