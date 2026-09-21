using SciSales.Domain.Products;

namespace SciSales.Application.Products;

/// <summary>Input for creating a product. Price is always in the catalog currency (USD).</summary>
public sealed record CreateProductCommand(string? Name, string? Description, decimal Price);

/// <summary>Input for a full update of an existing product.</summary>
public sealed record UpdateProductCommand(int Id, string? Name, string? Description, decimal Price);

/// <summary>A product as the API returns it.</summary>
public sealed record ProductResponse(
    int Id,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    DateTimeOffset CreatedDate)
{
    public static ProductResponse From(Product product) => new(
        product.Id,
        product.Name,
        product.Description,
        product.Price.Amount,
        product.Price.Currency.Code,
        product.CreatedDate);
}

/// <summary>One page of products plus what the caller needs to move between pages.</summary>
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;
}

/// <summary>A product's price converted to another currency, with the rate used.</summary>
public sealed record ProductPriceResponse(
    int ProductId,
    string Name,
    decimal BasePrice,
    string BaseCurrency,
    decimal ConvertedPrice,
    string TargetCurrency,
    decimal Rate,
    DateTimeOffset RateAsOf);
