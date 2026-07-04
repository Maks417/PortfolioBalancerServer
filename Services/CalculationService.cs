using PortfolioBalancerServer.Extensions;
using PortfolioBalancerServer.Interfaces;
using PortfolioBalancerServer.Models;

namespace PortfolioBalancerServer.Services;

public class CalculationService : ICalculationService
{
    public AssetsDiff SplitAssetsByRatio(
        decimal stocksAmount,
        decimal bondsAmount,
        decimal contributionAmount,
        decimal firstRatio,
        decimal secondRatio)
    {
        if (firstRatio == decimal.Zero)
        {
            return new AssetsDiff
            {
                StocksDiff = decimal.Zero,
                BondsDiff = contributionAmount.RoundToTwoDecimals()
            };
        }

        if (secondRatio == decimal.Zero)
        {
            return new AssetsDiff
            {
                StocksDiff = contributionAmount.RoundToTwoDecimals(),
                BondsDiff = decimal.Zero
            };
        }

        var (totalStocksAmount, totalBondsAmount) = GetNewTotalAmount(
            stocksAmount,
            bondsAmount,
            contributionAmount,
            firstRatio,
            secondRatio);

        var stocksDiff = totalStocksAmount - stocksAmount;
        var bondsDiff = totalBondsAmount - bondsAmount;

        var stocksRounded = RoundByContributionAmount(stocksDiff, contributionAmount).RoundToTwoDecimals();
        var bondsRounded = RoundByContributionAmount(bondsDiff, contributionAmount).RoundToTwoDecimals();

        AdjustRoundedAmounts(ref stocksRounded, ref bondsRounded, contributionAmount);

        return new AssetsDiff
        {
            StocksDiff = stocksRounded,
            BondsDiff = bondsRounded
        };
    }

    private static void AdjustRoundedAmounts(
        ref decimal stocksRounded,
        ref decimal bondsRounded,
        decimal contributionAmount)
    {
        var remainder = contributionAmount - stocksRounded - bondsRounded;
        if (remainder == decimal.Zero)
        {
            return;
        }

        if (stocksRounded >= bondsRounded)
        {
            stocksRounded += remainder;
        }
        else
        {
            bondsRounded += remainder;
        }
    }

    private static decimal RoundByContributionAmount(decimal value, decimal contributionAmount)
    {
        if (value > decimal.Zero && value > contributionAmount)
        {
            return contributionAmount;
        }

        return value > decimal.Zero ? value : decimal.Zero;
    }

    private static (decimal, decimal) GetNewTotalAmount(
        decimal stocksAmount,
        decimal bondsAmount,
        decimal contributionAmount,
        decimal firstRatio,
        decimal secondRatio)
    {
        var total = stocksAmount + bondsAmount + contributionAmount;
        return (total * firstRatio, total * secondRatio);
    }
}
