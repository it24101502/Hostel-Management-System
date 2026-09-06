namespace IdentityService.Models;

public class HostelRoom
{
    public ulong RoomId { get; set; }

    public ulong BlockId { get; set; }

    public string BlockCode { get; set; } = string.Empty;

    public string BlockName { get; set; } = string.Empty;

    public ushort FloorNumber { get; set; }

    public string RoomNumber { get; set; } = string.Empty;

    public ushort BedCapacity { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
