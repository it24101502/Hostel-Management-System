using IdentityService.DTOs;
using IdentityService.Options;
using IdentityService.Repositories;
using Microsoft.Extensions.Options;

namespace IdentityService.Services;

public class AuthService : IAuthService
{
    private const uint MaximumFailedAttempts = 5;
    private readonly LockoutOptions _lockoutOptions;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService? _jwtTokenService;

    public AuthService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
        _lockoutOptions = new LockoutOptions();
        _jwtTokenService = null;
    }

    public AuthService(
        IUserRepository userRepository,
        IOptions<LockoutOptions> lockoutOptions)
    {
        _userRepository = userRepository;
        _lockoutOptions = lockoutOptions.Value;
        _jwtTokenService = null;

        if (_lockoutOptions.DurationMinutes <= 0)
        {
            throw new InvalidOperationException(
                "Lockout duration must be greater than zero.");
        }
    }

    public AuthService(
        IUserRepository userRepository,
        IOptions<LockoutOptions> lockoutOptions,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _lockoutOptions = lockoutOptions.Value;
        _jwtTokenService = jwtTokenService;

        if (_lockoutOptions.DurationMinutes <= 0)
        {
            throw new InvalidOperationException(
                "Lockout duration must be greater than zero.");
        }
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
            ? DateTime.UtcNow.AddMinutes(
                _lockoutOptions.DurationMinutes)
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

        var tokenResult =
            _jwtTokenService?.CreateToken(user);

        return new LoginResponse
        {
            Message = "Login successful.",
            UserId = user.UserId,
            Username = user.Username,
            Role = user.RoleName,
            AccessToken =
                tokenResult?.AccessToken ?? string.Empty,
            ExpiresAt =
                tokenResult?.ExpiresAt ?? default
        };
    }
}