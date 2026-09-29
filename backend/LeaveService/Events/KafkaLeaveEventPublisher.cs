using System.Text.Json;
using Confluent.Kafka;
using LeaveService.Options;
using Microsoft.Extensions.Options;

namespace LeaveService.Events;

/// <summary>
/// Publishes leave events to Kafka without making the caller wait.
/// The request has already been saved in MySQL, so a slow or
/// unavailable Kafka broker must never delay or fail a leave
/// request (NFR-01: requests complete within 3 seconds).
/// </summary>
public sealed class KafkaLeaveEventPublisher
    : ILeaveEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaLeaveEventPublisher> _logger;

    public KafkaLeaveEventPublisher(
        IOptions<KafkaOptions> options,
        ILogger<KafkaLeaveEventPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;

        var configuration = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageTimeoutMs = 10000
        };

        _producer = new ProducerBuilder<string, string>(
            configuration).Build();
    }

    public Task PublishAsync(
        LeaveEvent eventMessage,
        CancellationToken cancellationToken = default)
    {
        string eventJson =
            JsonSerializer.Serialize(eventMessage);

        try
        {
            // Produce only places the message in the producer's
            // queue. The delivery handler runs later, when Kafka
            // confirms or rejects the message.
            _producer.Produce(
                _options.LeaveRequestedTopic,
                new Message<string, string>
                {
                    // Keying by request keeps every event of one
                    // leave request in order within a partition.
                    Key = eventMessage.LeaveRequestId.ToString(),
                    Value = eventJson
                },
                deliveryReport =>
                {
                    if (deliveryReport.Error.IsError)
                    {
                        _logger.LogWarning(
                            "Kafka did not accept {EventType} event {EventId}: {Reason}",
                            eventMessage.EventType,
                            eventMessage.EventId,
                            deliveryReport.Error.Reason);

                        return;
                    }

                    _logger.LogInformation(
                        "Published {EventType} event {EventId} to {TopicPartitionOffset}.",
                        eventMessage.EventType,
                        eventMessage.EventId,
                        deliveryReport.TopicPartitionOffset);
                });
        }
        catch (KafkaException exception)
        {
            _logger.LogWarning(
                exception,
                "Unable to queue {EventType} event {EventId}.",
                eventMessage.EventType,
                eventMessage.EventId);
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
