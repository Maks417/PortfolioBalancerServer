using System.ComponentModel.DataAnnotations;

namespace PortfolioBalancerServer.Options;

public class RateOptions
{
    public const string SectionName = "Rates";

    [Range(1, 168)]
    public int CacheTtlHours { get; set; } = 1;
}
