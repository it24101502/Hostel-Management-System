using IdentityService.DTOs;
using IdentityService.Models;

namespace IdentityService.Services;

public interface IJwtTokenService
{
    JwtTokenResult CreateToken(LoginUser user);
}