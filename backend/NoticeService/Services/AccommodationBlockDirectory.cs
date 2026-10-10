using System.Net.Http.Headers;
using System.Text.Json;

namespace NoticeService.Services;

public sealed class AccommodationBlockDirectory : IBlockDirectory
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AccommodationBlockDirectory> _logger;

    public AccommodationBlockDirectory(
        HttpClient httpClient,
        ILogger<AccommodationBlockDirectory> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> IsActiveBlockAsync(
        ulong blockId,
        string? authorizationHeader,
        CancellationToken cancellationToken = default)
    {
        if (_httpClient.BaseAddress is null)
        {
            throw new BlockDirectoryUnavailableException(
                "Services:AccommodationBaseUrl is not configured.");
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "api/admin/blocks");

            // Forward the caller's own token: the blocks endpoint
            // already allows WARDEN, HOSTEL_MASTER and ADMIN.
            if (AuthenticationHeaderValue.TryParse(
                    authorizationHeader,
                    out var header))
            {
                request.Headers.Authorization = header;
            }

            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            await using var body =
                await response.Content.ReadAsStreamAsync(
                    cancellationToken);

            using var document = await JsonDocument.ParseAsync(
                body,
                cancellationToken: cancellationToken);

            foreach (var block in document.RootElement.EnumerateArray())
            {
                if (block.TryGetProperty("blockId", out var id) &&
                    id.TryGetUInt64(out ulong value) &&
                    value == blockId)
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception exception) when (
            exception is HttpRequestException
                or TaskCanceledException
                or JsonException
                or InvalidOperationException)
        {
            _logger.LogWarning(
                exception,
                "Could not load hostel blocks from AccommodationService.");

            throw new BlockDirectoryUnavailableException(
                "Hostel blocks could not be verified right now.",
                exception);
        }
    }
}
