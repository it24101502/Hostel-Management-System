using System.Text.Json;
using AccommodationService.Events;
using AccommodationService.Options;
using AccommodationService.Services;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace AccommodationService.BackgroundServices;

public sealed class StudentDeactivatedConsumer
    : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _options;
    private readonly ILogger<StudentDeactivatedConsumer>
        _logger;

    public StudentDeactivatedConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        ILogger<StudentDeactivatedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var configuration = new ConsumerConfig
        {
            BootstrapServers =
                _options.BootstrapServers,
            GroupId = _options.ConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false
        };

        using IConsumer<string, string> consumer =
            new ConsumerBuilder<string, string>(
                configuration).Build();

        consumer.Subscribe(
            _options.StudentDeactivatedTopic);

        _logger.LogInformation(
            "Listening for student deactivation events on {Topic}.",
            _options.StudentDeactivatedTopic);

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? consumedMessage =
                null;

            try
            {
                consumedMessage =
                    consumer.Consume(stoppingToken);

                StudentDeactivatedEvent? eventMessage;

                try
                {
                    eventMessage =
                        JsonSerializer.Deserialize<
                            StudentDeactivatedEvent>(
                            consumedMessage.Message.Value);
                }
                catch (JsonException exception)
                {
                    _logger.LogError(
                        exception,
                        "Invalid Kafka message at {Offset}.",
                        consumedMessage.TopicPartitionOffset);

                    // Skip a permanently invalid message.
                    consumer.Commit(consumedMessage);
                    continue;
                }

                if (eventMessage is null)
                {
                    consumer.Commit(consumedMessage);
                    continue;
                }

                using IServiceScope scope =
                    _scopeFactory.CreateScope();

                IRoomAllocationService allocationService =
                    scope.ServiceProvider
                        .GetRequiredService<
                            IRoomAllocationService>();

                bool released =
                    await allocationService.ReleaseAsync(
                        eventMessage.StudentProfileId,
                        eventMessage.DeactivatedByUserId);

                consumer.Commit(consumedMessage);

                _logger.LogInformation(
                    "Processed event {EventId}. Student profile {StudentProfileId}; allocation released: {Released}.",
                    eventMessage.EventId,
                    eventMessage.StudentProfileId,
                    released);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (ConsumeException exception)
            {
                _logger.LogError(
                    exception,
                    "Kafka consumption failed. Retrying.");

                await DelayBeforeRetryAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Student deactivation event processing failed. Retrying.");

                if (consumedMessage is not null)
                {
                    try
                    {
                        consumer.Seek(
                            consumedMessage
                                .TopicPartitionOffset);
                    }
                    catch (KafkaException seekException)
                    {
                        _logger.LogError(
                            seekException,
                            "Unable to seek to failed Kafka message.");
                    }
                }

                await DelayBeforeRetryAsync(stoppingToken);
            }
        }

        consumer.Close();
    }

    private static async Task DelayBeforeRetryAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(
                TimeSpan.FromSeconds(5),
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
    }
}