using IdentityService.DTOs;

namespace IdentityService.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
}