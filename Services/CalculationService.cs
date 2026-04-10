using PortfolioBalancerServer.Domain;
using PortfolioBalancerServer.Extensions;
using PortfolioBalancerServer.Interfaces;
using PortfolioBalancerServer.Models;

namespace PortfolioBalancerServer.Services
{
    public class CalculationService : ICalculationService
    {
        public (decimal, decimal) ParseRatio(string ratio)
        {
            return RatioParser.TryParse(ratio, out var first, out var second)
                ? (first, second)
                : (decimal.Zero, decimal.Zero);
        }

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
                StocksDiff = RoundByContributionAmount(stocksDiff, contributionAmount).RoundTwoSigns(),
                BondsDiff = RoundByContributionAmount(bondsDiff, contributionAmount).RoundTwoSigns()
            };
        }

        private static decimal RoundByContributionAmount(decimal value, decimal contributionAmount)
        {
            if (value > decimal.Zero && value > contributionAmount)
            {
                return contributionAmount;

            }

            return value > decimal.Zero 
                ? value 
                : decimal.Zero;
        }

        private static (decimal, decimal) GetNewTotalAmount(decimal stocksAmount, decimal bondsAmount, decimal contributionAmount, decimal firstRatio, decimal secondRatio)
        {
            var total = stocksAmount + bondsAmount + contributionAmount;
            return (total * firstRatio, total * secondRatio);
        }
    }
}
