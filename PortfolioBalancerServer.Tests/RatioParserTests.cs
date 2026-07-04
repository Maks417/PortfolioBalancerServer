using PortfolioBalancerServer.Domain;

namespace PortfolioBalancerServer.Tests;

public class RatioParserTests
{
    [Theory]
    [InlineData("100", 1, 0)]
    [InlineData("0", 0, 1)]
    [InlineData("70/30", 0.7, 0.3)]
    [InlineData("50/50", 0.5, 0.5)]
    public void TryParse_ValidInput_ReturnsExpectedRatios(string ratio, decimal first, decimal second)
    {
        var success = RatioParser.TryParse(ratio, out var firstRatio, out var secondRatio);

        Assert.True(success);
        Assert.Equal(first, firstRatio);
        Assert.Equal(second, secondRatio);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("70/40")]
    [InlineData("99999")]
    [InlineData("7/30")]
    public void TryParse_InvalidInput_ReturnsFalse(string ratio)
    {
        var success = RatioParser.TryParse(ratio, out var firstRatio, out var secondRatio);

        Assert.False(success);
        Assert.Equal(0, firstRatio);
        Assert.Equal(0, secondRatio);
    }
}
