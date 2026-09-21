using SciSales.Domain.Common;
using SciSales.Domain.Products;

namespace SciSales.Application.Abstractions;

/// <summary>
/// The outbound port for the public exchange-rate API. A third party can be
/// slow, rate-limited or simply down, so the contract returns a
/// <see cref="Result{T}"/> instead of pretending it always answers.
/// </summary>
public interface IExchangeRateProvider
{
    Task<Result<ExchangeRate>> GetRateAsync(Currency from, Currency to, CancellationToken cancellationToken);
}
