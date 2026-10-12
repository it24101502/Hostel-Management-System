namespace ComplaintService.Models;

/// <summary>
/// Allowed complaint categories. These values match
/// chk_complaints_category in the database.
/// </summary>
public static class ComplaintCategories
{
    public static readonly IReadOnlyList<string> All = new[]
    {
        "MAINTENANCE",
        "ELECTRICAL",
        "PLUMBING",
        "CLEANLINESS",
        "SECURITY",
        "FOOD",
        "NOISE",
        "OTHER"
    };
}
