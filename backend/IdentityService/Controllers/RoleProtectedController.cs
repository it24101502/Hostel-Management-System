using IdentityService.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/protected")]
public class RoleProtectedController : ControllerBase
{
    [HttpGet("admin")]
    [RequireRole("ADMIN")]
    public IActionResult GetAdminResource()
    {
        return Ok(new
        {
            message = "Admin access granted."
        });
    }

    [HttpGet("staff")]
    [RequireRole("ADMIN", "WARDEN", "HOSTEL_MASTER")]
    public IActionResult GetStaffResource()
    {
        return Ok(new
        {
            message = "Staff access granted."
        });
    }

    [HttpGet("student")]
    [RequireRole("STUDENT")]
    public IActionResult GetStudentResource()
    {
        return Ok(new
        {
            message = "Student access granted."
        });
    }
}