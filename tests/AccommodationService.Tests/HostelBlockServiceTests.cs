using AccommodationService.DTOs;
using AccommodationService.Exceptions;
using AccommodationService.Models;
using AccommodationService.Repositories;
using AccommodationService.Services;

namespace AccommodationService.Tests;

public class HostelBlockServiceTests
{
    [Fact]
    public async Task GetActiveAsync_ReturnsActiveBlocks()
    {
        var repository = new FakeHostelBlockRepository();

        repository.Blocks.Add(new HostelBlock
        {
            BlockId = 1,
            BlockCode = "BLOCK-A",
            BlockName = "Hostel Block A",
            IsActive = true
        });

        repository.Blocks.Add(new HostelBlock
        {
            BlockId = 2,
            BlockCode = "BLOCK-B",
            BlockName = "Hostel Block B",
            IsActive = false
        });

        var service = new HostelBlockService(repository);

        var result = await service.GetActiveAsync();

        var block = Assert.Single(result);

        Assert.Equal((ulong)1, block.BlockId);
        Assert.Equal("BLOCK-A", block.BlockCode);
        Assert.Equal("Hostel Block A", block.BlockName);
        Assert.True(block.IsActive);
    }

    [Fact]
    public async Task CreateAsync_WithUniqueDetails_CreatesBlock()
    {
        var repository = new FakeHostelBlockRepository();
        var service = new HostelBlockService(repository);

        var request = new CreateHostelBlockRequest
        {
            BlockCode = " block-c ",
            BlockName = " Hostel Block C "
        };

        var result = await service.CreateAsync(request);

        Assert.Equal((ulong)1, result.BlockId);
        Assert.Equal("BLOCK-C", result.BlockCode);
        Assert.Equal("Hostel Block C", result.BlockName);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateDetails_ThrowsException()
    {
        var repository = new FakeHostelBlockRepository
        {
            DuplicateExists = true
        };

        var service = new HostelBlockService(repository);

        var request = new CreateHostelBlockRequest
        {
            BlockCode = "BLOCK-A",
            BlockName = "Hostel Block A"
        };

        await Assert.ThrowsAsync<DuplicateHostelBlockException>(
            () => service.CreateAsync(request));
    }

    private sealed class FakeHostelBlockRepository
        : IHostelBlockRepository
    {
        public List<HostelBlock> Blocks { get; } = [];

        public bool DuplicateExists { get; set; }

        public Task<IReadOnlyList<HostelBlock>> GetActiveAsync()
        {
            IReadOnlyList<HostelBlock> activeBlocks =
                Blocks
                    .Where(block => block.IsActive)
                    .ToList();

            return Task.FromResult(activeBlocks);
        }

        public Task<HostelBlock?> GetByIdAsync(ulong blockId)
        {
            HostelBlock? block = Blocks.FirstOrDefault(
                item => item.BlockId == blockId);

            return Task.FromResult(block);
        }

        public Task<bool> DuplicateExistsAsync(
            string blockCode,
            string blockName)
        {
            return Task.FromResult(DuplicateExists);
        }

        public Task<ulong> CreateAsync(
            CreateHostelBlockRequest request)
        {
            ulong blockId =
                Blocks.Count == 0
                    ? 1
                    : Blocks.Max(block => block.BlockId) + 1;

            Blocks.Add(new HostelBlock
            {
                BlockId = blockId,
                BlockCode =
                    request.BlockCode.Trim().ToUpperInvariant(),
                BlockName = request.BlockName.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            return Task.FromResult(blockId);
        }
    }
}