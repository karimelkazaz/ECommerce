using FluentValidation;
using Microsoft.Extensions.Options;

namespace ECommerce.Application.Common.Options;

public sealed class CacheEntryPolicyValidator
    : AbstractValidator<CacheEntryPolicy>, IValidateOptions<CacheEntryPolicy>
{
    public CacheEntryPolicyValidator()
    {
        RuleFor(x => x.AbsoluteExpirationDays)
            .GreaterThan(0)
            .WithMessage("Absolute expiration must be greater than 0 days.");

        RuleFor(x => x.SlidingExpirationDays)
            .GreaterThan(0)
            .WithMessage("Sliding expiration must be greater than 0 days.");

        RuleFor(x => x.LocalCacheExpirationMinutes)
            .GreaterThan(0)
            .WithMessage("Local cache expiration must be greater than 0 minutes.");

        RuleFor(x => x.SlidingRefreshMinutes)
            .GreaterThan(0)
            .WithMessage("Sliding refresh interval must be greater than 0 minutes.");

        RuleFor(x => x.SlidingExpirationDays)
            .LessThanOrEqualTo(x => x.AbsoluteExpirationDays)
            .WithMessage(
                "Sliding expiration cannot be greater than absolute expiration.");
    }

    public ValidateOptionsResult Validate(
        string? name,
        CacheEntryPolicy options)
    {
        if (options is null)
            return ValidateOptionsResult.Fail("Options cannot be null.");

        var result = base.Validate(options);

        if (result.IsValid)
            return ValidateOptionsResult.Success;

        return ValidateOptionsResult.Fail(
            result.Errors.Select(e => e.ErrorMessage));
    }
}