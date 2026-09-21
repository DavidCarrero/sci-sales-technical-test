using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SciSales.Application.Abstractions;
using SciSales.Infrastructure.ExchangeRates;
using SciSales.Infrastructure.Persistence;

namespace SciSales.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Binds configuration and registers the adapters. This is the only place
    /// that knows the ports are backed by SQL Server and an HTTP API.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // ValidateOnStart: a missing connection string fails at boot with a clear
        // message instead of on the first request with a null reference.
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<ExchangeRateOptions>()
            .Bind(configuration.GetSection(ExchangeRateOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<IProductRepository, ProductRepository>();

        services.AddMemoryCache();

        services.AddHttpClient<OpenExchangeRateProvider>(ConfigureExchangeRateClient)
            .AddStandardResilienceHandler();

        // The cache decorates the HTTP provider; the use cases only see the port.
        services.AddScoped<IExchangeRateProvider>(provider => new CachedExchangeRateProvider(
            provider.GetRequiredService<OpenExchangeRateProvider>(),
            provider.GetRequiredService<IMemoryCache>(),
            provider.GetRequiredService<IOptions<ExchangeRateOptions>>()));

        return services;
    }

    private static void ConfigureExchangeRateClient(IServiceProvider provider, HttpClient client)
    {
        var options = provider.GetRequiredService<IOptions<ExchangeRateOptions>>().Value;

        client.BaseAddress = new Uri(options.BaseAddress, UriKind.Absolute);
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    }
}
