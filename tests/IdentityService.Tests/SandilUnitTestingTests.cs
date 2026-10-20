using System.Globalization;
using IdentityService.DTOs;
using IdentityService.Exceptions;
using IdentityService.Models;
using IdentityService.Repositories;
using IdentityService.Services;
using NSubstitute;

namespace IdentityService.Tests;

public class SandilUnitTestingTests
{
    [Theory]
    [InlineData(25000, 25000, -1, "PAID")]
    [InlineData(25000, 26000, -1, "PAID")]
    [InlineData(25000, 10000, -1, "OVERDUE")]
    [InlineData(25000, 0, -1, "OVERDUE")]
    [InlineData(25000, 0, 1, "UNPAID")]
    [InlineData(25000, 10000, 0, "UNPAID")]
    [InlineData(25000, 0, 0, "UNPAID")]
    public void InvoiceStatus_UsesPaymentAndDueDateBoundaries(
        int totalAmount,
        int paidAmount,
        int dueDateOffset,
        string expectedStatus)
    {
        var today = new DateOnly(2026, 10, 17);

        string status = FeeInvoiceStatusCalculator.Determine(
            totalAmount,
            paidAmount,
            today.AddDays(dueDateOffset),
            today);

        Assert.Equal(expectedStatus, status);
    }

    [Theory]
    [InlineData("0", "greater than zero")]
    [InlineData("-500", "greater than zero")]
    [InlineData("1000.999", "two decimal places")]
    [InlineData("10000000000.00", "supported range")]
    public async Task CreateInvoice_RejectsInvalidAmountBeforeRepositoryAccess(
        string amountText,
        string expectedMessage)
    {
        var repository = Substitute.For<IFeeInvoiceRepository>();
        var service = new FeeInvoiceService(repository);
        var request = new CreateFeeInvoiceRequest
        {
            StudentProfileId = 10,
            FeeType = "hostel_fee",
            Amount = decimal.Parse(amountText, CultureInfo.InvariantCulture),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30)
        };

        var exception = await Assert.ThrowsAsync<FeeInvoiceValidationException>(
            () => service.CreateAsync(request));

