using IdentityService.Models;

namespace IdentityService.Repositories;

public interface ILoginAuditRepository
{
    Task RecordAttemptAsync(
        ulong? userId,
        string identifier,
        string outcome);

    Task<IReadOnlyList<LoginAuditLog>>
        GetRecentAttemptsAsync(int limit);
}