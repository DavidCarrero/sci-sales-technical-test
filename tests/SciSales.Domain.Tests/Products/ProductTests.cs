using SciSales.Domain.Products;
using Shouldly;

namespace SciSales.Domain.Tests.Products;

public sealed class ProductTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_keeps_the_values_it_was_given()
    {
        var result = Product.Create("Wireless Mouse", "Six-button ergonomic mouse.", 24.99m, Now);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Wireless Mouse");
        result.Value.Description.ShouldBe("Six-button ergonomic mouse.");
        result.Value.Price.Amount.ShouldBe(24.99m);
        result.Value.Price.Currency.ShouldBe(Currency.Usd);
        result.Value.CreatedDate.ShouldBe(Now);
    }

    [Fact]
    public void Create_leaves_the_identity_at_zero_until_the_database_assigns_one()
    {
        var product = Product.Create("Dock", "USB-C dock.", 149.90m, Now).Value;

        product.Id.ShouldBe(0);
    }

    [Fact]
    public void Create_trims_the_name_and_the_description()
    {
        var product = Product.Create("  Monitor  ", "  QHD panel.  ", 329m, Now).Value;

        product.Name.ShouldBe("Monitor");
        product.Description.ShouldBe("QHD panel.");
    }

    [Fact]
    public void Create_accepts_a_missing_description_as_empty()
    {
        var product = Product.Create("Cable", description: null, 9.99m, Now).Value;

        product.Description.ShouldBe(string.Empty);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_a_blank_name(string? name)
    {
        var result = Product.Create(name, "Anything.", 10m, Now);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ProductErrors.NameEmpty);
    }

    [Fact]
    public void Create_rejects_a_name_longer_than_the_column()
    {
        var result = Product.Create(new string('a', Product.NameMaxLength + 1), "Anything.", 10m, Now);

        result.Error.ShouldBe(ProductErrors.NameTooLong);
    }

    [Fact]
    public void Create_accepts_a_name_exactly_at_the_limit()
    {
        var result = Product.Create(new string('a', Product.NameMaxLength), "Anything.", 10m, Now);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Create_rejects_a_description_longer_than_the_column()
    {
        var result = Product.Create("Name", new string('a', Product.DescriptionMaxLength + 1), 10m, Now);

        result.Error.ShouldBe(ProductErrors.DescriptionTooLong);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-1000)]
    public void Create_rejects_a_price_that_is_not_positive(decimal price)
    {
        var result = Product.Create("Name", "Description.", price, Now);

        result.Error.ShouldBe(ProductErrors.PriceNotPositive);
    }

    [Fact]
    public void Create_rejects_a_price_with_more_decimals_than_the_column_stores()
    {
        var result = Product.Create("Name", "Description.", 19.999m, Now);

        result.Error.ShouldBe(ProductErrors.PriceTooManyDecimals);
    }

    [Fact]
    public void Update_replaces_every_field_except_the_creation_date()
    {
        var product = Product.Create("Old", "Old description.", 10m, Now).Value;

        var result = product.Update("New", "New description.", 20m);

        result.IsSuccess.ShouldBeTrue();
        product.Name.ShouldBe("New");
        product.Description.ShouldBe("New description.");
        product.Price.Amount.ShouldBe(20m);
        product.CreatedDate.ShouldBe(Now);
    }

    [Fact]
    public void Update_leaves_the_product_untouched_when_the_new_values_are_invalid()
    {
        var product = Product.Create("Old", "Old description.", 10m, Now).Value;

        var result = product.Update("New", "New description.", price: 0m);

        result.IsFailure.ShouldBeTrue();
        product.Name.ShouldBe("Old");
        product.Price.Amount.ShouldBe(10m);
    }

    [Fact]
    public void AssignIdentity_sets_the_id_once()
    {
        var product = Product.Create("Name", "Description.", 10m, Now).Value;

        product.AssignIdentity(42);

        product.Id.ShouldBe(42);
    }

    [Fact]
    public void AssignIdentity_refuses_to_overwrite_an_identity_that_is_already_set()
    {
        var product = Product.Create("Name", "Description.", 10m, Now).Value;
        product.AssignIdentity(42);

        Should.Throw<InvalidOperationException>(() => product.AssignIdentity(43));
    }

    [Fact]
    public void FromStorage_rebuilds_a_product_with_its_identity_and_catalog_currency()
    {
        var product = Product.FromStorage(7, "Stored", "From the database.", 55.25m, Now);

        product.Id.ShouldBe(7);
        product.Price.Amount.ShouldBe(55.25m);
        product.Price.Currency.ShouldBe(Currency.Usd);
    }
}
