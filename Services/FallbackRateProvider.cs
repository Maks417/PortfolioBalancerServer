using Microsoft.Extensions.Options;
using PortfolioBalancerServer.Interfaces;
using PortfolioBalancerServer.Models;
using PortfolioBalancerServer.Options;

namespace PortfolioBalancerServer.Services;

public sealed class FallbackRateProvider : IRateProvider
{
    private readonly IRateProvider _primary;
    private readonly FallbackRateOptions _options;

    public FallbackRateProvider(IRateProvider primary, IOptions<FallbackRateOptions> options)
    {
        _primary = primary;
        _options = options.Value;
    }

    public async Task<RateSnapshot> GetRatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primary.GetRatesAsync(cancellationToken);
        }
        catch (ExchangeRatesUnavailableException)
        {
            return new RateSnapshot(
                new Currency { Nominal = 1, Value = _options.UsdPerRub * 1m },
                new Currency { Nominal = 1, Value = _options.EurPerRub * 1m },
                DateTime.UtcNow,
                false,
                true,
                "fallback");
        }
    }
}
