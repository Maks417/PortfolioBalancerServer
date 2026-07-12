using PortfolioBalancerServer.Models;

namespace PortfolioBalancerServer.Interfaces;

public interface ICalculationService
{
    AssetsDiff SplitAssetsByRatio(
        decimal stocksAmount,
        decimal bondsAmount,
        decimal cashAmount,
        decimal contributionAmount,
        decimal[] targetRatios,
        string mode = "contribution",
        decimal? driftThreshold = null);
}
