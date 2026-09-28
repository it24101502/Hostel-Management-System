using LeaveService.Events;

namespace LeaveService.Tests.TestDoubles;

internal sealed class FakeLeaveEventPublisher : ILeaveEventPublisher
{
    public List<LeaveEvent> PublishedEvents { get; } = new();

    public Exception? ExceptionToThrow { get; set; }

    public Task PublishAsync(
        LeaveEvent eventMessage,
        CancellationToken cancellationToken = default)
    {
        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        PublishedEvents.Add(eventMessage);

        return Task.CompletedTask;
    }
}
