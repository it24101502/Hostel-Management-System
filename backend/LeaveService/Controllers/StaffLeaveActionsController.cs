using System.IdentityModel.Tokens.Jwt;
using LeaveService.Authorization;
using LeaveService.DTOs;
using LeaveService.Exceptions;
using LeaveService.Models;
using LeaveService.Services;
using Microsoft.AspNetCore.Mvc;

namespace LeaveService.Controllers;

/// <summary>
/// Actions that change a leave request: approve or reject it,
/// and record the student's departure and return. Wardens and
/// hostel masters only.
/// </summary>
[ApiController]
[Route("api/staff/leave-requests/{leaveRequestId:long}")]
[RequireRole(LeaveRoles.Warden, LeaveRoles.HostelMaster)]
public class StaffLeaveActionsController : ControllerBase
{
    private readonly ILeaveReviewService _leaveReviewService;

    public StaffLeaveActionsController(
        ILeaveReviewService leaveReviewService)
    {
        _leaveReviewService = leaveReviewService;
    }

    [HttpPut("decision")]
    public Task<IActionResult> Decide(
        ulong leaveRequestId,
        [FromBody] DecideLeaveRequest request)
    {
        return ExecuteAsync(
            (actorUserId, actorRole) =>
                _leaveReviewService.DecideAsync(
                    leaveRequestId,
                    request,
                    actorUserId,
                    actorRole));
    }

    [HttpPut("departure")]
    public Task<IActionResult> RecordDeparture(
        ulong leaveRequestId)
    {
        return ExecuteAsync(
            (actorUserId, actorRole) =>
                _leaveReviewService.RecordDepartureAsync(
                    leaveRequestId,
                    actorUserId,
                    actorRole));
    }

    [HttpPut("return")]
    public Task<IActionResult> RecordReturn(
        ulong leaveRequestId)
    {
        return ExecuteAsync(
            (actorUserId, actorRole) =>
                _leaveReviewService.RecordReturnAsync(
                    leaveRequestId,
                    actorUserId,
                    actorRole));
    }

    // Identifies the acting staff member, runs the action and
    // turns each kind of failure into the right HTTP status.
    private async Task<IActionResult> ExecuteAsync(
        Func<ulong, string, Task<LeaveRequestResponse>> action)
    {
        string? userIdValue =
            User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!ulong.TryParse(userIdValue, out ulong actorUserId))
        {
            return Unauthorized(new ErrorResponse
            {
                Message =
                    "The authenticated staff user ID is missing or invalid."
            });
        }

        // The middleware has already confirmed one of these roles.
        string actorRole = User.IsInRole(LeaveRoles.Warden)
            ? LeaveRoles.Warden
            : LeaveRoles.HostelMaster;

        try
        {
            return Ok(await action(actorUserId, actorRole));
        }
        catch (LeaveRequestValidationException exception)
        {
            return BadRequest(new ValidationErrorResponse
            {
                Message = exception.Message,
                Errors = exception.Errors
            });
        }
        catch (LeaveRequestNotFoundException exception)
        {
            return NotFound(new ErrorResponse
            {
                Message = exception.Message
            });
        }
        catch (InvalidLeaveStatusException exception)
        {
            return Conflict(new ErrorResponse
            {
                Message = exception.Message
            });
        }
    }
}
