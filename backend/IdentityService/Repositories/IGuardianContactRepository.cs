using IdentityService.DTOs;
using IdentityService.Models;

namespace IdentityService.Repositories;

public interface IGuardianContactRepository
{
    Task<bool> StudentProfileExistsAsync(ulong studentProfileId);

    Task<IReadOnlyList<GuardianContact>> GetByStudentProfileIdAsync(
        ulong studentProfileId);

    Task<GuardianContact?> GetByIdAsync(
        ulong studentProfileId,
        ulong contactId);

    Task<ulong> CreateAsync(
        ulong studentProfileId,
        CreateGuardianContactRequest request);

    Task<bool> UpdateAsync(
        ulong studentProfileId,
        ulong contactId,
        UpdateGuardianContactRequest request);
}