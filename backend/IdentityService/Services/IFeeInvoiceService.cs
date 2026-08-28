using IdentityService.DTOs;

namespace IdentityService.Services;

public interface IFeeInvoiceService
{
    Task<FeeInvoiceResponse> CreateAsync(
        CreateFeeInvoiceRequest request);
}