using IdentityService.DTOs;
using IdentityService.Models;
using IdentityService.Repositories;
using IdentityService.Services;

namespace IdentityService.Tests;

public class DeactivatedUserLoginTests
{
    [Fact]
    public async Task DeactivatedUser_CannotLogin()
    {
        string password = "Student@123";

        var repository = new FakeUserRepository
        {
            User = new LoginUser
            {
                UserId = 50,
                Username = "inactiveuser",
                Email = "inactive@example.com",

                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        password),

                FailedLoginAttempts = 0,
                IsLocked = false,
                LockoutEndAt = null,

                // This account has been deactivated.
                IsActive = false,

                RoleName = "STUDENT",
                IsRoleActive = true
            }
        };

        var service = new AuthService(repository);

        var request = new LoginRequest
        {
            Identifier = "inactive@example.com",
            Password = password
        };

        var result = await service.LoginAsync(request);

        Assert.Null(result);
        Assert.False(repository.SuccessfulLoginRecorded);
    }

    private sealed class FakeUserRepository
        : IUserRepository
    {
        public LoginUser? User { get; set; }

        public bool SuccessfulLoginRecorded
        {
            get;
            private set;
        }

        public Task<LoginUser?> FindByIdentifierAsync(
            string identifier)
        {
            return Task.FromResult(User);
        }

        public Task RecordSuccessfulLoginAsync(
            ulong userId)
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
            return Task.CompletedTask;
        }
    }
}