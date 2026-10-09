using System.ComponentModel.DataAnnotations;

namespace NoticeService.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Field)]
public sealed class ExpiryDateNotInPastAttribute : ValidationAttribute
{
    public ExpiryDateNotInPastAttribute()
        : base("Expiry date cannot be in the past.") { }

    protected override ValidationResult? IsValid(
        object? value, ValidationContext validationContext)
    {
        if (value is not DateOnly expiry)
        {
            return ValidationResult.Success;
        }

        var timeProvider =
            validationContext.GetService(typeof(TimeProvider)) as TimeProvider
            ?? TimeProvider.System;

        DateOnly today =
            DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        // Today is allowed: the archive job only archives expiry < today.
        return expiry >= today
            ? ValidationResult.Success
            : new ValidationResult(
                ErrorMessage,
                new[] { validationContext.MemberName ?? "ExpiryDate" });
    }
}