using IdentityService.Models;

namespace IdentityService.Repositories;

public interface IUserRepository
{
    Task<LoginUser?> FindByIdentifierAsync(string identifier);

    Task RecordSuccessfulLoginAsync(ulong userId);

    Task RecordFailedLoginAsync(
        ulong userId,
        uint failedAttempts,
        bool isLocked,
        DateTime? lockoutEndAt);
}