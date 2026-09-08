using IdentityService.Authorization;
using IdentityService.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/admin/auth-audit-logs")]
[RequireRole("ADMIN")]
public sealed class AuditLogsController : ControllerBase
{
    private readonly ILoginAuditRepository
        _loginAuditRepository;

    public AuditLogsController(
        ILoginAuditRepository loginAuditRepository)
    {
        _loginAuditRepository = loginAuditRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecentAttempts(
        [FromQuery] int limit = 100)
    {
        if (limit < 1 || limit > 500)
        {
            return BadRequest(new
            {
                message = "Limit must be between 1 and 500."
            });
        }

        var logs = await _loginAuditRepository
            .GetRecentAttemptsAsync(limit);

        return Ok(logs);
    }
}