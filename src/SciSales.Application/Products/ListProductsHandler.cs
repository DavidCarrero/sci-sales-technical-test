using SciSales.Application.Abstractions;

namespace SciSales.Application.Products;

public sealed class ListProductsHandler(IProductRepository repository)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public async Task<PagedResponse<ProductResponse>> HandleAsync(
        int page = 1,
        int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        // Clamped rather than rejected: a bad page number is not worth a 400 on a read.
        var safePage = Math.Max(page, 1);
        var safePageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var skip = (safePage - 1) * safePageSize;

        var total = await repository.CountAsync(cancellationToken);
        var products = await repository.ListAsync(skip, safePageSize, cancellationToken);

        return new PagedResponse<ProductResponse>(
            [.. products.Select(ProductResponse.From)],
            safePage,
            safePageSize,
            total);
    }
}
