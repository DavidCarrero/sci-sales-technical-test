using SciSales.Domain.Common;

namespace SciSales.Domain.Products;

/// <summary>
/// Every way a product operation can fail, named once so the API, the tests and
/// the logs all speak about the same thing.
/// </summary>
public static class ProductErrors
{
    public static readonly Error NameEmpty =
        Error.Validation("product.name.empty", "Name is required.");

    public static readonly Error NameTooLong =
        Error.Validation("product.name.too_long", $"Name cannot exceed {Product.NameMaxLength} characters.");

    public static readonly Error DescriptionTooLong =
        Error.Validation("product.description.too_long", $"Description cannot exceed {Product.DescriptionMaxLength} characters.");

    public static readonly Error PriceNotPositive =
        Error.Validation("product.price.not_positive", "Price must be greater than zero.");

    public static readonly Error PriceTooManyDecimals =
        Error.Validation("product.price.too_many_decimals", $"Price cannot have more than {Money.MaxDecimals} decimal places.");

    public static readonly Error PriceOutOfRange =
        Error.Validation("product.price.out_of_range", "Price is larger than the catalog allows.");

    public static readonly Error CurrencyEmpty =
        Error.Validation("currency.empty", "Currency is required.");

    public static readonly Error CurrencyMalformed =
        Error.Validation("currency.malformed", "Currency must be a three-letter ISO 4217 code, for example COP.");

    public static readonly Error DuplicateName =
        Error.Conflict("product.name.duplicate", "Another product already uses that name.");

    public static Error NotFound(int id) =>
        Error.NotFound("product.not_found", $"Product {id} does not exist.");
}
