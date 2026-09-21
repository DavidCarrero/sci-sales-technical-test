namespace SciSales.Domain.Products;

/// <summary>
/// How much of <paramref name="To"/> one unit of <paramref name="From"/> buys,
/// and when the provider last refreshed that number.
/// </summary>
public sealed record ExchangeRate(Currency From, Currency To, decimal Rate, DateTimeOffset AsOf);
