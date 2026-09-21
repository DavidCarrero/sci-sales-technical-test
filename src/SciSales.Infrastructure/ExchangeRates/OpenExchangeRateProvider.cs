using System.Globalization;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SciSales.Application.Abstractions;
using SciSales.Domain.Common;
using SciSales.Domain.Products;

namespace SciSales.Infrastructure.ExchangeRates;

/// <summary>
/// Talks to https://open.er-api.com. A public API is the part of the system most
/// likely to be slow or down, so every failure mode ends as a Result, never as an
/// exception crossing into the use case.
/// </summary>
internal sealed class OpenExchangeRateProvider(
    HttpClient httpClient,
    ILogger<OpenExchangeRateProvider> logger) : IExchangeRateProvider
{
    public async Task<Result<ExchangeRate>> GetRateAsync(
        Currency from,
        Currency to,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);

        var path = string.Create(CultureInfo.InvariantCulture, $"v6/latest/{from.Code}");

        try
        {
            var response = await httpClient.GetAsync(path, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                InfrastructureLog.RateProviderHttpError(logger, (int)response.StatusCode, from.Code);

                return Result.Failure<ExchangeRate>(ExchangeRateErrors.Unavailable);
            }

            var payload = await response.Content.ReadFromJsonAsync<ExchangeRateApiResponse>(cancellationToken);

            if (payload is null || !string.Equals(payload.Result, "success", StringComparison.OrdinalIgnoreCase))
            {
                InfrastructureLog.RateProviderRejected(logger, payload?.Result);

                return Result.Failure<ExchangeRate>(ExchangeRateErrors.Unavailable);
            }

            if (payload.Rates is null || !payload.Rates.TryGetValue(to.Code, out var rate) || rate <= 0m)
            {
                return Result.Failure<ExchangeRate>(ExchangeRateErrors.Unsupported(to));
            }

            return new ExchangeRate(
                from,
                to,
                rate,
                DateTimeOffset.FromUnixTimeSeconds(payload.TimeLastUpdateUnix));
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException
                                          && !cancellationToken.IsCancellationRequested)
        {
            // Network fault or timeout: logged in full here, generic to the caller.
            InfrastructureLog.RateProviderUnreachable(logger, exception);

            return Result.Failure<ExchangeRate>(ExchangeRateErrors.Unavailable);
        }
    }
}
