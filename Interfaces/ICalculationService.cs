using PortfolioBalancerServer.Models;

namespace PortfolioBalancerServer.Interfaces
{
    public interface ICalculationService
    {
        AssetsDiff SplitAssetsByRatio(decimal stocksAmount, decimal bondsAmount, decimal contributionAmount, decimal firstRatio, decimal secondRatio);
    }
}
