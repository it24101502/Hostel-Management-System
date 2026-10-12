using System.Text.Json;
using Confluent.Kafka;
using IdentityService.Events;
using IdentityService.Options;
using IdentityService.Repositories;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace IdentityService.BackgroundServices;

public sealed class StudentAllocationChangedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _options;
    private readonly ILogger<StudentAllocationChangedConsumer> _logger;

    public StudentAllocationChangedConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        ILogger<StudentAllocationChangedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Consume() blocks, so let the host finish starting first.
        await Task.Yield();

        using IConsumer<string, string> consumer =
            new ConsumerBuilder<string, string>(new ConsumerConfig
            {
                BootstrapServers = _options.BootstrapServers,
                GroupId = _options.ConsumerGroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                EnableAutoOffsetStore = false
            }).Build();

        consumer.Subscribe(_options.StudentAllocationChangedTopic);

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? message = null;

            try
            {
                message = consumer.Consume(stoppingToken);

                StudentAllocationChangedEvent? change;
                try
                {
                    change = JsonSerializer.Deserialize<
                        StudentAllocationChangedEvent>(message.Message.Value);
                }
                catch (JsonException exception)
                {
                    _logger.LogError(exception,
                        "Invalid allocation message at {Offset}; skipping.",
                        message.TopicPartitionOffset);
                    consumer.Commit(message);
                    continue;
                }

                if (change is null)
                {
                    consumer.Commit(message);
                    continue;
                }

                using IServiceScope scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider
                    .GetRequiredService<IStudentBlockRepository>();

                await repository.ApplyAllocationChangeAsync(
                    change.StudentProfileId,
                    change.BlockId,
                    change.BlockCode,
                    change.BlockName,
                    stoppingToken);

                consumer.Commit(message);

                _logger.LogInformation(
                    "Student profile {StudentProfileId} block set to {BlockId}.",
                    change.StudentProfileId, change.BlockId);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (ConsumeException exception)
            {
                _logger.LogError(exception, "Kafka consumption failed. Retrying.");
                await DelayAsync(stoppingToken);
            }
            catch (MySqlException exception)
                when (exception.Number == 1062 && message is not null)
            {
                // A clashing block code/name will never succeed on retry,
                // so skip it instead of blocking the partition forever.
                _logger.LogError(exception,
                    "Block conflict at {Offset}; skipping. Check hostel_blocks.",
                    message.TopicPartitionOffset);
                consumer.Commit(message);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception,
                    "Allocation event processing failed. Retrying.");

                if (message is not null)
                {
                    try { consumer.Seek(message.TopicPartitionOffset); }
                    catch (KafkaException seekException)
                    {
                        _logger.LogError(seekException, "Unable to seek.");
                    }
                }

                await DelayAsync(stoppingToken);
            }
        }

        consumer.Close();
    }

    private static async Task DelayAsync(CancellationToken token)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(5), token); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }
}