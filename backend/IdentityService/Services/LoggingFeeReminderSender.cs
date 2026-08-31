using IdentityService.Models;

namespace IdentityService.Services;

public class LoggingFeeReminderSender
    : IFeeReminderSender
{
    private readonly ILogger<
        LoggingFeeReminderSender> _logger;

    public LoggingFeeReminderSender(
        ILogger<LoggingFeeReminderSender>
            logger)
    {
        _logger = logger;
    }

    public Task SendAsync(
        FeeReminderNotification reminder,
        CancellationToken cancellationToken =
            default)
    {
        _logger.LogWarning(
            "Overdue fee reminder sent. " +
            "RecipientUserId: {RecipientUserId}, " +
            "Invoice: {InvoiceNumber}, " +
            "Message: {Message}",
            reminder.RecipientUserId,
            reminder.InvoiceNumber,
            reminder.Message);

        return Task.CompletedTask;
    }
}