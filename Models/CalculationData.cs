using System.ComponentModel.DataAnnotations;

namespace PortfolioBalancerServer.Models
{
    public class CalculationData
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

    public class Asset
    {
        [Required]
        public decimal Value { get; set; }

        [Required]
        public string Currency { get; set; }
    }
}
