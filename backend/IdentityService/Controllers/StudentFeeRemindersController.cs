using IdentityService.Authorization;
using IdentityService.DTOs;
using IdentityService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/student/fee-reminders")]
[RequireRole("STUDENT")]
public class StudentFeeRemindersController : ControllerBase
{
    private readonly IStudentFeeReminderService _service;

    public StudentFeeRemindersController(
        IStudentFeeReminderService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FeeReminderResponse>>>
        GetMine()
    {
        string? userIdValue =
            User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        // The student ID always comes from the token, never the URL.
        if (!ulong.TryParse(userIdValue, out ulong userId))
        {
            return Unauthorized(new
            {
                message =
                    "The authenticated user ID is missing or invalid."
            });
        }

        return Ok(await _service.GetMineAsync(userId));
    }
}