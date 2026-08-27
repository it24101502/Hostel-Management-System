using IdentityService.DTOs;

namespace IdentityService.Services;

public interface IStudentProfileService
{
    Task<StudentProfileResponse?> GetByIdAsync(
        ulong studentProfileId);

    Task<StudentProfileResponse?> GetOwnAsync(
        ulong userId);

    Task<StudentProfileResponse> CreateAsync(
        CreateStudentProfileRequest request);

    Task<StudentProfileResponse?> UpdateAsync(
        ulong studentProfileId,
        UpdateStudentProfileRequest request);

    Task<StudentProfileResponse?> UpdateOwnAsync(
        ulong userId,
        UpdateOwnStudentProfileRequest request);

    Task<StudentProfileResponse?> UpdateOwnPhotoAsync(
        ulong userId,
        string profilePhotoUrl);
}