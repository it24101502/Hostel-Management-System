namespace LeaveService.Events;

/// <summary>
/// The message published to the Kafka leave-events topic
/// whenever something happens to a leave request.
/// </summary>
public sealed record LeaveEvent(
    Guid EventId,
    string EventType,
    ulong LeaveRequestId,
    ulong StudentUserId,
    string Status,
    ulong? ActorUserId,
    DateTimeOffset OccurredAtUtc);
