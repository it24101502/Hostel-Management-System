using ComplaintService.Models;
using Microsoft.Extensions.Logging;

namespace ComplaintService.Services;

public class NotificationPublisher : INotificationPublisher
{
    private readonly ILogger<NotificationPublisher> _logger;

    public NotificationPublisher(ILogger<NotificationPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishStatusChangeNotificationAsync(
        ComplaintStatusChangedEvent notificationEvent, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Triggering notification for Student {StudentId}: Complaint {ComplaintId} changed status from '{OldStatus}' to '{NewStatus}'.",
            notificationEvent.StudentId,
            notificationEvent.ComplaintId,
            notificationEvent.OldStatus,
            notificationEvent.NewStatus
        );

        return Task.CompletedTask;
    }
}