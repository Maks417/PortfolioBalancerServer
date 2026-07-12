namespace PortfolioBalancerServer.Models
{
    public record AssetsDiff
    {
        public decimal StocksDiff { get; set; }

        public decimal BondsDiff { get; set; }

        public decimal CashDiff { get; set; }

        public string? Currency { get; set; }

        public string Mode { get; set; } = CalculationModes.Contribution;

        public string? ContributionOnlyNote { get; set; }

        public string? ToleranceNote { get; set; }

        public FxMetadata? Fx { get; set; }

        public PositionAllocation[]? StocksBreakdown { get; set; }

        public PositionAllocation[]? BondsBreakdown { get; set; }

        public PositionAllocation[]? CashBreakdown { get; set; }
    }

    public static class CalculationModes
    {
        public const string Contribution = "contribution";
        public const string Rebalance = "rebalance";
    }
}
