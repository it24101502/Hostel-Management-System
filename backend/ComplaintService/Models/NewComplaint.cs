namespace ComplaintService.Models;

/// <summary>
/// A validated complaint that is ready to be saved.
/// </summary>
public class NewComplaint
{
    public ulong StudentUserId { get; set; }

    public string StudentUsername { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
