using System.Text.Json;
using Confluent.Kafka;
using IdentityService.Options;
using Microsoft.Extensions.Options;

namespace IdentityService.Events;

public sealed class KafkaEventPublisher
    : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaEventPublisher> _logger;

    public KafkaEventPublisher(
        IOptions<KafkaOptions> options,
        ILogger<KafkaEventPublisher> logger)
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

    public async Task PublishStudentDeactivatedAsync(
        StudentDeactivatedEvent eventMessage,
        CancellationToken cancellationToken = default)
    {
        string eventJson =
            JsonSerializer.Serialize(eventMessage);

        DeliveryResult<string, string> result =
            await _producer.ProduceAsync(
                _options.StudentDeactivatedTopic,
                new Message<string, string>
                {
                    Key =
                        eventMessage.StudentProfileId.ToString(),
                    Value = eventJson
                },
                cancellationToken);

        _logger.LogInformation(
            "Published {EventName} event {EventId} to {TopicPartitionOffset}.",
            nameof(StudentDeactivatedEvent),
            eventMessage.EventId,
            result.TopicPartitionOffset);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}