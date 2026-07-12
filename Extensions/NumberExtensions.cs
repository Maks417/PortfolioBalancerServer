namespace PortfolioBalancerServer.Extensions;

public static class NumberExtensions
{
    public static decimal RoundToTwoDecimals(this decimal number) => decimal.Round(number, 2);
}
