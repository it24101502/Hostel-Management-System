using IdentityService.DTOs;

namespace IdentityService.Repositories;

public interface IFeeStatusReportRepository
{
    Task<IReadOnlyList<FeeStatusReportRow>>
        GetReportAsync(
            ulong? studentProfileId,
            ulong? blockId);
}