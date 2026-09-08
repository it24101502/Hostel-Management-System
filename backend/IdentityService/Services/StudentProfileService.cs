using IdentityService.DTOs;
using IdentityService.Exceptions;
using IdentityService.Models;
using IdentityService.Repositories;
using MySqlConnector;

namespace IdentityService.Services;

public class StudentProfileService
    : IStudentProfileService
{
    private readonly IStudentProfileRepository
        _profileRepository;

    private readonly IProfileUniquenessService
        _uniquenessService;

    public StudentProfileService(
        IStudentProfileRepository profileRepository,
        IProfileUniquenessService uniquenessService)
    {
        _profileRepository = profileRepository;
        _uniquenessService = uniquenessService;
    }

    public async Task<StudentProfileResponse?> GetByIdAsync(
        ulong studentProfileId)
    {
        StudentProfile? profile =
            await _profileRepository.GetByIdAsync(
                studentProfileId);

        return profile is null
            ? null
            : MapResponse(profile);
    }

    public async Task<StudentProfileResponse> CreateAsync(
        CreateStudentProfileRequest request)
    {
        bool userExists =
            await _profileRepository.UserExistsAsync(
                request.UserId);

        if (!userExists)
        {
            throw new KeyNotFoundException(
                "User account was not found.");
        }

        bool profileExists =
            await _profileRepository
                .ProfileExistsForUserAsync(
                    request.UserId);

        if (profileExists)
        {
            throw new InvalidOperationException(
                "A student profile already exists for this user.");
        }

        await _uniquenessService.ValidateForCreateAsync(
            request.Email,
            request.RegistrationNumber,
            request.UserId);

        try
        {
            ulong profileId =
                await _profileRepository.CreateAsync(
                    request);

            StudentProfile? createdProfile =
                await _profileRepository.GetByIdAsync(
                    profileId);

            if (createdProfile is null)
            {
                throw new InvalidOperationException(
                    "The profile was created but could not be retrieved.");
            }

            return MapResponse(createdProfile);
        }
        catch (MySqlException exception)
            when (exception.Number == 1062)
        {
            throw ConvertDuplicateException(exception);
        }
    }

    public async Task<StudentProfileResponse?> UpdateAsync(
        ulong studentProfileId,
        UpdateStudentProfileRequest request)
    {
        StudentProfile? existingProfile =
            await _profileRepository.GetByIdAsync(
                studentProfileId);

        if (existingProfile is null)
        {
            return null;
        }

        await _uniquenessService.ValidateForUpdateAsync(
            request.Email,
            request.RegistrationNumber,
            existingProfile.UserId,
            studentProfileId);

        try
        {
            bool updated =
                await _profileRepository.UpdateAsync(
                    studentProfileId,
                    request);

            if (!updated)
            {
                return null;
            }

            StudentProfile? updatedProfile =
                await _profileRepository.GetByIdAsync(
                    studentProfileId);

            return updatedProfile is null
                ? null
                : MapResponse(updatedProfile);
        }
        catch (MySqlException exception)
            when (exception.Number == 1062)
        {
            throw ConvertDuplicateException(exception);
        }
    }

    private static Exception ConvertDuplicateException(
        MySqlException exception)
    {
        string databaseMessage =
            exception.Message.ToLowerInvariant();

        if (databaseMessage.Contains(
                "normalized_email") ||
            databaseMessage.Contains(
                "uq_users_email"))
        {
            return new DuplicateEmailException();
        }

        if (databaseMessage.Contains(
                "registration"))
        {
            return new DuplicateRegistrationNumberException();
        }

        return new InvalidOperationException(
            "A duplicate value already exists.",
            exception);
    }

    private static StudentProfileResponse MapResponse(
        StudentProfile profile)
    {
        return new StudentProfileResponse
        {
            StudentProfileId =
                profile.StudentProfileId,

            UserId = profile.UserId,

            Email = profile.Email,

            RegistrationNumber =
                profile.RegistrationNumber,

            DateOfBirth = profile.DateOfBirth,

            Gender = profile.Gender,

            AddressLine1 = profile.AddressLine1,

            AddressLine2 = profile.AddressLine2,

            City = profile.City,

            District = profile.District,

            PostalCode = profile.PostalCode,

            ProgrammeName = profile.ProgrammeName,

            FacultyName = profile.FacultyName,

            AcademicYear = profile.AcademicYear,

            ProfilePhotoUrl =
                profile.ProfilePhotoUrl,

            CreatedAt = profile.CreatedAt,

            UpdatedAt = profile.UpdatedAt
        };
    }

    public async Task<StudentProfileResponse?>
        UpdateOwnPhotoAsync(
            ulong userId,
            string profilePhotoUrl)
    {
        StudentProfile? existingProfile =
            await _profileRepository.GetByUserIdAsync(
                userId);

        if (existingProfile is null)
        {
            return null;
        }

        bool updated =
            await _profileRepository.UpdateOwnPhotoAsync(
                userId,
                profilePhotoUrl);

        if (!updated)
        {
            return null;
        }

        StudentProfile? updatedProfile =
            await _profileRepository.GetByUserIdAsync(
                userId);

        return updatedProfile is null
            ? null
            : MapResponse(updatedProfile);
    }

    public async Task<StudentProfileResponse?> GetOwnAsync(
        ulong userId)
    {
        StudentProfile? profile =
            await _profileRepository.GetByUserIdAsync(
                userId);

        return profile is null
            ? null
            : MapResponse(profile);
    }

    public async Task<StudentProfileResponse?> UpdateOwnAsync(
        ulong userId,
        UpdateOwnStudentProfileRequest request)
    {
        if (request.AdditionalFields is { Count: > 0 })
        {
            throw new RestrictedProfileFieldException(
                request.AdditionalFields.Keys);
        }

        StudentProfile? existingProfile =
            await _profileRepository.GetByUserIdAsync(
                userId);

        if (existingProfile is null)
        {
            return null;
        }

        bool updated =
            await _profileRepository.UpdateOwnAsync(
                userId,
                request);

        if (!updated)
        {
            return null;
        }

        StudentProfile? updatedProfile =
            await _profileRepository.GetByUserIdAsync(
                userId);

        return updatedProfile is null
            ? null
            : MapResponse(updatedProfile);
    }
}