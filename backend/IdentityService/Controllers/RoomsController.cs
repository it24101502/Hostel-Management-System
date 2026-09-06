using IdentityService.Authorization;
using IdentityService.DTOs;
using IdentityService.Exceptions;
using IdentityService.Services;
using Microsoft.AspNetCore.Mvc;

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
        try
        {
            var createdRoom = await _roomService.CreateAsync(request);

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
        try
        {
            var updatedRoom =
                await _roomService.UpdateAsync(roomId, request);

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
    }

    [HttpDelete("{roomId:long}")]
    public async Task<IActionResult> DeleteRoom(ulong roomId)
    {
        bool deleted = await _roomService.DeleteAsync(roomId);

        if (!deleted)
        {
            return NotFound(new ErrorResponse
            {
                Message = "Room was not found."
            });
        }

        return NoContent();
    }
}
