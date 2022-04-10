namespace PortfolioBalancerServer.Models
{
    public record AssetsDiff
    {
        public decimal StocksDiff { get; set; }

        public decimal BondsDiff { get; set; }

        public string Currency { get; set; }
    }
}
