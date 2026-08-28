using IdentityService.Exceptions;
using IdentityService.Repositories;
using IdentityService.Services;

namespace IdentityService.Tests;

public class ProfileUniquenessServiceTests
{
    [Fact]
    public async Task Create_WithUniqueValues_PassesValidation()
    {
        var repository =
            new FakeProfileUniquenessRepository();

        var service =
            new ProfileUniquenessService(repository);

        await service.ValidateForCreateAsync(
            "student@example.com",
            "IT26000001",
            userId: 10);

        Assert.Equal(
            (ulong)10,
            repository.LastExcludedUserId);

        Assert.Equal(
            "STUDENT@EXAMPLE.COM",
            repository.LastNormalizedEmail);

        Assert.Equal(
            "IT26000001",
            repository.LastNormalizedRegistrationNumber);
    }

    [Fact]
    public async Task Create_WithDuplicateEmail_ThrowsClearException()
    {
        var repository =
            new FakeProfileUniquenessRepository
            {
                EmailExists = true
            };

        var service =
            new ProfileUniquenessService(repository);

        var exception =
            await Assert.ThrowsAsync<DuplicateEmailException>(
                () => service.ValidateForCreateAsync(
                    "existing@example.com",
                    "IT26000002",
                    userId: 10));

        Assert.Contains(
            "email address already exists",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_WithDuplicateRegistration_ThrowsClearException()
    {
        var repository =
            new FakeProfileUniquenessRepository
            {
                RegistrationNumberExists = true
            };

        var service =
            new ProfileUniquenessService(repository);

        var exception =
            await Assert.ThrowsAsync<
                DuplicateRegistrationNumberException>(
                () => service.ValidateForCreateAsync(
                    "unique@example.com",
                    "IT26000001",
                    userId: 10));

        Assert.Contains(
            "registration number already exists",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Update_WithCurrentValues_PassesValidation()
    {
        var repository =
            new FakeProfileUniquenessRepository();

        var service =
            new ProfileUniquenessService(repository);

        await service.ValidateForUpdateAsync(
            "student@example.com",
            "IT26000001",
            userId: 10,
            studentProfileId: 20);

        Assert.Equal(
            (ulong)10,
            repository.LastExcludedUserId);

        Assert.Equal(
            (ulong)20,
            repository.LastExcludedStudentProfileId);
    }

    [Fact]
    public async Task Update_WithAnotherUsersEmail_ThrowsException()
    {
        var repository =
            new FakeProfileUniquenessRepository
            {
                EmailExists = true
            };

        var service =
            new ProfileUniquenessService(repository);

        await Assert.ThrowsAsync<DuplicateEmailException>(
            () => service.ValidateForUpdateAsync(
                "duplicate@example.com",
                "IT26000010",
                userId: 10,
                studentProfileId: 20));
    }

    [Fact]
    public async Task Update_WithAnotherProfilesRegistration_ThrowsException()
    {
        var repository =
            new FakeProfileUniquenessRepository
            {
                RegistrationNumberExists = true
            };

        var service =
            new ProfileUniquenessService(repository);

        await Assert.ThrowsAsync<
            DuplicateRegistrationNumberException>(
                () => service.ValidateForUpdateAsync(
                    "unique@example.com",
                    "IT26000099",
                    userId: 10,
                    studentProfileId: 20));
    }

    private sealed class FakeProfileUniquenessRepository
        : IProfileUniquenessRepository
    {
        public bool EmailExists { get; set; }

        public bool RegistrationNumberExists { get; set; }

        public string? LastNormalizedEmail { get; private set; }

        public string? LastNormalizedRegistrationNumber
        {
            get;
            private set;
        }

        public ulong? LastExcludedUserId { get; private set; }

        public ulong? LastExcludedStudentProfileId
        {
            get;
            private set;
        }

        public Task<bool> EmailExistsAsync(
            string normalizedEmail,
            ulong? excludedUserId = null)
        {
            LastNormalizedEmail = normalizedEmail;
            LastExcludedUserId = excludedUserId;

            return Task.FromResult(EmailExists);
        }

        public Task<bool> RegistrationNumberExistsAsync(
            string normalizedRegistrationNumber,
            ulong? excludedStudentProfileId = null)
        {
            LastNormalizedRegistrationNumber =
                normalizedRegistrationNumber;

            LastExcludedStudentProfileId =
                excludedStudentProfileId;

            return Task.FromResult(
                RegistrationNumberExists);
        }
    }
}