using IdentityService.DTOs;
using IdentityService.Repositories;

namespace IdentityService.Services;

public class AuthService : IAuthService
{
    private const uint MaximumFailedAttempts = 5;
    private const int LockoutMinutes = 15;

    private readonly IUserRepository _userRepository;

    public AuthService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<LoginResponse?> LoginAsync(
        LoginRequest request)
    {
        string normalizedIdentifier =
            request.Identifier.Trim().ToUpperInvariant();

        var user =
            await _userRepository.FindByIdentifierAsync(
                normalizedIdentifier);

        // Do not reveal whether the user, account or role exists.
        if (user is null ||
            !user.IsActive ||
            !user.IsRoleActive)
        {
            return null;
        }

        // Reject an account that is still locked.
        if (user.IsLocked &&
            (!user.LockoutEndAt.HasValue ||
             user.LockoutEndAt.Value > DateTime.UtcNow))
        {
            return null;
        }

        bool passwordIsCorrect = BCrypt.Net.BCrypt.Verify(
            request.Password,
            user.PasswordHash);

        if (!passwordIsCorrect)
        {
            uint failedAttempts =
                user.FailedLoginAttempts + 1;

            bool shouldLock =
                failedAttempts >= MaximumFailedAttempts;

            DateTime? lockoutEndAt = shouldLock
                ? DateTime.UtcNow.AddMinutes(LockoutMinutes)
                : null;

            await _userRepository.RecordFailedLoginAsync(
                user.UserId,
                failedAttempts,
                shouldLock,
                lockoutEndAt);

            return null;
        }

        await _userRepository.RecordSuccessfulLoginAsync(
            user.UserId);

        return new LoginResponse
        {
            Message = "Login successful.",
            UserId = user.UserId,
            Username = user.Username,
            Role = user.RoleName
        };
    }
}