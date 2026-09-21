using SciSales.Application.Abstractions;
using SciSales.Domain.Common;
using SciSales.Domain.Products;

namespace SciSales.Application.Products;

public sealed class GetProductByIdHandler(IProductRepository repository)
{
    public async Task<Result<ProductResponse>> HandleAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await repository.GetByIdAsync(id, cancellationToken);

        return product is null
            ? Result.Failure<ProductResponse>(ProductErrors.NotFound(id))
            : ProductResponse.From(product);
    }
}
