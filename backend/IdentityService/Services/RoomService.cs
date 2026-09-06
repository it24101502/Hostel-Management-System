using IdentityService.DTOs;
using IdentityService.Exceptions;
using IdentityService.Models;
using IdentityService.Repositories;

namespace IdentityService.Services;

public class RoomService : IRoomService
{
    private readonly IRoomRepository _roomRepository;

    private readonly IRoomAuditRepository
    _roomAuditRepository;

    public RoomService(
        IRoomRepository roomRepository,
        IRoomAuditRepository roomAuditRepository)
    {
        _roomRepository = roomRepository;
        _roomAuditRepository = roomAuditRepository;
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
        CreateRoomRequest request,
        ulong administratorUserId)
    {
        EnsureCapacityIsValid(request.BedCapacity);

        await EnsureBlockExistsAsync(request.BlockId);

        await EnsureLocationIsUniqueAsync(
            request.BlockId,
            request.FloorNumber,
            request.RoomNumber);

        ulong roomId = await _roomRepository.CreateAsync(request);

        var createdRoom = await _roomRepository.GetByIdAsync(roomId);

        if (createdRoom is null)
        {
            throw new InvalidOperationException(
                "The room was created but could not be retrieved.");
        }

        await _roomAuditRepository.RecordAsync(
            administratorUserId,
            RoomAuditActions.Create,
            createdRoom);

        return MapResponse(createdRoom);
    }

    public async Task<RoomResponse?> UpdateAsync(
        ulong roomId,
        UpdateRoomRequest request,
        ulong administratorUserId)
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
        if (updatedRoom is null)
        {
            return null;
        }
        await _roomAuditRepository.RecordAsync(
            administratorUserId,
            RoomAuditActions.Update,
            updatedRoom);

        return MapResponse(updatedRoom);
    }

    public async Task<bool> DeleteAsync(
        ulong roomId,
        ulong administratorUserId)
    {
        var existingRoom =
            await _roomRepository.GetByIdAsync(roomId);

        if (existingRoom is null)
        {
            return false;
        }

        if (await _roomRepository.HasActiveOccupantsAsync(roomId))
        {
            throw new OccupiedRoomDeletionException();
        }

        bool deleted =
            await _roomRepository.DeleteAsync(roomId);

        if (!deleted)
        {
            return false;
        }

        await _roomAuditRepository.RecordAsync(
            administratorUserId,
            RoomAuditActions.Delete,
            existingRoom);

        return true;
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
