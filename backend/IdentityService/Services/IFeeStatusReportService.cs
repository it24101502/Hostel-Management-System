using IdentityService.DTOs;

namespace IdentityService.Services;

public interface IFeeStatusReportService
{
    Task<IReadOnlyList<FeeStatusReportRow>>
        GetReportAsync(
            ulong? studentProfileId,
            ulong? blockId);

    Task<byte[]> GenerateCsvAsync(
        ulong? studentProfileId,
        ulong? blockId);
}