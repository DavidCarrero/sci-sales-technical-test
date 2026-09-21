using SciSales.Application.Abstractions;
using SciSales.Domain.Common;
using SciSales.Domain.Products;

namespace SciSales.Application.Products;

public sealed class CreateProductHandler(IProductRepository repository, TimeProvider clock)
{
    public async Task<Result<ProductResponse>> HandleAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var product = Product.Create(
            command.Name,
            command.Description,
            command.Price,
            clock.GetUtcNow());

        if (product.IsFailure)
        {
            return Result.Failure<ProductResponse>(product.Error);
        }

        var created = await repository.CreateAsync(product.Value, cancellationToken);

        if (created.IsFailure)
        {
            return Result.Failure<ProductResponse>(created.Error);
        }

        product.Value.AssignIdentity(created.Value);

        return ProductResponse.From(product.Value);
    }
}
