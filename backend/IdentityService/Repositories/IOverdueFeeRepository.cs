using IdentityService.Models;

namespace IdentityService.Repositories;

public interface IOverdueFeeRepository
{
    Task<int> MarkOverdueAndCreateRemindersAsync(
        DateOnly processingDate,
        CancellationToken cancellationToken =
            default);

    Task<IReadOnlyList<FeeReminderNotification>>
        GetPendingRemindersAsync(
            CancellationToken cancellationToken =
                default);

    Task MarkReminderSentAsync(
        ulong reminderId,
        DateTime sentAt,
        CancellationToken cancellationToken =
            default);

    Task MarkReminderFailedAsync(
        ulong reminderId,
        string failureReason,
        CancellationToken cancellationToken =
            default);
}