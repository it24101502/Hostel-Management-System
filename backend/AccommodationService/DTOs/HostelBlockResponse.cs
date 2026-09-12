namespace AccommodationService.DTOs;

public class HostelBlockResponse
{
    public ulong BlockId { get; set; }

    public string BlockCode { get; set; } = string.Empty;

    public string BlockName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}