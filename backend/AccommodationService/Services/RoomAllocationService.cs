using AccommodationService.DTOs;
using AccommodationService.Events;
using AccommodationService.Models;
using AccommodationService.Repositories;
using Microsoft.Extensions.Logging;

namespace AccommodationService.Services;

public class RoomAllocationService : IRoomAllocationService
{
    private readonly IRoomAllocationRepository _allocationRepository;
    private readonly IAllocationEventPublisher? _eventPublisher;
    private readonly ILogger<RoomAllocationService>? _logger;

    public RoomAllocationService(
        IRoomAllocationRepository allocationRepository)
    {
        _allocationRepository = allocationRepository;
    }

    public RoomAllocationService(
        IRoomAllocationRepository allocationRepository,
        IAllocationEventPublisher eventPublisher,
        ILogger<RoomAllocationService> logger)
    {
        _allocationRepository = allocationRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AllocationResponse>> GetAllAsync()
    {
        var allocations = await _allocationRepository.GetAllAsync();

        return allocations
            .Select(MapResponse)
            .ToList();
    }

    public async Task<AllocationResponse?> GetByStudentIdAsync(ulong studentProfileId)
    {
        var allocation = await _allocationRepository.GetByStudentIdAsync(studentProfileId);

        return allocation is null
            ? null
            : MapResponse(allocation);
    }

    public async Task<AllocationResponse> AllocateAsync(
        AllocateStudentRequest request,
        ulong administratorUserId)
    {
        var allocation = await _allocationRepository.AllocateAsync(
            request, 
            administratorUserId);

        await PublishChangeAsync(allocation.StudentProfileId, allocation);

        return MapResponse(allocation);
    }

    public async Task<AllocationResponse> TransferAsync(
        ulong studentProfileId,
        TransferStudentRequest request,
        ulong administratorUserId)
    {
        var allocation = await _allocationRepository.TransferAsync(
            studentProfileId,
            request,
            administratorUserId);

        await PublishChangeAsync(studentProfileId, allocation);

        return MapResponse(allocation);
    }

    public async Task<bool> ReleaseAsync(
        ulong studentProfileId,
        ulong administratorUserId)
    {
        bool released = await _allocationRepository.ReleaseAsync(
            studentProfileId,
            administratorUserId);

        if (released)
        {
            await PublishChangeAsync(studentProfileId, null);
        }

        return released;
    }

    public Task<IReadOnlyList<RoomOccupancyResponse>> GetOccupancyReportAsync(
        ulong? blockId,
        ushort? floorNumber)
    {
        return _allocationRepository.GetOccupancyReportAsync(
            blockId,
            floorNumber);
    }

    // The allocation is already committed, so a messaging failure
    // must never fail the request.
    private async Task PublishChangeAsync(
        ulong studentProfileId,
        StudentRoomAllocation? allocation)
    {
        if (_eventPublisher is null) return;

        try
        {
            await _eventPublisher.PublishAsync(
                new StudentAllocationChangedEvent(
                    Guid.NewGuid(),
                    studentProfileId,
                    allocation?.BlockId,
                    allocation?.BlockCode,
                    allocation?.BlockName,
                    DateTimeOffset.UtcNow));
        }
        catch (Exception exception)
        {
            _logger?.LogWarning(
                exception,
                "Unable to publish allocation change for student {StudentProfileId}.",
                studentProfileId);
        }
    }

    private static AllocationResponse MapResponse(
        StudentRoomAllocation allocation)
    {
        int availableBeds = Math.Max(
            allocation.BedCapacity - allocation.CurrentOccupancy,
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
            CurrentOccupancy = allocation.CurrentOccupancy,
            AvailableBeds = availableBeds,
            Status = status,
            AllocatedAt = allocation.AllocatedAt,
            UpdatedAt = allocation.UpdatedAt
        };
    }
}