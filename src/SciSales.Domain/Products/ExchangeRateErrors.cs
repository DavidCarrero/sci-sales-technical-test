using SciSales.Domain.Common;

namespace SciSales.Domain.Products;

public static class ExchangeRateErrors
{
    public static Error Unsupported(Currency currency) => Error.Validation(
        "exchange_rate.unsupported_currency",
        $"The rate provider does not publish a rate for {currency.Code}.");

    public static readonly Error Unavailable = Error.Unavailable(
        "exchange_rate.unavailable",
        "The exchange rate provider did not answer. Try again in a moment.");
}