        Assert.Contains(expectedMessage, exception.Message,
            StringComparison.OrdinalIgnoreCase);
        await repository.DidNotReceive()
            .StudentProfileExistsAsync(Arg.Any<ulong>());
        await repository.DidNotReceive()
            .CreateAsync(Arg.Any<string>(), Arg.Any<CreateFeeInvoiceRequest>());
    }

    [Fact]
    public async Task FifthFailedLogin_LocksAccountForConfiguredDuration()
    {
        const string correctPassword = "CorrectPassword@123";
        var repository = Substitute.For<IUserRepository>();
        repository.FindByIdentifierAsync("STUDENT@EXAMPLE.COM")
            .Returns(CreateLoginUser(correctPassword, failedAttempts: 4));
        var service = new AuthService(repository);
        var request = new LoginRequest
        {
            Identifier = " student@example.com ",
            Password = "WrongPassword@123"
        };
        DateTime earliestLockoutEnd = DateTime.UtcNow.AddMinutes(15);

        LoginResponse? result = await service.LoginAsync(request);

        DateTime latestLockoutEnd = DateTime.UtcNow.AddMinutes(15);
        Assert.Null(result);
        await repository.Received(1).RecordFailedLoginAsync(
            1, 5, true,
            Arg.Is<DateTime?>(value => value.HasValue &&
                value.Value >= earliestLockoutEnd &&
                value.Value <= latestLockoutEnd));
        await repository.DidNotReceive()
            .RecordSuccessfulLoginAsync(Arg.Any<ulong>());
    }

    [Fact]
    public async Task CorrectPassword_IsRejectedWhileAccountIsLocked()
    {
        const string correctPassword = "CorrectPassword@123";
        var repository = Substitute.For<IUserRepository>();
        var lockedUser = CreateLoginUser(correctPassword, failedAttempts: 5);
        lockedUser.IsLocked = true;
        lockedUser.LockoutEndAt = DateTime.UtcNow.AddMinutes(10);
        repository.FindByIdentifierAsync("STUDENT@EXAMPLE.COM")
            .Returns(lockedUser);
        var service = new AuthService(repository);

        await Assert.ThrowsAsync<AccountLockedException>(() => service.LoginAsync(
            new LoginRequest
            {
                Identifier = "student@example.com",
                Password = correctPassword
            }));

        await repository.DidNotReceive()
            .RecordSuccessfulLoginAsync(Arg.Any<ulong>());
        await repository.DidNotReceive().RecordFailedLoginAsync(
            Arg.Any<ulong>(), Arg.Any<uint>(), Arg.Any<bool>(),
            Arg.Any<DateTime?>());
    }

    [Theory]
    [InlineData("cash", "CASH")]
    [InlineData(" Card ", "CARD")]
    [InlineData("bank_transfer", "BANK_TRANSFER")]
    [InlineData("OnLiNe", "ONLINE")]
    public async Task PaymentMethod_IsNormalizedBeforeRecording(
        string paymentMethod,
        string expectedMethod)
    {
        var repository = Substitute.For<IFeePaymentRepository>();
        repository.GetInvoiceStateAsync(1).Returns(new FeeInvoicePaymentState
        {
            InvoiceId = 1,
            TotalAmount = 25000m,
            PaidAmount = 0m,
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30),
            Status = "UNPAID"
        });
        repository.RecordAsync(1, 2, Arg.Any<string>(),
                Arg.Any<RecordFeePaymentRequest>())
            .Returns(new RecordFeePaymentResponse { PaymentId = 501 });
        var service = new FeePaymentService(repository);
        var request = new RecordFeePaymentRequest
        {
            Amount = 100m,
            PaymentMethod = paymentMethod,
            Notes = "  receipt checked  "
        };

        RecordFeePaymentResponse result = await service.RecordAsync(1, 2, request);

        Assert.Equal((ulong)501, result.PaymentId);
        Assert.Equal(expectedMethod, request.PaymentMethod);
        Assert.Equal("receipt checked", request.Notes);
        await repository.Received(1).RecordAsync(
            1, 2,
            Arg.Is<string>(reference => reference.StartsWith("PAY-")),
            Arg.Is<RecordFeePaymentRequest>(recorded =>
                recorded.Amount == 100m &&
                recorded.PaymentMethod == expectedMethod &&
                recorded.Notes == "receipt checked"));
    }

    [Theory]
    [InlineData("CHEQUE")]
    [InlineData("wire")]
    [InlineData("")]
    public async Task UnsupportedPaymentMethod_IsRejectedBeforeRepositoryAccess(
        string paymentMethod)
    {
        var repository = Substitute.For<IFeePaymentRepository>();
        var service = new FeePaymentService(repository);

        await Assert.ThrowsAsync<FeePaymentValidationException>(() =>
            service.RecordAsync(1, 2, new RecordFeePaymentRequest
            {
                Amount = 100m,
                PaymentMethod = paymentMethod
            }));

        await repository.DidNotReceive()
            .GetInvoiceStateAsync(Arg.Any<ulong>());
    }

    [Theory]
    [InlineData(" guardian ", "GUARDIAN")]
    [InlineData("eMeRgEnCy", "EMERGENCY")]
    public async Task GuardianContact_NormalizesTypeBeforeCreate(
        string contactType,
        string expectedType)
    {
        var repository = Substitute.For<IGuardianContactRepository>();
        repository.StudentProfileExistsAsync(7).Returns(true);
        repository.CreateAsync(7, Arg.Any<CreateGuardianContactRequest>())
            .Returns((ulong)12);
        repository.GetByIdAsync(7, 12).Returns(new GuardianContact
        {
            GuardianContactId = 12,
            StudentProfileId = 7,
            ContactType = expectedType,
            FullName = "Kamal Perera"
        });
        var service = new GuardianContactService(repository);
        var request = new CreateGuardianContactRequest
        {
            ContactType = contactType,
            FullName = "Kamal Perera",
            Relationship = "Father",
            PhoneNumber = "0771234567"
        };

        GuardianContactResponse result = await service.CreateAsync(7, request);

        Assert.Equal((ulong)12, result.GuardianContactId);
        Assert.Equal(expectedType, request.ContactType);
        await repository.Received(1).CreateAsync(7,
            Arg.Is<CreateGuardianContactRequest>(created =>
                created.ContactType == expectedType));
        await repository.Received(1).GetByIdAsync(7, 12);
    }

    private static LoginUser CreateLoginUser(
        string password,
        uint failedAttempts)
    {
        return new LoginUser
        {
            UserId = 1,
            Username = "student01",
            Email = "student@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FailedLoginAttempts = failedAttempts,
            IsActive = true,
            IsRoleActive = true,
            RoleName = "STUDENT"
        };
    }
}
