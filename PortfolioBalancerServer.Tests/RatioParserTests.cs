using PortfolioBalancerServer.Domain;

namespace PortfolioBalancerServer.Tests;

public class RatioParserTests
{
    [Theory]
    [InlineData("60/30/10", 0.6, 0.3, 0.1)]
    [InlineData("70/30", 0.7, 0.3, 0)]
    [InlineData("100", 1, 0, 0)]
    [InlineData("0", 0, 1, 0)]
    public void TryParseParts_ParsesValidRatios(string ratio, decimal stocks, decimal bonds, decimal cash)
    {
        var ok = RatioParser.TryParseParts(ratio, out var parts);

        Assert.True(ok);
        Assert.Equal(stocks, parts[0]);
        Assert.Equal(bonds, parts[1]);
        Assert.Equal(cash, parts[2]);
    }

    [Theory]
    [InlineData("70/40")]
    [InlineData("60/30/20")]
    public void TryParseParts_RejectsInvalidRatios(string ratio)
    {
        Assert.False(RatioParser.TryParseParts(ratio, out _));
    }
}
