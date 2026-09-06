using IdentityService.DTOs;

namespace IdentityService.Services;

public interface IRoomService
{
    Task<IReadOnlyList<RoomResponse>> GetAllAsync();

    Task<RoomResponse?> GetByIdAsync(ulong roomId);

    Task<RoomResponse> CreateAsync(CreateRoomRequest request);

    Task<RoomResponse?> UpdateAsync(
        ulong roomId,
        UpdateRoomRequest request);

    Task<bool> DeleteAsync(ulong roomId);
}
