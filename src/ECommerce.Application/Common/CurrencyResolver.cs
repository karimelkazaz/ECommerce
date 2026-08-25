using System.Diagnostics.CodeAnalysis;
using ECommerce.Domain.Common;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Application.Common;

public static class CurrencyResolver
{
    public static bool TryResolve(
        string? code,
        [NotNullWhen(false)] out Error? error,
        [NotNullWhen(true)] out Currency? currency)
    {
        currency = null;
        error = null;

        var normalized = code?.Trim().ToUpperInvariant();
        var match = Currency.All.FirstOrDefault(c => c.Code == normalized);

        if (match is not null)
        {
            currency = match;
            return true;
        }

        error = Error.Validation(
            "Money.InvalidCurrency",
            $"'{code}' is not a supported currency. Supported currencies: {string.Join(", ", Currency.All.Select(c => c.Code))}.");

        return false;
    }
}
