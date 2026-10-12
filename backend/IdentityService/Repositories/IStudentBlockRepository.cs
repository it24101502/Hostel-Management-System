namespace IdentityService.Repositories;

public interface IStudentBlockRepository
{
    /// Mirrors the block (same ID as AccommodationService) and sets
    /// the student's block. A null blockId clears it.
    Task ApplyAllocationChangeAsync(
        ulong studentProfileId,
        ulong? blockId,
        string? blockCode,
        string? blockName,
        CancellationToken cancellationToken = default);
}