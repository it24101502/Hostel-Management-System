using System.Globalization;
using LeaveService.Events;
using LeaveService.Models;
using LeaveService.Repositories;

namespace LeaveService.Services;

/// <summary>
/// Finds students who departed on approved leave but have not
/// returned by their expected return date, and alerts the staff
/// once for each of them (HMS-49).
/// </summary>
public class LeaveOverdueJobService : ILeaveOverdueJobService
{
    private readonly ILeaveRequestRepository _repository;
    private readonly ILeaveEventPublisher _eventPublisher;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<LeaveOverdueJobService> _logger;

    public LeaveOverdueJobService(
        ILeaveRequestRepository repository,
        ILeaveEventPublisher eventPublisher,
        TimeProvider timeProvider,
        ILogger<LeaveOverdueJobService> logger)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<LeaveOverdueJobResult> RunOnceAsync(
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        DateOnly processingDate =
            DateOnly.FromDateTime(utcNow.UtcDateTime);

        var candidates =
            await _repository.GetOverdueNotAlertedAsync(
                processingDate);

        int flaggedCount = 0;

        foreach (LeaveRequest request in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var notification = new NewLeaveNotification
                {
                    NotificationType =
                        LeaveNotificationTypes.ReturnOverdue,
                    Message =
                        $"{request.StudentUsername} has not returned from leave. " +
                        "The expected return date was " +
                        $"{request.ExpectedReturnDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}."
                };

                bool flagged = await _repository.TryFlagOverdueAsync(
                    request.LeaveRequestId,
                    notification,
                    utcNow.UtcDateTime);

                if (!flagged)
                {
                    // Already alerted, or the student just returned.
                    continue;
                }

                flaggedCount++;

                await _eventPublisher.PublishSafelyAsync(
                    new LeaveEvent(
                        Guid.NewGuid(),
                        LeaveEventTypes.ReturnOverdue,
                        request.LeaveRequestId,
                        request.StudentUserId,
                        request.Status,
                        null,
                        utcNow),
                    _logger);
            }
            catch (Exception exception)
                when (exception is not OperationCanceledException)
            {
                // One bad request must not stop the others.
                _logger.LogError(
                    exception,
                    "Failed to flag leave request {LeaveRequestId} as overdue.",
                    request.LeaveRequestId);
            }
        }

        var result = new LeaveOverdueJobResult
        {
            ProcessingDate = processingDate,
            RequestsFlagged = flaggedCount
        };

        _logger.LogInformation(
            "Leave overdue job completed. Date: {ProcessingDate}, Flagged: {Flagged}",
            result.ProcessingDate,
            result.RequestsFlagged);

        return result;
    }
}
