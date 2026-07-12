using PortfolioBalancerServer.Extensions;
using PortfolioBalancerServer.Interfaces;
using PortfolioBalancerServer.Models;

namespace PortfolioBalancerServer.Services;

public class CalculationService : ICalculationService
{
    public const string ContributionMode = "contribution";
    public const string RebalanceMode = "rebalance";

    public AssetsDiff SplitAssetsByRatio(
        decimal stocksAmount,
        decimal bondsAmount,
        decimal contributionAmount,
        decimal firstRatio,
        decimal secondRatio,
        string mode = ContributionMode)
    {
        var isRebalance = string.Equals(mode, RebalanceMode, StringComparison.OrdinalIgnoreCase);

        if (!isRebalance)
        {
            if (firstRatio == decimal.Zero)
            {
                return BuildContributionResult(
                    decimal.Zero,
                    contributionAmount.RoundToTwoDecimals(),
                    stocksAmount,
                    bondsAmount,
                    contributionAmount,
                    firstRatio,
                    secondRatio);
            }

            if (secondRatio == decimal.Zero)
            {
                return BuildContributionResult(
                    contributionAmount.RoundToTwoDecimals(),
                    decimal.Zero,
                    stocksAmount,
                    bondsAmount,
                    contributionAmount,
                    firstRatio,
                    secondRatio);
            }
        }

        var (totalStocksAmount, totalBondsAmount) = GetNewTotalAmount(
            stocksAmount,
            bondsAmount,
            contributionAmount,
            firstRatio,
            secondRatio);

        var stocksDiff = totalStocksAmount - stocksAmount;
        var bondsDiff = totalBondsAmount - bondsAmount;

        if (isRebalance)
        {
            return new AssetsDiff
            {
                StocksDiff = stocksDiff.RoundToTwoDecimals(),
                BondsDiff = bondsDiff.RoundToTwoDecimals(),
                Mode = RebalanceMode
            };
        }

        var stocksRounded = RoundByContributionAmount(stocksDiff, contributionAmount).RoundToTwoDecimals();
        var bondsRounded = RoundByContributionAmount(bondsDiff, contributionAmount).RoundToTwoDecimals();
        AdjustRoundedAmounts(ref stocksRounded, ref bondsRounded, contributionAmount);

        return BuildContributionResult(
            stocksRounded,
            bondsRounded,
            stocksAmount,
            bondsAmount,
            contributionAmount,
            firstRatio,
            secondRatio);
    }

    private static AssetsDiff BuildContributionResult(
        decimal stocksDiff,
        decimal bondsDiff,
        decimal stocksAmount,
        decimal bondsAmount,
        decimal contributionAmount,
        decimal firstRatio,
        decimal secondRatio) =>
        new()
        {
            StocksDiff = stocksDiff,
            BondsDiff = bondsDiff,
            Mode = ContributionMode,
            ContributionOnlyNote = BuildContributionOnlyNote(
                stocksAmount,
                bondsAmount,
                contributionAmount,
                firstRatio,
                secondRatio,
                stocksDiff,
                bondsDiff)
        };

    internal static string? BuildContributionOnlyNote(
        decimal stocksAmount,
        decimal bondsAmount,
        decimal contributionAmount,
        decimal firstRatio,
        decimal secondRatio,
        decimal stocksDiff,
        decimal bondsDiff)
    {
        var total = stocksAmount + bondsAmount + contributionAmount;
        if (total <= 0)
        {
            return null;
        }

        var currentStockPct = stocksAmount / total;
        var targetStockPct = firstRatio;
        var drift = Math.Abs(currentStockPct - targetStockPct);

        if (drift < 0.05m)
        {
            return null;
        }

        var overweightStocks = currentStockPct > targetStockPct && stocksDiff == 0 && bondsDiff > 0;
        var overweightBonds = currentStockPct < targetStockPct && bondsDiff == 0 && stocksDiff > 0;

        if (!overweightStocks && !overweightBonds)
        {
            return null;
        }

        return overweightStocks
            ? "Только взнос не позволит достичь целевой доли: портфель перевешен в сторону акций. Для точной балансировки может потребоваться продажа части акций."
            : "Только взнос не позволит достичь целевой доли: портфель перевешен в сторону облигаций. Для точной балансировки может потребоваться продажа части облигаций.";
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
