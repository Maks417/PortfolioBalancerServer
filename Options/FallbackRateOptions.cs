using System.ComponentModel.DataAnnotations;

namespace PortfolioBalancerServer.Options;

public class FallbackRateOptions
{
    public const string SectionName = "FallbackRates";

    [Range(1, 1000)]
    public decimal UsdPerRub { get; set; } = 90m;

    [Range(1, 1000)]
    public decimal EurPerRub { get; set; } = 100m;
}
