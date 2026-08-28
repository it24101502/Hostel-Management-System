using IdentityService.DTOs;
using IdentityService.Exceptions;
using IdentityService.Models;
using IdentityService.Repositories;
using IdentityService.Services;

namespace IdentityService.Tests;

public class FeePaymentServiceTests
{
    [Fact]
    public async Task
        PartialPayment_KeepsInvoiceUnpaid()
    {
        var repository =
            CreateRepository();

        var service =
            new FeePaymentService(repository);

        RecordFeePaymentResponse result =
            await service.RecordAsync(
                invoiceId: 1,
                recordedByUserId: 2,
                CreateRequest(10000m));

        Assert.Equal(
            10000m,
            result.PaidAmount);

        Assert.Equal(
            15000m,
            result.OutstandingAmount);

        Assert.Equal(
            "UNPAID",
            result.InvoiceStatus);

        Assert.Equal(
            "COMPLETED",
            result.PaymentStatus);
    }

    [Fact]
    public async Task
        FullPayment_UpdatesInvoiceToPaid()
    {
        var repository =
            CreateRepository();

        var service =
            new FeePaymentService(repository);

        RecordFeePaymentResponse result =
            await service.RecordAsync(
                invoiceId: 1,
                recordedByUserId: 2,
                CreateRequest(25000m));

        Assert.Equal(
            25000m,
            result.PaidAmount);

        Assert.Equal(
            0m,
            result.OutstandingAmount);

        Assert.Equal(
            "PAID",
            result.InvoiceStatus);
    }

    [Fact]
    public void
        NoPayment_KeepsInvoiceUnpaid()
    {
        DateOnly today =
            DateOnly.FromDateTime(
                DateTime.UtcNow);

        string status =
            FeeInvoiceStatusCalculator.Determine(
                totalAmount: 25000m,
                paidAmount: 0m,
                dueDate: today.AddDays(30),
                today: today);

        Assert.Equal(
            "UNPAID",
            status);
    }

    [Fact]
    public async Task
        PartialPaymentAfterDueDate_IsOverdue()
    {
        var repository =
            CreateRepository();

        repository.InvoiceState!.DueDate =
            DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(-1));

        var service =
            new FeePaymentService(repository);

        RecordFeePaymentResponse result =
            await service.RecordAsync(
                invoiceId: 1,
                recordedByUserId: 2,
                CreateRequest(10000m));

        Assert.Equal(
            "OVERDUE",
            result.InvoiceStatus);

