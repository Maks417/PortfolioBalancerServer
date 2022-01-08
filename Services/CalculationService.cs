using PortfolioBalancerServer.Interfaces;
using PortfolioBalancerServer.Models;

namespace PortfolioBalancerServer.Services
{
    public class CalculationService : ICalculationService
    {
        public AssetsDiff SplitAssetsByRatio(decimal stocksAmount, decimal bondsAmount, decimal contributionAmount, decimal firstRatio, decimal secondRatio)
        {
            if (secondRatio == decimal.Zero)
            {
                return new AssetsDiff
                {
                    StocksDiff = contributionAmount,
                    BondsDiff = decimal.Zero
                };
            }

            var (totalStocksAmount, totalBondsAmount) = GetNewTotalAmount(stocksAmount, bondsAmount, contributionAmount, firstRatio, secondRatio);

            var stocksDiff = totalStocksAmount - stocksAmount;
            var bondsDiff = totalBondsAmount - bondsAmount;

            return new AssetsDiff
            {
                StocksDiff = RoundByContributionAmount(stocksDiff, contributionAmount),
                BondsDiff = RoundByContributionAmount(bondsDiff, contributionAmount)
            };
        }

        private static decimal RoundByContributionAmount(decimal value, decimal contributionAmount)
        {
            if (value > decimal.Zero && value > contributionAmount)
            {
                return contributionAmount;

            }
            else if (value > decimal.Zero)
            {
                return value;
            }

            return decimal.Zero;
        }

        private static (decimal, decimal) GetNewTotalAmount(decimal stocksAmount, decimal bondsAmount, decimal contributionAmount, decimal firstRatio, decimal secondRatio)
        {
            var total = stocksAmount + bondsAmount + contributionAmount;
            return (total * firstRatio, total * secondRatio);
        }
    }
}
