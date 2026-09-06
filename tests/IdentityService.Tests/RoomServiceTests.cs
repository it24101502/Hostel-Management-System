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
        var service = new RoomService(repository, new FakeRoomAuditRepository());

        var request = new CreateRoomRequest
        {
            BlockId = 1,
            FloorNumber = 2,
            RoomNumber = " 201 ",
            BedCapacity = 4
        };

        var result = await service.CreateAsync(request, 1);

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

        var service = new RoomService(repository, new FakeRoomAuditRepository());

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

        var service = new RoomService(repository, new FakeRoomAuditRepository());

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

        var service = new RoomService(repository, new FakeRoomAuditRepository());

        var request = new UpdateRoomRequest
        {
            BlockId = 1,
            FloorNumber = 4,
            RoomNumber = "402",
            BedCapacity = 6
        };

        var result = await service.UpdateAsync(20, request, 1);

        Assert.NotNull(result);
        Assert.Equal("402", result.RoomNumber);
        Assert.Equal((ushort)6, result.BedCapacity);
    }

    [Fact]
    public async Task DeleteRoom_WithExistingId_DeletesRoom()
    {
        var repository = new FakeRoomRepository();
        repository.Rooms.Add(CreateRoom(30, "501"));

        var service = new RoomService(repository, new FakeRoomAuditRepository());

        bool result = await service.DeleteAsync(30, 1);

        Assert.True(result);
        Assert.Empty(repository.Rooms);
    }

    [Fact]
    public async Task DeleteRoom_WithActiveOccupants_ThrowsException()
    {
        var repository = new FakeRoomRepository
        {
            HasActiveOccupants = true
        };

        repository.Rooms.Add(CreateRoom(50, "701"));

        var service = new RoomService(repository, new FakeRoomAuditRepository());

        var exception =
            await Assert.ThrowsAsync<OccupiedRoomDeletionException>(
                () => service.DeleteAsync(50, 1));

        Assert.Equal(
            "The room cannot be deleted because it has active occupants.",
            exception.Message);

        Assert.Single(repository.Rooms);
        Assert.Equal((ulong)50, repository.Rooms[0].RoomId);
    }

    [Fact]
    public async Task CreateRoom_RecordsAuditEntry()
    {
        var repository = new FakeRoomRepository();
        var auditRepository =
            new FakeRoomAuditRepository();

        var service = new RoomService(
            repository,
            auditRepository);

        var request = new CreateRoomRequest
        {
            BlockId = 1,
            FloorNumber = 1,
            RoomNumber = "801",
            BedCapacity = 4
        };

        var result =
            await service.CreateAsync(request, 99);

        var audit = Assert.Single(
            auditRepository.Records);

        Assert.Equal(
            (ulong)99,
            audit.AdministratorUserId);

        Assert.Equal(
            RoomAuditActions.Create,
            audit.Action);

        Assert.Equal(result.RoomId, audit.Room.RoomId);
        Assert.Equal("801", audit.Room.RoomNumber);
    }

    [Fact]
    public async Task UpdateRoom_RecordsAuditEntry()
    {
        var repository = new FakeRoomRepository();
        repository.Rooms.Add(CreateRoom(60, "901"));

        var auditRepository =
           new FakeRoomAuditRepository();

        var service = new RoomService(
            repository,
            auditRepository);

        var request = new UpdateRoomRequest
        {
            BlockId = 1,
            FloorNumber = 9,
            RoomNumber = "902",
            BedCapacity = 6
        };

        await service.UpdateAsync(60, request, 99);

        var audit = Assert.Single(
            auditRepository.Records);

        Assert.Equal(
            RoomAuditActions.Update,
            audit.Action);

        Assert.Equal((ulong)60, audit.Room.RoomId);
        Assert.Equal("902", audit.Room.RoomNumber);
        Assert.Equal((ushort)6, audit.Room.BedCapacity);
    }

    [Fact]
    public async Task DeleteRoom_RecordsAuditEntry()
    {
        var repository = new FakeRoomRepository();
        repository.Rooms.Add(CreateRoom(70, "1001"));

        var auditRepository =
            new FakeRoomAuditRepository();

        var service = new RoomService(
            repository,
            auditRepository);

        bool deleted =
            await service.DeleteAsync(70, 99);

        Assert.True(deleted);

        var audit = Assert.Single(
            auditRepository.Records);

        Assert.Equal(
            RoomAuditActions.Delete,
            audit.Action);

        Assert.Equal((ulong)70, audit.Room.RoomId);
        Assert.Equal("1001", audit.Room.RoomNumber);
    }

    [Fact]
    public async Task CreateRoom_WithZeroCapacity_ThrowsException()
    {
        var repository = new FakeRoomRepository();
        var service = new RoomService(
                          repository,
                          new FakeRoomAuditRepository());

        var request = new CreateRoomRequest
        {
            BlockId = 1,
            FloorNumber = 1,
            RoomNumber = "101",
            BedCapacity = 0
        };

        var exception =
            await Assert.ThrowsAsync<InvalidRoomCapacityException>(
                () => service.CreateAsync(request, 1));

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

        var service = new RoomService(repository, new FakeRoomAuditRepository());

        var request = new UpdateRoomRequest
        {
            BlockId = 1,
            FloorNumber = 6,
            RoomNumber = "601",
            BedCapacity = 0
        };

        var exception =
            await Assert.ThrowsAsync<InvalidRoomCapacityException>(
                () => service.UpdateAsync(40, request, 1));

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

    private sealed class FakeRoomAuditRepository
        : IRoomAuditRepository
    {
        public List<(
            ulong AdministratorUserId,
            string Action,
            HostelRoom Room)> Records { get; } = [];

        public Task RecordAsync(
            ulong administratorUserId,
            string action,
            HostelRoom room)
        {
            Records.Add((
                administratorUserId,
                action,
                room));

            return Task.CompletedTask;
        }
    }

    private sealed class FakeRoomRepository : IRoomRepository
    {
        public List<HostelRoom> Rooms { get; } = [];

        public bool BlockExists { get; set; } = true;

        public bool DuplicateLocationExists { get; set; }

        public bool HasActiveOccupants { get; set; }

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

        public Task<bool> HasActiveOccupantsAsync(ulong roomId)
        {
            return Task.FromResult(HasActiveOccupants);
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
