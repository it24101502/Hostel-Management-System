using System.IdentityModel.Tokens.Jwt;
using ComplaintService.Authorization;
using ComplaintService.DTOs;
using ComplaintService.Exceptions;
using ComplaintService.Models;
using ComplaintService.Services;
using Microsoft.AspNetCore.Mvc;

namespace ComplaintService.Controllers;

[ApiController]
[Route("api/student/complaints")]
[RequireRole(ComplaintRoles.Student)]
public class StudentComplaintsController : ControllerBase
{
    private readonly IStudentComplaintService _complaintService;

    public StudentComplaintsController(
        IStudentComplaintService complaintService)
    {
        _complaintService = complaintService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyComplaints()
    {
        if (!TryGetAuthenticatedUserId(out ulong studentUserId))
        {
            return Unauthorized(CreateUserIdError());
        }

        var complaints =
            await _complaintService.GetMyComplaintsAsync(studentUserId);

        return Ok(complaints);
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> GetStudentNotifications()
    {
        if (!TryGetAuthenticatedUserId(out ulong studentUserId))
        {
            return Unauthorized(CreateUserIdError());
        }

        var notifications =
            await _complaintService.GetNotificationsByStudentIdAsync(studentUserId);

        return Ok(notifications);
    }

    [HttpGet("{complaintId:long}")]
    public async Task<IActionResult> GetMyComplaint(ulong complaintId)
    {
        if (!TryGetAuthenticatedUserId(out ulong studentUserId))
        {
            return Unauthorized(CreateUserIdError());
        }

        var complaint =
            await _complaintService.GetMyComplaintAsync(
                complaintId,
                studentUserId);

        if (complaint is null)
        {
            return NotFound(new ErrorResponse
            {
                Message = "The complaint was not found."
            });
        }

        return Ok(complaint);
    }

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitComplaintRequest request)
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
                Message = "The authenticated student's username is missing."
            });
        }

        try
        {
            var created =
                await _complaintService.SubmitAsync(
                    request,
                    studentUserId,
                    studentUsername);

            return CreatedAtAction(
                nameof(GetMyComplaint),
                new { complaintId = created.ComplaintId },
                created);
        }
        catch (ComplaintValidationException exception)
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
            User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value 
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        return ulong.TryParse(userIdValue, out userId);
    }

    private static ErrorResponse CreateUserIdError()
    {
        return new ErrorResponse
        {
            Message = "The authenticated student ID is missing or invalid."
        };
    }

    [HttpPut("notifications/{notificationId:long}/read")]
    public async Task<IActionResult> MarkNotificationRead(ulong notificationId)
    {
        if (!TryGetAuthenticatedUserId(out ulong studentUserId))
        {
            return Unauthorized(CreateUserIdError());
        }

        bool found = await _complaintService.MarkNotificationReadAsync(
            notificationId,
            studentUserId);

        if (!found)
        {
            return NotFound(new ErrorResponse
            {
                Message = "The notification was not found."
            });
        }

        return NoContent();
    }
}