        Assert.Equal(
            15000m,
            result.OutstandingAmount);
    }

    [Fact]
    public async Task
        MissingPaymentAmount_IsRejected()
    {
        var service =
            new FeePaymentService(
                CreateRepository());

        RecordFeePaymentRequest request =
            CreateRequest(1000m);

        request.Amount = null;

        FeePaymentValidationException exception =
            await Assert.ThrowsAsync<
                FeePaymentValidationException>(
                () => service.RecordAsync(
                    1,
                    2,
                    request));

        Assert.Contains(
            "amount is required",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task
        NegativePaymentAmount_IsRejected()
    {
        var service =
            new FeePaymentService(
                CreateRepository());

        await Assert.ThrowsAsync<
            FeePaymentValidationException>(
            () => service.RecordAsync(
                1,
                2,
                CreateRequest(-100m)));
    }

    [Fact]
    public async Task
        InvalidPaymentMethod_IsRejected()
    {
        var service =
            new FeePaymentService(
                CreateRepository());

        RecordFeePaymentRequest request =
            CreateRequest(1000m);

        request.PaymentMethod = "CHEQUE";

        FeePaymentValidationException exception =
            await Assert.ThrowsAsync<
                FeePaymentValidationException>(
                () => service.RecordAsync(
                    1,
                    2,
                    request));

        Assert.Contains(
            "payment method",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task
        PaymentAboveOutstandingBalance_IsRejected()
    {
        var service =
            new FeePaymentService(
                CreateRepository());

        FeePaymentValidationException exception =
            await Assert.ThrowsAsync<
                FeePaymentValidationException>(
                () => service.RecordAsync(
                    1,
                    2,
                    CreateRequest(25000.01m)));

        Assert.Contains(
            "outstanding amount",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task
        PaymentForPaidInvoice_IsRejected()
    {
        var repository =
            CreateRepository();

        repository.InvoiceState!.PaidAmount =
            25000m;

        repository.InvoiceState.Status =
            "PAID";

        var service =
            new FeePaymentService(repository);

        FeePaymentValidationException exception =
            await Assert.ThrowsAsync<
                FeePaymentValidationException>(
                () => service.RecordAsync(
                    1,
                    2,
                    CreateRequest(100m)));

        Assert.Contains(
            "already fully paid",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task
        PaymentForMissingInvoice_ReturnsNotFound()
    {
        var repository =
            CreateRepository();

        repository.InvoiceState = null;

        var service =
            new FeePaymentService(repository);

        KeyNotFoundException exception =
            await Assert.ThrowsAsync<
                KeyNotFoundException>(
                () => service.RecordAsync(
                    999,
                    2,
                    CreateRequest(1000m)));

        Assert.Contains(
            "invoice was not found",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    private static RecordFeePaymentRequest
        CreateRequest(decimal amount)
    {
        return new RecordFeePaymentRequest
        {
            Amount = amount,
            PaymentMethod = "bank_transfer",
            Notes = "Payment recorded in test"
        };
    }

    private static FakeFeePaymentRepository
        CreateRepository()
    {
        return new FakeFeePaymentRepository
        {
            InvoiceState =
                new FeeInvoicePaymentState
                {
                    InvoiceId = 1,
                    TotalAmount = 25000m,
                    PaidAmount = 0m,

                    DueDate =
                        DateOnly.FromDateTime(
                            DateTime.UtcNow
                                .AddDays(30)),

                    Status = "UNPAID"
                }
        };
    }

    private sealed class
        FakeFeePaymentRepository
        : IFeePaymentRepository
    {
        public FeeInvoicePaymentState?
            InvoiceState
        {
            get;
            set;
        }

        public string? LastPaymentReference
        {
            get;
            private set;
        }

        public ulong LastRecordedByUserId
        {
            get;
            private set;
        }

        public Task<FeeInvoicePaymentState?>
            GetInvoiceStateAsync(
                ulong invoiceId)
        {
            return Task.FromResult(
                InvoiceState);
        }

        public Task<RecordFeePaymentResponse>
            RecordAsync(
                ulong invoiceId,
                ulong recordedByUserId,
                string paymentReference,
                RecordFeePaymentRequest request)
        {
            if (InvoiceState is null)
            {
                throw new KeyNotFoundException(
                    "Fee invoice was not found.");
            }

            LastPaymentReference =
                paymentReference;

            LastRecordedByUserId =
                recordedByUserId;

            decimal newPaidAmount =
                InvoiceState.PaidAmount +
                request.Amount!.Value;

            DateOnly today =
                DateOnly.FromDateTime(
                    DateTime.UtcNow);

            string newStatus =
                FeeInvoiceStatusCalculator
                    .Determine(
                        InvoiceState.TotalAmount,
                        newPaidAmount,
                        InvoiceState.DueDate,
                        today);

            InvoiceState.PaidAmount =
                newPaidAmount;

            InvoiceState.Status =
                newStatus;

            return Task.FromResult(
                new RecordFeePaymentResponse
                {
                    PaymentId = 501,

                    InvoiceId = invoiceId,

                    PaymentReference =
                        paymentReference,

                    PaymentAmount =
                        request.Amount.Value,

                    PaymentMethod =
                        request.PaymentMethod,

                    PaymentStatus =
                        "COMPLETED",

                    PaidAt =
                        DateTime.UtcNow,

                    TotalAmount =
                        InvoiceState.TotalAmount,

                    PaidAmount =
                        newPaidAmount,

                    OutstandingAmount =
                        InvoiceState.TotalAmount -
                        newPaidAmount,

                    InvoiceStatus =
                        newStatus
                });
        }
    }
}