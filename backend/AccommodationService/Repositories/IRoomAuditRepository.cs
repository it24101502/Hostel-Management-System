using AccommodationService.Models;

namespace AccommodationService.Repositories;

public interface IRoomAuditRepository
{
    Task RecordAsync(
        ulong administratorUserId,
        string action,
        HostelRoom room);
}
