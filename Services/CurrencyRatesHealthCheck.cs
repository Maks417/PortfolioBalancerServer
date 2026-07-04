using Microsoft.Extensions.Diagnostics.HealthChecks;
using PortfolioBalancerServer.Interfaces;

namespace PortfolioBalancerServer.Services;

public sealed class CurrencyRatesHealthCheck : IHealthCheck
{
    private readonly ICurrencyConverter _currencyConverter;

    public CurrencyRatesHealthCheck(ICurrencyConverter currencyConverter)
    {
        _currencyConverter = currencyConverter;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var available = await _currencyConverter.AreRatesAvailableAsync(cancellationToken);
        return available
            ? HealthCheckResult.Healthy("Exchange rates are available.")
            : HealthCheckResult.Unhealthy("Exchange rates are unavailable.");
    }
}
