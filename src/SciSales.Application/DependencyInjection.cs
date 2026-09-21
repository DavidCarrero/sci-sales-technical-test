using Microsoft.Extensions.DependencyInjection;
using SciSales.Application.Products;

namespace SciSales.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the use cases. They are plain classes, so there is no mediator
    /// to configure and the call stack from endpoint to repository stays readable.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<CreateProductHandler>();
        services.AddScoped<GetProductByIdHandler>();
        services.AddScoped<ListProductsHandler>();
        services.AddScoped<UpdateProductHandler>();
        services.AddScoped<DeleteProductHandler>();
        services.AddScoped<GetProductPriceInCurrencyHandler>();

        return services;
    }
}
