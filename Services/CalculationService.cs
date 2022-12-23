using PortfolioBalancerServer.Extensions;
using PortfolioBalancerServer.Interfaces;
using PortfolioBalancerServer.Models;

namespace PortfolioBalancerServer.Services
{
    public class CalculationService : ICalculationService
    {
        public (decimal, decimal) ParseRatio(string ratio)
        {
            if (string.IsNullOrEmpty(ratio)
                || ratio.Length > 5
                || (!ratio.Equals("100", StringComparison.OrdinalIgnoreCase) && !ratio.Contains('/', StringComparison.OrdinalIgnoreCase)))
            {
                return (decimal.Zero, decimal.Zero);
            }

            if (ratio.Equals("100", StringComparison.OrdinalIgnoreCase))
            {
                return (1, decimal.Zero);
            }

            var ratios = ratio.Split('/');
            if (ratios.Length == 2
                && decimal.TryParse(ratios[0], out var firstRatio)
                && decimal.TryParse(ratios[1], out var secondRatio)
                && firstRatio + secondRatio == 100)
            {
                return (firstRatio / 100, secondRatio / 100);
            }

            return (decimal.Zero, decimal.Zero);

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
