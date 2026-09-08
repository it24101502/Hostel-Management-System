namespace IdentityService.Services;

public interface IProfileUniquenessService
{
    Task ValidateForCreateAsync(
        string email,
        string registrationNumber,
        ulong userId);

    Task ValidateForUpdateAsync(
        string email,
        string registrationNumber,
        ulong userId,
        ulong studentProfileId);
}