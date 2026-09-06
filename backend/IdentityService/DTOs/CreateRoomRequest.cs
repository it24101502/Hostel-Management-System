using System.ComponentModel.DataAnnotations;

namespace IdentityService.DTOs;

public class CreateRoomRequest
{
    public ulong BlockId { get; set; }

    public ushort FloorNumber { get; set; }

    [Required]
    [StringLength(20)]
    public string RoomNumber { get; set; } = string.Empty;

    [Range(
        1,
        ushort.MaxValue,
        ErrorMessage = "Bed capacity must be greater than zero.")]
    public ushort BedCapacity { get; set; }
}
