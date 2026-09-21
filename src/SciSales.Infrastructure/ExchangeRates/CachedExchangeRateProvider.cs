using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SciSales.Application.Abstractions;
using SciSales.Domain.Common;
using SciSales.Domain.Products;

namespace SciSales.Infrastructure.ExchangeRates;

/// <summary>
/// Decorates the real provider with a short in-memory cache. Only successful
/// answers are cached: a failure must be retried, not remembered.
/// </summary>
internal sealed class CachedExchangeRateProvider(
    IExchangeRateProvider inner,
    IMemoryCache cache,
    IOptions<ExchangeRateOptions> options) : IExchangeRateProvider
{
    private readonly TimeSpan _ttl = TimeSpan.FromMinutes(options.Value.CacheMinutes);

    public async Task<Result<ExchangeRate>> GetRateAsync(
        Currency from,
        Currency to,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);

        if (_ttl <= TimeSpan.Zero)
        {
            return await inner.GetRateAsync(from, to, cancellationToken);
        }

        var key = $"rate:{from.Code}:{to.Code}";

        if (cache.TryGetValue(key, out ExchangeRate? cached) && cached is not null)
        {
            return cached;
        }

        var result = await inner.GetRateAsync(from, to, cancellationToken);

        if (result.IsSuccess)
        {
            cache.Set(key, result.Value, _ttl);
        }

        return result;
    }
}
