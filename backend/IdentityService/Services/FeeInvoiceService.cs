using IdentityService.DTOs;
using IdentityService.Exceptions;
using IdentityService.Models;
using IdentityService.Repositories;

namespace IdentityService.Services;

public class FeeInvoiceService
    : IFeeInvoiceService
{
    private const decimal MaximumAmount =
        9999999999.99m;

    private readonly IFeeInvoiceRepository
        _repository;

    public FeeInvoiceService(
        IFeeInvoiceRepository repository)
    {
        _repository = repository;
    }

    public async Task<FeeInvoiceResponse>
        CreateAsync(
            CreateFeeInvoiceRequest request)
    {
        ValidateRequest(request);

        ulong studentProfileId =
            request.StudentProfileId!.Value;

        bool profileExists =
            await _repository
                .StudentProfileExistsAsync(
                    studentProfileId);

        if (!profileExists)
        {
            throw new KeyNotFoundException(
                "Student profile was not found.");
        }

        request.FeeType =
            request.FeeType
                .Trim()
                .ToUpperInvariant();

        request.Description =
            string.IsNullOrWhiteSpace(
                request.Description)
                ? null
                : request.Description.Trim();

        string invoiceNumber =
            GenerateInvoiceNumber();

        ulong invoiceId =
            await _repository.CreateAsync(
                invoiceNumber,
                request);

        FeeInvoice? createdInvoice =
            await _repository.GetByIdAsync(
                invoiceId);

        if (createdInvoice is null)
        {
            throw new InvalidOperationException(
                "The invoice was created but could not be retrieved.");
        }

        return MapResponse(createdInvoice);
    }

    private static void ValidateRequest(
        CreateFeeInvoiceRequest request)
    {
        if (
            !request.StudentProfileId.HasValue ||
            request.StudentProfileId.Value == 0)
        {
            throw new FeeInvoiceValidationException(
                "Student profile ID is required.");
        }

        if (string.IsNullOrWhiteSpace(
                request.FeeType))
        {
            throw new FeeInvoiceValidationException(
                "Fee type is required.");
        }

        string feeType =
            request.FeeType.Trim();

        if (
            feeType.Length < 2 ||
            feeType.Length > 100)
        {
            throw new FeeInvoiceValidationException(
                "Fee type must contain between 2 and 100 characters.");
        }

        if (
            request.Description is not null &&
            request.Description.Trim().Length > 500)
        {
            throw new FeeInvoiceValidationException(
                "Description must not exceed 500 characters.");
        }

        if (!request.Amount.HasValue)
        {
            throw new FeeInvoiceValidationException(
                "Amount is required.");
        }

        decimal amount =
            request.Amount.Value;

        if (
            amount <= 0 ||
            amount > MaximumAmount)
        {
            throw new FeeInvoiceValidationException(
                "Amount must be greater than zero and within the supported range.");
        }

        if (
            decimal.Round(
                amount,
                2,
                MidpointRounding.AwayFromZero)
            != amount)
        {
            throw new FeeInvoiceValidationException(
                "Amount must not contain more than two decimal places.");
        }

        if (!request.DueDate.HasValue)
        {
            throw new FeeInvoiceValidationException(
                "Due date is required.");
        }

        DateOnly today =
            DateOnly.FromDateTime(
                DateTime.UtcNow);

        if (request.DueDate.Value < today)
        {
            throw new FeeInvoiceValidationException(
                "Due date cannot be in the past.");
        }
    }

    private static string GenerateInvoiceNumber()
    {
        string uniquePart =
            Guid.NewGuid()
                .ToString("N")[..8]
                .ToUpperInvariant();

        return
            $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}-{uniquePart}";
    }

    private static FeeInvoiceResponse MapResponse(
        FeeInvoice invoice)
    {
        return new FeeInvoiceResponse
        {
            InvoiceId =
                invoice.InvoiceId,

            StudentProfileId =
                invoice.StudentProfileId,

            InvoiceNumber =
                invoice.InvoiceNumber,

            FeeType =
                invoice.FeeType,

            Description =
                invoice.Description,

            TotalAmount =
                invoice.TotalAmount,

            PaidAmount =
                invoice.PaidAmount,

            IssuedAt =
                invoice.IssuedAt,

            DueDate =
                invoice.DueDate,

            Status =
                invoice.Status,

            CreatedAt =
                invoice.CreatedAt,

            UpdatedAt =
                invoice.UpdatedAt
        };
    }
}