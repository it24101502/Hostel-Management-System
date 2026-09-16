using IdentityService.Controllers;
using IdentityService.DTOs;
using IdentityService.Models;
using IdentityService.Repositories;
using IdentityService.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task ValidCredentials_ReturnSuccessResponse()
    {
        const string password = "Student@123";

        var repository = new FakeUserRepository
        {
            User = CreateUser(password)
        };

        var authService = new AuthService(repository);
        var controller = new AuthController(authService);

        var request = new LoginRequest
        {
            Identifier = "student@example.com",
            Password = password
        };

        var actionResult = await controller.Login(request);

        var okResult =
            Assert.IsType<OkObjectResult>(actionResult);

        var response =
            Assert.IsType<LoginResponse>(okResult.Value);

        Assert.Equal("Login successful.", response.Message);
        Assert.Equal((ulong)1, response.UserId);
        Assert.Equal("student01", response.Username);
        Assert.Equal("STUDENT", response.Role);
        Assert.True(repository.SuccessfulLoginRecorded);
        Assert.False(repository.FailedLoginRecorded);
    }

    [Fact]
    public async Task InvalidPassword_ReturnsGenericError()
    {
        var repository = new FakeUserRepository
        {
            User = CreateUser("CorrectPassword@123")
        };

        var authService = new AuthService(repository);
        var controller = new AuthController(authService);

        var request = new LoginRequest
        {
            Identifier = "student@example.com",
            Password = "WrongPassword@123"
        };

        var result = await controller.Login(request);

        var unauthorized =
            Assert.IsType<UnauthorizedObjectResult>(result);

        var error =
            Assert.IsType<ErrorResponse>(unauthorized.Value);

        Assert.Equal("Invalid credentials.", error.Message);
        Assert.True(repository.FailedLoginRecorded);
    }

    [Fact]
    public async Task UnknownUser_ReturnsSameGenericError()
    {
        var repository = new FakeUserRepository
        {
            User = null
        };

        var authService = new AuthService(repository);
        var controller = new AuthController(authService);

        var request = new LoginRequest
        {
            Identifier = "unknown@example.com",
            Password = "SomePassword@123"
        };

        var result = await controller.Login(request);

        var unauthorized =
            Assert.IsType<UnauthorizedObjectResult>(result);

        var error =
            Assert.IsType<ErrorResponse>(unauthorized.Value);

        Assert.Equal("Invalid credentials.", error.Message);
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

    private sealed class FakeUserRepository : IUserRepository
    {
        public LoginUser? User { get; set; }

        public bool SuccessfulLoginRecorded { get; private set; }

        public bool FailedLoginRecorded { get; private set; }

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
            return Task.CompletedTask;
        }
    }
}