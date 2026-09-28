using LeaveService.Options;
using LeaveService.Services;
using Microsoft.Extensions.Options;

namespace LeaveService.BackgroundServices;

/// <summary>
/// Runs the overdue-return job on a timer. The timer loop is
/// deliberately thin; all decisions are made in
/// LeaveOverdueJobService so they can be unit tested.
/// </summary>
public class LeaveOverdueBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly LeaveOverdueJobOptions _options;
    private readonly ILogger<LeaveOverdueBackgroundService> _logger;

    public LeaveOverdueBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<LeaveOverdueJobOptions> options,
        ILogger<LeaveOverdueBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "The leave overdue job is disabled.");

            return;
        }

        int intervalMinutes = Math.Max(1, _options.IntervalMinutes);
        TimeSpan interval = TimeSpan.FromMinutes(intervalMinutes);

        _logger.LogInformation(
            "Leave overdue job started. Interval: {IntervalMinutes} minutes.",
            intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunJobAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "The leave overdue job failed.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task RunJobAsync(
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();

        ILeaveOverdueJobService jobService =
            scope.ServiceProvider
                .GetRequiredService<ILeaveOverdueJobService>();

        await jobService.RunOnceAsync(cancellationToken);
    }
}
