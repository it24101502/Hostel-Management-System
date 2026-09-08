using IdentityService.DTOs;

namespace IdentityService.Services;

public interface IAdminUserService
{
    Task<IReadOnlyList<UserResponse>> GetAllAsync();

    Task<UserResponse?> GetByIdAsync(ulong userId);

    Task<UserResponse> CreateAsync(
        CreateUserRequest request);

    Task<UserResponse?> UpdateAsync(
        ulong userId,
        UpdateUserRequest request);

    Task<bool> DeactivateAsync(ulong userId);
}