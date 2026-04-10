namespace PortfolioBalancerServer.Domain
{
    public static class RatioParser
    {
        public static bool TryParse(string? ratio, out decimal firstRatio, out decimal secondRatio)
        {
            firstRatio = decimal.Zero;
            secondRatio = decimal.Zero;

            if (string.IsNullOrEmpty(ratio)
                || ratio.Length > 5
                || (!ratio.Equals("100", StringComparison.OrdinalIgnoreCase) && !ratio.Contains('/', StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            if (ratio.Equals("100", StringComparison.OrdinalIgnoreCase))
            {
                firstRatio = 1;
                return true;
            }

            var ratios = ratio.Split('/');
            if (ratios.Length == 2
                && decimal.TryParse(ratios[0], out var first)
                && decimal.TryParse(ratios[1], out var second)
                && first + second == 100)
            {
                firstRatio = first / 100;
                secondRatio = second / 100;
                return true;
            }

            return false;
        }
    }
}
