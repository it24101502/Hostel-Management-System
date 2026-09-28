using LeaveService.Authorization;
using LeaveService.DTOs;
using LeaveService.Exceptions;
using LeaveService.Models;
using LeaveService.Services;
using Microsoft.AspNetCore.Mvc;

namespace LeaveService.Controllers;

/// <summary>
/// Read-only leave and movement report (HMS-50). Administrators
/// may view it, but only wardens and hostel masters can act on
/// requests (see StaffLeaveActionsController).
/// </summary>
[ApiController]
[Route("api/staff/leave-requests")]
[RequireRole(
    LeaveRoles.Warden,
    LeaveRoles.HostelMaster,
    LeaveRoles.Admin)]
public class StaffLeaveRequestsController : ControllerBase
{
    private readonly ILeaveReviewService _leaveReviewService;

    public StaffLeaveRequestsController(
        ILeaveReviewService leaveReviewService)
    {
        _leaveReviewService = leaveReviewService;
    }

    [HttpGet("report")]
    public async Task<IActionResult> GetReport(
        [FromQuery] LeaveReportFilter filter)
    {
        try
        {
            var report =
                await _leaveReviewService.GetReportAsync(filter);

            return Ok(report);
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
}
