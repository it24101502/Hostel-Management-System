using IdentityService.Models;

namespace IdentityService.Services;

public interface IFeeReminderSender
{
    Task SendAsync(
        FeeReminderNotification reminder,
        CancellationToken cancellationToken =
            default);
}