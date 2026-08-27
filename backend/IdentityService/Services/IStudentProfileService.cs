using IdentityService.DTOs;

namespace IdentityService.Services;

public interface IStudentProfileService
{
    Task<StudentProfileResponse?> GetByIdAsync(
        ulong studentProfileId);

    Task<StudentProfileResponse> CreateAsync(
        CreateStudentProfileRequest request);

    Task<StudentProfileResponse?> UpdateAsync(
        ulong studentProfileId,
        UpdateStudentProfileRequest request);
}