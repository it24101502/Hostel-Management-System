using IdentityService.Authorization;
using IdentityService.DTOs;
using IdentityService.Exceptions;
using IdentityService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/admin/rooms")]
[RequireRole("ADMIN")]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _roomService;

    public RoomsController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateRoom(
        [FromBody] CreateRoomRequest request)
    {
        if (!TryGetAuthenticatedUserId(
                out ulong administratorUserId))
        {
            return Unauthorized(new ErrorResponse
            {
            Message =
                "The authenticated Administrator ID is missing or invalid."
            });
        }
        try
        {
            var createdRoom = await _roomService.CreateAsync(request, administratorUserId);

            return CreatedAtAction(
                nameof(GetRoomById),
                new { roomId = createdRoom.RoomId },
                createdRoom);
        }
        catch (DuplicateRoomException exception)
        {
            return Conflict(new ErrorResponse
            {
                Message = exception.Message
            });
        }
        catch (HostelBlockNotFoundException exception)
        {
            return BadRequest(new ErrorResponse
            {
                Message = exception.Message
            });
        }
        catch (InvalidRoomCapacityException exception)
        {
            return BadRequest(new ErrorResponse
            {
                Message = exception.Message
            });
        }
    }
    [HttpGet]
    public async Task<IActionResult> GetRooms()
    {
        var rooms = await _roomService.GetAllAsync();

        return Ok(rooms);
    }

    [HttpGet("{roomId:long}")]
    public async Task<IActionResult> GetRoomById(ulong roomId)
    {
        var room = await _roomService.GetByIdAsync(roomId);

        if (room is null)
        {
            return NotFound(new ErrorResponse
            {
                Message = "Room was not found."
            });
        }

        return Ok(room);
    }

    [HttpPut("{roomId:long}")]
    public async Task<IActionResult> UpdateRoom(
        ulong roomId,
        [FromBody] UpdateRoomRequest request)
    {
        if (!TryGetAuthenticatedUserId(
                out ulong administratorUserId))
        {
            return Unauthorized(new ErrorResponse
            {
                Message =
                    "The authenticated Administrator ID is missing or invalid."
            });
        }
        try
        {
            var updatedRoom =
                await _roomService.UpdateAsync(roomId, request, administratorUserId);

            if (updatedRoom is null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = "Room was not found."
                });
            }

            return Ok(updatedRoom);
        }
        catch (DuplicateRoomException exception)
        {
            return Conflict(new ErrorResponse
            {
                Message = exception.Message
            });
        }
        catch (HostelBlockNotFoundException exception)
        {
            return BadRequest(new ErrorResponse
            {
                Message = exception.Message
            });
        }
        catch (InvalidRoomCapacityException exception)
        {
            return BadRequest(new ErrorResponse
        {
            Message = exception.Message
            });
        }
    }

    [HttpDelete("{roomId:long}")]
    public async Task<IActionResult> DeleteRoom(ulong roomId)
    {
        if (!TryGetAuthenticatedUserId(
                out ulong administratorUserId))
        {
            return Unauthorized(new ErrorResponse
            {
                Message =
                    "The authenticated Administrator ID is missing or invalid."
            });
        }
        try
        {
            bool deleted =
                await _roomService.DeleteAsync(roomId, administratorUserId);

            if (!deleted)
            {
                return NotFound(new ErrorResponse
                {
                    Message = "Room was not found."
                });
            }

            return NoContent();
        }
        catch (OccupiedRoomDeletionException exception)
        {
        return Conflict(new ErrorResponse
            {
                Message = exception.Message
            });
        }
    }
    private bool TryGetAuthenticatedUserId(
        out ulong userId)
    {
        string? userIdValue =
            User.FindFirst(
                JwtRegisteredClaimNames.Sub)
                ?.Value;

        return ulong.TryParse(
            userIdValue,
            out userId);
    }
}
