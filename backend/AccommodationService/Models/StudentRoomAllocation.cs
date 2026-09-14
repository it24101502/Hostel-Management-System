namespace AccommodationService.Models;

public class StudentRoomAllocation
{
    public ulong AllocationId { get; set; }

    public ulong StudentProfileId { get; set; }

    public ulong RoomId { get; set; }

    public ulong BlockId { get; set; }

    public string BlockCode { get; set; } = string.Empty;

    public string BlockName { get; set; } = string.Empty;

    public ushort FloorNumber { get; set; }

    public string RoomNumber { get; set; } = string.Empty;

    public ushort BedCapacity { get; set; }

    public bool IsActive { get; set; }

    public int CurrentOccupancy { get; set; }

    public DateTime AllocatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}