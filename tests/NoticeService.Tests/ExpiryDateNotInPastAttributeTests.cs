using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using NoticeService.Tests.TestDoubles;
using NoticeService.Validation;

namespace NoticeService.Tests;

public class ExpiryDateNotInPastAttributeTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 10, 9, 0, 0, TimeSpan.Zero);

    private static ValidationResult? Validate(DateOnly value)
    {
        var services = new ServiceCollection()
            .AddSingleton<TimeProvider>(new FixedTimeProvider(Now))
            .BuildServiceProvider();

        var context = new ValidationContext(new object(), services, null)
        {
            MemberName = "ExpiryDate"
        };

        return new ExpiryDateNotInPastAttribute()
            .GetValidationResult(value, context);
    }

    [Fact]
    public void Yesterday_IsRejected() =>
        Assert.NotNull(Validate(new DateOnly(2026, 10, 9)));

    [Fact]
    public void Today_IsAccepted() =>
        Assert.Equal(ValidationResult.Success, Validate(new DateOnly(2026, 10, 10)));

    [Fact]
    public void Tomorrow_IsAccepted() =>
        Assert.Equal(ValidationResult.Success, Validate(new DateOnly(2026, 10, 11)));

    [Fact]
    public void DefaultDate_IsRejected() =>
        Assert.NotNull(Validate(default));
}