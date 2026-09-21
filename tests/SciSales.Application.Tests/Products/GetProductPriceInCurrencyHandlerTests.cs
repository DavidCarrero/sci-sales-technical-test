using NSubstitute;
using SciSales.Application.Abstractions;
using SciSales.Application.Products;
using SciSales.Domain.Common;
using SciSales.Domain.Products;
using Shouldly;

namespace SciSales.Application.Tests.Products;

public sealed class GetProductPriceInCurrencyHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);

    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly IExchangeRateProvider _rates = Substitute.For<IExchangeRateProvider>();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private GetProductPriceInCurrencyHandler Handler => new(_repository, _rates);

    [Fact]
    public async Task Converts_the_catalog_price_with_the_published_rate()
    {
        GivenProduct(id: 1, price: 100m);
        GivenRate("COP", 3_950.25m);

        var result = await Handler.HandleAsync(1, "COP", Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.BasePrice.ShouldBe(100m);
        result.Value.BaseCurrency.ShouldBe("USD");
        result.Value.ConvertedPrice.ShouldBe(395_025.00m);
        result.Value.TargetCurrency.ShouldBe("COP");
        result.Value.Rate.ShouldBe(3_950.25m);
    }

    [Fact]
    public async Task Answers_a_request_for_the_catalog_currency_without_calling_the_provider()
    {
        GivenProduct(id: 1, price: 100m);

        var result = await Handler.HandleAsync(1, "usd", Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ConvertedPrice.ShouldBe(100m);
        result.Value.Rate.ShouldBe(1m);
        await _rates.DidNotReceiveWithAnyArgs().GetRateAsync(Arg.Any<Currency>(), Arg.Any<Currency>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_a_malformed_currency_before_reading_the_product()
    {
        var result = await Handler.HandleAsync(1, "PESOS", Ct);

        result.Error.ShouldBe(ProductErrors.CurrencyMalformed);
        await _repository.DidNotReceiveWithAnyArgs().GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reports_not_found_when_the_product_does_not_exist()
    {
        _repository.GetByIdAsync(404, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await Handler.HandleAsync(404, "COP", Ct);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Passes_the_providers_failure_through_instead_of_inventing_a_rate()
    {
        GivenProduct(id: 1, price: 100m);
        _rates.GetRateAsync(Arg.Any<Currency>(), Arg.Any<Currency>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ExchangeRate>(ExchangeRateErrors.Unavailable));

        var result = await Handler.HandleAsync(1, "COP", Ct);

        result.Error.ShouldBe(ExchangeRateErrors.Unavailable);
        result.Error.Type.ShouldBe(ErrorType.Unavailable);
    }

    private void GivenProduct(int id, decimal price) =>
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Product.FromStorage(id, "Monitor", "QHD panel.", price, Now));

    private void GivenRate(string target, decimal rate)
    {
        var currency = Currency.Create(target).Value;

        _rates.GetRateAsync(Currency.Usd, currency, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ExchangeRate(Currency.Usd, currency, rate, Now)));
    }
}
