using SciSales.Domain.Products;
using Shouldly;

namespace SciSales.Domain.Tests.Products;

public sealed class CurrencyTests
{
    [Theory]
    [InlineData("cop", "COP")]
    [InlineData("  eur  ", "EUR")]
    [InlineData("Usd", "USD")]
    public void Create_normalizes_the_code(string input, string expected)
    {
        var result = Currency.Create(input);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Code.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_a_blank_code(string? code)
    {
        Currency.Create(code).Error.ShouldBe(ProductErrors.CurrencyEmpty);
    }

    [Theory]
    [InlineData("CO")]
    [InlineData("COPS")]
    [InlineData("C0P")]
    [InlineData("CO$")]
    public void Create_rejects_anything_that_is_not_three_letters(string code)
    {
        Currency.Create(code).Error.ShouldBe(ProductErrors.CurrencyMalformed);
    }

    [Fact]
    public void A_code_typed_in_lowercase_equals_the_same_code_in_uppercase()
    {
        Currency.Create("usd").Value.ShouldBe(Currency.Usd);
    }
}
