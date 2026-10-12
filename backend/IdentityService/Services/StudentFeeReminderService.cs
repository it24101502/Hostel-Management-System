using IdentityService.DTOs;
using IdentityService.Repositories;

namespace IdentityService.Services;

public class StudentFeeReminderService
    : IStudentFeeReminderService
{
    private readonly IStudentFeeReminderRepository _repository;

    public StudentFeeReminderService(
        IStudentFeeReminderRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<FeeReminderResponse>> GetMineAsync(
        ulong userId)
    {
        return _repository.GetForUserAsync(userId);
    }
}