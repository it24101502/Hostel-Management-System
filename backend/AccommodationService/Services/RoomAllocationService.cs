using AccommodationService.DTOs;
using AccommodationService.Models;
using AccommodationService.Repositories;

namespace AccommodationService.Services;

public class RoomAllocationService : IRoomAllocationService
{
    private readonly IRoomAllocationRepository
        _allocationRepository;

    public RoomAllocationService(
        IRoomAllocationRepository allocationRepository)
    {
        _allocationRepository = allocationRepository;
    }

    public async Task<IReadOnlyList<AllocationResponse>>
        GetAllAsync()
    {
        var allocations =
            await _allocationRepository.GetAllAsync();

        return allocations
            .Select(MapResponse)
            .ToList();
    }

    public async Task<AllocationResponse?>
        GetByStudentIdAsync(ulong studentProfileId)
    {
        var allocation =
            await _allocationRepository.GetByStudentIdAsync(
                studentProfileId);

        return allocation is null
            ? null
            : MapResponse(allocation);
    }

    public async Task<AllocationResponse> AllocateAsync(
        AllocateStudentRequest request,
        ulong administratorUserId)
    {
        var allocation =
            await _allocationRepository.AllocateAsync(
                request,
                administratorUserId);

        return MapResponse(allocation);
    }

    public async Task<AllocationResponse> TransferAsync(
        ulong studentProfileId,
        TransferStudentRequest request,
        ulong administratorUserId)
    {
        var allocation =
            await _allocationRepository.TransferAsync(
                studentProfileId,
                request,
                administratorUserId);

        return MapResponse(allocation);
    }

    public Task<IReadOnlyList<RoomOccupancyResponse>>
        GetOccupancyReportAsync(
            ulong? blockId,
            ushort? floorNumber)
    {
        return _allocationRepository.GetOccupancyReportAsync(
            blockId,
            floorNumber);
    }

    private static AllocationResponse MapResponse(
        StudentRoomAllocation allocation)
    {
        int availableBeds = Math.Max(
            allocation.BedCapacity -
            allocation.CurrentOccupancy,
            0);

        string status = !allocation.IsActive
            ? "INACTIVE"
            : availableBeds == 0
                ? "FULL"
                : "AVAILABLE";

        return new AllocationResponse
        {
            AllocationId = allocation.AllocationId,
            StudentProfileId = allocation.StudentProfileId,
            RoomId = allocation.RoomId,
            BlockId = allocation.BlockId,
            BlockCode = allocation.BlockCode,
            BlockName = allocation.BlockName,
            FloorNumber = allocation.FloorNumber,
            RoomNumber = allocation.RoomNumber,
            BedCapacity = allocation.BedCapacity,
            IsActive = allocation.IsActive,
            CurrentOccupancy =
                allocation.CurrentOccupancy,
            AvailableBeds = availableBeds,
            Status = status,
            AllocatedAt = allocation.AllocatedAt,
            UpdatedAt = allocation.UpdatedAt
        };
    }
}