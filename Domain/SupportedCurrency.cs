namespace PortfolioBalancerServer.Domain;

public static class SupportedCurrency
{
    public static readonly HashSet<string> Codes = new(StringComparer.OrdinalIgnoreCase)
    {
        "rub",
        "usd",
        "eur"
    };

    public static string Normalize(string currency) => currency.Trim().ToLowerInvariant();

    public static bool IsSupported(string currency) => Codes.Contains(currency);
}
