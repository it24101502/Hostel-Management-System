using System.Text.Json;
using AccommodationService.Options;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace AccommodationService.Events;

public sealed class KafkaAllocationEventPublisher
    : IAllocationEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaAllocationEventPublisher> _logger;

    public KafkaAllocationEventPublisher(
        IOptions<KafkaOptions> options,
        ILogger<KafkaAllocationEventPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;

        _producer = new ProducerBuilder<string, string>(
            new ProducerConfig
            {
                BootstrapServers = _options.BootstrapServers,
                Acks = Acks.All,
                EnableIdempotence = true,
                MessageTimeoutMs = 10000
            }).Build();
    }

    public Task PublishAsync(
        StudentAllocationChangedEvent eventMessage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Keyed by student so one student's changes stay in order.
            _producer.Produce(
                _options.StudentAllocationChangedTopic,
                new Message<string, string>
                {
                    Key = eventMessage.StudentProfileId.ToString(),
                    Value = JsonSerializer.Serialize(eventMessage)
                },
                report =>
                {
                    if (report.Error.IsError)
                    {
                        _logger.LogWarning(
                            "Kafka rejected allocation event {EventId}: {Reason}",
                            eventMessage.EventId, report.Error.Reason);
                    }
                });
        }
        catch (KafkaException exception)
        {
            _logger.LogWarning(exception,
                "Unable to queue allocation event {EventId}.",
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