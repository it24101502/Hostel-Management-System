using IdentityService.Authorization;
using IdentityService.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers;

/// <summary>
/// Lists active staff accounts so other services (for example
/// ComplaintService) can offer and verify assignees without
/// reading the users table themselves.
/// </summary>
[ApiController]
[Route("api/staff-directory")]
[RequireRole("ADMIN", "WARDEN", "HOSTEL_MASTER")]
public sealed class StaffDirectoryController : ControllerBase
{
    private static readonly HashSet<string> StaffRoles =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "ADMIN",
            "WARDEN",
            "HOSTEL_MASTER"
        };

    private readonly IAdminUserRepository _userRepository;

    public StaffDirectoryController(
        IAdminUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetStaff()
    {
        var users = await _userRepository.GetAllAsync();

        return Ok(users
            .Where(user => user.IsActive &&
                           StaffRoles.Contains(user.RoleName))
            .Select(user => new
            {
                user.UserId,
                user.RoleName,
                user.Username,
                FullName = $"{user.FirstName} {user.LastName}".Trim()
            }));
    }

    // 200 if the user is an active staff member, otherwise 404.
    [HttpGet("{userId:long}")]
    public async Task<IActionResult> GetStaffMember(ulong userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user is null ||
            !user.IsActive ||
            !StaffRoles.Contains(user.RoleName))
        {
            return NotFound(new
            {
                message = $"User {userId} is not an active staff member."
            });
        }

        return Ok(new
        {
            user.UserId,
            user.RoleName,
            user.Username,
            FullName = $"{user.FirstName} {user.LastName}".Trim()
        });
    }
}
