using PortfolioBalancerServer.Models;

namespace PortfolioBalancerServer.Domain;

public static class PositionBreakdown
{
    public static PositionAllocation[] DistributeBuys(
        IReadOnlyList<Asset> rows,
        decimal budgetAmount,
        string budgetCurrency,
        Func<decimal, string, decimal> toRub,
        Func<decimal, string, decimal> fromRub)
    {
        if (rows.Count == 0 || budgetAmount <= 0)
        {
            return [];
        }

        var budgetBase = toRub(budgetAmount, budgetCurrency);
        var baseValues = rows.Select(row => toRub(row.Value, row.Currency)).ToArray();
        var baseBuys = Waterfill.DistributeEqual(baseValues, budgetBase);

        return rows.Select((row, index) => new PositionAllocation
        {
            Amount = Math.Round(fromRub(baseBuys[index], row.Currency), 2),
            Currency = row.Currency,
            IsSell = false
        }).ToArray();
    }

    public static PositionAllocation[] DistributeRebalance(
        IReadOnlyList<Asset> rows,
        decimal classDiff,
        string resultCurrency,
        Func<decimal, string, decimal> toRub,
        Func<decimal, string, decimal> fromRub)
    {
        if (rows.Count == 0 || classDiff == 0)
        {
            return [];
        }

        if (classDiff > 0)
        {
            return DistributeBuys(rows, classDiff, resultCurrency, toRub, fromRub);
        }

        var sellBudget = Math.Abs(classDiff);
        var budgetBase = toRub(sellBudget, resultCurrency);
        var baseValues = rows.Select(row => toRub(row.Value, row.Currency)).ToArray();
        var totalBase = baseValues.Sum();
        if (totalBase <= 0)
        {
            return [];
        }

        return rows.Select((row, index) =>
        {
            var share = baseValues[index] / totalBase;
            var sellBase = budgetBase * share;
            return new PositionAllocation
            {
                Amount = Math.Round(fromRub(sellBase, row.Currency), 2),
                Currency = row.Currency,
                IsSell = true
            };
        }).ToArray();
    }
}
