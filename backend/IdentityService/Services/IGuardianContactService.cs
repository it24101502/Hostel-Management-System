using IdentityService.DTOs;

namespace IdentityService.Services;

public interface IGuardianContactService
{
    Task<IReadOnlyList<GuardianContactResponse>>
        GetByStudentProfileIdAsync(ulong studentProfileId);

    Task<GuardianContactResponse?> GetByIdAsync(
        ulong studentProfileId,
        ulong contactId);

    Task<GuardianContactResponse> CreateAsync(
        ulong studentProfileId,
        CreateGuardianContactRequest request);

    Task<GuardianContactResponse?> UpdateAsync(
        ulong studentProfileId,
        ulong contactId,
        UpdateGuardianContactRequest request);
}