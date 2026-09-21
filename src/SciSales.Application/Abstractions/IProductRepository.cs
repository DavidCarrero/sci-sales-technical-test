using SciSales.Domain.Common;
using SciSales.Domain.Products;

namespace SciSales.Application.Abstractions;

/// <summary>
/// What the use cases need from storage, written in product language. The fact
/// that the other side is SQL Server and stored procedures lives in
/// Infrastructure and never leaks through this interface.
/// </summary>
public interface IProductRepository
{
    /// <summary>Persists a new product and returns its identity value.</summary>
    Task<Result<int>> CreateAsync(Product product, CancellationToken cancellationToken);

    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Product>> ListAsync(int skip, int take, CancellationToken cancellationToken);

    Task<int> CountAsync(CancellationToken cancellationToken);

    Task<Result> UpdateAsync(Product product, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
}
