using System.Text.Json;
using ComplaintService.Models;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ComplaintService.Services;

public interface INotificationPublisher
{
    Task PublishStatusChangeNotificationAsync(ComplaintStatusChangedEvent notificationEvent);
}

public class KafkaNotificationPublisher : INotificationPublisher, IDisposable
{
    private const string TopicName = "complaint-updated";
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaNotificationPublisher> _logger;

    public KafkaNotificationPublisher(IConfiguration configuration, ILogger<KafkaNotificationPublisher> logger)
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
        try
        {
            string key = notificationEvent.ComplaintId.ToString();
            string val = JsonSerializer.Serialize(notificationEvent);

            var message = new Message<string, string>
            {
                Key = key,
                Value = val
            };

            var result = await _producer.ProduceAsync(TopicName, message);
            _logger.LogInformation("Published status change for complaint #{ComplaintId} to Kafka topic {Topic} at offset {Offset}", 
                notificationEvent.ComplaintId, result.Topic, result.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to deliver Kafka event for complaint #{ComplaintId}: {Reason}", 
                notificationEvent.ComplaintId, ex.Error.Reason);
            throw;
        }
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}