using LeaveService.Models;

namespace LeaveService.Services;

public interface ILeaveOverdueJobService
{
    Task<LeaveOverdueJobResult> RunOnceAsync(
        CancellationToken cancellationToken = default);
}
