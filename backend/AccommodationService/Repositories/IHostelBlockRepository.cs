using AccommodationService.DTOs;
using AccommodationService.Models;

namespace AccommodationService.Repositories;

public interface IHostelBlockRepository
{
    Task<IReadOnlyList<HostelBlock>> GetActiveAsync();

    Task<HostelBlock?> GetByIdAsync(ulong blockId);

    Task<bool> DuplicateExistsAsync(
        string blockCode,
        string blockName);

    Task<ulong> CreateAsync(
        CreateHostelBlockRequest request);
}