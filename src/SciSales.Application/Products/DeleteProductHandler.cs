using SciSales.Application.Abstractions;
using SciSales.Domain.Common;

namespace SciSales.Application.Products;

public sealed class DeleteProductHandler(IProductRepository repository)
{
    public Task<Result> HandleAsync(int id, CancellationToken cancellationToken = default) =>
        repository.DeleteAsync(id, cancellationToken);
}
