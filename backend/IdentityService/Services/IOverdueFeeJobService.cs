using IdentityService.Models;

namespace IdentityService.Services;

public interface IOverdueFeeJobService
{
    Task<OverdueFeeJobResult> RunOnceAsync(
        CancellationToken cancellationToken =
            default);
}