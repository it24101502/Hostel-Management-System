using IdentityService.DTOs;
using IdentityService.Models;
using IdentityService.Repositories;
using IdentityService.Services;
using IdentityService.Exceptions;

namespace IdentityService.Tests;

public class RoomServiceTests
{
    [Fact]
    public async Task CreateRoom_WithValidDetails_CreatesRoom()
    {
        var repository = new FakeRoomRepository();
        var service = new RoomService(repository);

        var request = new CreateRoomRequest
        {
            BlockId = 1,
            FloorNumber = 2,
            RoomNumber = " 201 ",
            BedCapacity = 4
        };

        var result = await service.CreateAsync(request);

        Assert.Equal((ulong)1, result.RoomId);
        Assert.Equal((ulong)1, result.BlockId);
        Assert.Equal((ushort)2, result.FloorNumber);
        Assert.Equal("201", result.RoomNumber);
        Assert.Equal((ushort)4, result.BedCapacity);
    }

    [Fact]
    public async Task GetAllRooms_ReturnsAllRooms()
    {
        var repository = new FakeRoomRepository();
        repository.Rooms.Add(CreateRoom(1, "101"));
        repository.Rooms.Add(CreateRoom(2, "102"));

        var service = new RoomService(repository);

        var result = await service.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, room => room.RoomNumber == "101");
        Assert.Contains(result, room => room.RoomNumber == "102");
    }

    [Fact]
    public async Task GetRoom_WithExistingId_ReturnsRoom()
    {
        var repository = new FakeRoomRepository();
        repository.Rooms.Add(CreateRoom(10, "301"));

        var service = new RoomService(repository);

        var result = await service.GetByIdAsync(10);

        Assert.NotNull(result);
        Assert.Equal((ulong)10, result.RoomId);
        Assert.Equal("301", result.RoomNumber);
    }

    [Fact]
    public async Task UpdateRoom_WithValidDetails_UpdatesRoom()
    {
        var repository = new FakeRoomRepository();
        repository.Rooms.Add(CreateRoom(20, "401"));

        var service = new RoomService(repository);

        var request = new UpdateRoomRequest
        {
            BlockId = 1,
            FloorNumber = 4,
            RoomNumber = "402",
            BedCapacity = 6
        };

        var result = await service.UpdateAsync(20, request);

        Assert.NotNull(result);
        Assert.Equal("402", result.RoomNumber);
        Assert.Equal((ushort)6, result.BedCapacity);
    }

    [Fact]
    public async Task DeleteRoom_WithExistingId_DeletesRoom()
    {
        var repository = new FakeRoomRepository();
        repository.Rooms.Add(CreateRoom(30, "501"));

        var service = new RoomService(repository);

        bool result = await service.DeleteAsync(30);

        Assert.True(result);
        Assert.Empty(repository.Rooms);
    }

    [Fact]
    public async Task CreateRoom_WithZeroCapacity_ThrowsException()
    {
        var repository = new FakeRoomRepository();
        var service = new RoomService(repository);

        var request = new CreateRoomRequest
        {
            BlockId = 1,
            FloorNumber = 1,
            RoomNumber = "101",
            BedCapacity = 0
        };

        var exception =
            await Assert.ThrowsAsync<InvalidRoomCapacityException>(
                () => service.CreateAsync(request));

        Assert.Equal(
            "Bed capacity must be greater than zero.",
            exception.Message);

        Assert.Empty(repository.Rooms);
    }

    [Fact]
    public async Task UpdateRoom_WithZeroCapacity_ThrowsException()
    {
        var repository = new FakeRoomRepository();
        repository.Rooms.Add(CreateRoom(40, "601"));

        var service = new RoomService(repository);

        var request = new UpdateRoomRequest
        {
            BlockId = 1,
            FloorNumber = 6,
            RoomNumber = "601",
            BedCapacity = 0
        };

        var exception =
            await Assert.ThrowsAsync<InvalidRoomCapacityException>(
                () => service.UpdateAsync(40, request));

        Assert.Equal(
            "Bed capacity must be greater than zero.",
            exception.Message);

        Assert.Equal((ushort)4, repository.Rooms[0].BedCapacity);
    }

    private static HostelRoom CreateRoom(
        ulong roomId,
        string roomNumber)
    {
        return new HostelRoom
        {
            RoomId = roomId,
            BlockId = 1,
            BlockCode = "A",
            BlockName = "Block A",
            FloorNumber = 1,
            RoomNumber = roomNumber,
            BedCapacity = 4,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private sealed class FakeRoomRepository : IRoomRepository
    {
        public List<HostelRoom> Rooms { get; } = [];

        public bool BlockExists { get; set; } = true;

        public bool DuplicateLocationExists { get; set; }

        public Task<IReadOnlyList<HostelRoom>> GetAllAsync()
        {
            IReadOnlyList<HostelRoom> result = Rooms;

            return Task.FromResult(result);
        }

        public Task<HostelRoom?> GetByIdAsync(ulong roomId)
        {
            var room = Rooms.FirstOrDefault(
                item => item.RoomId == roomId);

            return Task.FromResult(room);
        }

        public Task<bool> BlockExistsAsync(ulong blockId)
        {
            return Task.FromResult(BlockExists);
        }

        public Task<bool> LocationExistsAsync(
            ulong blockId,
            ushort floorNumber,
            string roomNumber,
            ulong? excludedRoomId = null)
        {
            return Task.FromResult(DuplicateLocationExists);
        }

        public Task<ulong> CreateAsync(CreateRoomRequest request)
        {
            ulong roomId = Rooms.Count == 0
                ? 1
                : Rooms.Max(room => room.RoomId) + 1;

            Rooms.Add(new HostelRoom
            {
                RoomId = roomId,
                BlockId = request.BlockId,
                BlockCode = "A",
                BlockName = "Block A",
                FloorNumber = request.FloorNumber,
                RoomNumber = request.RoomNumber.Trim(),
                BedCapacity = request.BedCapacity,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            return Task.FromResult(roomId);
        }

        public Task<bool> UpdateAsync(
            ulong roomId,
            UpdateRoomRequest request)
        {
            var room = Rooms.FirstOrDefault(
                item => item.RoomId == roomId);

            if (room is null)
            {
                return Task.FromResult(false);
            }

            room.BlockId = request.BlockId;
            room.FloorNumber = request.FloorNumber;
            room.RoomNumber = request.RoomNumber.Trim();
            room.BedCapacity = request.BedCapacity;
            room.UpdatedAt = DateTime.UtcNow;

            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(ulong roomId)
        {
            var room = Rooms.FirstOrDefault(
                item => item.RoomId == roomId);

            if (room is null)
            {
                return Task.FromResult(false);
            }

            Rooms.Remove(room);

            return Task.FromResult(true);
        }
    }
}
