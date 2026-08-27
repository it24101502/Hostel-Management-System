using IdentityService.DTOs;
using IdentityService.Exceptions;
using IdentityService.Models;
using IdentityService.Repositories;
using IdentityService.Services;

namespace IdentityService.Tests;

public class StudentSelfProfileServiceTests
{
    [Fact]
    public async Task GetOwn_WithAuthenticatedUser_ReturnsOwnProfile()
    {
        var repository =
            new FakeStudentProfileRepository();

        repository.Profiles.Add(
            CreateProfile(
                profileId: 1,
                userId: 10));

        repository.Profiles.Add(
            CreateProfile(
                profileId: 2,
                userId: 20));

        var service =
            CreateService(repository);

        StudentProfileResponse? result =
            await service.GetOwnAsync(userId: 10);

        Assert.NotNull(result);
        Assert.Equal((ulong)10, result.UserId);
        Assert.Equal((ulong)1, result.StudentProfileId);
    }

    [Fact]
    public async Task UpdateOwn_WithPermittedFields_UpdatesProfile()
    {
        var repository =
            new FakeStudentProfileRepository();

        repository.Profiles.Add(
            CreateProfile(
                profileId: 1,
                userId: 10));

        var service =
            CreateService(repository);

        var request =
            new UpdateOwnStudentProfileRequest
            {
                AddressLine1 = "25 New Road",
                AddressLine2 = "Apartment 2",
                City = "Colombo",
                District = "Colombo",
                PostalCode = "10100",
                ProfilePhotoUrl =
                    "https://example.com/photo.jpg"
            };

        StudentProfileResponse? result =
            await service.UpdateOwnAsync(
                userId: 10,
                request);

        Assert.NotNull(result);
        Assert.Equal(
            "25 New Road",
            result.AddressLine1);

        Assert.Equal("Colombo", result.City);

        Assert.Equal(
            "10100",
            result.PostalCode);

        Assert.True(repository.UpdateOwnCalled);
    }

    [Fact]
    public async Task UpdateOwn_WithRestrictedField_IsRejected()
    {
        var repository =
            new FakeStudentProfileRepository();

        repository.Profiles.Add(
            CreateProfile(
                profileId: 1,
                userId: 10));

        var service =
            CreateService(repository);

        var request =
            new UpdateOwnStudentProfileRequest
            {
                City = "Colombo"
            };

        request.AdditionalFields.Add(
            "registrationNumber",
            default);

        RestrictedProfileFieldException exception =
            await Assert.ThrowsAsync<
                RestrictedProfileFieldException>(
                () => service.UpdateOwnAsync(
                    userId: 10,
                    request));

        Assert.Contains(
            "registrationNumber",
            exception.RestrictedFields);

        Assert.False(repository.UpdateOwnCalled);
    }

    [Fact]
    public async Task UpdateOwn_UsesAuthenticatedUserIdOnly()
    {
        var repository =
            new FakeStudentProfileRepository();

        repository.Profiles.Add(
            CreateProfile(
                profileId: 1,
                userId: 10));

        repository.Profiles.Add(
            CreateProfile(
                profileId: 2,
                userId: 20));

        var service =
            CreateService(repository);

        var request =
            new UpdateOwnStudentProfileRequest
            {
                City = "Kandy"
            };

        await service.UpdateOwnAsync(
            userId: 10,
            request);

        StudentProfile profile10 =
            repository.Profiles.Single(
                profile => profile.UserId == 10);

        StudentProfile profile20 =
            repository.Profiles.Single(
                profile => profile.UserId == 20);

        Assert.Equal("Kandy", profile10.City);
        Assert.Equal("Old City", profile20.City);

        Assert.Equal(
            (ulong)10,
            repository.LastUpdatedUserId);
    }

    [Fact]
    public async Task GetOwn_WithMissingProfile_ReturnsNull()
    {
        var repository =
            new FakeStudentProfileRepository();

        var service =
            CreateService(repository);

        StudentProfileResponse? result =
            await service.GetOwnAsync(userId: 999);

        Assert.Null(result);
    }

    private static StudentProfileService CreateService(
        IStudentProfileRepository repository)
    {
        return new StudentProfileService(
            repository,
            new FakeProfileUniquenessService());
    }

    private static StudentProfile CreateProfile(
        ulong profileId,
        ulong userId)
    {
        return new StudentProfile
        {
            StudentProfileId = profileId,
            UserId = userId,
            Email = $"student{userId}@example.com",
            RegistrationNumber =
                $"IT2600{userId:D4}",
            AddressLine1 = "Old Address",
            City = "Old City",
            District = "Old District",
            PostalCode = "10000",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private sealed class FakeStudentProfileRepository
        : IStudentProfileRepository
    {
        public List<StudentProfile> Profiles { get; } = [];

        public bool UpdateOwnCalled { get; private set; }

        public ulong? LastUpdatedUserId { get; private set; }

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

            UpdateOwnCalled = true;
            LastUpdatedUserId = userId;

            profile.AddressLine1 =
                request.AddressLine1;

            profile.AddressLine2 =
                request.AddressLine2;

            profile.City = request.City;

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

            profile.UpdatedAt =
                DateTime.UtcNow;

            return Task.FromResult(true);
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

        public Task<bool> UserExistsAsync(ulong userId)
        {
            return Task.FromResult(true);
        }

        public Task<bool> ProfileExistsForUserAsync(
            ulong userId)
        {
            return Task.FromResult(
                Profiles.Any(profile =>
                    profile.UserId == userId));
        }

        public Task<ulong> CreateAsync(
            CreateStudentProfileRequest request)
        {
            throw new NotSupportedException();
        }

        public Task<bool> UpdateAsync(
            ulong studentProfileId,
            UpdateStudentProfileRequest request)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeProfileUniquenessService
        : IProfileUniquenessService
    {
        public Task ValidateForCreateAsync(
            string email,
            string registrationNumber,
            ulong userId)
        {
            return Task.CompletedTask;
        }

        public Task ValidateForUpdateAsync(
            string email,
            string registrationNumber,
            ulong userId,
            ulong studentProfileId)
        {
            return Task.CompletedTask;
        }

    }
}