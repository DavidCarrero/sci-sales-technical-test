using System.Globalization;
using SciSales.Domain.Common;

namespace SciSales.Domain.Products;

/// <summary>
/// An amount with its currency. Two amounts are the same money only when both
/// the number and the currency match, which is what keeps a price in USD from
/// being silently compared against one in COP.
/// </summary>
public sealed record Money
{
    public const int MaxDecimals = 2;

    private Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public Currency Currency { get; }

    public static Result<Money> Create(decimal amount, Currency currency)
    {
        if (amount <= 0m)
        {
            return Result.Failure<Money>(ProductErrors.PriceNotPositive);
        }

        if (decimal.Round(amount, MaxDecimals) != amount)
        {
            return Result.Failure<Money>(ProductErrors.PriceTooManyDecimals);
        }

        // decimal(18,2) in SQL Server: 16 integer digits at most.
        if (amount >= 10_000_000_000_000_000m)
        {
            return Result.Failure<Money>(ProductErrors.PriceOutOfRange);
        }

        return new Money(amount, currency);
    }

    /// <summary>Rebuilds money already validated by the database. Not for user input.</summary>
    public static Money FromStorage(decimal amount, Currency currency) => new(amount, currency);

    public Money ConvertTo(Currency target, decimal rate)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rate);

        return new Money(decimal.Round(Amount * rate, MaxDecimals, MidpointRounding.ToEven), target);
    }

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Amount:0.00} {Currency.Code}");
}
