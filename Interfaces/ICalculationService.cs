using PortfolioBalancerServer.Models;

namespace PortfolioBalancerServer.Interfaces
{
    public interface ICalculationService
    {
        (decimal, decimal) ParseRatio(string ratio);

        AssetsDiff SplitAssetsByRatio(decimal stocksAmount, decimal bondsAmount, decimal contributionAmount, decimal firstRatio, decimal secondRatio);
    }
}
