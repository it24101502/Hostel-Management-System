namespace NoticeService.Services;

/// <summary>
/// Checks hostel block IDs against AccommodationService, the
/// service that owns hostel blocks.
/// </summary>
public interface IBlockDirectory
{
    /// <summary>
    /// True if the block exists and is active. Throws
    /// <see cref="BlockDirectoryUnavailableException"/> when the
    /// block list cannot be loaded.
    /// </summary>
    Task<bool> IsActiveBlockAsync(
        ulong blockId,
        string? authorizationHeader,
        CancellationToken cancellationToken = default);
}

public sealed class BlockDirectoryUnavailableException : Exception
{
    public BlockDirectoryUnavailableException(
        string message,
        Exception? inner = null)
        : base(message, inner)
    {
    }
}
