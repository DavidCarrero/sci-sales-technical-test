using SciSales.Api.Extensions;
using SciSales.Application.Products;

namespace SciSales.Api.Endpoints;

internal static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var products = app.MapGroup("/api/products")
            .WithTags("Products");

        products.MapPost("/", CreateAsync)
            .WithSummary("Creates a product")
            .WithDescription("Price is expressed in USD, the catalog currency.")
            .Produces<ProductResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        products.MapGet("/", ListAsync)
            .WithSummary("Lists products, newest first")
            .Produces<PagedResponse<ProductResponse>>();

        products.MapGet("/{id:int}", GetByIdAsync)
            .WithSummary("Gets one product")
            .Produces<ProductResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        products.MapPut("/{id:int}", UpdateAsync)
            .WithSummary("Replaces a product")
            .Produces<ProductResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        products.MapDelete("/{id:int}", DeleteAsync)
            .WithSummary("Deletes a product")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        products.MapGet("/{id:int}/price", GetPriceAsync)
            .WithSummary("Prices a product in another currency")
            .WithDescription(
                "Converts the USD price using the live rate published by open.er-api.com. " +
                "Example: /api/products/1/price?currency=COP")
            .Produces<ProductPriceResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return app;
    }

    private static async Task<IResult> CreateAsync(
        CreateProductCommand command,
        CreateProductHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/products/{result.Value.Id}", result.Value)
            : result.ToProblem();
    }

    private static async Task<IResult> ListAsync(
        ListProductsHandler handler,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = ListProductsHandler.DefaultPageSize)
    {
        var response = await handler.HandleAsync(page, pageSize, cancellationToken);

        return Results.Ok(response);
    }

    private static async Task<IResult> GetByIdAsync(
        int id,
        GetProductByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<IResult> UpdateAsync(
        int id,
        UpdateProductBody body,
        UpdateProductHandler handler,
        CancellationToken cancellationToken)
    {
        // The id comes from the route, never from the body: that is the only
        // version a caller cannot use to touch a record that is not the one in the URL.
        var command = new UpdateProductCommand(id, body.Name, body.Description, body.Price);

        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<IResult> DeleteAsync(
        int id,
        DeleteProductHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }

    private static async Task<IResult> GetPriceAsync(
        int id,
        string currency,
        GetProductPriceInCurrencyHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, currency, cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    /// <summary>The PUT body. It has no Id on purpose; the route owns that.</summary>
    internal sealed record UpdateProductBody(string? Name, string? Description, decimal Price);
}
