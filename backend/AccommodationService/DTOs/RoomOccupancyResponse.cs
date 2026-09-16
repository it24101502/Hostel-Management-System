namespace AccommodationService.DTOs;

public class RoomOccupancyResponse
{
    public ulong RoomId { get; set; }

    public ulong BlockId { get; set; }

    public string BlockCode { get; set; } = string.Empty;

    public string BlockName { get; set; } = string.Empty;

    public ushort FloorNumber { get; set; }

    public string RoomNumber { get; set; } = string.Empty;

    public ushort BedCapacity { get; set; }

    public int CurrentOccupancy { get; set; }

    public int AvailableBeds { get; set; }

    public string Status { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}