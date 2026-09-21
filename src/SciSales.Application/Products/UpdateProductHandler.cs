using SciSales.Application.Abstractions;
using SciSales.Domain.Common;
using SciSales.Domain.Products;

namespace SciSales.Application.Products;

public sealed class UpdateProductHandler(IProductRepository repository)
{
    public async Task<Result<ProductResponse>> HandleAsync(
        UpdateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var product = await repository.GetByIdAsync(command.Id, cancellationToken);

        if (product is null)
        {
            return Result.Failure<ProductResponse>(ProductErrors.NotFound(command.Id));
        }

        // The entity decides whether the new values are acceptable, not the handler.
        var updated = product.Update(command.Name, command.Description, command.Price);

        if (updated.IsFailure)
        {
            return Result.Failure<ProductResponse>(updated.Error);
        }

        var persisted = await repository.UpdateAsync(product, cancellationToken);

        return persisted.IsFailure
            ? Result.Failure<ProductResponse>(persisted.Error)
            : ProductResponse.From(product);
    }
}
