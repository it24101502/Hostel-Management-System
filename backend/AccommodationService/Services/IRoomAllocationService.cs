using AccommodationService.DTOs;

namespace AccommodationService.Services;

public interface IRoomAllocationService
{
    Task<IReadOnlyList<AllocationResponse>> GetAllAsync();

    Task<AllocationResponse?> GetByStudentIdAsync(
        ulong studentProfileId);

    Task<AllocationResponse> AllocateAsync(
        AllocateStudentRequest request,
        ulong administratorUserId);

    Task<AllocationResponse> TransferAsync(
        ulong studentProfileId,
        TransferStudentRequest request,
        ulong administratorUserId);

    Task<IReadOnlyList<RoomOccupancyResponse>>
        GetOccupancyReportAsync(
            ulong? blockId,
            ushort? floorNumber);
}