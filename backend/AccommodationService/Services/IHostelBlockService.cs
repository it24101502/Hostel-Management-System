using AccommodationService.DTOs;

namespace AccommodationService.Services;

public interface IHostelBlockService
{
    Task<IReadOnlyList<HostelBlockResponse>> GetActiveAsync();

    Task<HostelBlockResponse> CreateAsync(
        CreateHostelBlockRequest request);
}