using IdentityService.DTOs;
using IdentityService.Exceptions;
using IdentityService.Models;
using IdentityService.Repositories;

namespace IdentityService.Services;

public class FeePaymentService
    : IFeePaymentService
{
    private const decimal MaximumAmount =
        9999999999.99m;

    private static readonly HashSet<string>
        SupportedPaymentMethods =
        new(
            StringComparer.OrdinalIgnoreCase)
        {
            "CASH",
            "CARD",
            "BANK_TRANSFER",
            "ONLINE"
        };

    private readonly IFeePaymentRepository
        _repository;

    public FeePaymentService(
        IFeePaymentRepository repository)
    {
        _repository = repository;
    }

    public async Task<RecordFeePaymentResponse>
        RecordAsync(
            ulong invoiceId,
            ulong recordedByUserId,
            RecordFeePaymentRequest request)
    {
        ValidateRequest(
            invoiceId,
            recordedByUserId,
            request);

        FeeInvoicePaymentState? invoice =
            await _repository
                .GetInvoiceStateAsync(
                    invoiceId);

        if (invoice is null)
        {
            throw new KeyNotFoundException(
                "Fee invoice was not found.");
        }

        ValidateInvoice(
            invoice,
            request.Amount!.Value);

        request.PaymentMethod =
            request.PaymentMethod
                .Trim()
                .ToUpperInvariant();

        request.Notes =
            string.IsNullOrWhiteSpace(
                request.Notes)
                ? null
                : request.Notes.Trim();

        string paymentReference =
            GeneratePaymentReference();

        return await _repository.RecordAsync(
            invoiceId,
            recordedByUserId,
            paymentReference,
            request);
    }

    private static void ValidateRequest(
        ulong invoiceId,
        ulong recordedByUserId,
        RecordFeePaymentRequest request)
    {
        if (invoiceId == 0)
        {
            throw new FeePaymentValidationException(
                "Invoice ID is required.");
        }

        if (recordedByUserId == 0)
        {
            throw new FeePaymentValidationException(
                "The authenticated Administrator ID is invalid.");
        }

        if (!request.Amount.HasValue)
        {
            throw new FeePaymentValidationException(
                "Payment amount is required.");
        }

        decimal amount =
            request.Amount.Value;

        if (
            amount <= 0 ||
            amount > MaximumAmount)
        {
            throw new FeePaymentValidationException(
                "Payment amount must be greater than zero and within the supported range.");
        }

        if (
            decimal.Round(
                amount,
                2,
                MidpointRounding.AwayFromZero)
            != amount)
        {
            throw new FeePaymentValidationException(
                "Payment amount must not contain more than two decimal places.");
        }

        if (string.IsNullOrWhiteSpace(
                request.PaymentMethod))
        {
            throw new FeePaymentValidationException(
                "Payment method is required.");
        }

        string paymentMethod =
            request.PaymentMethod.Trim();

        if (!SupportedPaymentMethods.Contains(
                paymentMethod))
        {
            throw new FeePaymentValidationException(
                "Payment method must be CASH, CARD, BANK_TRANSFER or ONLINE.");
        }

        if (
            request.Notes is not null &&
            request.Notes.Trim().Length > 500)
        {
            throw new FeePaymentValidationException(
                "Notes must not exceed 500 characters.");
        }
    }

    private static void ValidateInvoice(
        FeeInvoicePaymentState invoice,
        decimal paymentAmount)
    {
        if (
            invoice.Status.Equals(
                "CANCELLED",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new FeePaymentValidationException(
                "Payments cannot be recorded for a cancelled invoice.");
        }

        if (
            invoice.Status.Equals(
                "PAID",
                StringComparison.OrdinalIgnoreCase) ||
            invoice.PaidAmount >=
                invoice.TotalAmount)
        {
            throw new FeePaymentValidationException(
                "The invoice is already fully paid.");
        }

        decimal outstandingAmount =
            invoice.TotalAmount -
            invoice.PaidAmount;

        if (paymentAmount > outstandingAmount)
        {
            throw new FeePaymentValidationException(
                $"Payment amount cannot exceed the outstanding amount of {outstandingAmount:F2}.");
        }
    }

    private static string
        GeneratePaymentReference()
    {
        string uniquePart =
            Guid.NewGuid()
                .ToString("N")[..8]
                .ToUpperInvariant();

        return
            $"PAY-{DateTime.UtcNow:yyyyMMddHHmmss}-{uniquePart}";
    }
}