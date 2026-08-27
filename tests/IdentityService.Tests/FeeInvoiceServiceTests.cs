using IdentityService.DTOs;
using IdentityService.Exceptions;
using IdentityService.Models;
using IdentityService.Repositories;
using IdentityService.Services;

namespace IdentityService.Tests;

public class FeeInvoiceServiceTests
{
    [Fact]
    public async Task
        Create_WithValidAmountAndDueDate_CreatesInvoice()
    {
        var repository =
            new FakeFeeInvoiceRepository();

        var service =
            new FeeInvoiceService(repository);

        CreateFeeInvoiceRequest request =
            CreateValidRequest();

        FeeInvoiceResponse result =
            await service.CreateAsync(request);

        Assert.Equal((ulong)101, result.InvoiceId);
        Assert.Equal(
            (ulong)10,
            result.StudentProfileId);

        Assert.Equal(
            25000.00m,
            result.TotalAmount);

        Assert.Equal(
            0.00m,
            result.PaidAmount);

        Assert.Equal(
            "PENDING",
            result.Status);

        Assert.Equal(
            "HOSTEL_FEE",
            result.FeeType);

        Assert.StartsWith(
            "INV-",
            result.InvoiceNumber);

        Assert.NotNull(
            repository.LastInvoiceNumber);
    }

    [Fact]
    public async Task
        Create_WithMissingAmount_ThrowsClearException()
    {
        var service =
            CreateService();

        CreateFeeInvoiceRequest request =
            CreateValidRequest();

        request.Amount = null;

        FeeInvoiceValidationException exception =
            await Assert.ThrowsAsync<
                FeeInvoiceValidationException>(
                () => service.CreateAsync(request));

        Assert.Contains(
            "Amount is required",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task
        Create_WithZeroAmount_ThrowsException()
    {
        var service =
            CreateService();

        CreateFeeInvoiceRequest request =
            CreateValidRequest();

        request.Amount = 0;

        await Assert.ThrowsAsync<
            FeeInvoiceValidationException>(
            () => service.CreateAsync(request));
    }

    [Fact]
    public async Task
        Create_WithNegativeAmount_ThrowsException()
    {
        var service =
            CreateService();

        CreateFeeInvoiceRequest request =
            CreateValidRequest();

        request.Amount = -500;

        await Assert.ThrowsAsync<
            FeeInvoiceValidationException>(
            () => service.CreateAsync(request));
    }

    [Fact]
    public async Task
        Create_WithMoreThanTwoDecimalPlaces_ThrowsException()
    {
        var service =
            CreateService();

        CreateFeeInvoiceRequest request =
            CreateValidRequest();

        request.Amount = 1000.999m;

        FeeInvoiceValidationException exception =
            await Assert.ThrowsAsync<
                FeeInvoiceValidationException>(
                () => service.CreateAsync(request));

        Assert.Contains(
            "two decimal places",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task
        Create_WithMissingDueDate_ThrowsClearException()
    {
        var service =
            CreateService();

        CreateFeeInvoiceRequest request =
            CreateValidRequest();

        request.DueDate = null;

        FeeInvoiceValidationException exception =
            await Assert.ThrowsAsync<
                FeeInvoiceValidationException>(
                () => service.CreateAsync(request));

        Assert.Contains(
            "Due date is required",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task
        Create_WithPastDueDate_ThrowsClearException()
    {
        var service =
            CreateService();

        CreateFeeInvoiceRequest request =
            CreateValidRequest();

        request.DueDate =
            DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(-1));

        FeeInvoiceValidationException exception =
            await Assert.ThrowsAsync<
                FeeInvoiceValidationException>(
                () => service.CreateAsync(request));

        Assert.Contains(
            "past",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task
        Create_WithUnknownStudentProfile_ThrowsNotFound()
    {
        var repository =
            new FakeFeeInvoiceRepository
            {
                StudentProfileExists = false
            };

        var service =
            new FeeInvoiceService(repository);

        KeyNotFoundException exception =
            await Assert.ThrowsAsync<
                KeyNotFoundException>(
                () => service.CreateAsync(
                    CreateValidRequest()));

        Assert.Contains(
            "Student profile was not found",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    private static FeeInvoiceService
        CreateService()
    {
        return new FeeInvoiceService(
            new FakeFeeInvoiceRepository());
    }

    private static CreateFeeInvoiceRequest
        CreateValidRequest()
    {
        return new CreateFeeInvoiceRequest
        {
            StudentProfileId = 10,
            FeeType = "hostel_fee",
            Description =
                "Hostel fee for Semester 1",
            Amount = 25000.00m,
            DueDate =
                DateOnly.FromDateTime(
                    DateTime.UtcNow.AddDays(30))
        };
    }

    private sealed class
        FakeFeeInvoiceRepository
        : IFeeInvoiceRepository
    {
        public bool StudentProfileExists
        {
            get;
            set;
        } = true;

        public string? LastInvoiceNumber
        {
            get;
            private set;
        }

        public FeeInvoice? CreatedInvoice
        {
            get;
            private set;
        }

        public Task<bool>
            StudentProfileExistsAsync(
                ulong studentProfileId)
        {
            return Task.FromResult(
                StudentProfileExists);
        }

        public Task<ulong> CreateAsync(
            string invoiceNumber,
            CreateFeeInvoiceRequest request)
        {
            LastInvoiceNumber =
                invoiceNumber;

            DateTime now =
                DateTime.UtcNow;

            CreatedInvoice =
                new FeeInvoice
                {
                    InvoiceId = 101,

                    StudentProfileId =
                        request
                            .StudentProfileId!
                            .Value,

                    InvoiceNumber =
                        invoiceNumber,

                    FeeType =
                        request.FeeType,

                    Description =
                        request.Description,

                    TotalAmount =
                        request.Amount!.Value,

                    PaidAmount = 0.00m,

                    IssuedAt = now,

                    DueDate =
                        request.DueDate!.Value,

                    Status = "PENDING",

                    CreatedAt = now,

                    UpdatedAt = now
                };

            return Task.FromResult(
                (ulong)101);
        }

        public Task<FeeInvoice?>
            GetByIdAsync(
                ulong invoiceId)
        {
            return Task.FromResult(
                CreatedInvoice);
        }
    }
}