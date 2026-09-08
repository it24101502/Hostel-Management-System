using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IdentityService.DTOs;
using IdentityService.Models;
using IdentityService.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace IdentityService.Services;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _jwtOptions;

    public JwtTokenService(
        IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;

        if (string.IsNullOrWhiteSpace(_jwtOptions.Key) ||
            _jwtOptions.Key.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT signing key must contain at least 32 characters.");
        }

        if (string.IsNullOrWhiteSpace(_jwtOptions.Issuer))
        {
            throw new InvalidOperationException(
                "JWT issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(_jwtOptions.Audience))
        {
            throw new InvalidOperationException(
                "JWT audience is required.");
        }

        if (_jwtOptions.ExpiryMinutes <= 0)
        {
            throw new InvalidOperationException(
                "JWT expiry must be greater than zero.");
        }
    }

    public JwtTokenResult CreateToken(LoginUser user)
    {
        DateTime issuedAt = DateTime.UtcNow;

        DateTime expiresAt = issuedAt.AddMinutes(
            _jwtOptions.ExpiryMinutes);

        var claims = new[]
        {
            new Claim(
                JwtRegisteredClaimNames.Sub,
                user.UserId.ToString(
                    CultureInfo.InvariantCulture)),

            new Claim(
                JwtRegisteredClaimNames.UniqueName,
                user.Username),

            new Claim(
                ClaimTypes.Role,
                user.RoleName),

            new Claim(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        };

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtOptions.Key));

        var signingCredentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: issuedAt,
            expires: expiresAt,
            signingCredentials: signingCredentials);

        return new JwtTokenResult
        {
            AccessToken =
                new JwtSecurityTokenHandler().WriteToken(token),

            ExpiresAt = expiresAt
        };
    }
}