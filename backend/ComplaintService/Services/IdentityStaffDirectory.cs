using System.Net;
using System.Net.Http.Headers;

namespace ComplaintService.Services;

public sealed class IdentityStaffDirectory : IStaffDirectory
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<IdentityStaffDirectory> _logger;

    public IdentityStaffDirectory(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        ILogger<IdentityStaffDirectory> logger)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<bool> IsActiveStaffAsync(
        ulong userId,
        CancellationToken cancellationToken = default)
    {
        if (_httpClient.BaseAddress is null)
        {
            throw new StaffDirectoryUnavailableException(
                "Services:IdentityBaseUrl is not configured.");
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/staff-directory/{userId}");

            // Forward the caller's own token. The staff directory is
            // open to every role that can assign complaints.
            string? authorization =
                _httpContextAccessor.HttpContext?
                    .Request.Headers.Authorization.ToString();

            if (AuthenticationHeaderValue.TryParse(
                    authorization,
                    out var header))
            {
                request.Headers.Authorization = header;
            }

            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }

            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception exception) when (
            exception is HttpRequestException
                or TaskCanceledException)
        {
            _logger.LogWarning(
                exception,
                "Could not verify staff user {UserId} with IdentityService.",
                userId);

            throw new StaffDirectoryUnavailableException(
                "The staff member could not be verified right now.",
                exception);
        }
    }
}
