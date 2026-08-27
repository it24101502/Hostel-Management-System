using IdentityService.DTOs;
using IdentityService.Exceptions;
using IdentityService.Models;
using IdentityService.Repositories;
using IdentityService.Services;

namespace IdentityService.Tests;

public class StudentProfileValidationTests
{
    [Fact]
    public async Task Create_WithUniqueValues_CreatesProfile()
    {
        var repository =
            new FakeStudentProfileRepository();

        var uniquenessService =
            new FakeProfileUniquenessService();

        var service =
            new StudentProfileService(
                repository,
                uniquenessService);

        StudentProfileResponse result =
            await service.CreateAsync(
                CreateRequest());

        Assert.Equal(
            "student@example.com",
            result.Email);

        Assert.Equal(
            "IT26000001",
            result.RegistrationNumber);

        Assert.True(
            uniquenessService.CreateValidationCalled);

        Assert.True(repository.CreateCalled);
    }

    [Fact]
    public async Task Create_WithDuplicateEmail_IsRejected()
    {
        var repository =
            new FakeStudentProfileRepository();

        var uniquenessService =
            new FakeProfileUniquenessService
            {
                ThrowDuplicateEmail = true
            };

        var service =
            new StudentProfileService(
                repository,
                uniquenessService);

        await Assert.ThrowsAsync<DuplicateEmailException>(
            () => service.CreateAsync(
                CreateRequest()));

        Assert.False(repository.CreateCalled);
    }

    [Fact]
    public async Task Update_WithUniqueValues_UpdatesProfile()
    {
        var repository =
            new FakeStudentProfileRepository();

        repository.Profiles.Add(
            CreateProfile());

        var uniquenessService =
            new FakeProfileUniquenessService();

        var service =
            new StudentProfileService(
                repository,
                uniquenessService);

        var request = new UpdateStudentProfileRequest
        {
            Email = "updated@example.com",
            RegistrationNumber = "IT26000002",
            DateOfBirth = new DateTime(2003, 5, 15),
            Gender = "FEMALE",
            City = "Colombo",
            ProgrammeName =
                "BSc (Hons) in Information Technology",
            FacultyName = "Faculty of Computing",
            AcademicYear = 3
        };

        StudentProfileResponse? result =
            await service.UpdateAsync(1, request);

        Assert.NotNull(result);

        Assert.Equal(
            "updated@example.com",
            result.Email);

        Assert.Equal(
            "IT26000002",
            result.RegistrationNumber);

        Assert.True(
            uniquenessService.UpdateValidationCalled);

        Assert.True(repository.UpdateCalled);
    }

    [Fact]
    public async Task Update_WithDuplicateRegistration_IsRejected()
    {
        var repository =
            new FakeStudentProfileRepository();

        repository.Profiles.Add(
            CreateProfile());

        var uniquenessService =
            new FakeProfileUniquenessService
            {
                ThrowDuplicateRegistration = true
            };

        var service =
            new StudentProfileService(
                repository,
                uniquenessService);

        var request = new UpdateStudentProfileRequest
        {
            Email = "student@example.com",
            RegistrationNumber = "IT26000999"
        };

        await Assert.ThrowsAsync<
            DuplicateRegistrationNumberException>(
                () => service.UpdateAsync(
                    1,
                    request));

        Assert.False(repository.UpdateCalled);
    }

    private static CreateStudentProfileRequest
        CreateRequest()
    {
        return new CreateStudentProfileRequest
        {
            UserId = 1,
            Email = "student@example.com",
            RegistrationNumber = "IT26000001",
            DateOfBirth =
                new DateTime(2003, 5, 15),
            Gender = "FEMALE",
            City = "Colombo",
            ProgrammeName =
                "BSc (Hons) in Information Technology",
            FacultyName = "Faculty of Computing",
            AcademicYear = 3
        };
    }

