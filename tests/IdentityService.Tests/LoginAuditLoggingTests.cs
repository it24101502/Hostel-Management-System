using IdentityService.DTOs;
using IdentityService.Models;
using IdentityService.Options;
using IdentityService.Repositories;
using IdentityService.Services;

namespace IdentityService.Tests;

public class LoginAuditLoggingTests
{
    [Fact]
    public async Task SuccessfulLogin_RecordsSuccessAuditEntry()
    {
        const string password = "Student@01";

        var userRepository = new AuditTestUserRepository
        {
            User = CreateUser(password)
        };

        var auditRepository =
            new AuditTestRepository();

        var authService = CreateAuthService(
            userRepository,
            auditRepository);

        var result = await authService.LoginAsync(
            new LoginRequest
            {
                Identifier = "student01@example.com",
                Password = password
            });

        Assert.NotNull(result);

        var entry = Assert.Single(
            auditRepository.Logs);

        Assert.Equal((ulong)1, entry.UserId);

        Assert.Equal(
            "STUDENT01@EXAMPLE.COM",
            entry.Identifier);

        Assert.Equal(
            LoginAuditOutcomes.Success,
            entry.Outcome);

        Assert.True(
            entry.AttemptedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task FailedLogin_RecordsFailureAuditEntry()
    {
        var userRepository =
            new AuditTestUserRepository
            {
                User = null
            };

        var auditRepository =
            new AuditTestRepository();

        var authService = CreateAuthService(
            userRepository,
            auditRepository);

        var result = await authService.LoginAsync(
            new LoginRequest
            {
                Identifier = "unknown@example.com",
                Password = "WrongPassword123"
            });

        Assert.Null(result);

        var entry = Assert.Single(
            auditRepository.Logs);

        Assert.Null(entry.UserId);

        Assert.Equal(
            "UNKNOWN@EXAMPLE.COM",
            entry.Identifier);

        Assert.Equal(
            LoginAuditOutcomes.Failure,
            entry.Outcome);

        Assert.True(
            entry.AttemptedAt <= DateTime.UtcNow);
    }

    private static AuthService CreateAuthService(
        IUserRepository userRepository,
        ILoginAuditRepository auditRepository)
    {
        var lockoutOptions =
            Microsoft.Extensions.Options.Options.Create(
                new LockoutOptions
                {
                    DurationMinutes = 15
                });

        return new AuthService(
            userRepository,
            lockoutOptions,
            new AuditTestJwtService(),
            auditRepository);
    }

    private static LoginUser CreateUser(
        string password)
    {
        return new LoginUser
        {
            UserId = 1,
            Username = "student01",
            Email = "student01@example.com",
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

    private sealed class AuditTestUserRepository
        : IUserRepository
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

    private sealed class AuditTestRepository
        : ILoginAuditRepository
    {
        public List<LoginAuditLog> Logs { get; } = [];

        public Task RecordAttemptAsync(
            ulong? userId,
            string identifier,
            string outcome)
        {
            Logs.Add(new LoginAuditLog
            {
                AuditLogId =
                    (ulong)(Logs.Count + 1),
                UserId = userId,
                Identifier = identifier,
                Outcome = outcome,
                AttemptedAt = DateTime.UtcNow
            });

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<LoginAuditLog>>
            GetRecentAttemptsAsync(int limit)
        {
            IReadOnlyList<LoginAuditLog> result =
                Logs.Take(limit).ToList();

            return Task.FromResult(result);
        }
    }

    private sealed class AuditTestJwtService
        : IJwtTokenService
    {
        public JwtTokenResult CreateToken(
            LoginUser user)
        {
            return new JwtTokenResult
            {
                AccessToken = "test-token",
                ExpiresAt =
                    DateTime.UtcNow.AddMinutes(15)
            };
        }
    }
}