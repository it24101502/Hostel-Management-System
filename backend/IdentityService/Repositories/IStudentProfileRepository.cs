using IdentityService.DTOs;
using IdentityService.Models;

namespace IdentityService.Repositories;

public interface IStudentProfileRepository
{
    Task<bool> UserExistsAsync(ulong userId);

    Task<bool> ProfileExistsForUserAsync(ulong userId);

    Task<StudentProfile?> GetByIdAsync(
        ulong studentProfileId);

    Task<ulong> CreateAsync(
        CreateStudentProfileRequest request);

    Task<bool> UpdateAsync(
        ulong studentProfileId,
        UpdateStudentProfileRequest request);
}