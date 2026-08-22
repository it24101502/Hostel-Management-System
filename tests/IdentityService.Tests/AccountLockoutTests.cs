using IdentityService.DTOs;
using IdentityService.Models;
using IdentityService.Options;
using IdentityService.Repositories;
using IdentityService.Services;

namespace IdentityService.Tests;

public class AccountLockoutTests
{
    [Fact]
    public async Task FifthFailedAttempt_LocksAccount()
    {
        var repository = new LockoutTestRepository
        {
            User = CreateUser(
                password: "CorrectPassword@123",
                failedAttempts: 4)
        };

        var service = CreateService(
            repository,
            durationMinutes: 15);

        var request = new LoginRequest
        {
            Identifier = "student@example.com",
            Password = "WrongPassword@123"
        };

        var result = await service.LoginAsync(request);

        Assert.Null(result);
        Assert.True(repository.FailedLoginRecorded);
        Assert.Equal(
            (uint)5,
            repository.RecordedFailedAttempts);
        Assert.True(repository.RecordedIsLocked);
        Assert.NotNull(repository.RecordedLockoutEndAt);
    }

    [Fact]
    public async Task LockedAccount_WithCorrectPassword_CannotLogin()
    {
        var repository = new LockoutTestRepository
        {
            User = CreateUser(
                password: "CorrectPassword@123",
                failedAttempts: 5,
                isLocked: true,
                lockoutEndAt: DateTime.UtcNow.AddMinutes(10))
        };

        var service = CreateService(
            repository,
            durationMinutes: 15);

        var request = new LoginRequest
        {
            Identifier = "student@example.com",
            Password = "CorrectPassword@123"
        };

        var result = await service.LoginAsync(request);

        Assert.Null(result);
        Assert.False(repository.SuccessfulLoginRecorded);
        Assert.False(repository.FailedLoginRecorded);
    }

    [Fact]
    public async Task LockoutDuration_UsesConfiguredValue()
    {
        const int configuredDurationMinutes = 30;

        var repository = new LockoutTestRepository
        {
            User = CreateUser(
                password: "CorrectPassword@123",
                failedAttempts: 4)
        };

        var service = CreateService(
            repository,
            configuredDurationMinutes);

        var beforeAttempt = DateTime.UtcNow;

        await service.LoginAsync(new LoginRequest
        {
            Identifier = "student@example.com",
            Password = "WrongPassword@123"
        });

        var afterAttempt = DateTime.UtcNow;

        Assert.NotNull(repository.RecordedLockoutEndAt);

        Assert.InRange(
            repository.RecordedLockoutEndAt!.Value,
            beforeAttempt.AddMinutes(configuredDurationMinutes),
            afterAttempt.AddMinutes(configuredDurationMinutes));
    }

    [Fact]
    public async Task ExpiredLockout_WithCorrectPassword_AllowsLogin()
    {
        var repository = new LockoutTestRepository
        {
            User = CreateUser(
                password: "CorrectPassword@123",
                failedAttempts: 5,
                isLocked: true,
                lockoutEndAt: DateTime.UtcNow.AddMinutes(-1))
        };

        var service = CreateService(
            repository,
            durationMinutes: 15);

        var result = await service.LoginAsync(new LoginRequest
        {
            Identifier = "student@example.com",
            Password = "CorrectPassword@123"
        });

        Assert.NotNull(result);
        Assert.True(repository.SuccessfulLoginRecorded);
        Assert.False(repository.FailedLoginRecorded);
    }

    private static AuthService CreateService(
        IUserRepository repository,
        int durationMinutes)
    {
        var options =
            Microsoft.Extensions.Options.Options.Create(
                new LockoutOptions
                {
                    DurationMinutes = durationMinutes
                });

        return new AuthService(repository, options);
    }

    private static LoginUser CreateUser(
        string password,
        uint failedAttempts,
        bool isLocked = false,
        DateTime? lockoutEndAt = null)
    {
        return new LoginUser
        {
            UserId = 1,
            Username = "student01",
            Email = "student@example.com",
            PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(password),
            FailedLoginAttempts = failedAttempts,
            IsLocked = isLocked,
            LockoutEndAt = lockoutEndAt,
            IsActive = true,
            RoleName = "STUDENT",
            IsRoleActive = true
        };
    }

    private sealed class LockoutTestRepository
        : IUserRepository
    {
        public LoginUser? User { get; set; }

        public bool SuccessfulLoginRecorded { get; private set; }

        public bool FailedLoginRecorded { get; private set; }

        public uint RecordedFailedAttempts { get; private set; }

        public bool RecordedIsLocked { get; private set; }

        public DateTime? RecordedLockoutEndAt { get; private set; }

        public Task<LoginUser?> FindByIdentifierAsync(
            string identifier)
        {
            return Task.FromResult(User);
        }

        public Task RecordSuccessfulLoginAsync(ulong userId)
        {
            SuccessfulLoginRecorded = true;
            return Task.CompletedTask;
        }

        public Task RecordFailedLoginAsync(
            ulong userId,
            uint failedAttempts,
            bool isLocked,
            DateTime? lockoutEndAt)
        {
            FailedLoginRecorded = true;
            RecordedFailedAttempts = failedAttempts;
            RecordedIsLocked = isLocked;
            RecordedLockoutEndAt = lockoutEndAt;

            return Task.CompletedTask;
        }
    }
}