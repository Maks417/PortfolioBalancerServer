using PortfolioBalancerServer.Models;

namespace PortfolioBalancerServer.Interfaces;

public interface ICurrencyConverter
{
    Task<ConversionResult> ConvertAsync(
        IEnumerable<Asset> stocks,
        IEnumerable<Asset> bonds,
        Asset contribution,
        CancellationToken cancellationToken = default);

    Task<RatesResponse> GetRatesResponseAsync(CancellationToken cancellationToken = default);

    Task<bool> AreRatesAvailableAsync(CancellationToken cancellationToken = default);
}
