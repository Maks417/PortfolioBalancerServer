using System.ComponentModel.DataAnnotations;

namespace PortfolioBalancerServer.Models
{
    public record CalculationData
    {
        [Required]
        public string Ratio { get; set; }

        [Required]
        public Asset[] StockValues { get; set; }

        [Required]
        public Asset[] BondValues { get; set; }

        [Required]
        public Asset ContributionAmount { get; set; }
    }

    public record Asset
    {
        [Required]
        public decimal Value { get; set; }

        [Required]
        public string Currency { get; set; }
    }
}
