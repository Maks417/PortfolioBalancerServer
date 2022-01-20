namespace PortfolioBalancerServer.Extensions
{
    public static class NumberExtensions
    {
        public static decimal RoundTwoSigns(this decimal number)
        {
            return decimal.Round(number, 2);
        }
    }
}
