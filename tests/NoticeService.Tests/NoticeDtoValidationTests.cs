using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NoticeService.DTOs;
using NoticeService.Tests.TestDoubles;
using Xunit;

namespace NoticeService.Tests;

/// <summary>
/// Validation rules on CreateNoticeRequest and UpdateNoticeRequest (HMS-60).
/// The attributes sit on the positional record parameters, so each test
/// runs every validation attribute against the matching constructor
/// argument, the same way ASP.NET Core model binding does.
/// </summary>
public class NoticeDtoValidationTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 10, 9, 0, 0, TimeSpan.Zero);

    private static readonly DateOnly Today = new(2026, 10, 10);

    public static IEnumerable<object[]> RequestTypes()
    {
        yield return new object[] { typeof(CreateNoticeRequest) };
        yield return new object[] { typeof(UpdateNoticeRequest) };
    }

    /// <summary>
    /// Builds the record and returns the names of the parameters whose
    /// validation attributes fail.
    /// </summary>
    private static List<string> FailedMembers(
        Type recordType,
        string title = "Water maintenance",
        string content = "Water is off from 2 PM.",
        string noticeType = "NOTICE",
        ulong? hostelBlockId = null,
        DateOnly? expiryDate = null)
    {
        object?[] arguments =
        {
            title,
            content,
            noticeType,
            hostelBlockId,
            expiryDate ?? Today.AddDays(3)
        };

        ConstructorInfo constructor = recordType.GetConstructors().Single();
        object instance = constructor.Invoke(arguments);
        ParameterInfo[] parameters = constructor.GetParameters();

        using ServiceProvider services = new ServiceCollection()
            .AddSingleton<TimeProvider>(new FixedTimeProvider(Now))
            .BuildServiceProvider();

        var failed = new List<string>();

        for (int index = 0; index < parameters.Length; index++)
        {
            var context = new ValidationContext(instance, services, null)
            {
                MemberName = parameters[index].Name
            };

            foreach (var attribute in
                     parameters[index].GetCustomAttributes<ValidationAttribute>())
            {
                if (attribute.GetValidationResult(arguments[index], context)
                    != ValidationResult.Success)
                {
                    failed.Add(parameters[index].Name!);
                }
            }
        }

        return failed;
    }

    // ---------- valid input ----------

    [Theory]
    [MemberData(nameof(RequestTypes))]
    public void ValidNotice_PassesEveryRule(Type recordType)
    {
        Assert.Empty(FailedMembers(recordType));
    }

    [Theory]
    [MemberData(nameof(RequestTypes))]
    public void ValidSchedule_ForASpecificBlock_PassesEveryRule(Type recordType)
    {
        Assert.Empty(
            FailedMembers(
                recordType,
                noticeType: "SCHEDULE",
                hostelBlockId: 7));
    }

    // ---------- title ----------

    [Theory]
    [MemberData(nameof(RequestTypes))]
    public void EmptyTitle_IsRejected(Type recordType)
    {
        Assert.Contains("Title", FailedMembers(recordType, title: ""));
    }

    [Theory]
    [MemberData(nameof(RequestTypes))]
    public void TitleAtMaximumLength_IsAccepted(Type recordType)
    {
        Assert.Empty(FailedMembers(recordType, title: new string('a', 200)));
    }

    [Theory]
    [MemberData(nameof(RequestTypes))]
    public void TitleOverMaximumLength_IsRejected(Type recordType)
    {
        Assert.Contains(
            "Title",
            FailedMembers(recordType, title: new string('a', 201)));
    }

    // ---------- content ----------

    [Theory]
    [MemberData(nameof(RequestTypes))]
    public void EmptyContent_IsRejected(Type recordType)
    {
        Assert.Contains("Content", FailedMembers(recordType, content: ""));
    }

    [Theory]
    [MemberData(nameof(RequestTypes))]
    public void ContentAtMaximumLength_IsAccepted(Type recordType)
    {
        Assert.Empty(
            FailedMembers(recordType, content: new string('a', 4000)));
    }

    [Theory]
    [MemberData(nameof(RequestTypes))]
    public void ContentOverMaximumLength_IsRejected(Type recordType)
    {
        Assert.Contains(
            "Content",
            FailedMembers(recordType, content: new string('a', 4001)));
    }

    // ---------- notice type ----------

    [Theory]
    [InlineData(typeof(CreateNoticeRequest), "NOTICE")]
    [InlineData(typeof(CreateNoticeRequest), "SCHEDULE")]
    [InlineData(typeof(UpdateNoticeRequest), "NOTICE")]
    [InlineData(typeof(UpdateNoticeRequest), "SCHEDULE")]
    public void AllowedNoticeTypes_AreAccepted(Type recordType, string noticeType)
    {
        Assert.Empty(FailedMembers(recordType, noticeType: noticeType));
    }

    [Theory]
    [InlineData(typeof(CreateNoticeRequest), "ANNOUNCEMENT")]
    [InlineData(typeof(CreateNoticeRequest), "")]
    [InlineData(typeof(CreateNoticeRequest), "notice")]
    [InlineData(typeof(CreateNoticeRequest), "NOTICE ")]
    [InlineData(typeof(UpdateNoticeRequest), "ANNOUNCEMENT")]
    [InlineData(typeof(UpdateNoticeRequest), "")]
    [InlineData(typeof(UpdateNoticeRequest), "schedule")]
    [InlineData(typeof(UpdateNoticeRequest), "SCHEDULE ")]
    public void UnknownOrMisspeltNoticeTypes_AreRejected(
        Type recordType,
        string noticeType)
    {
        Assert.Contains(
            "NoticeType",
            FailedMembers(recordType, noticeType: noticeType));
    }

    // ---------- expiry date ----------

    [Theory]
    [MemberData(nameof(RequestTypes))]
    public void ExpiryYesterday_IsRejected(Type recordType)
    {
        Assert.Contains(
            "ExpiryDate",
            FailedMembers(recordType, expiryDate: Today.AddDays(-1)));
    }

    [Theory]
    [MemberData(nameof(RequestTypes))]
    public void ExpiryToday_IsAccepted(Type recordType)
    {
        Assert.Empty(FailedMembers(recordType, expiryDate: Today));
    }

    [Theory]
    [MemberData(nameof(RequestTypes))]
    public void ExpiryFarInTheFuture_IsAccepted(Type recordType)
    {
        Assert.Empty(
            FailedMembers(recordType, expiryDate: Today.AddYears(2)));
    }

    // ---------- several problems at once ----------

    [Theory]
    [MemberData(nameof(RequestTypes))]
    public void SeveralInvalidFields_AreAllReported(Type recordType)
    {
        List<string> failed = FailedMembers(
            recordType,
            title: "",
            content: "",
            noticeType: "BAD",
            expiryDate: Today.AddDays(-5));

        Assert.Contains("Title", failed);
        Assert.Contains("Content", failed);
        Assert.Contains("NoticeType", failed);
        Assert.Contains("ExpiryDate", failed);
    }

    // ---------- the attribute itself ----------

    [Fact]
    public void ExpiryDateAttribute_UsesTheErrorMessageTheFrontendExpects()
    {
        var attribute = new NoticeService.Validation.ExpiryDateNotInPastAttribute();

        using ServiceProvider services = new ServiceCollection()
            .AddSingleton<TimeProvider>(new FixedTimeProvider(Now))
            .BuildServiceProvider();

        var context = new ValidationContext(new object(), services, null)
        {
            MemberName = "ExpiryDate"
        };

        ValidationResult? result =
            attribute.GetValidationResult(Today.AddDays(-1), context);

        Assert.NotNull(result);
        Assert.Equal("Expiry date cannot be in the past.", result.ErrorMessage);
        Assert.Contains("ExpiryDate", result.MemberNames);
    }

    [Fact]
    public void ExpiryDateAttribute_IgnoresValuesThatAreNotDates()
    {
        var attribute = new NoticeService.Validation.ExpiryDateNotInPastAttribute();

        var context = new ValidationContext(new object());

        Assert.Equal(
            ValidationResult.Success,
            attribute.GetValidationResult("not a date", context));
    }

    [Fact]
    public void ExpiryDateAttribute_WithoutATimeProviderService_UsesTheSystemClock()
    {
        var attribute = new NoticeService.Validation.ExpiryDateNotInPastAttribute();

        var context = new ValidationContext(new object());

        DateOnly farFuture = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(5));
        DateOnly longAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5));

        Assert.Equal(
            ValidationResult.Success,
            attribute.GetValidationResult(farFuture, context));

        Assert.NotEqual(
            ValidationResult.Success,
            attribute.GetValidationResult(longAgo, context));
    }
}
