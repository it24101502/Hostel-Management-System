namespace AccommodationService.Events;

public interface IAllocationEventPublisher
{
    Task PublishAsync(
        StudentAllocationChangedEvent eventMessage,
        CancellationToken cancellationToken = default);
}