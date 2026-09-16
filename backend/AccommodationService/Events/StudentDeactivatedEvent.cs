namespace AccommodationService.Events;

public sealed record StudentDeactivatedEvent(
    Guid EventId,
    ulong UserId,
    ulong StudentProfileId,
    ulong DeactivatedByUserId,
    DateTimeOffset OccurredAtUtc);