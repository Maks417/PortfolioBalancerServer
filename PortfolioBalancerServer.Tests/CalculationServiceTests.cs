using PortfolioBalancerServer.Services;

namespace PortfolioBalancerServer.Tests;

public class CalculationServiceTests
{
    private readonly CalculationService _sut = new();

    [Fact]
    public void ParseRatio_100_ReturnsFullStocksWeight()
    {
        var (first, second) = _sut.ParseRatio("100");
        Assert.Equal(1m, first);
        Assert.Equal(0m, second);
    }

    [Fact]
    public void ParseRatio_70_30_ReturnsFractions()
    {
        var (first, second) = _sut.ParseRatio("70/30");
        Assert.Equal(0.7m, first);
        Assert.Equal(0.3m, second);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("70/40")]
    [InlineData("99999")]
    [InlineData("7/30")]
    public void ParseRatio_InvalidInput_ReturnsZeroPair(string ratio)
    {
        var (first, second) = _sut.ParseRatio(ratio);
        Assert.Equal(0, first);
        Assert.Equal(0, second);
    }

    [Fact]
    public void SplitAssetsByRatio_SecondRatioZero_AssignsContributionToStocks()
    {
        var diff = _sut.SplitAssetsByRatio(100, 50, 25, 1, 0);
        Assert.Equal(25, diff.StocksDiff);
        Assert.Equal(0, diff.BondsDiff);
    }

    [Fact]
    public void SplitAssetsByRatio_TwoSided_RebalancesTowardRatio()
    {
        var diff = _sut.SplitAssetsByRatio(60, 40, 100, 0.5m, 0.5m);
        Assert.Equal(40, diff.StocksDiff);
        Assert.Equal(60, diff.BondsDiff);
    }
}
