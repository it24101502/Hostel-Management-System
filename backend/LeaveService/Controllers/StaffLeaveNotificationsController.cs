using System.IdentityModel.Tokens.Jwt;
using LeaveService.Authorization;
using LeaveService.DTOs;
using LeaveService.Models;
using LeaveService.Services;
using Microsoft.AspNetCore.Mvc;

namespace LeaveService.Controllers;

[ApiController]
[Route("api/staff/leave-notifications")]
[RequireRole(LeaveRoles.Warden, LeaveRoles.HostelMaster)]
public class StaffLeaveNotificationsController : ControllerBase
{
    private readonly ILeaveNotificationService _notificationService;

    public StaffLeaveNotificationsController(
        ILeaveNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] bool unreadOnly = false)
    {
        var notifications =
            await _notificationService.GetStaffNotificationsAsync(
                unreadOnly);

        return Ok(notifications);
    }

    [HttpPut("{notificationId:long}/read")]
    public async Task<IActionResult> MarkRead(
        ulong notificationId)
    {
        string? userIdValue =
            User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!ulong.TryParse(userIdValue, out ulong staffUserId))
        {
            return Unauthorized(new ErrorResponse
            {
                Message =
                    "The authenticated staff user ID is missing or invalid."
            });
        }

        bool exists =
            await _notificationService.MarkReadAsync(
                notificationId,
                staffUserId);

        if (!exists)
        {
            return NotFound(new ErrorResponse
            {
                Message = "The notification was not found."
            });
        }

        return NoContent();
    }
}
