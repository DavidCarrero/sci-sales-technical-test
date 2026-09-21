using SciSales.Domain.Products;
using Shouldly;

namespace SciSales.Domain.Tests.Products;

public sealed class MoneyTests
{
    [Fact]
    public void Two_amounts_in_the_same_currency_are_equal()
    {
        var one = Money.Create(10.50m, Currency.Usd).Value;
        var other = Money.Create(10.50m, Currency.Usd).Value;

        one.ShouldBe(other);
    }

    [Fact]
    public void The_same_amount_in_a_different_currency_is_not_the_same_money()
    {
        var usd = Money.Create(10.50m, Currency.Usd).Value;
        var cop = Money.Create(10.50m, Currency.Create("COP").Value).Value;

        usd.ShouldNotBe(cop);
    }

    [Fact]
    public void ConvertTo_applies_the_rate_and_switches_the_currency()
    {
        var usd = Money.Create(100m, Currency.Usd).Value;
        var cop = Currency.Create("COP").Value;

        var converted = usd.ConvertTo(cop, 3_950.25m);

        converted.Amount.ShouldBe(395_025.00m);
        converted.Currency.ShouldBe(cop);
    }

    [Fact]
    public void ConvertTo_rounds_to_two_decimals()
    {
        var usd = Money.Create(24.99m, Currency.Usd).Value;
        var cop = Currency.Create("COP").Value;

        var converted = usd.ConvertTo(cop, 3.3333m);

        converted.Amount.ShouldBe(83.30m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ConvertTo_refuses_a_rate_that_is_not_positive(decimal rate)
    {
        var usd = Money.Create(10m, Currency.Usd).Value;
        var cop = Currency.Create("COP").Value;

        Should.Throw<ArgumentOutOfRangeException>(() => usd.ConvertTo(cop, rate));
    }

    [Fact]
    public void ToString_shows_the_amount_with_its_currency()
    {
        var usd = Money.Create(7m, Currency.Usd).Value;

        usd.ToString().ShouldBe("7.00 USD");
    }
}
