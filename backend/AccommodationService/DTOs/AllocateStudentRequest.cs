using System.ComponentModel.DataAnnotations;

namespace AccommodationService.DTOs;

public class AllocateStudentRequest
{
    [Range(
        1,
        long.MaxValue,
        ErrorMessage = "Student profile ID must be greater than zero.")]
    public ulong StudentProfileId { get; set; }

    [Range(
        1,
        long.MaxValue,
        ErrorMessage = "Room ID must be greater than zero.")]
    public ulong RoomId { get; set; }
}