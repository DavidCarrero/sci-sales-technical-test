using SciSales.Domain.Products;

namespace SciSales.Infrastructure.Persistence;

/// <summary>
/// The shape the stored procedures return. It exists so Dapper has something
/// flat to fill and the domain entity keeps its private setters.
/// </summary>
internal sealed class ProductRow
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public decimal Price { get; init; }

    /// <summary>DATETIME2 carries no offset; the column always holds UTC.</summary>
    public DateTime CreatedDate { get; init; }

    public Product ToDomain() => Product.FromStorage(
        Id,
        Name,
        Description,
        Price,
        new DateTimeOffset(DateTime.SpecifyKind(CreatedDate, DateTimeKind.Utc)));
}
