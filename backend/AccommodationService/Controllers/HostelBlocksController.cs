using AccommodationService.Authorization;
using AccommodationService.DTOs;
using AccommodationService.Exceptions;
using AccommodationService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AccommodationService.Controllers;

[ApiController]
[Route("api/admin/blocks")]
[RequireRole("ADMIN")]
public class HostelBlocksController : ControllerBase
{
    private readonly IHostelBlockService
        _hostelBlockService;

    public HostelBlocksController(
        IHostelBlockService hostelBlockService)
    {
        _hostelBlockService = hostelBlockService;
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveBlocks()
    {
        var blocks =
            await _hostelBlockService.GetActiveAsync();

        return Ok(blocks);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBlock(
        [FromBody] CreateHostelBlockRequest request)
    {
        try
        {
            var createdBlock =
                await _hostelBlockService.CreateAsync(request);

            return StatusCode(
                StatusCodes.Status201Created,
                createdBlock);
        }
        catch (DuplicateHostelBlockException exception)
        {
            return Conflict(new ErrorResponse
            {
                Message = exception.Message
            });
        }
    }
}