using PortfolioBalancerServer.Services;

namespace PortfolioBalancerServer.Tests;

public class CalculationServiceTests
{
    private readonly CalculationService _sut = new();

    [Fact]
    public void SplitAssetsByRatio_SecondRatioZero_AssignsContributionToStocks()
    {
        var diff = _sut.SplitAssetsByRatio(100, 50, 25, 1, 0);

        Assert.Equal(25, diff.StocksDiff);
        Assert.Equal(0, diff.BondsDiff);
    }

    [Fact]
    public void SplitAssetsByRatio_FirstRatioZero_AssignsContributionToBonds()
    {
        var diff = _sut.SplitAssetsByRatio(100, 50, 25, 0, 1);

        Assert.Equal(0, diff.StocksDiff);
        Assert.Equal(25, diff.BondsDiff);
    }

    [Fact]
    public void SplitAssetsByRatio_TwoSided_RebalancesTowardRatio()
    {
        var diff = _sut.SplitAssetsByRatio(60, 40, 100, 0.5m, 0.5m);

        Assert.Equal(40, diff.StocksDiff);
        Assert.Equal(60, diff.BondsDiff);
        Assert.Equal(100, diff.StocksDiff + diff.BondsDiff);
    }

    [Fact]
    public void SplitAssetsByRatio_Rounding_KeepsContributionTotal()
    {
        var diff = _sut.SplitAssetsByRatio(33.33m, 33.33m, 33.34m, 0.5m, 0.5m);

        Assert.Equal(33.34m, diff.StocksDiff + diff.BondsDiff);
    }

    [Fact]
    public void SplitAssetsByRatio_RebalanceMode_AllowsSellRecommendations()
    {
        var diff = _sut.SplitAssetsByRatio(80, 20, 0, 0.5m, 0.5m, CalculationService.RebalanceMode);

        Assert.Equal(-30, diff.StocksDiff);
        Assert.Equal(30, diff.BondsDiff);
        Assert.Equal(CalculationService.RebalanceMode, diff.Mode);
    }

    [Fact]
    public void SplitAssetsByRatio_ContributionMode_AddsNoteWhenTargetUnreachable()
    {
        var diff = _sut.SplitAssetsByRatio(120, 30, 50, 0.5m, 0.5m);

        Assert.Equal(0, diff.StocksDiff);
        Assert.Equal(50, diff.BondsDiff);
        Assert.Contains("акций", diff.ContributionOnlyNote);
    }
}
