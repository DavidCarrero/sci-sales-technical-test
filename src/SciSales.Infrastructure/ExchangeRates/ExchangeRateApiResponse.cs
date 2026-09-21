using System.Text.Json.Serialization;

namespace SciSales.Infrastructure.ExchangeRates;

/// <summary>
/// The payload of GET /v6/latest/{base} on open.er-api.com. Kept internal: the
/// third party's field names stop at the edge of this project.
/// </summary>
internal sealed record ExchangeRateApiResponse
{
    [JsonPropertyName("result")]
    public string? Result { get; init; }

    [JsonPropertyName("base_code")]
    public string? BaseCode { get; init; }

    [JsonPropertyName("time_last_update_unix")]
    public long TimeLastUpdateUnix { get; init; }

    [JsonPropertyName("rates")]
    public Dictionary<string, decimal>? Rates { get; init; }
}
