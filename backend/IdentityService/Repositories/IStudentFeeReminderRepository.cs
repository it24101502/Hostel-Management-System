using IdentityService.DTOs;

namespace IdentityService.Repositories;

public interface IStudentFeeReminderRepository
{
    Task<IReadOnlyList<FeeReminderResponse>> GetForUserAsync(
        ulong userId);
}