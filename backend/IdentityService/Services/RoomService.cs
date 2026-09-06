using IdentityService.DTOs;
using IdentityService.Exceptions;
using IdentityService.Models;
using IdentityService.Repositories;

namespace IdentityService.Services;

public class RoomService : IRoomService
{
    private readonly IRoomRepository _roomRepository;

    public RoomService(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository;
    }

    public async Task<IReadOnlyList<RoomResponse>> GetAllAsync()
    {
        var rooms = await _roomRepository.GetAllAsync();

        return rooms.Select(MapResponse).ToList();
    }

    public async Task<RoomResponse?> GetByIdAsync(ulong roomId)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);

        return room is null
            ? null
            : MapResponse(room);
    }

    public async Task<RoomResponse> CreateAsync(
        CreateRoomRequest request)
    {
        EnsureCapacityIsValid(request.BedCapacity);

        await EnsureBlockExistsAsync(request.BlockId);

        await EnsureLocationIsUniqueAsync(
            request.BlockId,
            request.FloorNumber,
            request.RoomNumber);

        ulong roomId = await _roomRepository.CreateAsync(request);

        var createdRoom = await _roomRepository.GetByIdAsync(roomId);

        return createdRoom is null
            ? throw new InvalidOperationException(
                "The room was created but could not be retrieved.")
            : MapResponse(createdRoom);
    }

    public async Task<RoomResponse?> UpdateAsync(
        ulong roomId,
        UpdateRoomRequest request)
    {
        EnsureCapacityIsValid(request.BedCapacity);

        var existingRoom = await _roomRepository.GetByIdAsync(roomId);

        if (existingRoom is null)
        {
            return null;
        }

        await EnsureBlockExistsAsync(request.BlockId);

        await EnsureLocationIsUniqueAsync(
            request.BlockId,
            request.FloorNumber,
            request.RoomNumber,
            roomId);

        bool updated = await _roomRepository.UpdateAsync(roomId, request);

        if (!updated)
        {
            return null;
        }

        var updatedRoom = await _roomRepository.GetByIdAsync(roomId);

        return updatedRoom is null
            ? null
            : MapResponse(updatedRoom);
    }

    public async Task<bool> DeleteAsync(ulong roomId)
    {
        var existingRoom = await _roomRepository.GetByIdAsync(roomId);

        return existingRoom is not null &&
               await _roomRepository.DeleteAsync(roomId);
    }

    private static void EnsureCapacityIsValid(ushort bedCapacity)
    {
        if (bedCapacity == 0)
        {
            throw new InvalidRoomCapacityException();
        }
    }

    private async Task EnsureBlockExistsAsync(ulong blockId)
    {
        if (!await _roomRepository.BlockExistsAsync(blockId))
        {
            throw new HostelBlockNotFoundException();
        }
    }

    private async Task EnsureLocationIsUniqueAsync(
        ulong blockId,
        ushort floorNumber,
        string roomNumber,
        ulong? excludedRoomId = null)
    {
        bool duplicateExists =
            await _roomRepository.LocationExistsAsync(
                blockId,
                floorNumber,
                roomNumber.Trim(),
                excludedRoomId);

        if (duplicateExists)
        {
            throw new DuplicateRoomException();
        }
    }

    private static RoomResponse MapResponse(HostelRoom room)
    {
        return new RoomResponse
        {
            RoomId = room.RoomId,
            BlockId = room.BlockId,
            BlockCode = room.BlockCode,
            BlockName = room.BlockName,
            FloorNumber = room.FloorNumber,
            RoomNumber = room.RoomNumber,
            BedCapacity = room.BedCapacity,
            IsActive = room.IsActive,
            CreatedAt = room.CreatedAt,
            UpdatedAt = room.UpdatedAt
        };
    }
}
