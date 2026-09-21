using System.ComponentModel.DataAnnotations;

namespace SciSales.Infrastructure.ExchangeRates;

public sealed class ExchangeRateOptions
{
    public const string SectionName = "ExchangeRates";

    /// <summary>
    /// Base address of the public API. Default is the open endpoint of
    /// exchangerate-api.com, which needs no key and publishes COP among others.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [Url]
    public string BaseAddress { get; init; } = "https://open.er-api.com/";

    [Range(1, 60)]
    public int TimeoutSeconds { get; init; } = 10;

    /// <summary>
    /// The provider refreshes once a day, so caching for minutes costs nothing in
    /// accuracy and keeps the catalog responsive when several products are priced
    /// in a row.
    /// </summary>
    [Range(0, 1440)]
    public int CacheMinutes { get; init; } = 10;
}
