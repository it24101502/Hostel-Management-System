using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IdentityService.DTOs;
using IdentityService.Models;
using IdentityService.Options;
using IdentityService.Repositories;
using IdentityService.Services;
using Microsoft.IdentityModel.Tokens;

namespace IdentityService.Tests;

public class JwtTokenTests
{
    private const string TestKey =
        "This-Is-A-Secure-Test-Key-With-More-Than-32-Characters-123456";

    private const string TestIssuer =
        "HostelManagement.IdentityService";

    private const string TestAudience =
        "HostelManagement.Client";

    [Fact]
    public async Task SuccessfulLogin_ReturnsValidJwt()
    {
        const string password = "Student@123";

        var repository = new JwtTestRepository
        {
            User = CreateUser(password)
        };

        var jwtService = CreateJwtService();

        var lockoutOptions =
            Microsoft.Extensions.Options.Options.Create(
                new LockoutOptions
                {
                    DurationMinutes = 15
                });

        var authService = new AuthService(
            repository,
            lockoutOptions,
            jwtService);

        var response = await authService.LoginAsync(
            new LoginRequest
            {
                Identifier = "student@example.com",
                Password = password
            });

        Assert.NotNull(response);
        Assert.False(
            string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.True(response.ExpiresAt > DateTime.UtcNow);

        var principal = ValidateToken(
            response.AccessToken);

        Assert.True(
            principal.Identity?.IsAuthenticated);

        Assert.True(
            principal.IsInRole("STUDENT"));
    }

    [Fact]
    public void ExpiredToken_IsRejected()
    {
        string expiredToken = CreateExpiredToken();

        Assert.Throws<SecurityTokenExpiredException>(
            () => ValidateToken(expiredToken));
    }

    [Fact]
    public void TamperedToken_IsRejected()
    {
        var jwtService = CreateJwtService();

        string validToken =
            jwtService.CreateToken(
                CreateUser("Student@123"))
            .AccessToken;

        string[] tokenParts = validToken.Split('.');

        char replacement =
            tokenParts[2][0] == 'A' ? 'B' : 'A';

        tokenParts[2] =
            replacement + tokenParts[2][1..];

        string tamperedToken =
            string.Join(".", tokenParts);

        Assert.Throws<SecurityTokenSignatureKeyNotFoundException>(
            () => ValidateToken(tamperedToken));
    }

    private static JwtTokenService CreateJwtService()
    {
        var options =
            Microsoft.Extensions.Options.Options.Create(
                new JwtOptions
                {
                    Key = TestKey,
                    Issuer = TestIssuer,
                    Audience = TestAudience,
                    ExpiryMinutes = 15
                });

        return new JwtTokenService(options);
    }

    private static ClaimsPrincipal ValidateToken(
        string token)
    {
        var validationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(TestKey)),

                ValidateIssuer = true,
                ValidIssuer = TestIssuer,

                ValidateAudience = true,
                ValidAudience = TestAudience,

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,

                RoleClaimType = ClaimTypes.Role
            };

        return new JwtSecurityTokenHandler()
            .ValidateToken(
                token,
                validationParameters,
                out _);
    }

    private static string CreateExpiredToken()
    {
        DateTime now = DateTime.UtcNow;

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(TestKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims:
            [
                new Claim(
                    ClaimTypes.Role,
                    "STUDENT")
            ],
            notBefore: now.AddMinutes(-10),
            expires: now.AddMinutes(-5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    private static LoginUser CreateUser(string password)
    {
        return new LoginUser
        {
            UserId = 1,
            Username = "student01",
            Email = "student@example.com",
            PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(password),
            FailedLoginAttempts = 0,
            IsLocked = false,
            LockoutEndAt = null,
            IsActive = true,
            RoleName = "STUDENT",
            IsRoleActive = true
        };
    }

    private sealed class JwtTestRepository : IUserRepository
    {
        public LoginUser? User { get; set; }

        public Task<LoginUser?> FindByIdentifierAsync(
            string identifier)
        {
            return Task.FromResult(User);
        }

        public Task RecordSuccessfulLoginAsync(
            ulong userId)
        {
            return Task.CompletedTask;
        }

        public Task RecordFailedLoginAsync(
            ulong userId,
            uint failedAttempts,
            bool isLocked,
            DateTime? lockoutEndAt)
        {
            return Task.CompletedTask;
        }
    }
}