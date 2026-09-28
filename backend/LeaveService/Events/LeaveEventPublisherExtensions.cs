namespace LeaveService.Events;

public static class LeaveEventPublisherExtensions
{
    /// <summary>
    /// Publishes an event but never lets a messaging problem
    /// escape. By the time an event is published the change is
    /// already committed to MySQL, so a Kafka failure must not
    /// turn a successful operation into an error.
    /// </summary>
    public static async Task PublishSafelyAsync(
        this ILeaveEventPublisher publisher,
        LeaveEvent eventMessage,
        ILogger logger)
    {
        try
        {
            await publisher.PublishAsync(eventMessage);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Unable to publish {EventType} for leave request {LeaveRequestId}.",
                eventMessage.EventType,
                eventMessage.LeaveRequestId);
        }
    }
}