    private static StudentProfile CreateProfile()
    {
        return new StudentProfile
        {
            StudentProfileId = 1,
            UserId = 1,
            Email = "student@example.com",
            RegistrationNumber = "IT26000001",
            DateOfBirth =
                new DateTime(2003, 5, 15),
            Gender = "FEMALE",
            City = "Colombo",
            AcademicYear = 3,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private sealed class FakeProfileUniquenessService
        : IProfileUniquenessService
    {
        public bool ThrowDuplicateEmail { get; set; }

        public bool ThrowDuplicateRegistration { get; set; }

        public bool CreateValidationCalled { get; private set; }

        public bool UpdateValidationCalled { get; private set; }

        public Task ValidateForCreateAsync(
            string email,
            string registrationNumber,
            ulong userId)
        {
            CreateValidationCalled = true;

            if (ThrowDuplicateEmail)
            {
                throw new DuplicateEmailException();
            }

            if (ThrowDuplicateRegistration)
            {
                throw new DuplicateRegistrationNumberException();
            }

            return Task.CompletedTask;
        }

        public Task ValidateForUpdateAsync(
            string email,
            string registrationNumber,
            ulong userId,
            ulong studentProfileId)
        {
            UpdateValidationCalled = true;

            if (ThrowDuplicateEmail)
            {
                throw new DuplicateEmailException();
            }

            if (ThrowDuplicateRegistration)
            {
                throw new DuplicateRegistrationNumberException();
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeStudentProfileRepository
        : IStudentProfileRepository
    {
        public List<StudentProfile> Profiles { get; } = [];

        public bool CreateCalled { get; private set; }

        public bool UpdateCalled { get; private set; }

        public Task<bool> UserExistsAsync(ulong userId)
        {
            return Task.FromResult(true);
        }

        public Task<bool> ProfileExistsForUserAsync(
            ulong userId)
        {
            bool exists =
                Profiles.Any(profile =>
                    profile.UserId == userId);

            return Task.FromResult(exists);
        }

        public Task<StudentProfile?> GetByIdAsync(
            ulong studentProfileId)
        {
            StudentProfile? profile =
                Profiles.FirstOrDefault(item =>
                    item.StudentProfileId ==
                    studentProfileId);

            return Task.FromResult(profile);
        }

        public Task<ulong> CreateAsync(
            CreateStudentProfileRequest request)
        {
            CreateCalled = true;

            ulong profileId =
                Profiles.Count == 0
                    ? 1
                    : Profiles.Max(profile =>
                        profile.StudentProfileId) + 1;

            Profiles.Add(new StudentProfile
            {
                StudentProfileId = profileId,
                UserId = request.UserId,
                Email = request.Email,
                RegistrationNumber =
                    request.RegistrationNumber,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                AddressLine1 = request.AddressLine1,
                AddressLine2 = request.AddressLine2,
                City = request.City,
                District = request.District,
                PostalCode = request.PostalCode,
                ProgrammeName =
                    request.ProgrammeName,
                FacultyName = request.FacultyName,
                AcademicYear = request.AcademicYear,
                ProfilePhotoUrl =
                    request.ProfilePhotoUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            return Task.FromResult(profileId);
        }

        public Task<bool> UpdateAsync(
            ulong studentProfileId,
            UpdateStudentProfileRequest request)
        {
            StudentProfile? profile =
                Profiles.FirstOrDefault(item =>
                    item.StudentProfileId ==
                    studentProfileId);

            if (profile is null)
            {
                return Task.FromResult(false);
            }

            UpdateCalled = true;

            profile.Email = request.Email;

            profile.RegistrationNumber =
                request.RegistrationNumber;

            profile.DateOfBirth =
                request.DateOfBirth;

            profile.Gender = request.Gender;
            profile.AddressLine1 =
                request.AddressLine1;
            profile.AddressLine2 =
                request.AddressLine2;
            profile.City = request.City;
            profile.District = request.District;
            profile.PostalCode = request.PostalCode;
            profile.ProgrammeName =
                request.ProgrammeName;
            profile.FacultyName =
                request.FacultyName;
            profile.AcademicYear =
                request.AcademicYear;
            profile.ProfilePhotoUrl =
                request.ProfilePhotoUrl;
            profile.UpdatedAt = DateTime.UtcNow;

            return Task.FromResult(true);
        }

        public Task<StudentProfile?> GetByUserIdAsync(
            ulong userId)
        {
            StudentProfile? profile =
                Profiles.FirstOrDefault(item =>
                    item.UserId == userId);

            return Task.FromResult(profile);
        }

        public Task<bool> UpdateOwnAsync(
            ulong userId,
            UpdateOwnStudentProfileRequest request)
        {
            StudentProfile? profile =
                Profiles.FirstOrDefault(item =>
                    item.UserId == userId);

            if (profile is null)
            {
                return Task.FromResult(false);
            }

            profile.AddressLine1 =
                request.AddressLine1;

            profile.AddressLine2 =
                request.AddressLine2;

            profile.City =
                request.City;

            profile.District =
                request.District;

            profile.PostalCode =
                request.PostalCode;

            profile.ProfilePhotoUrl =
                request.ProfilePhotoUrl;

            profile.UpdatedAt =
                DateTime.UtcNow;

            return Task.FromResult(true);
        }

        public Task<bool> UpdateOwnPhotoAsync(
            ulong userId,
            string profilePhotoUrl)
        {
            StudentProfile? profile =
                Profiles.FirstOrDefault(item =>
                    item.UserId == userId);

            if (profile is null)
            {
                return Task.FromResult(false);
            }

            profile.ProfilePhotoUrl =
                profilePhotoUrl;

            return Task.FromResult(true);
        }
    }
}