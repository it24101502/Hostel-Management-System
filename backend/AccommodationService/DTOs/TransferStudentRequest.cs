using System.ComponentModel.DataAnnotations;

namespace AccommodationService.DTOs;

public class TransferStudentRequest
{
    [Range(
        1,
        long.MaxValue,
        ErrorMessage = "New room ID must be greater than zero.")]
    public ulong NewRoomId { get; set; }
}