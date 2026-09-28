namespace LeaveService.Events;

public interface ILeaveEventPublisher
{
    Task PublishAsync(
        LeaveEvent eventMessage,
        CancellationToken cancellationToken = default);
}
