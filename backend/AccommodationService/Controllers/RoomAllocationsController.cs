using System.IdentityModel.Tokens.Jwt;
using AccommodationService.Authorization;
using AccommodationService.DTOs;
using AccommodationService.Exceptions;
using AccommodationService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AccommodationService.Controllers;

[ApiController]
[Route("api/admin/allocations")]
[RequireRole("ADMIN")]
public class RoomAllocationsController : ControllerBase
{
    private readonly IRoomAllocationService _allocationService;

    public RoomAllocationsController(
        IRoomAllocationService allocationService)
    {
        _allocationService = allocationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllocations()
    {
        var allocations =
            await _allocationService.GetAllAsync();

        return Ok(allocations);
    }

    [HttpGet("student/{studentProfileId:long}")]
    public async Task<IActionResult> GetByStudentId(
        ulong studentProfileId)
    {
        var allocation =
            await _allocationService.GetByStudentIdAsync(
                studentProfileId);

        if (allocation is null)
        {
            return NotFound(new ErrorResponse
            {
                Message =
                    "The student's active room allocation was not found."
            });
        }

        return Ok(allocation);
    }

    [HttpPost]
    public async Task<IActionResult> AllocateStudent(
        [FromBody] AllocateStudentRequest request)
    {
        if (!TryGetAuthenticatedUserId(
                out ulong administratorUserId))
        {
            return Unauthorized(CreateUserIdError());
        }

        try
        {
            var allocation =
                await _allocationService.AllocateAsync(
                    request,
                    administratorUserId);

            return CreatedAtAction(
                nameof(GetByStudentId),
                new
                {
                    studentProfileId =
                        allocation.StudentProfileId
                },
                allocation);
        }
        catch (RoomNotFoundException exception)
        {
            return NotFound(CreateError(exception));
        }
        catch (InactiveRoomException exception)
        {
            return BadRequest(CreateError(exception));
        }
        catch (RoomCapacityExceededException exception)
        {
            return Conflict(CreateError(exception));
        }
        catch (StudentAlreadyAllocatedException exception)
        {
            return Conflict(CreateError(exception));
        }
    }

    [HttpPut("student/{studentProfileId:long}/transfer")]
    public async Task<IActionResult> TransferStudent(
        ulong studentProfileId,
        [FromBody] TransferStudentRequest request)
    {
        if (!TryGetAuthenticatedUserId(
                out ulong administratorUserId))
        {
            return Unauthorized(CreateUserIdError());
        }

        try
        {
            var allocation =
                await _allocationService.TransferAsync(
                    studentProfileId,
                    request,
                    administratorUserId);

            return Ok(allocation);
        }
        catch (StudentAllocationNotFoundException exception)
        {
            return NotFound(CreateError(exception));
        }
        catch (RoomNotFoundException exception)
        {
            return NotFound(CreateError(exception));
        }
        catch (InactiveRoomException exception)
        {
            return BadRequest(CreateError(exception));
        }
        catch (RoomCapacityExceededException exception)
        {
            return Conflict(CreateError(exception));
        }
        catch (SameRoomTransferException exception)
        {
            return BadRequest(CreateError(exception));
        }
    }

    [HttpGet("occupancy")]
    public async Task<IActionResult> GetOccupancyReport(
        [FromQuery] ulong? blockId,
        [FromQuery] ushort? floorNumber)
    {
        var report =
            await _allocationService.GetOccupancyReportAsync(
                blockId,
                floorNumber);

        return Ok(report);
    }

    private bool TryGetAuthenticatedUserId(out ulong userId)
    {
        string? userIdValue =
            User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        return ulong.TryParse(userIdValue, out userId);
    }

    private static ErrorResponse CreateUserIdError()
    {
        return new ErrorResponse
        {
            Message =
                "The authenticated Administrator ID is missing or invalid."
        };
    }

    private static ErrorResponse CreateError(Exception exception)
    {
        return new ErrorResponse
        {
            Message = exception.Message
        };
    }
}
