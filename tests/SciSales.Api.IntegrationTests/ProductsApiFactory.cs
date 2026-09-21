using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SciSales.Application.Abstractions;
using SciSales.Domain.Common;
using SciSales.Domain.Products;

namespace SciSales.Api.IntegrationTests;

/// <summary>
/// Boots the real pipeline against the container database. The exchange-rate port
/// is the one thing replaced: a test must not depend on a third party being up,
/// and the provider's own behaviour is covered by its unit tests.
/// </summary>
internal sealed class ProductsApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    public const decimal StubRate = 3_950.25m;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = connectionString,
            }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IExchangeRateProvider>();
            services.AddScoped<IExchangeRateProvider, StubExchangeRateProvider>();
        });
    }

    private sealed class StubExchangeRateProvider : IExchangeRateProvider
    {
        public Task<Result<ExchangeRate>> GetRateAsync(Currency from, Currency to, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success(new ExchangeRate(from, to, StubRate, DateTimeOffset.UnixEpoch)));
    }
}
