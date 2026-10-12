using IdentityService.DTOs;

namespace IdentityService.Services;

public interface IStudentFeeReminderService
{
    Task<IReadOnlyList<FeeReminderResponse>> GetMineAsync(
        ulong userId);
}