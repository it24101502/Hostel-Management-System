using IdentityService.Models;
using IdentityService.Repositories;

namespace IdentityService.Services;

public class OverdueFeeJobService
    : IOverdueFeeJobService
{
    private readonly IOverdueFeeRepository
        _repository;

    private readonly IFeeReminderSender
        _reminderSender;

    private readonly TimeProvider
        _timeProvider;

    private readonly ILogger<
        OverdueFeeJobService> _logger;

    public OverdueFeeJobService(
        IOverdueFeeRepository repository,
        IFeeReminderSender reminderSender,
        TimeProvider timeProvider,
        ILogger<OverdueFeeJobService> logger)
    {
        _repository = repository;
        _reminderSender = reminderSender;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<OverdueFeeJobResult>
        RunOnceAsync(
            CancellationToken cancellationToken =
                default)
    {
        DateTimeOffset utcNow =
            _timeProvider.GetUtcNow();

        DateOnly processingDate =
            DateOnly.FromDateTime(
                utcNow.UtcDateTime);

        int markedCount =
            await _repository
                .MarkOverdueAndCreateRemindersAsync(
                    processingDate,
                    cancellationToken);

        IReadOnlyList<
            FeeReminderNotification>
            pendingReminders =
                await _repository
                    .GetPendingRemindersAsync(
                        cancellationToken);

        int sentCount = 0;
        int failedCount = 0;

        foreach (
            FeeReminderNotification reminder
            in pendingReminders)
        {
            try
            {
                await _reminderSender.SendAsync(
                    reminder,
                    cancellationToken);

                await _repository
                    .MarkReminderSentAsync(
                        reminder.ReminderId,
                        utcNow.UtcDateTime,
                        cancellationToken);

                sentCount++;
            }
            catch (OperationCanceledException)
                when (
                    cancellationToken
                        .IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                failedCount++;

                string failureReason =
                    exception.Message.Length <= 500
                        ? exception.Message
                        : exception.Message[..500];

                await _repository
                    .MarkReminderFailedAsync(
                        reminder.ReminderId,
                        failureReason,
                        cancellationToken);

                _logger.LogError(
                    exception,
                    "Failed to send overdue fee reminder {ReminderId}.",
                    reminder.ReminderId);
            }
        }

        var result =
            new OverdueFeeJobResult
            {
                ProcessingDate =
                    processingDate,

                InvoicesMarkedOverdue =
                    markedCount,

                RemindersSent =
                    sentCount,

                RemindersFailed =
                    failedCount
            };

        _logger.LogInformation(
            "Overdue fee job completed. " +
            "Date: {ProcessingDate}, " +
            "Marked: {MarkedCount}, " +
            "Sent: {SentCount}, " +
            "Failed: {FailedCount}",
            result.ProcessingDate,
            result.InvoicesMarkedOverdue,
            result.RemindersSent,
            result.RemindersFailed);

        return result;
    }
}