using IdentityService.Authorization;
using IdentityService.DTOs;
using IdentityService.Exceptions;
using IdentityService.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/admin/users")]
[RequireRole("ADMIN")]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _adminUserService;

    public AdminUsersController(
        IAdminUserService adminUserService)
    {
        _adminUserService = adminUserService;
    }

    // CREATE: POST /api/admin/users
    [HttpPost]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest request)
    {
        try
        {
            var createdUser =
                await _adminUserService.CreateAsync(request);

            return CreatedAtAction(
                nameof(GetUserById),
                new { userId = createdUser.UserId },
                createdUser);
        }
        catch (DuplicateUserException exception)
        {
            return Conflict(new ErrorResponse
            {
                Message = exception.Message
            });
        }
        catch (RoleNotFoundException exception)
        {
            return BadRequest(new ErrorResponse
            {
                Message = exception.Message
            });
        }
    }

    // VIEW ALL: GET /api/admin/users
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users =
            await _adminUserService.GetAllAsync();

        return Ok(users);
    }

    // VIEW ONE: GET /api/admin/users/1
    [HttpGet("{userId:long}")]
    public async Task<IActionResult> GetUserById(
        ulong userId)
    {
        var user =
            await _adminUserService.GetByIdAsync(userId);

        if (user is null)
        {
            return NotFound(new ErrorResponse
            {
                Message = "User account was not found."
            });
        }

        return Ok(user);
    }

    // UPDATE: PUT /api/admin/users/1
    [HttpPut("{userId:long}")]
    public async Task<IActionResult> UpdateUser(
        ulong userId,
        [FromBody] UpdateUserRequest request)
    {
        try
        {
            var updatedUser =
                await _adminUserService.UpdateAsync(
                    userId,
                    request);

            if (updatedUser is null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = "User account was not found."
                });
            }

            return Ok(updatedUser);
        }
        catch (DuplicateUserException exception)
        {
            return Conflict(new ErrorResponse
            {
                Message = exception.Message
            });
        }
        catch (RoleNotFoundException exception)
        {
            return BadRequest(new ErrorResponse
            {
                Message = exception.Message
            });
        }
    }

    // DEACTIVATE: PATCH /api/admin/users/1/deactivate
    [HttpPatch("{userId:long}/deactivate")]
    public async Task<IActionResult> DeactivateUser(
        ulong userId)
    {
        bool deactivated =
            await _adminUserService.DeactivateAsync(
                userId);

        if (!deactivated)
        {
            return NotFound(new ErrorResponse
            {
                Message = "User account was not found."
            });
        }

        return Ok(new
        {
            message = "User account deactivated successfully."
        });
    }
}