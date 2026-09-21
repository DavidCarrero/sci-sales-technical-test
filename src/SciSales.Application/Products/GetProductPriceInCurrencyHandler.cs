using SciSales.Application.Abstractions;
using SciSales.Domain.Common;
using SciSales.Domain.Products;

namespace SciSales.Application.Products;

/// <summary>
/// The real use case behind the API consumption requirement: the catalog keeps
/// prices in USD, and a buyer wants to see what a product costs in their own
/// currency at today's rate.
/// </summary>
public sealed class GetProductPriceInCurrencyHandler(
    IProductRepository repository,
    IExchangeRateProvider exchangeRates)
{
    public async Task<Result<ProductPriceResponse>> HandleAsync(
        int productId,
        string? targetCurrencyCode,
        CancellationToken cancellationToken = default)
    {
        var currency = Currency.Create(targetCurrencyCode);

        if (currency.IsFailure)
        {
            return Result.Failure<ProductPriceResponse>(currency.Error);
        }

        var product = await repository.GetByIdAsync(productId, cancellationToken);

        if (product is null)
        {
            return Result.Failure<ProductPriceResponse>(ProductErrors.NotFound(productId));
        }

        // Asking for USD on a USD catalog is answered without touching the third party.
        if (currency.Value == product.Price.Currency)
        {
            return Build(product, product.Price, rate: 1m, asOf: DateTimeOffset.UtcNow);
        }

        var rate = await exchangeRates.GetRateAsync(product.Price.Currency, currency.Value, cancellationToken);

        if (rate.IsFailure)
        {
            return Result.Failure<ProductPriceResponse>(rate.Error);
        }

        var converted = product.Price.ConvertTo(currency.Value, rate.Value.Rate);

        return Build(product, converted, rate.Value.Rate, rate.Value.AsOf);
    }

    private static ProductPriceResponse Build(Product product, Money converted, decimal rate, DateTimeOffset asOf) =>
        new(
            product.Id,
            product.Name,
            product.Price.Amount,
            product.Price.Currency.Code,
            converted.Amount,
            converted.Currency.Code,
            rate,
            asOf);
}
