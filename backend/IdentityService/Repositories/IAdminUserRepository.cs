using IdentityService.DTOs;
using IdentityService.Models;

namespace IdentityService.Repositories;

public interface IAdminUserRepository
{
    Task<UserAccount?> GetByIdAsync(ulong userId);

    Task<IReadOnlyList<UserAccount>> GetAllAsync();

    Task<bool> RoleExistsAsync(ulong roleId);

    Task<bool> UsernameOrEmailExistsAsync(
        string normalizedUsername,
        string normalizedEmail,
        ulong? excludedUserId = null);

    Task<ulong> CreateAsync(
        CreateUserRequest request,
        string passwordHash);

    Task<bool> UpdateAsync(
        ulong userId,
        UpdateUserRequest request);

    Task<bool> DeactivateAsync(ulong userId);
}