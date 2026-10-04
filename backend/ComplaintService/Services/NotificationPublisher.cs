using System.Text.Json;
using ComplaintService.Models;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ComplaintService.Services;

public class NotificationPublisher : INotificationPublisher, IDisposable
{
    private const string TopicName = "complaint-updated";
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<NotificationPublisher> _logger;

    public NotificationPublisher(IConfiguration configuration, ILogger<NotificationPublisher> logger)
    {
        _logger = logger;

        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,
            EnableDeliveryReports = true
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishStatusChangeNotificationAsync(ComplaintStatusChangedEvent notificationEvent)
    {
        _logger.LogInformation(
            "Triggering notification for Student {StudentId}: Complaint {ComplaintId} changed status from '{OldStatus}' to '{NewStatus}'.",
            notificationEvent.StudentId,
            notificationEvent.ComplaintId,
            notificationEvent.OldStatus,
            notificationEvent.NewStatus
        );

        try
        {
            string key = notificationEvent.ComplaintId.ToString();
            string value = JsonSerializer.Serialize(notificationEvent);

            var message = new Message<string, string>
            {
                Key = key,
                Value = value
            };

            var result = await _producer.ProduceAsync(TopicName, message);

            _logger.LogInformation(
                "Successfully published status change for complaint #{ComplaintId} to Kafka topic {Topic} at offset {Offset}",
                notificationEvent.ComplaintId,
                result.Topic,
                result.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(
                ex,
                "Failed to deliver Kafka event for complaint #{ComplaintId}: {Reason}",
                notificationEvent.ComplaintId,
                ex.Error.Reason);

            throw;
        }
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}