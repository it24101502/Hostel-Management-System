using ComplaintService.Models;

namespace ComplaintService.Services;

public interface INotificationPublisher{
    Task PublishStatusChangeNotificationAsync(ComplaintStatusChangedEvent notificationEvent, CancellationToken cancellationToken = default);
}