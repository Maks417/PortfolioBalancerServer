using PortfolioBalancerServer.Domain;
using PortfolioBalancerServer.Models;

namespace PortfolioBalancerServer.Tests;

public class TradeRoundingTests
{
    [Fact]
    public void ApplyMinTrade_ZerosOutSmallTrades()
    {
        var rows = new[]
        {
            new PositionAllocation { Amount = 15m, Currency = "rub", IsSell = false },
            new PositionAllocation { Amount = 120m, Currency = "rub", IsSell = false }
        };

        var result = TradeRounding.ApplyMinTrade(rows, 50m, "rub");

        Assert.Equal(0, result[0].Amount);
        Assert.Equal(100, result[1].Amount);
    }
}
