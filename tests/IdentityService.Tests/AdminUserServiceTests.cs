using IdentityService.DTOs;
using IdentityService.Models;
using IdentityService.Repositories;
using IdentityService.Services;

namespace IdentityService.Tests;

public class AdminUserServiceTests
{
    [Fact]
    public async Task CreateUser_WithValidDetails_CreatesUser()
    {
        var repository = new FakeAdminUserRepository
        {
            RoleExists = true
        };

        var service = new AdminUserService(repository);

        var request = new CreateUserRequest
        {
            Username = "student02",
            Email = "student02@example.com",
            FirstName = "Test",
            LastName = "Student",
            Password = "Student@123",
            RoleId = 4
        };

        var result = await service.CreateAsync(request);

        Assert.Equal("student02", result.Username);
        Assert.Equal("student02@example.com", result.Email);
        Assert.Equal((ulong)4, result.RoleId);
        Assert.True(result.IsActive);

        Assert.NotNull(repository.CreatedPasswordHash);

        Assert.True(
            BCrypt.Net.BCrypt.Verify(
                "Student@123",
                repository.CreatedPasswordHash));
    }

    [Fact]
    public async Task GetAllUsers_ReturnsAllUsers()
    {
        var repository = new FakeAdminUserRepository();

        repository.Users.Add(
            CreateUserAccount(1, "admin01", 1));

        repository.Users.Add(
            CreateUserAccount(2, "student01", 4));

        var service = new AdminUserService(repository);

        var result = await service.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(
            result,
            user => user.Username == "admin01");

        Assert.Contains(
            result,
            user => user.Username == "student01");
    }

    [Fact]
    public async Task GetUser_WithExistingId_ReturnsUser()
    {
        var repository = new FakeAdminUserRepository();

        repository.Users.Add(
            CreateUserAccount(10, "warden01", 2));

        var service = new AdminUserService(repository);

        var result = await service.GetByIdAsync(10);

        Assert.NotNull(result);
        Assert.Equal((ulong)10, result.UserId);
        Assert.Equal("warden01", result.Username);
    }

    [Fact]
    public async Task UpdateUser_WithValidDetails_UpdatesUser()
    {
        var repository = new FakeAdminUserRepository
        {
            RoleExists = true
        };

        repository.Users.Add(
            CreateUserAccount(20, "oldusername", 4));

        var service = new AdminUserService(repository);

        var request = new UpdateUserRequest
        {
            Username = "newusername",
            Email = "newuser@example.com",
            FirstName = "Updated",
            LastName = "Student",
            PhoneNumber = "0771234567",
            RoleId = 4
        };

        var result =
            await service.UpdateAsync(20, request);

        Assert.NotNull(result);
        Assert.Equal("newusername", result.Username);
        Assert.Equal(
            "newuser@example.com",
            result.Email);

        Assert.Equal(
            "0771234567",
            result.PhoneNumber);
    }

    [Fact]
    public async Task DeactivateUser_WithExistingId_DeactivatesUser()
    {
        var repository = new FakeAdminUserRepository();

        repository.Users.Add(
            CreateUserAccount(30, "student03", 4));

        var service = new AdminUserService(repository);

        bool result =
            await service.DeactivateAsync(30);

        Assert.True(result);

        var deactivatedUser =
            await repository.GetByIdAsync(30);

        Assert.NotNull(deactivatedUser);
        Assert.False(deactivatedUser.IsActive);
    }

    private static UserAccount CreateUserAccount(
        ulong userId,
        string username,
        ulong roleId)
    {
        return new UserAccount
        {
            UserId = userId,
            RoleId = roleId,
            RoleName = GetRoleName(roleId),
            Username = username,
            Email = $"{username}@example.com",
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static string GetRoleName(ulong roleId)
    {
        return roleId switch
        {
            1 => "ADMIN",
            2 => "WARDEN",
            3 => "HOSTEL_MASTER",
            4 => "STUDENT",
            _ => "UNKNOWN"
        };
    }

    private sealed class FakeAdminUserRepository
        : IAdminUserRepository
    {
        public List<UserAccount> Users { get; } = [];

        public bool RoleExists { get; set; } = true;

        public bool DuplicateExists { get; set; }

        public string? CreatedPasswordHash { get; private set; }

        public Task<IReadOnlyList<UserAccount>>
            GetAllAsync()
        {
            IReadOnlyList<UserAccount> result = Users;

            return Task.FromResult(result);
        }

        public Task<UserAccount?> GetByIdAsync(
            ulong userId)
        {
            var user =
                Users.FirstOrDefault(
                    item => item.UserId == userId);

            return Task.FromResult(user);
        }

        public Task<bool> RoleExistsAsync(ulong roleId)
        {
            return Task.FromResult(RoleExists);
        }

        public Task<bool> UsernameOrEmailExistsAsync(
            string normalizedUsername,
            string normalizedEmail,
            ulong? excludedUserId = null)
        {
            return Task.FromResult(DuplicateExists);
        }

        public Task<ulong> CreateAsync(
            CreateUserRequest request,
            string passwordHash)
        {
            ulong newUserId =
                Users.Count == 0
                    ? 1
                    : Users.Max(user => user.UserId) + 1;

            CreatedPasswordHash = passwordHash;

            Users.Add(new UserAccount
            {
                UserId = newUserId,
                RoleId = request.RoleId,
                RoleName = GetRoleName(request.RoleId),
                Username = request.Username.Trim(),
                Email = request.Email.Trim(),
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                PhoneNumber = request.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            return Task.FromResult(newUserId);
        }

        public Task<bool> UpdateAsync(
            ulong userId,
            UpdateUserRequest request)
        {
            var user =
                Users.FirstOrDefault(
                    item => item.UserId == userId);

            if (user is null)
            {
                return Task.FromResult(false);
            }

            user.RoleId = request.RoleId;
            user.RoleName = GetRoleName(request.RoleId);
            user.Username = request.Username.Trim();
            user.Email = request.Email.Trim();
            user.FirstName = request.FirstName.Trim();
            user.LastName = request.LastName.Trim();
            user.PhoneNumber = request.PhoneNumber;
            user.UpdatedAt = DateTime.UtcNow;

            return Task.FromResult(true);
        }

        public Task<bool> DeactivateAsync(ulong userId)
        {
            var user =
                Users.FirstOrDefault(
                    item => item.UserId == userId);

            if (user is null)
            {
                return Task.FromResult(false);
            }

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            return Task.FromResult(true);
        }
    }
}