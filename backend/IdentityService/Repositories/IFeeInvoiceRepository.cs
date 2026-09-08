using IdentityService.DTOs;
using IdentityService.Models;

namespace IdentityService.Repositories;

public interface IFeeInvoiceRepository
{
    Task<bool> StudentProfileExistsAsync(
        ulong studentProfileId);

    Task<ulong> CreateAsync(
        string invoiceNumber,
        CreateFeeInvoiceRequest request);

    Task<FeeInvoice?> GetByIdAsync(
        ulong invoiceId);
}