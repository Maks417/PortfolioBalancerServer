using System.ComponentModel.DataAnnotations;

namespace PortfolioBalancerServer.Options
{
    public class CurrencyServiceOptions
    {
        [Required]
        [Url]
        public string CurrencyServiceUrl { get; set; } = string.Empty;
    }
}
