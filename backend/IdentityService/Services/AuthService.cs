using IdentityService.DTOs;
using IdentityService.Models;
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
    private readonly ILoginAuditRepository? _loginAuditRepository;

    public AuthService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
        _lockoutOptions = new LockoutOptions();
        _jwtTokenService = null;
        _loginAuditRepository = null;
    }

    public AuthService(
        IUserRepository userRepository,
        IOptions<LockoutOptions> lockoutOptions)
    {
        _userRepository = userRepository;
        _lockoutOptions = lockoutOptions.Value;
        _jwtTokenService = null;
        _loginAuditRepository = null;

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
        _loginAuditRepository = null;

        if (_lockoutOptions.DurationMinutes <= 0)
        {
            throw new InvalidOperationException(
                "Lockout duration must be greater than zero.");
        }
    }

    public AuthService(
        IUserRepository userRepository,
        IOptions<LockoutOptions> lockoutOptions,
        IJwtTokenService jwtTokenService,
        ILoginAuditRepository loginAuditRepository)
    {
        _userRepository = userRepository;
        _lockoutOptions = lockoutOptions.Value;
        _jwtTokenService = jwtTokenService;
        _loginAuditRepository = loginAuditRepository;

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
            await RecordAuditAsync(
                user?.UserId,
                normalizedIdentifier,
                LoginAuditOutcomes.Failure);

            return null;
        }

        // Reject an account that is still locked.
        if (user.IsLocked &&
            (!user.LockoutEndAt.HasValue ||
             user.LockoutEndAt.Value > DateTime.UtcNow))
        {
            await RecordAuditAsync(
                user.UserId,
                normalizedIdentifier,
                LoginAuditOutcomes.Failure);

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

            await RecordAuditAsync(
                user.UserId,
                normalizedIdentifier,
                LoginAuditOutcomes.Failure);

            return null;
        }

        await _userRepository.RecordSuccessfulLoginAsync(
            user.UserId);

        var tokenResult =
            _jwtTokenService?.CreateToken(user);

        await RecordAuditAsync(
            user.UserId,
            normalizedIdentifier,
            LoginAuditOutcomes.Success);

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

    private Task RecordAuditAsync(
        ulong? userId,
        string identifier,
        string outcome)
    {
        if (_loginAuditRepository is null)
        {
            return Task.CompletedTask;
        }

        return _loginAuditRepository.RecordAttemptAsync(
            userId,
            identifier,
            outcome);
    }
}