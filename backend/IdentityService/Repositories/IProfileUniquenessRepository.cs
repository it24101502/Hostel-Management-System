namespace IdentityService.Repositories;

public interface IProfileUniquenessRepository
{
    Task<bool> EmailExistsAsync(
        string normalizedEmail,
        ulong? excludedUserId = null);

    Task<bool> RegistrationNumberExistsAsync(
        string normalizedRegistrationNumber,
        ulong? excludedStudentProfileId = null);
}