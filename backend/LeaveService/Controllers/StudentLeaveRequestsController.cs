using System.IdentityModel.Tokens.Jwt;
using LeaveService.Authorization;
using LeaveService.DTOs;
using LeaveService.Exceptions;
using LeaveService.Models;
using LeaveService.Services;
using Microsoft.AspNetCore.Mvc;

namespace LeaveService.Controllers;

[ApiController]
[Route("api/student/leave-requests")]
[RequireRole(LeaveRoles.Student)]
public class StudentLeaveRequestsController : ControllerBase
{
    private readonly ILeaveRequestService _leaveRequestService;

    public StudentLeaveRequestsController(
        ILeaveRequestService leaveRequestService)
    {
        _leaveRequestService = leaveRequestService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyRequests()
    {
        if (!TryGetAuthenticatedUserId(out ulong studentUserId))
        {
            return Unauthorized(CreateUserIdError());
        }

        var requests =
            await _leaveRequestService.GetMyRequestsAsync(
                studentUserId);

        return Ok(requests);
    }

    [HttpGet("{leaveRequestId:long}")]
    public async Task<IActionResult> GetMyRequest(
        ulong leaveRequestId)
    {
        if (!TryGetAuthenticatedUserId(out ulong studentUserId))
        {
            return Unauthorized(CreateUserIdError());
        }

        var request =
            await _leaveRequestService.GetMyRequestAsync(
                leaveRequestId,
                studentUserId);

        if (request is null)
        {
            return NotFound(new ErrorResponse
            {
                Message = "The leave request was not found."
            });
        }

        return Ok(request);
    }

    [HttpPost]
    public async Task<IActionResult> Submit(
        [FromBody] SubmitLeaveRequest request)
    {
        if (!TryGetAuthenticatedUserId(out ulong studentUserId))
        {
            return Unauthorized(CreateUserIdError());
        }

        string? studentUsername = User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(studentUsername))
        {
            return Unauthorized(new ErrorResponse
            {
                Message =
                    "The authenticated student's username is missing."
            });
        }

        try
        {
            var created =
                await _leaveRequestService.SubmitAsync(
                    request,
                    studentUserId,
                    studentUsername);

            return CreatedAtAction(
                nameof(GetMyRequest),
                new { leaveRequestId = created.LeaveRequestId },
                created);
        }
        catch (LeaveRequestValidationException exception)
        {
            return BadRequest(new ValidationErrorResponse
            {
                Message = exception.Message,
                Errors = exception.Errors
            });
        }
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
                "The authenticated student ID is missing or invalid."
        };
    }
}
