namespace ComplaintService.DTOs;

public class AssignComplaintRequest
{
    // Leave empty to assign the complaint to yourself.
    public ulong? AssignedToUserId { get; set; }
}