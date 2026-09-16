namespace IdentityService.Events;

public interface IEventPublisher
{
    Task PublishStudentDeactivatedAsync(
        StudentDeactivatedEvent eventMessage,
        CancellationToken cancellationToken = default);
}