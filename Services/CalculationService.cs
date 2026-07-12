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
        decimal cashAmount,
        decimal contributionAmount,
        decimal[] targetRatios,
        string mode = ContributionMode,
        decimal? driftThreshold = null)
    {
        var stocksRatio = targetRatios.Length > 0 ? targetRatios[0] : 0m;
        var bondsRatio = targetRatios.Length > 1 ? targetRatios[1] : 0m;
        var cashRatio = targetRatios.Length > 2 ? targetRatios[2] : 0m;
        var isRebalance = string.Equals(mode, RebalanceMode, StringComparison.OrdinalIgnoreCase);

        if (isRebalance
            && driftThreshold is > 0
            && IsWithinTolerance(stocksAmount, bondsAmount, cashAmount, contributionAmount, targetRatios, driftThreshold.Value))
        {
            return new AssetsDiff
            {
                Mode = RebalanceMode,
                ToleranceNote =
                    $"Отклонение в пределах допуска {driftThreshold.Value:0.#}%. Ребалансировка не требуется."
            };
        }

        if (!isRebalance)
        {
            if (stocksRatio == decimal.Zero && bondsRatio == decimal.Zero && cashRatio > 0)
            {
                return BuildContributionResult(
                    decimal.Zero,
                    decimal.Zero,
                    contributionAmount.RoundToTwoDecimals(),
                    stocksAmount,
                    bondsAmount,
                    cashAmount,
                    contributionAmount,
                    targetRatios,
                    decimal.Zero,
                    decimal.Zero,
                    contributionAmount.RoundToTwoDecimals());
            }

            if (bondsRatio == decimal.Zero && cashRatio == decimal.Zero && stocksRatio > 0)
            {
                return BuildContributionResult(
                    contributionAmount.RoundToTwoDecimals(),
                    decimal.Zero,
                    decimal.Zero,
                    stocksAmount,
                    bondsAmount,
                    cashAmount,
                    contributionAmount,
                    targetRatios,
                    contributionAmount.RoundToTwoDecimals(),
                    decimal.Zero,
                    decimal.Zero);
            }

            if (stocksRatio == decimal.Zero && cashRatio == decimal.Zero && bondsRatio > 0)
            {
                return BuildContributionResult(
                    decimal.Zero,
                    contributionAmount.RoundToTwoDecimals(),
                    decimal.Zero,
                    stocksAmount,
                    bondsAmount,
                    cashAmount,
                    contributionAmount,
                    targetRatios,
                    decimal.Zero,
                    contributionAmount.RoundToTwoDecimals(),
                    decimal.Zero);
            }
        }

        var (totalStocksAmount, totalBondsAmount, totalCashAmount) = GetNewTotalAmount(
            stocksAmount,
            bondsAmount,
            cashAmount,
            contributionAmount,
            stocksRatio,
            bondsRatio,
            cashRatio);

        var stocksDiff = totalStocksAmount - stocksAmount;
        var bondsDiff = totalBondsAmount - bondsAmount;
        var cashDiff = totalCashAmount - cashAmount;

        if (isRebalance)
        {
            return new AssetsDiff
            {
                StocksDiff = stocksDiff.RoundToTwoDecimals(),
                BondsDiff = bondsDiff.RoundToTwoDecimals(),
                CashDiff = cashDiff.RoundToTwoDecimals(),
                Mode = RebalanceMode
            };
        }

        var positiveDiffs = new[] { stocksDiff, bondsDiff, cashDiff }
            .Select(diff => Math.Max(diff, 0m))
            .ToArray();
        var rounded = positiveDiffs
            .Select(diff => RoundByContributionAmount(diff, contributionAmount).RoundToTwoDecimals())
            .ToArray();
        AdjustRoundedAmounts(rounded, contributionAmount);

        return BuildContributionResult(
            rounded[0],
            rounded[1],
            rounded[2],
            stocksAmount,
            bondsAmount,
            cashAmount,
            contributionAmount,
            targetRatios,
            rounded[0],
            rounded[1],
            rounded[2]);
    }

    private static bool IsWithinTolerance(
        decimal stocksAmount,
        decimal bondsAmount,
        decimal cashAmount,
        decimal contributionAmount,
        decimal[] targetRatios,
        decimal driftThreshold)
    {
        var total = stocksAmount + bondsAmount + cashAmount + contributionAmount;
        if (total <= 0)
        {
            return true;
        }

        var current = new[]
        {
            stocksAmount / total * 100m,
            bondsAmount / total * 100m,
            cashAmount / total * 100m
        };
        var targets = new[]
        {
            (targetRatios.Length > 0 ? targetRatios[0] : 0m) * 100m,
            (targetRatios.Length > 1 ? targetRatios[1] : 0m) * 100m,
            (targetRatios.Length > 2 ? targetRatios[2] : 0m) * 100m
        };

        return current.Zip(targets, (actual, target) => Math.Abs(actual - target))
            .DefaultIfEmpty(0m)
            .Max() <= driftThreshold;
    }

    private static AssetsDiff BuildContributionResult(
        decimal stocksDiff,
        decimal bondsDiff,
        decimal cashDiff,
        decimal stocksAmount,
        decimal bondsAmount,
        decimal cashAmount,
        decimal contributionAmount,
        decimal[] targetRatios,
        decimal roundedStocks,
        decimal roundedBonds,
        decimal roundedCash) =>
        new()
        {
            StocksDiff = stocksDiff,
            BondsDiff = bondsDiff,
            CashDiff = cashDiff,
            Mode = ContributionMode,
            ContributionOnlyNote = BuildContributionOnlyNote(
                stocksAmount,
                bondsAmount,
                cashAmount,
                contributionAmount,
                targetRatios,
                roundedStocks,
                roundedBonds,
                roundedCash)
        };

    internal static string? BuildContributionOnlyNote(
        decimal stocksAmount,
        decimal bondsAmount,
        decimal cashAmount,
        decimal contributionAmount,
        decimal[] targetRatios,
        decimal stocksDiff,
        decimal bondsDiff,
        decimal cashDiff)
    {
        var total = stocksAmount + bondsAmount + cashAmount + contributionAmount;
        if (total <= 0)
        {
            return null;
        }

        var stocksRatio = targetRatios.Length > 0 ? targetRatios[0] : 0m;
        var bondsRatio = targetRatios.Length > 1 ? targetRatios[1] : 0m;
        var cashRatio = targetRatios.Length > 2 ? targetRatios[2] : 0m;

        var currentStockPct = stocksAmount / total;
        var currentBondPct = bondsAmount / total;
        var currentCashPct = cashAmount / total;

        var overweightMessages = new List<string>();
        if (currentStockPct > stocksRatio + 0.05m && stocksDiff == 0 && (bondsDiff > 0 || cashDiff > 0))
        {
            overweightMessages.Add("акций");
        }
        if (currentBondPct > bondsRatio + 0.05m && bondsDiff == 0 && (stocksDiff > 0 || cashDiff > 0))
        {
            overweightMessages.Add("облигаций");
        }
        if (cashRatio > 0 && currentCashPct > cashRatio + 0.05m && cashDiff == 0 && (stocksDiff > 0 || bondsDiff > 0))
        {
            overweightMessages.Add("наличных");
        }

        if (overweightMessages.Count == 0)
        {
            return null;
        }

        return $"Только взнос не позволит достичь целевой доли: портфель перевешен в сторону {string.Join(" и ", overweightMessages)}. Для точной балансировки может потребоваться продажа.";
    }

    private static void AdjustRoundedAmounts(decimal[] rounded, decimal contributionAmount)
    {
        var remainder = contributionAmount - rounded.Sum();
        if (remainder == decimal.Zero || rounded.Length == 0)
        {
            return;
        }

        var index = Array.IndexOf(rounded, rounded.Max());
        rounded[index] += remainder;
    }

    private static decimal RoundByContributionAmount(decimal value, decimal contributionAmount)
    {
        if (value > decimal.Zero && value > contributionAmount)
        {
            return contributionAmount;
        }

        return value > decimal.Zero ? value : decimal.Zero;
    }

    private static (decimal, decimal, decimal) GetNewTotalAmount(
        decimal stocksAmount,
        decimal bondsAmount,
        decimal cashAmount,
        decimal contributionAmount,
        decimal stocksRatio,
        decimal bondsRatio,
        decimal cashRatio)
    {
        var total = stocksAmount + bondsAmount + cashAmount + contributionAmount;
        return (total * stocksRatio, total * bondsRatio, total * cashRatio);
    }
}
