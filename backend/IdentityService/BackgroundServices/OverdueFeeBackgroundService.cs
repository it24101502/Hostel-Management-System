using IdentityService.Options;
using IdentityService.Services;
using Microsoft.Extensions.Options;

namespace IdentityService.BackgroundServices;

public class OverdueFeeBackgroundService
    : BackgroundService
{
    private readonly IServiceScopeFactory
        _scopeFactory;

    private readonly OverdueFeeJobOptions
        _options;

    private readonly ILogger<
        OverdueFeeBackgroundService> _logger;

    public OverdueFeeBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<OverdueFeeJobOptions> options,
        ILogger<OverdueFeeBackgroundService>
            logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task
        ExecuteAsync(
            CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "The overdue fee job is disabled.");

            return;
        }

        int intervalMinutes =
            Math.Max(
                1,
                _options.IntervalMinutes);

        TimeSpan interval =
            TimeSpan.FromMinutes(
                intervalMinutes);

        _logger.LogInformation(
            "Overdue fee job started. " +
            "Interval: {IntervalMinutes} minutes.",
            intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunJobAsync(
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (
                    stoppingToken
                        .IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "The overdue fee job failed.");
            }

            try
            {
                await Task.Delay(
                    interval,
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (
                    stoppingToken
                        .IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task RunJobAsync(
        CancellationToken cancellationToken)
    {
        using IServiceScope scope =
            _scopeFactory.CreateScope();

        IOverdueFeeJobService jobService =
            scope.ServiceProvider
                .GetRequiredService<
                    IOverdueFeeJobService>();

        await jobService.RunOnceAsync(
            cancellationToken);
    }
}