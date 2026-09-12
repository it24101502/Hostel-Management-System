using AccommodationService.DTOs;
using AccommodationService.Exceptions;
using AccommodationService.Models;
using AccommodationService.Repositories;
using AccommodationService.Services;

namespace AccommodationService.Tests;

public class RoomAllocationServiceTests
{
    [Fact]
    public async Task AllocateStudent_WithAvailableRoom_ReturnsAllocation()
    {
        var repository = new FakeRoomAllocationRepository
        {
            AllocationResult = CreateAllocation(
                studentProfileId: 101,
                roomId: 10,
                bedCapacity: 4,
                currentOccupancy: 2)
        };

        var service = new RoomAllocationService(repository);

        var request = new AllocateStudentRequest
        {
            StudentProfileId = 101,
            RoomId = 10
        };

        var result = await service.AllocateAsync(request, 7);

        Assert.Equal((ulong)101, result.StudentProfileId);
        Assert.Equal((ulong)10, result.RoomId);
        Assert.Equal(2, result.CurrentOccupancy);
        Assert.Equal(2, result.AvailableBeds);
        Assert.Equal("AVAILABLE", result.Status);
        Assert.Equal((ulong)7, repository.LastAdministratorUserId);
    }

    [Fact]
    public async Task AllocateStudent_WhenRoomIsFull_ThrowsException()
    {
        var repository = new FakeRoomAllocationRepository
        {
            AllocationException =
                new RoomCapacityExceededException()
        };

        var service = new RoomAllocationService(repository);

        var request = new AllocateStudentRequest
        {
            StudentProfileId = 102,
            RoomId = 11
        };

        await Assert.ThrowsAsync<RoomCapacityExceededException>(
            () => service.AllocateAsync(request, 7));
    }

    [Fact]
    public async Task TransferStudent_ToAvailableRoom_ReturnsUpdatedAllocation()
    {
        var repository = new FakeRoomAllocationRepository
        {
            TransferResult = CreateAllocation(
                studentProfileId: 103,
                roomId: 12,
                bedCapacity: 2,
                currentOccupancy: 2)
        };

        var service = new RoomAllocationService(repository);

        var request = new TransferStudentRequest
        {
            NewRoomId = 12
        };

        var result = await service.TransferAsync(103, request, 8);

        Assert.Equal((ulong)103, result.StudentProfileId);
        Assert.Equal((ulong)12, result.RoomId);
        Assert.Equal(0, result.AvailableBeds);
        Assert.Equal("FULL", result.Status);
        Assert.Equal((ulong)103, repository.LastStudentProfileId);
        Assert.Equal((ulong)8, repository.LastAdministratorUserId);
    }

    [Fact]
    public async Task GetByStudentId_WhenMissing_ReturnsNull()
    {
        var repository = new FakeRoomAllocationRepository();
        var service = new RoomAllocationService(repository);

        var result = await service.GetByStudentIdAsync(404);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetOccupancyReport_WithFilters_ForwardsFilters()
    {
        var expectedReport = new List<RoomOccupancyResponse>
        {
            new()
            {
                RoomId = 10,
                BlockId = 2,
                BlockCode = "B",
                BlockName = "Block B",
                FloorNumber = 3,
                RoomNumber = "301",
                BedCapacity = 4,
                CurrentOccupancy = 1,
                AvailableBeds = 3,
                Status = "AVAILABLE",
                IsActive = true
            }
        };

        var repository = new FakeRoomAllocationRepository
        {
            OccupancyReport = expectedReport
        };

        var service = new RoomAllocationService(repository);

        var result =
            await service.GetOccupancyReportAsync(2, 3);

        Assert.Same(expectedReport, result);
        Assert.Equal((ulong)2, repository.LastBlockId);
        Assert.Equal((ushort)3, repository.LastFloorNumber);
    }

    private static StudentRoomAllocation CreateAllocation(
        ulong studentProfileId,
        ulong roomId,
        ushort bedCapacity,
        int currentOccupancy)
    {
        return new StudentRoomAllocation
        {
            AllocationId = 1,
            StudentProfileId = studentProfileId,
            RoomId = roomId,
            BlockId = 1,
            BlockCode = "A",
            BlockName = "Block A",
            FloorNumber = 1,
            RoomNumber = "101",
            BedCapacity = bedCapacity,
            IsActive = true,
            CurrentOccupancy = currentOccupancy,
            AllocatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private sealed class FakeRoomAllocationRepository
        : IRoomAllocationRepository
    {
        public StudentRoomAllocation? AllocationResult { get; set; }

        public StudentRoomAllocation? TransferResult { get; set; }

        public Exception? AllocationException { get; set; }

        public IReadOnlyList<RoomOccupancyResponse> OccupancyReport
            { get; set; } = Array.Empty<RoomOccupancyResponse>();

        public ulong LastAdministratorUserId { get; private set; }

        public ulong LastStudentProfileId { get; private set; }

        public ulong? LastBlockId { get; private set; }

        public ushort? LastFloorNumber { get; private set; }

        public Task<IReadOnlyList<StudentRoomAllocation>> GetAllAsync()
        {
            IReadOnlyList<StudentRoomAllocation> result =
                AllocationResult is null
                    ? Array.Empty<StudentRoomAllocation>()
                    : new[] { AllocationResult };

            return Task.FromResult(result);
        }

        public Task<StudentRoomAllocation?> GetByStudentIdAsync(
            ulong studentProfileId)
        {
            LastStudentProfileId = studentProfileId;
            return Task.FromResult(AllocationResult);
        }

        public Task<StudentRoomAllocation> AllocateAsync(
            AllocateStudentRequest request,
            ulong administratorUserId)
        {
            LastStudentProfileId = request.StudentProfileId;
            LastAdministratorUserId = administratorUserId;

            if (AllocationException is not null)
            {
                return Task.FromException<StudentRoomAllocation>(
                    AllocationException);
            }

            return Task.FromResult(
                AllocationResult
                ?? throw new InvalidOperationException(
                    "AllocationResult is not configured."));
        }

        public Task<StudentRoomAllocation> TransferAsync(
            ulong studentProfileId,
            TransferStudentRequest request,
            ulong administratorUserId)
        {
            LastStudentProfileId = studentProfileId;
            LastAdministratorUserId = administratorUserId;

            return Task.FromResult(
                TransferResult
                ?? throw new InvalidOperationException(
                    "TransferResult is not configured."));
        }

        public Task<IReadOnlyList<RoomOccupancyResponse>>
            GetOccupancyReportAsync(
                ulong? blockId,
                ushort? floorNumber)
        {
            LastBlockId = blockId;
            LastFloorNumber = floorNumber;

            return Task.FromResult(OccupancyReport);
        }

        public Task<bool> ReleaseAsync(
            ulong studentProfileId,
            ulong administratorUserId)
        {
            return Task.FromResult(false);
        }

    }
}
