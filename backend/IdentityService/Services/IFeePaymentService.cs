using IdentityService.DTOs;

namespace IdentityService.Services;

public interface IFeePaymentService
{
    Task<RecordFeePaymentResponse>
        RecordAsync(
            ulong invoiceId,
            ulong recordedByUserId,
            RecordFeePaymentRequest request);
}