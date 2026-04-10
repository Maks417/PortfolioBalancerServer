using PortfolioBalancerServer.Models;

namespace PortfolioBalancerServer.Interfaces
{
    public interface ICurrencyConverter
    {
        Task<(decimal stocksAmount, decimal bondsAmount, decimal contributionAmount)> Convert(IEnumerable<Asset> stocks, IEnumerable<Asset> bonds, Asset contributionAmount);
    }
}
