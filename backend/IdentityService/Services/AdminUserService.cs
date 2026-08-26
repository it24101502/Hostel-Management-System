using IdentityService.DTOs;
using IdentityService.Exceptions;
using IdentityService.Models;
using IdentityService.Repositories;

namespace IdentityService.Services;

public class AdminUserService : IAdminUserService
{
    private readonly IAdminUserRepository
        _adminUserRepository;

    public AdminUserService(
        IAdminUserRepository adminUserRepository)
    {
        _adminUserRepository = adminUserRepository;
    }

    public async Task<IReadOnlyList<UserResponse>>
        GetAllAsync()
    {
        var users =
            await _adminUserRepository.GetAllAsync();

        return users.Select(MapResponse).ToList();
    }

    public async Task<UserResponse?> GetByIdAsync(
        ulong userId)
    {
        var user =
            await _adminUserRepository.GetByIdAsync(userId);

        return user is null
            ? null
            : MapResponse(user);
    }

    public async Task<UserResponse> CreateAsync(
        CreateUserRequest request)
    {
        bool roleExists =
            await _adminUserRepository.RoleExistsAsync(
                request.RoleId);

        if (!roleExists)
        {
            throw new RoleNotFoundException();
        }

        string normalizedUsername =
            request.Username.Trim().ToUpperInvariant();

        string normalizedEmail =
            request.Email.Trim().ToUpperInvariant();

        bool duplicateExists =
            await _adminUserRepository
                .UsernameOrEmailExistsAsync(
                    normalizedUsername,
                    normalizedEmail);

        if (duplicateExists)
        {
            throw new DuplicateUserException();
        }

        string passwordHash =
            BCrypt.Net.BCrypt.HashPassword(
                request.Password);

        ulong userId =
            await _adminUserRepository.CreateAsync(
                request,
                passwordHash);

        var createdUser =
            await _adminUserRepository.GetByIdAsync(
                userId);

        if (createdUser is null)
        {
            throw new InvalidOperationException(
                "The user was created but could not be retrieved.");
        }

        return MapResponse(createdUser);
    }

    public async Task<UserResponse?> UpdateAsync(
        ulong userId,
        UpdateUserRequest request)
    {
        var existingUser =
            await _adminUserRepository.GetByIdAsync(
                userId);

        if (existingUser is null)
        {
            return null;
        }

        bool roleExists =
            await _adminUserRepository.RoleExistsAsync(
                request.RoleId);

        if (!roleExists)
        {
            throw new RoleNotFoundException();
        }

        string normalizedUsername =
            request.Username.Trim().ToUpperInvariant();

        string normalizedEmail =
            request.Email.Trim().ToUpperInvariant();

        bool duplicateExists =
            await _adminUserRepository
                .UsernameOrEmailExistsAsync(
                    normalizedUsername,
                    normalizedEmail,
                    userId);

        if (duplicateExists)
        {
            throw new DuplicateUserException();
        }

        bool updated =
            await _adminUserRepository.UpdateAsync(
                userId,
                request);

        if (!updated)
        {
            return null;
        }

        var updatedUser =
            await _adminUserRepository.GetByIdAsync(
                userId);

        return updatedUser is null
            ? null
            : MapResponse(updatedUser);
    }

    public async Task<bool> DeactivateAsync(
        ulong userId)
    {
        var existingUser =
            await _adminUserRepository.GetByIdAsync(
                userId);

        if (existingUser is null)
        {
            return false;
        }

        // Deactivation is idempotent:
        // an already inactive user remains inactive.
        if (!existingUser.IsActive)
        {
            return true;
        }

        return await _adminUserRepository
            .DeactivateAsync(userId);
    }

    private static UserResponse MapResponse(
        UserAccount user)
    {
        return new UserResponse
        {
            UserId = user.UserId,
            RoleId = user.RoleId,
            RoleName = user.RoleName,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}