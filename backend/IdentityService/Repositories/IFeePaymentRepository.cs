using IdentityService.DTOs;
using IdentityService.Models;

namespace IdentityService.Repositories;

public interface IFeePaymentRepository
{
    Task<FeeInvoicePaymentState?>
        GetInvoiceStateAsync(
            ulong invoiceId);

    Task<RecordFeePaymentResponse>
        RecordAsync(
            ulong invoiceId,
            ulong recordedByUserId,
            string paymentReference,
            RecordFeePaymentRequest request);
}