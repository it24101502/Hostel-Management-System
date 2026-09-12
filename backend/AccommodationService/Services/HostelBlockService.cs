using AccommodationService.DTOs;
using AccommodationService.Exceptions;
using AccommodationService.Models;
using AccommodationService.Repositories;

namespace AccommodationService.Services;

public class HostelBlockService : IHostelBlockService
{
    private readonly IHostelBlockRepository
        _hostelBlockRepository;

    public HostelBlockService(
        IHostelBlockRepository hostelBlockRepository)
    {
        _hostelBlockRepository = hostelBlockRepository;
    }

    public async Task<IReadOnlyList<HostelBlockResponse>>
        GetActiveAsync()
    {
        var blocks =
            await _hostelBlockRepository.GetActiveAsync();

        return blocks.Select(MapResponse).ToList();
    }

    public async Task<HostelBlockResponse> CreateAsync(
        CreateHostelBlockRequest request)
    {
        bool duplicateExists =
            await _hostelBlockRepository.DuplicateExistsAsync(
                request.BlockCode,
                request.BlockName);

        if (duplicateExists)
        {
            throw new DuplicateHostelBlockException();
        }

        ulong blockId =
            await _hostelBlockRepository.CreateAsync(request);

        var createdBlock =
            await _hostelBlockRepository.GetByIdAsync(blockId);

        if (createdBlock is null)
        {
            throw new InvalidOperationException(
                "The hostel block was created but could not be retrieved.");
        }

        return MapResponse(createdBlock);
    }

    private static HostelBlockResponse MapResponse(
        HostelBlock block)
    {
        return new HostelBlockResponse
        {
            BlockId = block.BlockId,
            BlockCode = block.BlockCode,
            BlockName = block.BlockName,
            IsActive = block.IsActive
        };
    }
}