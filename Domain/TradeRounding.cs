using PortfolioBalancerServer.Models;

namespace PortfolioBalancerServer.Domain;

public static class TradeRounding
{
    public static PositionAllocation[] ApplyMinTrade(
        PositionAllocation[] rows,
        decimal? minTradeAmount,
        string resultCurrency)
    {
        if (rows.Length == 0 || minTradeAmount is not > 0)
        {
            return rows;
        }

        return rows.Select(row =>
        {
            var amount = Math.Abs(row.Amount);
            if (amount < minTradeAmount.Value)
            {
                return row with { Amount = 0 };
            }

            var rounded = Math.Round(amount / minTradeAmount.Value, MidpointRounding.AwayFromZero)
                * minTradeAmount.Value;
            return row with { Amount = row.IsSell ? -rounded : rounded };
        }).ToArray();
    }
}
