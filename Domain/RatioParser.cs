namespace PortfolioBalancerServer.Domain;

public static class RatioParser
{
    public static bool TryParse(string? ratio, out decimal firstRatio, out decimal secondRatio)
    {
        firstRatio = decimal.Zero;
        secondRatio = decimal.Zero;

        if (!TryParseParts(ratio, out var parts))
        {
            return false;
        }

        firstRatio = parts[0];
        secondRatio = parts[1];
        return true;
    }

    public static bool TryParseParts(string? ratio, out decimal[] parts)
    {
        parts = [];

        if (string.IsNullOrWhiteSpace(ratio))
        {
            return false;
        }

        if (ratio.Equals("100", StringComparison.OrdinalIgnoreCase))
        {
            parts = [1m, 0m, 0m];
            return true;
        }

        if (ratio.Equals("0", StringComparison.OrdinalIgnoreCase))
        {
            parts = [0m, 1m, 0m];
            return true;
        }

        if (!ratio.Contains('/', StringComparison.Ordinal))
        {
            return false;
        }

        var segments = ratio.Split('/');
        if (segments.Length is not (2 or 3))
        {
            return false;
        }

        var values = new decimal[segments.Length];
        for (var i = 0; i < segments.Length; i++)
        {
            if (!decimal.TryParse(segments[i], out values[i]))
            {
                return false;
            }
        }

        if (values.Sum() != 100)
        {
            return false;
        }

        parts = segments.Length == 2
            ? [values[0] / 100m, values[1] / 100m, 0m]
            : [values[0] / 100m, values[1] / 100m, values[2] / 100m];

        return true;
    }
}
