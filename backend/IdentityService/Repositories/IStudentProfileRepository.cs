using IdentityService.DTOs;
using IdentityService.Models;

namespace IdentityService.Repositories;

public interface IStudentProfileRepository
{
    Task<bool> UserExistsAsync(ulong userId);

    Task<bool> ProfileExistsForUserAsync(ulong userId);

    Task<StudentProfile?> GetByIdAsync(
        ulong studentProfileId);

    Task<StudentProfile?> GetByUserIdAsync(
        ulong userId);

    Task<ulong> CreateAsync(
        CreateStudentProfileRequest request);

    Task<bool> UpdateAsync(
        ulong studentProfileId,
        UpdateStudentProfileRequest request);

    Task<bool> UpdateOwnAsync(
        ulong userId,
        UpdateOwnStudentProfileRequest request);
}