using PortfolioBalancerServer.Models;

namespace PortfolioBalancerServer.Interfaces;

public interface IRateProvider
{
    Task<RateSnapshot> GetRatesAsync(CancellationToken cancellationToken = default);
}
