using SciSales.Domain.Common;

namespace SciSales.Domain.Products;

/// <summary>An ISO 4217 alphabetic code. Uppercase, exactly three letters.</summary>
public sealed record Currency
{
    /// <summary>The currency the catalog keeps its prices in.</summary>
    public static readonly Currency Usd = new("USD");

    private Currency(string code) => Code = code;

    public string Code { get; }

    public static Result<Currency> Create(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<Currency>(ProductErrors.CurrencyEmpty);
        }

        var normalized = code.Trim().ToUpperInvariant();

        if (normalized.Length != 3 || !normalized.All(char.IsAsciiLetterUpper))
        {
            return Result.Failure<Currency>(ProductErrors.CurrencyMalformed);
        }

        return new Currency(normalized);
    }

    public override string ToString() => Code;
}
