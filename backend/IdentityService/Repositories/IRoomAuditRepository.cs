using IdentityService.Models;

namespace IdentityService.Repositories;

public interface IRoomAuditRepository
{
    Task RecordAsync(
        ulong administratorUserId,
        string action,
        HostelRoom room);
}