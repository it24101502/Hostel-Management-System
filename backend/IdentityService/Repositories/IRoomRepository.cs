using IdentityService.DTOs;
using IdentityService.Models;

namespace IdentityService.Repositories;

public interface IRoomRepository
{
    Task<IReadOnlyList<HostelRoom>> GetAllAsync();

    Task<HostelRoom?> GetByIdAsync(ulong roomId);

    Task<bool> BlockExistsAsync(ulong blockId);

    Task<bool> LocationExistsAsync(
        ulong blockId,
        ushort floorNumber,
        string roomNumber,
        ulong? excludedRoomId = null);

    Task<ulong> CreateAsync(CreateRoomRequest request);

    Task<bool> UpdateAsync(
        ulong roomId,
        UpdateRoomRequest request);

    Task<bool> HasActiveOccupantsAsync(ulong roomId);

    Task<bool> DeleteAsync(ulong roomId);
}
