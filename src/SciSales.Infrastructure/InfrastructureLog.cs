using Microsoft.Extensions.Logging;

namespace SciSales.Infrastructure;

/// <summary>
/// Log messages as source-generated delegates. Nothing is formatted unless the
/// level is enabled, and every message lives in one file where it can be read
/// end to end.
/// </summary>
internal static partial class InfrastructureLog
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Create rejected: a product named {ProductName} already exists.")]
    public static partial void DuplicateProductName(ILogger logger, string productName);

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Warning,
        Message = "Exchange rate provider answered HTTP {StatusCode} for base {BaseCurrency}.")]
    public static partial void RateProviderHttpError(ILogger logger, int statusCode, string baseCurrency);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Warning,
        Message = "Exchange rate provider answered with result '{ProviderResult}'.")]
    public static partial void RateProviderRejected(ILogger logger, string? providerResult);

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Warning,
        Message = "Exchange rate provider is unreachable.")]
    public static partial void RateProviderUnreachable(ILogger logger, Exception exception);
}
