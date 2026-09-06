using System.ComponentModel.DataAnnotations;

namespace IdentityService.DTOs;

public class CreateRoomRequest
{
    public ulong BlockId { get; set; }

    public ushort FloorNumber { get; set; }

    [Required]
    [StringLength(20)]
    public string RoomNumber { get; set; } = string.Empty;

    public ushort BedCapacity { get; set; }
}
