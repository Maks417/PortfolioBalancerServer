namespace PortfolioBalancerServer.Extensions;

public static class NumberExtensions
{
    public static decimal RoundToTwoDecimals(this decimal number) => decimal.Round(number, 2);

    [Obsolete("Use RoundToTwoDecimals instead.")]
    public static decimal RoundTwoSigns(this decimal number) => number.RoundToTwoDecimals();
}
