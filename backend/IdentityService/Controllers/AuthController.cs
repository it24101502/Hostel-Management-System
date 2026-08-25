using IdentityService.DTOs;
using IdentityService.Exceptions;
using IdentityService.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
    {
        try
        {
            var result =
                await _authService.LoginAsync(request);

            if (result is null)
            {
                return Unauthorized(new ErrorResponse
                {
                    Message = "Invalid credentials."
                });
            }

            return Ok(result);
        }
        catch (AccountLockedException exception)
        {
            return StatusCode(
                StatusCodes.Status423Locked,
                new ErrorResponse
                {
                    Message = exception.Message
                });
        }
    }
}