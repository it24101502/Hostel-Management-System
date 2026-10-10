using System.IdentityModel.Tokens.Jwt;
using ComplaintService.Authorization;
using ComplaintService.DTOs;
using ComplaintService.Exceptions;
using ComplaintService.Models;
using ComplaintService.Services;
using Microsoft.AspNetCore.Mvc;

namespace ComplaintService.Controllers;

[ApiController]
[Route("api/staff/complaints")]
[RequireRole(
    ComplaintRoles.Warden,
    ComplaintRoles.HostelMaster,
    ComplaintRoles.Admin)]
public class StaffComplaintsController : ControllerBase
{
    private readonly IStaffComplaintService _service;

    public StaffComplaintsController(IStaffComplaintService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetComplaints(
        [FromQuery] string? status,
        [FromQuery] string? category)
    {
        try
        {
            return Ok(await _service.GetComplaintsAsync(status, category));
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

    [HttpGet("report")]
    public async Task<IActionResult> GetReport(
        [FromQuery] string? status,
        [FromQuery] string? category)
    {
        try
        {
            return Ok(await _service.GetReportAsync(status, category));
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

    [HttpGet("report/csv")]
    public async Task<IActionResult> DownloadReportCsv(
        [FromQuery] string? status,
        [FromQuery] string? category)
    {
        try
        {
            byte[] csv = await _service.GenerateReportCsvAsync(status, category);

            return File(
                csv,
                "text/csv; charset=utf-8",
                $"complaint-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
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

    [HttpPut("{complaintId:long}/assign")]
    public Task<IActionResult> Assign(
        ulong complaintId,
        [FromBody] AssignComplaintRequest request)
    {
        return ExecuteAsync((userId, role) =>
            _service.AssignAsync(complaintId, request, userId, role));
    }

    [HttpPut("{complaintId:long}/status")]
    public Task<IActionResult> ChangeStatus(
        ulong complaintId,
        [FromBody] UpdateComplaintStatusRequest request)
    {
        return ExecuteAsync((userId, role) =>
            _service.ChangeStatusAsync(complaintId, request, userId, role));
    }

    private async Task<IActionResult> ExecuteAsync(
        Func<ulong, string, Task<ComplaintResponse>> action)
    {
        string? userIdValue =
            User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!ulong.TryParse(userIdValue, out ulong userId))
        {
            return Unauthorized(new ErrorResponse
            {
                Message = "The authenticated user ID is missing or invalid."
            });
        }

        string role = User.IsInRole(ComplaintRoles.Warden)
            ? ComplaintRoles.Warden
            : User.IsInRole(ComplaintRoles.HostelMaster)
                ? ComplaintRoles.HostelMaster
                : ComplaintRoles.Admin;

        try
        {
            return Ok(await action(userId, role));
        }
        catch (ComplaintValidationException exception)
        {
            return BadRequest(new ValidationErrorResponse
            {
                Message = exception.Message,
                Errors = exception.Errors
            });
        }
        catch (ComplaintNotFoundException exception)
        {
            return NotFound(new ErrorResponse { Message = exception.Message });
        }
        catch (InvalidComplaintStatusException exception)
        {
            return Conflict(new ErrorResponse { Message = exception.Message });
        }
        catch (StaffDirectoryUnavailableException exception)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new ErrorResponse { Message = exception.Message });
        }
    }
}