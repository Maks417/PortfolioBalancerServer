using PortfolioBalancerServer.Domain;

namespace PortfolioBalancerServer.Tests;

public class WaterfillTests
{
    [Fact]
    public void DistributeEqual_SpreadsBudgetAcrossPositions()
    {
        var result = Waterfill.DistributeEqual([10m, 30m], 20m);

        Assert.Equal(2, result.Length);
        Assert.Equal(20m, result.Sum());
    }
}
