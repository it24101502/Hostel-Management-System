using AccommodationService.DTOs;
using AccommodationService.Models;

namespace AccommodationService.Repositories;

public interface IRoomAllocationRepository
{
    Task<IReadOnlyList<StudentRoomAllocation>> GetAllAsync();

    Task<StudentRoomAllocation?> GetByStudentIdAsync(
        ulong studentProfileId);

    Task<StudentRoomAllocation> AllocateAsync(
        AllocateStudentRequest request,
        ulong administratorUserId);

    Task<StudentRoomAllocation> TransferAsync(
        ulong studentProfileId,
        TransferStudentRequest request,
        ulong administratorUserId);

    Task<IReadOnlyList<RoomOccupancyResponse>>
        GetOccupancyReportAsync(
            ulong? blockId,
            ushort? floorNumber);
}