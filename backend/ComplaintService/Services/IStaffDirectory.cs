namespace ComplaintService.Services;

/// <summary>
/// Checks staff accounts with IdentityService, the service that
/// owns user accounts.
/// </summary>
public interface IStaffDirectory
{
    /// <summary>
    /// True if the user is an active WARDEN, HOSTEL_MASTER or ADMIN.
    /// Throws <see cref="StaffDirectoryUnavailableException"/> if
    /// IdentityService cannot be reached.
    /// </summary>
    Task<bool> IsActiveStaffAsync(
        ulong userId,
        CancellationToken cancellationToken = default);
}

public sealed class StaffDirectoryUnavailableException : Exception
{
    public StaffDirectoryUnavailableException(
        string message,
        Exception? inner = null)
        : base(message, inner)
    {
    }
}
