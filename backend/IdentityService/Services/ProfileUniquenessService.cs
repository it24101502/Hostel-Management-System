using IdentityService.Exceptions;
using IdentityService.Repositories;

namespace IdentityService.Services;

public class ProfileUniquenessService
    : IProfileUniquenessService
{
    private readonly IProfileUniquenessRepository _repository;

    public ProfileUniquenessService(
        IProfileUniquenessRepository repository)
    {
        _repository = repository;
    }

    public async Task ValidateForCreateAsync(
        string email,
        string registrationNumber,
        ulong userId)
    {
        string normalizedEmail =
            NormalizeEmail(email);

        string normalizedRegistrationNumber =
            NormalizeRegistrationNumber(
                registrationNumber);

        bool emailExists =
            await _repository.EmailExistsAsync(
                normalizedEmail,
                userId);

        if (emailExists)
        {
            throw new DuplicateEmailException();
        }

        bool registrationExists =
            await _repository
                .RegistrationNumberExistsAsync(
                    normalizedRegistrationNumber);

        if (registrationExists)
        {
            throw new DuplicateRegistrationNumberException();
        }
    }

    public async Task ValidateForUpdateAsync(
        string email,
        string registrationNumber,
        ulong userId,
        ulong studentProfileId)
    {
        string normalizedEmail =
            NormalizeEmail(email);

        string normalizedRegistrationNumber =
            NormalizeRegistrationNumber(
                registrationNumber);

        bool emailExists =
            await _repository.EmailExistsAsync(
                normalizedEmail,
                userId);

        if (emailExists)
        {
            throw new DuplicateEmailException();
        }

        bool registrationExists =
            await _repository
                .RegistrationNumberExistsAsync(
                    normalizedRegistrationNumber,
                    studentProfileId);

        if (registrationExists)
        {
            throw new DuplicateRegistrationNumberException();
        }
    }

    private static string NormalizeEmail(string email)
    {
        return email
            .Trim()
            .ToUpperInvariant();
    }

    private static string NormalizeRegistrationNumber(
        string registrationNumber)
    {
        return registrationNumber
            .Trim()
            .ToUpperInvariant();
    }
}